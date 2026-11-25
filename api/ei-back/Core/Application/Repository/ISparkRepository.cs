using ei_back.Core.Domain.Entity;

namespace ei_back.Core.Application.Repository;

public interface ISparkRepository
{
    Task AcquireLockAsync(CancellationToken cancellationToken);
    Task<SparkSettings?> GetSettingsAsync(CancellationToken cancellationToken);
    Task<SparkCycle?> CurrentCycleAsync(CancellationToken cancellationToken);
    Task<List<SparkCycle>> CyclesAsync(CancellationToken cancellationToken);
    Task<int> AvailableAsync(Guid cycleId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<long> PriorCommitmentsAsync(Guid cycleId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<SparkGrant>> UserGrantsAsync(Guid userId, CancellationToken cancellationToken);
    Task<SparkQueueRequest?> ActiveRequestAsync(Guid userId, CancellationToken cancellationToken);
    Task<SparkQueueRequest?> FindRequestAsync(Guid id, CancellationToken cancellationToken);
    Task<SparkGrant?> FindGrantForRequestAsync(Guid requestId, CancellationToken cancellationToken);
    Task<List<SparkQueueRequest>> WaitingAsync(CancellationToken cancellationToken);
    Task<long> NextQueueOrderAsync(CancellationToken cancellationToken);
    Task<int> DistinctDeclinesAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken);
    Task<List<SparkQueueRequest>> PendingNoticesAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task<SparkDailySnapshot?> FindSnapshotAsync(Guid cycleId, DateOnly date, CancellationToken cancellationToken);
    Task<SparkDailySnapshot?> LatestSnapshotAsync(CancellationToken cancellationToken);
    Task<List<SparkDailySnapshot>> SnapshotsAsync(Guid cycleId, CancellationToken cancellationToken);
    Task<List<SparkDailySnapshot>> PendingReportsAsync(CancellationToken cancellationToken);
    Task<List<SparkCycleChange>> CycleChangesAsync(Guid cycleId, CancellationToken cancellationToken);

    void AddSettings(SparkSettings settings);
    void AddCycle(SparkCycle cycle);
    void AddGrant(SparkGrant grant);
    void AddRequest(SparkQueueRequest request);
    void AddSnapshot(SparkDailySnapshot snapshot);
    void AddCycleChange(SparkCycleChange change);
}
