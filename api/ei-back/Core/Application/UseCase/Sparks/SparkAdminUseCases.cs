using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Sparks;
using ei_back.Core.Application.UseCase.Sparks.Dtos;
using ei_back.Core.Application.UseCase.Sparks.Interfaces;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;

namespace ei_back.Core.Application.UseCase.Sparks;

public sealed class GetSparkAdministrationUseCase(
    ISparkRepository repository, SparkAllocationService allocation, TimeProvider clock) : IGetSparkAdministrationUseCase
{
    public async Task<SparkAdminResponse> Handler(Guid? cycleId, CancellationToken cancellationToken)
    {
        var settings = await allocation.GetSettingsAsync(cancellationToken);
        var cycles = await repository.CyclesAsync(cancellationToken);
        var current = cycles.FirstOrDefault(cycle => cycle.State != SparkCycleState.Closed);
        var cycle = cycleId is { } id
            ? cycles.FirstOrDefault(candidate => candidate.Id == id) ?? throw new NotFoundException("Spark cycle was not found.")
            : current;
        var now = clock.GetUtcNow();
        var date = SparkCalendar.LocalDate(now);
        var waiting = await repository.WaitingAsync(cancellationToken);
        var declined = await repository.DistinctDeclinesAsync(SparkCalendar.Midnight(date.AddDays(-1)), SparkCalendar.Midnight(date), cancellationToken);
        var declinedDate = date.AddDays(-1);
        var latest = await repository.LatestSnapshotAsync(cancellationToken);
        var dollars = current?.ProviderDollars ?? latest?.ProviderDollars;
        var observedAt = current?.ProviderObservedAt ?? latest?.ProviderObservedAt;

        SparkCycleResponse? summary = null;
        List<SparkSnapshotResponse> snapshots = [];
        List<SparkCycleChangeResponse> changes = [];
        if (cycle is not null)
        {
            var availability = await repository.AvailableAsync(cycle.Id, now, cancellationToken);
            summary = ToCycle(cycle) with { Available = availability, ControlLine = cycle.ControlLine(now) };
            snapshots = (await repository.SnapshotsAsync(cycle.Id, cancellationToken))
                .Select(snapshot => new SparkSnapshotResponse(snapshot.Date, snapshot.Capacity, snapshot.Available, snapshot.ControlLine)).ToList();
            changes = (await repository.CycleChangesAsync(cycle.Id, cancellationToken))
                .Select(change => new SparkCycleChangeResponse(change.ChangedAt, change.PreviousCapacity, change.NewCapacity,
                    change.AdminId, change.PreviousState?.ToString(), change.NewState?.ToString())).ToList();
        }

        var defaults = new SparkSettingsRequest(settings.Capacity, settings.InitialAmount, settings.RefillAmount,
            settings.EstimatedUnitCost, settings.ClosingDay, settings.RenewalLeadDays);
        return new SparkAdminResponse(summary, cycles.Select(ToCycle).ToList(), defaults, snapshots, changes,
            new SparkQueueMetricsResponse(waiting.Count(request => request.Kind == SparkGrantKind.Initial),
                waiting.Count(request => request.Kind == SparkGrantKind.Refill), declined, declinedDate),
            new SparkProviderResponse(dollars, dollars is { } balance ? SparkOperationalPolicy.ToEstimatedSparks(balance, settings.EstimatedUnitCost) : null,
                observedAt, current?.ProviderStale ?? latest?.ProviderStale ?? true));
    }

    private static SparkCycleResponse ToCycle(SparkCycle cycle) => new(cycle.Id, cycle.StartsAt, cycle.EndsAt,
        cycle.InitialCapacity, cycle.Capacity, cycle.State.ToString(), cycle.BlockReason);
}

public sealed class UpdateSparkSettingsUseCase(
    ISparkRepository repository, SparkAllocationService allocation, IUnitOfWork unitOfWork) : IUpdateSparkSettingsUseCase
{
    public async Task Handler(SparkSettingsRequest request, CancellationToken cancellationToken)
    {
        await allocation.GetSettingsAsync(cancellationToken);
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await repository.AcquireLockAsync(token);
            var settings = await repository.GetSettingsAsync(token) ?? throw new InvalidOperationException("Spark settings are missing.");
            settings.Update(request.MonthlyCapacity, request.InitialGrant, request.RefillLot,
                request.EstimatedCostPerSpark, request.ClosingDay, request.RenewalLeadDays);
            return true;
        }, cancellationToken);
    }
}

public sealed class IncreaseSparkCapacityUseCase(
    ISparkRepository repository, IUnitOfWork unitOfWork, TimeProvider clock) : IIncreaseSparkCapacityUseCase
{
    public async Task Handler(Guid cycleId, Guid adminId, int amount, CancellationToken cancellationToken)
    {
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await repository.AcquireLockAsync(token);
            var cycle = await repository.CurrentCycleAsync(token);
            var now = clock.GetUtcNow();
            if (cycle is null || cycle.Id != cycleId || cycle.EndsAt <= now)
                throw new NotFoundException("Current Spark cycle was not found.");
            if (amount <= 0 || amount > int.MaxValue - cycle.Capacity)
                throw new BadRequestException("The capacity increase is outside the allowed range.");

            var previous = cycle.Capacity;
            cycle.IncreaseCapacity(previous + amount);
            repository.AddCycleChange(new SparkCycleChange(cycle.Id, adminId, now, previous, cycle.Capacity, null, null));
            return true;
        }, cancellationToken);
    }
}

public sealed class ResetSparkCycleUseCase(
    ISparkRepository repository, ISparkCreditsClient credits, IUnitOfWork unitOfWork, TimeProvider clock) : IResetSparkCycleUseCase
{
    public async Task<bool> Handler(Guid cycleId, Guid adminId, CancellationToken cancellationToken)
    {
        var dollars = await credits.GetBalanceAsync(cancellationToken);
        var observedAt = clock.GetUtcNow();

        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await repository.AcquireLockAsync(token);
            var cycle = await repository.CurrentCycleAsync(token);
            var now = clock.GetUtcNow();
            if (cycle is null || cycle.Id != cycleId || cycle.EndsAt <= now)
                throw new NotFoundException("Current Spark cycle was not found.");
            if (cycle.State != SparkCycleState.Blocked || cycle.BlockReason != SparkOperationalPolicy.InsufficientCredits)
                throw new BadRequestException("Only a deficit-blocked Spark cycle can be reset.");

            var settings = await repository.GetSettingsAsync(token) ?? throw new InvalidOperationException("Spark settings are missing.");
            var commitments = await repository.PriorCommitmentsAsync(cycle.Id, now, token);
            var minimumCoverage = (int)Math.Ceiling(cycle.ControlLine(now));
            cycle.SetProviderObservation(dollars, observedAt);
            if (SparkOperationalPolicy.CoverageBlockReason(dollars, settings.EstimatedUnitCost, minimumCoverage, commitments) is not null)
                return false;

            cycle.SetState(SparkCycleState.Open);
            repository.AddCycleChange(new SparkCycleChange(cycle.Id, adminId, now, null, null, SparkCycleState.Blocked, SparkCycleState.Open));
            return true;
        }, cancellationToken);
    }
}
