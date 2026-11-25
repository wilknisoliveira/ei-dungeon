using ei_back.Core.Application.Repository;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using Microsoft.Extensions.Options;

namespace ei_back.Core.Application.Service.Sparks;

public sealed record SparkStatus(Guid? GrantId, int Balance, DateTimeOffset? ExpiresAt, bool CanRequest,
    Guid? QueueRequestId, SparkQueueState? QueueState, SparkGrantKind? QueueKind, SparkCycleState? CycleState,
    DateTimeOffset ServerTime);

public sealed class SparkAllocationService(ISparkRepository repository, IUnitOfWork unitOfWork, TimeProvider clock,
    IOptions<SparkOptions> options)
{
    public Task<SparkSettings> GetSettingsAsync(CancellationToken cancellationToken) =>
        UnderLockAsync(now => LoadSettingsAsync(cancellationToken), cancellationToken);

    public Task<SparkStatus> StatusAsync(Guid userId, CancellationToken cancellationToken) =>
        UnderLockAsync(async now => ToStatus(await InspectAsync(userId, now, cancellationToken), now),
            cancellationToken);

    public Task<SparkStatus> RequestAsync(Guid userId, CancellationToken cancellationToken, string cultureName = "en") =>
        UnderLockAsync(async now =>
        {
            var account = await InspectAsync(userId, now, cancellationToken);
            if (account.Queue is not null || !account.Eligible || !await repository.UserExistsAsync(userId, cancellationToken))
                return ToStatus(account, now);

            var settings = await LoadSettingsAsync(cancellationToken);
            var amount = 0;
            if (account.Cycle is { } cycle && cycle.CanAllocate(now)
                && (await repository.WaitingAsync(cancellationToken)).Count == 0)
            {
                var available = await repository.AvailableAsync(cycle.Id, now, cancellationToken);
                amount = GrantAmount(account.Kind, settings, available, cycle.ControlLine(now));
            }

            if (amount > 0)
            {
                var grant = new SparkGrant(userId, account.Cycle!.Id, account.Kind, amount, now);
                repository.AddGrant(grant);
                account = account with { Grant = grant, Eligible = false, Kind = SparkGrantKind.Refill };
            }
            else
            {
                var request = new SparkQueueRequest(userId, account.Kind, now, PlayerCulture(cultureName));
                repository.AddRequest(request);
                account = account with { Queue = request };
            }

            return ToStatus(account, now);
        }, cancellationToken);

    public Task<SparkStatus> DecideAsync(Guid userId, Guid requestId, bool accept,
        CancellationToken cancellationToken) =>
        UnderLockAsync(async now =>
        {
            var account = await InspectAsync(userId, now, cancellationToken);
            if (account.Queue is not { State: SparkQueueState.Offered } request || request.Id != requestId
                || !await repository.UserExistsAsync(userId, cancellationToken))
                return ToStatus(account, now);

            if (accept)
            {
                if (account.Eligible)
                    request.Accept(now, await repository.NextQueueOrderAsync(cancellationToken));
            }
            else
            {
                request.Decline(now);
                account = account with { Queue = null };
            }

            return ToStatus(account, now);
        }, cancellationToken);

    public Task ProcessQueueAsync(CancellationToken cancellationToken) =>
        UnderLockAsync(async now =>
        {
            var cycle = await repository.CurrentCycleAsync(cancellationToken);
            if (cycle is not null)
                await ProcessQueueAsync(cycle, await LoadSettingsAsync(cancellationToken), now, cancellationToken);
            return true;
        }, cancellationToken);

    // The coordinator may process a newly opened cycle inside its own Spark transaction.
    public async Task ProcessQueueAsync(SparkCycle cycle, SparkSettings settings, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var minute = now.UtcTicks / TimeSpan.TicksPerMinute;
        if (!cycle.CanAllocate(now) || (cycle.LastQueueRunAt is { } previous
            && minute - previous.UtcTicks / TimeSpan.TicksPerMinute < options.Value.QueueIntervalMinutes))
            return;

        var available = await repository.AvailableAsync(cycle.Id, now, cancellationToken);
        var controlLine = cycle.ControlLine(now);
        foreach (var request in await repository.WaitingAsync(cancellationToken))
        {
            var amount = GrantAmount(request.Kind, settings, available, controlLine);
            if (amount == 0) break;

            repository.AddGrant(new SparkGrant(request.UserId, cycle.Id, request.Kind, amount, now, request.Id));
            request.Fulfill();
            available -= amount;
        }

        cycle.MarkQueueRun(now);
    }

    private async Task<SparkSettings> LoadSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await repository.GetSettingsAsync(cancellationToken);
        if (settings is not null) return settings;

        var defaults = options.Value;
        settings = new SparkSettings(defaults.Capacity, defaults.InitialAmount, defaults.RefillAmount,
            defaults.EstimatedUnitCost, defaults.ClosingDay, defaults.RenewalLeadDays);
        repository.AddSettings(settings);
        return settings;
    }

    private async Task<AccountAvailability> InspectAsync(Guid userId, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var cycle = await repository.CurrentCycleAsync(cancellationToken);
        var grants = await repository.UserGrantsAsync(userId, cancellationToken);
        var active = grants.FirstOrDefault(grant => grant.ExpiresAt > now && grant.Consumed < grant.Amount);
        var kind = grants.Any(grant => grant.Kind == SparkGrantKind.Initial) ? SparkGrantKind.Refill : SparkGrantKind.Initial;
        var request = await repository.ActiveRequestAsync(userId, cancellationToken);
        return new AccountAvailability(cycle, active, request, kind, active is null);
    }

    private Task<T> UnderLockAsync<T>(Func<DateTimeOffset, Task<T>> operation, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await repository.AcquireLockAsync(ct);
            return await operation(clock.GetUtcNow());
        }, cancellationToken);

    private static int GrantAmount(SparkGrantKind kind, SparkSettings settings, int available, decimal controlLine) =>
        kind == SparkGrantKind.Initial
            ? Math.Clamp(available, 0, settings.InitialAmount)
            : available - (decimal)settings.RefillAmount >= controlLine ? settings.RefillAmount : 0;

    private static SparkStatus ToStatus(AccountAvailability account, DateTimeOffset now)
    {
        var cycleState = account.Cycle?.State;
        if (cycleState == SparkCycleState.Open && account.Cycle is { } cycle && !cycle.CanAllocate(now))
            cycleState = SparkCycleState.Blocked;

        return new SparkStatus(account.Grant?.Id, account.Grant?.Spendable(now) ?? 0, account.Grant?.ExpiresAt,
            account.Eligible && account.Queue is null, account.Queue?.Id, account.Queue?.State,
            account.Queue?.Kind ?? account.Kind, cycleState, now);
    }

    private static string PlayerCulture(string cultureName)
    {
        if (cultureName.StartsWith("pt", StringComparison.OrdinalIgnoreCase)) return "pt-BR";
        return cultureName.StartsWith("es", StringComparison.OrdinalIgnoreCase) ? "es" : "en";
    }

    private sealed record AccountAvailability(SparkCycle? Cycle, SparkGrant? Grant, SparkQueueRequest? Queue,
        SparkGrantKind Kind, bool Eligible);
}
