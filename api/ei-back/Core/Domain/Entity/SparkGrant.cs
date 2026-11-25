using System.Text.Json;
using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Domain.Entity;

public sealed record SparkOperationReceipt(Guid Id, SparkOperationKind Kind, Guid? GameId, Guid ResultId);

public sealed class SparkGrant
{
    private SparkGrant() { }

    public SparkGrant(Guid userId, Guid cycleId, SparkGrantKind kind, int amount, DateTimeOffset grantedAt,
        Guid? queueRequestId = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        UserId = userId;
        CycleId = cycleId;
        Kind = kind;
        Amount = amount;
        GrantedAt = grantedAt.ToUniversalTime();
        ExpiresAt = GrantedAt.AddHours(24);
        QueueRequestId = queueRequestId;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public Guid CycleId { get; private set; }
    public Guid? QueueRequestId { get; private set; }
    public SparkGrantKind Kind { get; private set; }
    public int Amount { get; private set; }
    public int Consumed { get; private set; }
    public DateTimeOffset GrantedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public Guid? ReservationId { get; private set; }
    public Guid? ReservationLeaseId { get; private set; }
    public SparkOperationKind? ReservationKind { get; private set; }
    public Guid? ReservationGameId { get; private set; }
    public int Reserved { get; private set; }
    public DateTimeOffset? ReservedUntil { get; private set; }
    // Receipts live with the grant. They are needed to recognize retries after later operations.
    public string Receipts { get; private set; } = "[]";

    public bool HasReservation(DateTimeOffset now) => ReservationId != null && ReservedUntil > now;
    public int Spendable(DateTimeOffset now) =>
        ExpiresAt > now ? Amount - Consumed - (HasReservation(now) ? Reserved : 0) : 0;

    public SparkOperationReceipt? FindReceipt(Guid operationId) =>
        ReadReceipts().Find(receipt => receipt.Id == operationId);

    public void Reserve(Guid operationId, Guid leaseId, SparkOperationKind kind, Guid? gameId, DateTimeOffset deadline,
        DateTimeOffset now)
    {
        if (Spendable(now) < 1 || HasReservation(now) || deadline <= now)
            throw new InvalidOperationException("The grant cannot reserve a Spark.");

        ReservationId = operationId;
        ReservationLeaseId = leaseId;
        ReservationKind = kind;
        ReservationGameId = gameId;
        Reserved = 1;
        ReservedUntil = deadline;
    }

    public void Confirm(Guid resultId)
    {
        var receipts = ReadReceipts();
        receipts.Add(new SparkOperationReceipt(ReservationId!.Value, ReservationKind!.Value, ReservationGameId, resultId));
        Receipts = JsonSerializer.Serialize(receipts);
        Consumed += Reserved;
        Release();
    }

    public void Release()
    {
        ReservationId = null;
        ReservationLeaseId = null;
        ReservationKind = null;
        ReservationGameId = null;
        Reserved = 0;
        ReservedUntil = null;
    }

    private List<SparkOperationReceipt> ReadReceipts() =>
        JsonSerializer.Deserialize<List<SparkOperationReceipt>>(Receipts)!;
}
