namespace ei_back.Core.Application.Service.Sparks;

public static class SparkOperationalPolicy
{
    public const string ProviderUnavailable = "ProviderUnavailable";
    public const string InsufficientCredits = "InsufficientCredits";

    public static long ToEstimatedSparks(decimal dollars, decimal unitCost)
    {
        if (unitCost <= 0)
            throw new ArgumentOutOfRangeException(nameof(unitCost));

        // Saturate before division so even extreme provider or configuration values remain representable.
        if (unitCost <= 1 && dollars >= long.MaxValue * unitCost)
            return long.MaxValue;
        if (unitCost <= 1 && dollars <= long.MinValue * unitCost)
            return long.MinValue;
        return (long)Math.Clamp(decimal.Floor(dollars / unitCost), long.MinValue, long.MaxValue);
    }

    public static string? CoverageBlockReason(decimal? dollars, decimal unitCost, int minimumCoverage, long commitments) =>
        dollars is { } balance
            ? ToEstimatedSparks(balance, unitCost) >= (long)minimumCoverage + commitments ? null : InsufficientCredits
            : ProviderUnavailable;

    public static bool ShouldReport(int waiting, int declined, int available, decimal controlLine, long? estimatedSparks, bool renewalDeficit) =>
        waiting > 0 || declined > 0 || available < controlLine || estimatedSparks < controlLine || renewalDeficit;
}
