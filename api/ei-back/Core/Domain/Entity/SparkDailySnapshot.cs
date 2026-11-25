namespace ei_back.Core.Domain.Entity;

public sealed class SparkDailySnapshot
{
    private SparkDailySnapshot() { }

    public SparkDailySnapshot(Guid cycleId, DateOnly date, int capacity, int available, decimal controlLine,
        int waitingInitial, int waitingRefill, int declined, decimal? providerDollars, long? providerSparks,
        DateTimeOffset? providerObservedAt, bool providerStale)
    {
        CycleId = cycleId;
        Date = date;
        Capacity = capacity;
        Available = available;
        ControlLine = controlLine;
        WaitingInitial = waitingInitial;
        WaitingRefill = waitingRefill;
        Declined = declined;
        ProviderDollars = providerDollars;
        ProviderSparks = providerSparks;
        ProviderObservedAt = providerObservedAt;
        ProviderStale = providerStale;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CycleId { get; private set; }
    public DateOnly Date { get; private set; }
    public int Capacity { get; private set; }
    public int Available { get; private set; }
    public decimal ControlLine { get; private set; }
    public int WaitingInitial { get; private set; }
    public int WaitingRefill { get; private set; }
    public int Declined { get; private set; }
    public decimal? ProviderDollars { get; private set; }
    public long? ProviderSparks { get; private set; }
    public DateTimeOffset? ProviderObservedAt { get; private set; }
    public bool ProviderStale { get; private set; }
    public string[] ReportRecipients { get; private set; } = [];
    public string? ReportSubject { get; private set; }
    public string? ReportBody { get; private set; }
    public DateTimeOffset? ReportSentAt { get; private set; }

    public void SetPendingReport(string[] recipients, string subject, string body)
    {
        ReportRecipients = recipients;
        ReportSubject = subject;
        ReportBody = body;
    }

    public void MarkReportSent(DateTimeOffset now) => ReportSentAt ??= now;
}
