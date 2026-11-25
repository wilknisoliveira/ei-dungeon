namespace ei_back.Core.Application.Service.Sparks;

public sealed class SparkOptions
{
    public int Capacity { get; set; } = 820;
    public int InitialAmount { get; set; } = 15;
    public int RefillAmount { get; set; } = 15;
    public decimal EstimatedUnitCost { get; set; } = 0.0061m;
    public int ClosingDay { get; set; } = 10;
    public int RenewalLeadDays { get; set; } = 3;
    public int QueueIntervalMinutes { get; set; } = 60;
    public int ReservationTimeoutMinutes { get; set; } = 10;
    public string? OpenRouterManagementKey { get; set; }
    public string[] AdministrativeRecipients { get; set; } = [];
}
