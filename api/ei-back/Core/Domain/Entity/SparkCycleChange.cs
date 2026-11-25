using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Domain.Entity;

public sealed class SparkCycleChange
{
    private SparkCycleChange() { }

    public SparkCycleChange(Guid cycleId, Guid adminId, DateTimeOffset changedAt, int? previousCapacity,
        int? newCapacity, SparkCycleState? previousState, SparkCycleState? newState)
    {
        CycleId = cycleId;
        AdminId = adminId;
        ChangedAt = changedAt;
        PreviousCapacity = previousCapacity;
        NewCapacity = newCapacity;
        PreviousState = previousState;
        NewState = newState;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CycleId { get; private set; }
    public Guid AdminId { get; private set; }
    public DateTimeOffset ChangedAt { get; private set; }
    public int? PreviousCapacity { get; private set; }
    public int? NewCapacity { get; private set; }
    public SparkCycleState? PreviousState { get; private set; }
    public SparkCycleState? NewState { get; private set; }
}
