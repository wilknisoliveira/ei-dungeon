using ei_back.Core.Application.Repository;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using Microsoft.Extensions.Options;

namespace ei_back.Core.Application.Service.Sparks;

public sealed class SparkCycleOperations(
    ISparkRepository repository,
    IUnitOfWork unitOfWork,
    SparkAllocationService allocation,
    ISparkCreditsClient credits,
    TimeProvider clock,
    IOptions<SparkOptions> options)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await allocation.GetSettingsAsync(cancellationToken);
        var now = clock.GetUtcNow();
        var current = await repository.CurrentCycleAsync(cancellationToken);
        var date = SparkCalendar.LocalDate(now);
        var observe = NeedsCoverage(current, now) || current is not null &&
            await repository.FindSnapshotAsync(current.Id, date, cancellationToken) is null;

        // Give the transaction a fresh view after external I/O.
        unitOfWork.Rollback();
        var dollars = observe ? await credits.GetBalanceAsync(cancellationToken) : null;
        var observedAt = clock.GetUtcNow();

        var cycleId = await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await repository.AcquireLockAsync(token);
            var active = await repository.CurrentCycleAsync(token);
            var timestamp = clock.GetUtcNow();
            if (NeedsCoverage(active, timestamp) && !observe)
                return (Guid?)null;

            var settings = await repository.GetSettingsAsync(token) ?? throw new InvalidOperationException("Spark settings are missing.");
            if (active is null || active.EndsAt <= timestamp)
            {
                var window = SparkCalendar.Window(timestamp, settings.ClosingDay);
                var start = active?.EndsAt ?? window.Start;
                var end = SparkCalendar.NextBoundary(start, settings.ClosingDay);
                if (end <= timestamp)
                {
                    start = window.Start > start ? window.Start : start;
                    end = window.End;
                }

                var previous = active;
                previous?.SetState(SparkCycleState.Closed);
                active = new SparkCycle(start, end, settings.Capacity);
                if (dollars is null && previous?.ProviderDollars is { } last && previous.ProviderObservedAt is { } lastObservedAt)
                    active.SetProviderObservation(last, lastObservedAt);
                repository.AddCycle(active);
            }

            if (observe && (active.ProviderObservedAt is null || active.ProviderObservedAt <= observedAt))
                active.SetProviderObservation(dollars, observedAt);

            if (NeedsCoverage(active, timestamp))
            {
                var earlier = await repository.PriorCommitmentsAsync(active.Id, timestamp, token);
                var reason = SparkOperationalPolicy.CoverageBlockReason(dollars, settings.EstimatedUnitCost, active.Capacity, earlier);
                active.SetState(reason is null ? SparkCycleState.Open : SparkCycleState.Blocked, reason);
            }

            return (Guid?)active.Id;
        }, cancellationToken);

        if (cycleId is null)
            return;

        await allocation.ProcessQueueAsync(cancellationToken);
        await CaptureDailyAsync(cycleId.Value, observe, dollars, cancellationToken);
    }

    private static bool NeedsCoverage(SparkCycle? cycle, DateTimeOffset now) =>
        cycle is null || cycle.EndsAt <= now ||
        cycle.State == SparkCycleState.Blocked && cycle.BlockReason is null or SparkOperationalPolicy.ProviderUnavailable;

    private async Task CaptureDailyAsync(Guid cycleId, bool observed, decimal? dollars,
        CancellationToken cancellationToken)
    {
        if (!observed)
            return;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await repository.AcquireLockAsync(token);
            var cycle = await repository.CurrentCycleAsync(token);
            var now = clock.GetUtcNow();
            var date = SparkCalendar.LocalDate(now);
            if (cycle is null || cycle.Id != cycleId || cycle.EndsAt <= now ||
                await repository.FindSnapshotAsync(cycle.Id, date, token) is not null)
                return false;

            var settings = await repository.GetSettingsAsync(token) ?? throw new InvalidOperationException("Spark settings are missing.");
            var waiting = await repository.WaitingAsync(token);
            var initial = waiting.Count(item => item.Kind == SparkGrantKind.Initial);
            var refill = waiting.Count(item => item.Kind == SparkGrantKind.Refill);
            var declined = await repository.DistinctDeclinesAsync(SparkCalendar.Midnight(date.AddDays(-1)), SparkCalendar.Midnight(date), token);
            var available = await repository.AvailableAsync(cycle.Id, now, token);
            var controlLine = cycle.ControlLine(now);
            var observedBalance = cycle.ProviderDollars;
            long? estimated = observedBalance is { } balance ? SparkOperationalPolicy.ToEstimatedSparks(balance, settings.EstimatedUnitCost) : null;
            long? confirmedEstimate = dollars is { } fresh ? SparkOperationalPolicy.ToEstimatedSparks(fresh, settings.EstimatedUnitCost) : null;

            var snapshot = new SparkDailySnapshot(cycle.Id, date, cycle.Capacity, available, controlLine,
                initial, refill, declined, observedBalance, estimated, cycle.ProviderObservedAt, cycle.ProviderStale);
            var start = SparkCalendar.LocalDate(cycle.StartsAt);
            var end = SparkCalendar.LocalDate(cycle.EndsAt);
            var renewalWindow = SparkCalendar.IsRenewalWindow(new SparkCalendarWindow(date, start, end), settings.RenewalLeadDays);
            var renewalTarget = date <= start.AddDays(1) ? cycle.InitialCapacity : settings.Capacity;
            var renewalDeficit = renewalWindow && confirmedEstimate < renewalTarget;
            if (renewalWindow && confirmedEstimate is { } credits && cycle.BlockReason == SparkOperationalPolicy.InsufficientCredits)
            {
                var earlier = await repository.PriorCommitmentsAsync(cycle.Id, now, token);
                renewalDeficit |= credits < (long)cycle.Capacity + earlier;
            }

            var recipients = options.Value.AdministrativeRecipients.Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (recipients.Length > 0 && SparkOperationalPolicy.ShouldReport(waiting.Count, declined, available, controlLine, confirmedEstimate, renewalDeficit))
                snapshot.SetPendingReport(recipients, "EI Dungeon Free Sparks report", FormatReport(snapshot, cycle, renewalDeficit));

            repository.AddSnapshot(snapshot);
            return true;
        }, cancellationToken);
    }

    private static string FormatReport(SparkDailySnapshot snapshot, SparkCycle cycle, bool renewalDeficit)
    {
        var report = FormattableString.Invariant($"""
            Free Sparks report for {snapshot.Date:yyyy-MM-dd}
            Waiting players: {snapshot.WaitingInitial + snapshot.WaitingRefill} ({snapshot.WaitingInitial} initial, {snapshot.WaitingRefill} refill)
            Players who declined yesterday: {snapshot.Declined}
            Available Sparks: {snapshot.Available} of {snapshot.Capacity}
            Control line: {snapshot.ControlLine:0.##}
            """);
        var observation = snapshot.ProviderDollars is { } dollars
            ? FormattableString.Invariant($"USD {dollars:0.00}, approximately {snapshot.ProviderSparks} Sparks, observed {snapshot.ProviderObservedAt:u}")
            : "unavailable";
        if (snapshot.ProviderStale && snapshot.ProviderDollars is not null)
            observation += " (last successful observation)";
        report += "\nOpenRouter balance: " + observation;
        if (renewalDeficit)
            report += "\nOpenRouter renewal credits are insufficient.";
        if (cycle.BlockReason == SparkOperationalPolicy.InsufficientCredits)
            report += "\nNew grants are paused. A manual reset checks OpenRouter coverage against the current control line plus earlier commitments.";
        return report;
    }
}
