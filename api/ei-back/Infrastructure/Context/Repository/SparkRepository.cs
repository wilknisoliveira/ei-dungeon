using ei_back.Core.Application.Repository;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ei_back.Infrastructure.Context.Repository;

public sealed class SparkRepository(EIContext context) : ISparkRepository
{
    public async Task AcquireLockAsync(CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction == null)
            throw new InvalidOperationException("The Spark lock requires an active transaction.");

        await context.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock(1179473264, 1)", cancellationToken);

        // Gameplay stages its result in the same context. Refresh unchanged Spark state only.
        foreach (var entry in context.ChangeTracker.Entries().Where(entry => entry.State == EntityState.Unchanged
            && entry.Entity is SparkGrant or SparkQueueRequest or SparkCycle
                or SparkSettings or SparkDailySnapshot or SparkCycleChange).ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    public async Task<SparkSettings?> GetSettingsAsync(CancellationToken cancellationToken) =>
        context.SparkSettings.Local.SingleOrDefault()
        ?? await context.SparkSettings.SingleOrDefaultAsync(cancellationToken);

    public async Task<SparkCycle?> CurrentCycleAsync(CancellationToken cancellationToken) =>
        context.SparkCycles.Local.SingleOrDefault(cycle => context.Entry(cycle).State == EntityState.Added)
        ?? await context.SparkCycles.OrderByDescending(cycle => cycle.StartsAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<SparkCycle>> CyclesAsync(CancellationToken cancellationToken) =>
        context.SparkCycles.OrderByDescending(cycle => cycle.StartsAt).ToListAsync(cancellationToken);

    public async Task<int> AvailableAsync(Guid cycleId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tracked = context.SparkCycles.Local.FirstOrDefault(cycle => cycle.Id == cycleId);
        var capacity = tracked?.Capacity ?? await context.SparkCycles.Where(cycle => cycle.Id == cycleId)
            .Select(cycle => cycle.Capacity).SingleAsync(cancellationToken);
        var grants = context.SparkGrants.Where(grant => grant.CycleId == cycleId);
        var occupied = await SumAllocationAsync(grants, now, includeConsumed: true, cancellationToken);
        return checked((int)(capacity - occupied));
    }

    public Task<long> PriorCommitmentsAsync(Guid cycleId, DateTimeOffset now, CancellationToken cancellationToken) =>
        SumAllocationAsync(context.SparkGrants.Where(grant => grant.CycleId != cycleId), now, includeConsumed: false,
            cancellationToken);

    private Task<long> SumAllocationAsync(IQueryable<SparkGrant> grants, DateTimeOffset now, bool includeConsumed,
        CancellationToken cancellationToken) =>
        grants.SumAsync(grant => (includeConsumed ? (long)grant.Consumed : 0L)
            + (grant.ExpiresAt > now && context.Users.Any(user => user.Id == grant.UserId)
                ? (long)grant.Amount - grant.Consumed
                : grant.ReservedUntil > now ? (long)grant.Reserved : 0L), cancellationToken);

    public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken) =>
        context.Users.AnyAsync(user => user.Id == userId, cancellationToken);

    public Task<List<SparkGrant>> UserGrantsAsync(Guid userId, CancellationToken cancellationToken) =>
        context.SparkGrants.Where(grant => grant.UserId == userId)
            .OrderByDescending(grant => grant.GrantedAt).ToListAsync(cancellationToken);

    public Task<SparkQueueRequest?> ActiveRequestAsync(Guid userId, CancellationToken cancellationToken) =>
        context.SparkQueueRequests.SingleOrDefaultAsync(request => request.UserId == userId
            && (request.State == SparkQueueState.Offered || request.State == SparkQueueState.Waiting),
            cancellationToken);

    public Task<SparkQueueRequest?> FindRequestAsync(Guid id, CancellationToken cancellationToken) =>
        context.SparkQueueRequests.SingleOrDefaultAsync(request => request.Id == id, cancellationToken);

    public Task<SparkGrant?> FindGrantForRequestAsync(Guid requestId, CancellationToken cancellationToken) =>
        context.SparkGrants.SingleOrDefaultAsync(grant => grant.QueueRequestId == requestId, cancellationToken);

    public Task<List<SparkQueueRequest>> WaitingAsync(CancellationToken cancellationToken) =>
        context.SparkQueueRequests.Where(request => request.State == SparkQueueState.Waiting
                && context.Users.Any(user => user.Id == request.UserId && user.Role == UserRole.CommonUser))
            .OrderBy(request => request.Kind).ThenBy(request => request.EntryOrder).ToListAsync(cancellationToken);

    public async Task<long> NextQueueOrderAsync(CancellationToken cancellationToken) =>
        1 + (await context.SparkQueueRequests.MaxAsync(request => request.EntryOrder, cancellationToken) ?? 0);

    public Task<int> DistinctDeclinesAsync(DateTimeOffset start, DateTimeOffset end,
        CancellationToken cancellationToken) =>
        context.SparkQueueRequests.Where(request => request.State == SparkQueueState.Declined
                && request.DecidedAt >= start && request.DecidedAt < end)
            .Select(request => request.UserId).Distinct().CountAsync(cancellationToken);

    public Task<List<SparkQueueRequest>> PendingNoticesAsync(DateTimeOffset now,
        CancellationToken cancellationToken) =>
        context.SparkQueueRequests.Where(request => request.State == SparkQueueState.Fulfilled
                && request.NoticeSentAt == null && context.Users.Any(user => user.Id == request.UserId)
                && context.SparkGrants.Any(grant => grant.QueueRequestId == request.Id && grant.ExpiresAt > now))
            .ToListAsync(cancellationToken);

    public Task<SparkDailySnapshot?> FindSnapshotAsync(Guid cycleId, DateOnly date,
        CancellationToken cancellationToken) =>
        context.SparkDailySnapshots.SingleOrDefaultAsync(snapshot => snapshot.CycleId == cycleId && snapshot.Date == date,
            cancellationToken);

    public Task<SparkDailySnapshot?> LatestSnapshotAsync(CancellationToken cancellationToken) =>
        context.SparkDailySnapshots.OrderByDescending(snapshot => snapshot.Date)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<SparkDailySnapshot>> SnapshotsAsync(Guid cycleId, CancellationToken cancellationToken) =>
        context.SparkDailySnapshots.Where(snapshot => snapshot.CycleId == cycleId)
            .OrderBy(snapshot => snapshot.Date).ToListAsync(cancellationToken);

    public Task<List<SparkDailySnapshot>> PendingReportsAsync(CancellationToken cancellationToken) =>
        context.SparkDailySnapshots.Where(snapshot => snapshot.ReportBody != null && snapshot.ReportSentAt == null)
            .OrderBy(snapshot => snapshot.Date).ToListAsync(cancellationToken);

    public Task<List<SparkCycleChange>> CycleChangesAsync(Guid cycleId, CancellationToken cancellationToken) =>
        context.SparkCycleChanges.Where(change => change.CycleId == cycleId)
            .OrderBy(change => change.ChangedAt).ToListAsync(cancellationToken);

    public void AddSettings(SparkSettings settings) => context.SparkSettings.Add(settings);
    public void AddCycle(SparkCycle cycle) => context.SparkCycles.Add(cycle);
    public void AddGrant(SparkGrant grant) => context.SparkGrants.Add(grant);
    public void AddRequest(SparkQueueRequest request) => context.SparkQueueRequests.Add(request);
    public void AddSnapshot(SparkDailySnapshot snapshot) => context.SparkDailySnapshots.Add(snapshot);
    public void AddCycleChange(SparkCycleChange change) => context.SparkCycleChanges.Add(change);
}
