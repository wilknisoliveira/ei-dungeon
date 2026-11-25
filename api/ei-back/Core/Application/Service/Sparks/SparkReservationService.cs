using ei_back.Core.Application.Repository;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using Microsoft.Extensions.Options;

namespace ei_back.Core.Application.Service.Sparks;

public enum SparkReservationState { Reserved, Replayed, Busy, Insufficient, Conflict }

public sealed record SparkReservationResult(SparkReservationState State, Guid? ResultId = null, Guid? LeaseId = null);

public sealed class SparkReservationService(ISparkRepository repository, IUnitOfWork unitOfWork, TimeProvider clock,
    IOptions<SparkOptions> options, ILogger<SparkReservationService>? logger = null)
{
    public Task<SparkReservationResult> ReserveAsync(Guid userId, Guid operationId, SparkOperationKind kind,
        Guid? gameId, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await repository.AcquireLockAsync(ct);
            var now = clock.GetUtcNow();
            if (operationId == Guid.Empty || !await repository.UserExistsAsync(userId, ct))
                return new SparkReservationResult(SparkReservationState.Conflict);

            var grants = await repository.UserGrantsAsync(userId, ct);
            var receipt = grants.Select(grant => grant.FindReceipt(operationId)).FirstOrDefault(value => value is not null);
            if (receipt is not null)
                return receipt.Kind == kind && receipt.GameId == gameId
                    ? new SparkReservationResult(SparkReservationState.Replayed, ResultId: receipt.ResultId)
                    : new SparkReservationResult(SparkReservationState.Conflict);

            if (grants.Any(grant => grant.HasReservation(now)))
                return new SparkReservationResult(SparkReservationState.Busy);

            var available = grants.FirstOrDefault(grant => grant.Spendable(now) > 0);
            if (available is null)
                return new SparkReservationResult(SparkReservationState.Insufficient);

            // Expired leases do not consume capacity and must never fence a new attempt.
            foreach (var expired in grants.Where(grant => grant.ReservationId is not null))
                expired.Release();

            var leaseId = Guid.NewGuid();
            available.Reserve(operationId, leaseId, kind, gameId,
                now.AddMinutes(options.Value.ReservationTimeoutMinutes), now);
            return new SparkReservationResult(SparkReservationState.Reserved, LeaseId: leaseId);
        }, cancellationToken);

    public async Task<bool> CompleteAsync(Guid userId, Guid operationId, Guid leaseId, Guid resultId,
        Func<CancellationToken, Task> persist, CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await repository.AcquireLockAsync(ct);
                var grants = await repository.UserGrantsAsync(userId, ct);
                var now = clock.GetUtcNow();
                var grant = FindLease(grants, operationId, leaseId);
                if (grant is null || !grant.HasReservation(now))
                    throw new ReservationLostException();

                await persist(ct);
                if (!grant.HasReservation(clock.GetUtcNow()))
                    throw new ReservationLostException();

                grant.Confirm(resultId);
                return true;
            }, cancellationToken);
        }
        catch (ReservationLostException)
        {
            return false;
        }
    }

    public Task ReleaseAsync(Guid userId, Guid operationId, Guid leaseId, CancellationToken cancellationToken)
    {
        // A streaming failure may have left unsaved play rows in the current context.
        unitOfWork.Rollback();
        return unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await repository.AcquireLockAsync(ct);
            var grants = await repository.UserGrantsAsync(userId, ct);
            FindLease(grants, operationId, leaseId)?.Release();
            return true;
        }, cancellationToken);
    }

    public async Task TryReleaseAsync(Guid userId, Guid operationId, Guid leaseId)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await ReleaseAsync(userId, operationId, leaseId, timeout.Token);
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception, "Unable to release Spark operation {OperationId}. The reservation will expire.",
                operationId);
        }
    }

    private static SparkGrant? FindLease(IEnumerable<SparkGrant> grants, Guid operationId, Guid leaseId) =>
        grants.SingleOrDefault(grant => grant.ReservationId == operationId && grant.ReservationLeaseId == leaseId);

    private sealed class ReservationLostException : Exception;
}
