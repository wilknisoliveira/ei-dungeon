using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Email;
using ei_back.Core.Application.Service.Sparks;
using ei_back.Core.Application.UseCase.Sparks;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using Microsoft.Extensions.Options;

namespace ei_back.Tests.Infrastructure;

public class SparkCycleOperationsTests
{
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public async Task Run_WhenProviderRecovers_OpensSameCycleAndProcessesQueueBeforeSnapshot()
    {
        var now = new DateTimeOffset(2026, 11, 10, 3, 0, 0, TimeSpan.Zero);
        var clock = new FixedClock(now);
        var repository = A.Fake<ISparkRepository>();
        var unitOfWork = A.Fake<IUnitOfWork>();
        RunTransactions(unitOfWork);
        var settings = new SparkSettings(820, 15, 15, 0.0061m, 10, 3);
        var options = Options.Create(new SparkOptions());
        var credits = A.Fake<ISparkCreditsClient>();
        A.CallTo(() => credits.GetBalanceAsync(A<CancellationToken>._)).ReturnsNextFromSequence(
            Task.FromResult<decimal?>(null), Task.FromResult<decimal?>(826 * 0.0061m));
        A.CallTo(() => repository.GetSettingsAsync(A<CancellationToken>._)).Returns(settings);
        var previous = new SparkCycle(now.AddMonths(-1), now, 820);
        previous.SetState(SparkCycleState.Open);
        SparkCycle current = previous;
        A.CallTo(() => repository.CurrentCycleAsync(A<CancellationToken>._)).ReturnsLazily(() => current);
        A.CallTo(() => repository.AddCycle(A<SparkCycle>._)).Invokes((SparkCycle cycle) => current = cycle);
        A.CallTo(() => repository.PriorCommitmentsAsync(A<Guid>._, A<DateTimeOffset>._, A<CancellationToken>._)).Returns(6L);
        A.CallTo(() => repository.AvailableAsync(A<Guid>._, A<DateTimeOffset>._, A<CancellationToken>._)).Returns(820);
        SparkDailySnapshot? snapshot = null;
        A.CallTo(() => repository.FindSnapshotAsync(A<Guid>._, A<DateOnly>._, A<CancellationToken>._)).ReturnsLazily(() => snapshot);
        A.CallTo(() => repository.AddSnapshot(A<SparkDailySnapshot>._)).Invokes((SparkDailySnapshot added) => snapshot = added);
        var waiting = new SparkQueueRequest(Guid.NewGuid(), SparkGrantKind.Initial, now.AddMinutes(-5), "en");
        waiting.Accept(now.AddMinutes(-4), 1);
        A.CallTo(() => repository.WaitingAsync(A<CancellationToken>._)).ReturnsLazily(() =>
            waiting.State == SparkQueueState.Waiting ? new List<SparkQueueRequest> { waiting } : []);
        var service = new SparkCycleOperations(repository, unitOfWork,
            new SparkAllocationService(repository, unitOfWork, clock, options), credits, clock,
            Options.Create(new AdministrativeNotificationOptions()));

        await service.RunAsync(default);
        var newId = current.Id;
        previous.State.Should().Be(SparkCycleState.Closed);
        current.State.Should().Be(SparkCycleState.Blocked);
        current.BlockReason.Should().Be(SparkOperationalPolicy.ProviderUnavailable);
        snapshot!.ProviderStale.Should().BeTrue();

        await service.RunAsync(default);
        current.Id.Should().Be(newId);
        current.State.Should().Be(SparkCycleState.Open);
        waiting.State.Should().Be(SparkQueueState.Fulfilled);
        snapshot.Date.Should().Be(new DateOnly(2026, 11, 10));
        A.CallTo(() => repository.AddSnapshot(A<SparkDailySnapshot>._)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData(458)]
    [InlineData(775)]
    public async Task Reset_WhenCreditsCoverRoundedControlLineAndEarlierCommitments_OpensSameCycle(int fundedSparks)
    {
        var start = new DateTimeOffset(2026, 11, 10, 3, 0, 0, TimeSpan.Zero);
        var now = start.AddDays(15).AddMinutes(-1);
        var repository = A.Fake<ISparkRepository>();
        var unitOfWork = A.Fake<IUnitOfWork>();
        RunTransactions(unitOfWork);
        var credits = A.Fake<ISparkCreditsClient>();
        var cycle = new SparkCycle(start, start.AddDays(30), 820);
        cycle.SetState(SparkCycleState.Blocked, SparkOperationalPolicy.InsufficientCredits);
        var admin = Guid.NewGuid();
        A.CallTo(() => repository.CurrentCycleAsync(A<CancellationToken>._)).Returns(cycle);
        A.CallTo(() => repository.GetSettingsAsync(A<CancellationToken>._)).Returns(new SparkSettings(820, 15, 15, 0.0061m, 10, 3));
        A.CallTo(() => repository.PriorCommitmentsAsync(cycle.Id, now, A<CancellationToken>._)).Returns(6L);
        A.CallTo(() => credits.GetBalanceAsync(A<CancellationToken>._)).ReturnsNextFromSequence(
            Task.FromResult<decimal?>(null), Task.FromResult<decimal?>(457 * 0.0061m),
            Task.FromResult<decimal?>(fundedSparks * 0.0061m));
        var service = new ResetSparkCycleUseCase(repository, credits, unitOfWork, new FixedClock(now));

        (await service.Handler(cycle.Id, admin, default)).Should().BeFalse();
        cycle.State.Should().Be(SparkCycleState.Blocked);
        (await service.Handler(cycle.Id, admin, default)).Should().BeFalse();
        cycle.State.Should().Be(SparkCycleState.Blocked);
        (await service.Handler(cycle.Id, admin, default)).Should().BeTrue();
        cycle.State.Should().Be(SparkCycleState.Open);
        cycle.Capacity.Should().Be(820);
        A.CallTo(() => repository.AddCycleChange(A<SparkCycleChange>.That.Matches(change =>
            change.CycleId == cycle.Id && change.AdminId == admin && change.NewState == SparkCycleState.Open)))
            .MustHaveHappenedOnceExactly();
    }

    private static void RunTransactions(IUnitOfWork unitOfWork)
    {
        RunTransaction<bool>(unitOfWork);
        RunTransaction<Guid?>(unitOfWork);
        RunTransaction<SparkSettings>(unitOfWork);
    }

    private static void RunTransaction<T>(IUnitOfWork unitOfWork) =>
        A.CallTo(() => unitOfWork.ExecuteInTransactionAsync(A<Func<CancellationToken, Task<T>>>._, A<CancellationToken>._))
            .ReturnsLazily((Func<CancellationToken, Task<T>> operation, CancellationToken token) => operation(token));
}
