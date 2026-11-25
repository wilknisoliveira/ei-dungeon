using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Domain.Entity;

public sealed class SparkQueueRequest
{
    private SparkQueueRequest() { }

    public SparkQueueRequest(Guid userId, SparkGrantKind kind, DateTimeOffset offeredAt, string cultureName)
    {
        UserId = userId;
        Kind = kind;
        OfferedAt = offeredAt;
        CultureName = cultureName;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public SparkGrantKind Kind { get; private set; }
    public SparkQueueState State { get; private set; }
    public DateTimeOffset OfferedAt { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public DateTimeOffset? EnteredAt { get; private set; }
    public long? EntryOrder { get; private set; }
    public string CultureName { get; private set; } = "en";
    public DateTimeOffset? NoticeSentAt { get; private set; }

    public void Accept(DateTimeOffset now, long entryOrder)
    {
        EnsureState(SparkQueueState.Offered);
        State = SparkQueueState.Waiting;
        DecidedAt = EnteredAt = now;
        EntryOrder = entryOrder;
    }

    public void Decline(DateTimeOffset now)
    {
        EnsureState(SparkQueueState.Offered);
        State = SparkQueueState.Declined;
        DecidedAt = now;
    }

    public void Fulfill()
    {
        EnsureState(SparkQueueState.Waiting);
        State = SparkQueueState.Fulfilled;
    }

    public void MarkNoticeSent(DateTimeOffset now)
    {
        EnsureState(SparkQueueState.Fulfilled);
        NoticeSentAt ??= now;
    }

    private void EnsureState(SparkQueueState expected)
    {
        if (State != expected) throw new InvalidOperationException("The queue request cannot make this transition.");
    }
}
