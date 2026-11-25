namespace ei_back.Core.Domain.Entity;

public sealed class SparkSettings
{
    private SparkSettings() { }

    public SparkSettings(int capacity, int initialAmount, int refillAmount, decimal unitCost, int closingDay,
        int renewalLeadDays) => Update(capacity, initialAmount, refillAmount, unitCost, closingDay, renewalLeadDays);

    public int Id { get; private set; } = 1;
    public int Capacity { get; private set; }
    public int InitialAmount { get; private set; }
    public int RefillAmount { get; private set; }
    public decimal EstimatedUnitCost { get; private set; }
    public int ClosingDay { get; private set; }
    public int RenewalLeadDays { get; private set; }

    public void Update(int capacity, int initialAmount, int refillAmount, decimal unitCost, int closingDay,
        int renewalLeadDays)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialAmount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(refillAmount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(unitCost);
        ArgumentOutOfRangeException.ThrowIfLessThan(closingDay, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(closingDay, 31);
        ArgumentOutOfRangeException.ThrowIfNegative(renewalLeadDays);
        Capacity = capacity;
        InitialAmount = initialAmount;
        RefillAmount = refillAmount;
        EstimatedUnitCost = unitCost;
        ClosingDay = closingDay;
        RenewalLeadDays = renewalLeadDays;
    }
}
