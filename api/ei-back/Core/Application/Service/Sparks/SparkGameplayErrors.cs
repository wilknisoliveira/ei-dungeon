using ei_back.Core.Domain.DomainExceptions;

namespace ei_back.Core.Application.Service.Sparks;

internal static class SparkGameplayErrors
{
    public static SparkOperationException InvalidRequest() =>
        new("sparks_invalid_operation", "An operation ID and valid gameplay intent are required.");

    public static SparkOperationException Conflict() =>
        new("sparks_conflict", "This operation is no longer valid. Refresh the game and try again.");

    public static SparkOperationException EmptyResponse() =>
        new("sparks_generation_failed", "The game master returned an empty response.");

    public static SparkOperationException FromReservation(SparkReservationState state) => state switch
    {
        SparkReservationState.Insufficient => new("sparks_insufficient", "There are not enough Sparks for this operation."),
        SparkReservationState.Busy => new("sparks_busy", "Another gameplay operation is in progress."),
        _ => Conflict()
    };
}
