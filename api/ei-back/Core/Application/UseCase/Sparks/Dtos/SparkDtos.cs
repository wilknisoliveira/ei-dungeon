using System.ComponentModel.DataAnnotations;
using ei_back.Core.Application.Service.Sparks;

namespace ei_back.Core.Application.UseCase.Sparks.Dtos;

public sealed record SparkStatusResponse(
    int Balance,
    DateTimeOffset? ExpiresAt,
    bool CanRequest,
    string RequestKind,
    string? QueueState,
    Guid? QueueRequestId,
    DateTimeOffset ServerTime)
{
    public static SparkStatusResponse From(SparkStatus status) => new(
        status.Balance, status.ExpiresAt, status.CanRequest, status.QueueKind?.ToString() ?? "Initial",
        status.QueueState?.ToString(), status.QueueRequestId, status.ServerTime);
}

public sealed record SparkDecisionRequest([Required] bool? Accepted);
public sealed record SparkAvailabilityResponse(int InitialGrant);
public sealed record SparkCycleIncreaseRequest([Range(1, int.MaxValue)] int Amount);
public sealed record SparkSettingsRequest(
    [Range(0, int.MaxValue)] int MonthlyCapacity,
    [Range(1, int.MaxValue)] int InitialGrant,
    [Range(1, int.MaxValue)] int RefillLot,
    [Range(typeof(decimal), "0.0000000001", "1000000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)] decimal EstimatedCostPerSpark,
    [Range(1, 31)] int ClosingDay,
    [Range(0, 31)] int RenewalLeadDays);
public sealed record SparkCycleResponse(Guid Id, DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    int InitialCapacity, int Capacity, string State, string? BlockReason, int? Available = null, decimal? ControlLine = null);
public sealed record SparkSnapshotResponse(DateOnly Date, int Capacity, int Available, decimal ControlLine);
public sealed record SparkCycleChangeResponse(DateTimeOffset OccurredAt, int? PreviousCapacity, int? Capacity,
    Guid AdminId, string? PreviousState, string? State);
public sealed record SparkQueueMetricsResponse(int InitialWaiting, int RefillWaiting, int Declined, DateOnly DeclinedDate);
public sealed record SparkProviderResponse(decimal? Dollars, long? EstimatedSparks, DateTimeOffset? ObservedAt, bool Stale);
public sealed record SparkAdminResponse(SparkCycleResponse? Cycle, IReadOnlyList<SparkCycleResponse> Cycles,
    SparkSettingsRequest Settings, IReadOnlyList<SparkSnapshotResponse> Snapshots, IReadOnlyList<SparkCycleChangeResponse> Changes,
    SparkQueueMetricsResponse Queue, SparkProviderResponse OpenRouter);
