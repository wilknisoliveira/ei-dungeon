using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Domain.Entity;

public sealed class SparkCycle
{
    private SparkCycle() { }

    public SparkCycle(DateTimeOffset startsAt, DateTimeOffset endsAt, int capacity)
    {
        if (endsAt <= startsAt) throw new ArgumentOutOfRangeException(nameof(endsAt));
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        StartsAt = startsAt.ToUniversalTime();
        EndsAt = endsAt.ToUniversalTime();
        Capacity = InitialCapacity = capacity;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public int InitialCapacity { get; private set; }
    public int Capacity { get; private set; }
    public SparkCycleState State { get; private set; } = SparkCycleState.Blocked;
    public string? BlockReason { get; private set; }
    public DateTimeOffset? LastQueueRunAt { get; private set; }
    public decimal? ProviderDollars { get; private set; }
    public DateTimeOffset? ProviderObservedAt { get; private set; }
    public bool ProviderStale { get; private set; }

    public bool CanAllocate(DateTimeOffset now) =>
        State == SparkCycleState.Open && now >= StartsAt && now < EndsAt;

    public decimal ControlLine(DateTimeOffset now)
    {
        var elapsed = Math.Clamp((now - StartsAt).Ticks, 0L, (EndsAt - StartsAt).Ticks);
        var fraction = (decimal)elapsed / (EndsAt - StartsAt).Ticks;
        return Capacity * (1m - 0.9m * fraction);
    }

    public void SetState(SparkCycleState state, string? blockReason = null)
    {
        State = state;
        BlockReason = blockReason;
    }

    public void IncreaseCapacity(int newCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(newCapacity, Capacity);
        Capacity = newCapacity;
    }

    public void MarkQueueRun(DateTimeOffset now) => LastQueueRunAt = now;

    public void SetProviderObservation(decimal? value, DateTimeOffset observedAt)
    {
        ProviderStale = value is null;
        if (value is { } dollars)
        {
            ProviderDollars = dollars;
            ProviderObservedAt = observedAt;
        }
    }
}
