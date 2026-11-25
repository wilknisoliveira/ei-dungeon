using ei_back.Core.Application.Service.Sparks;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Context;
using ei_back.Infrastructure.Context.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ei_back.Tests.Infrastructure;

[CollectionDefinition("Spark cycle", DisableParallelization = true)]
public sealed class SparkCycleTestCollection;

[Collection("Spark cycle")]
public sealed class SparkPersistenceTests
{
    [Fact]
    public void ControlLine_IncreaseHalfway_UsesNewCapacityAtTheSamePercentage()
    {
        var start = DateTimeOffset.UnixEpoch;
        var cycle = new SparkCycle(start, start.AddDays(30), 820);
        cycle.ControlLine(start.AddDays(15)).Should().Be(451m);

        cycle.IncreaseCapacity(1020);

        cycle.ControlLine(start.AddDays(15)).Should().Be(561m);
        cycle.ControlLine(start.AddDays(30)).Should().Be(102m);
    }

    [VerificationDatabaseFact]
    public async Task ConcurrentInitialRequests_LimitedPoolIssuesSinglePartialGrant()
    {
        var users = new[] { NewUser(), NewUser() };
        var clock = NewClock();
        var cycle = OpenCycle(clock, 10);
        await using var setup = VerificationTestSupport.Open();
        setup.Users.AddRange(users);
        setup.SparkCycles.Add(cycle);
        await setup.SaveChangesAsync();
        var userIds = users.Select(user => user.Id).ToArray();
        try
        {
            var statuses = await Task.WhenAll(userIds.Select(async userId =>
            {
                await using var db = VerificationTestSupport.Open();
                return await Allocation(db, clock).RequestAsync(userId, CancellationToken.None);
            }));

            statuses.Select(status => status.Balance).Should().BeEquivalentTo(new[] { 10, 0 });
            statuses.Count(status => status.QueueState == SparkQueueState.Offered).Should().Be(1);
            var granted = await setup.SparkGrants.SingleAsync(grant => grant.CycleId == cycle.Id);
            granted.Kind.Should().Be(SparkGrantKind.Initial);
            (await new SparkRepository(setup).AvailableAsync(cycle.Id, clock.GetUtcNow(), CancellationToken.None))
                .Should().Be(0);
            var repeat = await Allocation(setup, clock).RequestAsync(granted.UserId, CancellationToken.None);
            repeat.GrantId.Should().Be(granted.Id);
        }
        finally
        {
            await CleanupAsync(cycle.Id, userIds);
        }
    }

    [VerificationDatabaseFact]
    public async Task RequestRefill_OnExactLineAfterAllocation_ProvidesFullRefill()
    {
        var applicant = NewUser();
        var other = NewUser();
        var userIds = new[] { applicant.Id, other.Id };
        var clock = NewClock();
        var now = clock.GetUtcNow();
        var cycle = OpenCycle(clock, 100);
        await using var db = VerificationTestSupport.Open();
        db.Users.AddRange(applicant, other);
        db.SparkCycles.Add(cycle);
        db.SparkGrants.AddRange(new SparkGrant(applicant.Id, cycle.Id, SparkGrantKind.Initial, 15, now.AddDays(-2)),
            new SparkGrant(other.Id, cycle.Id, SparkGrantKind.Initial, 30, now.AddHours(-1)));
        await db.SaveChangesAsync();
        try
        {
            var status = await Allocation(db, clock).RequestAsync(applicant.Id, CancellationToken.None);

            status.Balance.Should().Be(15);
            (await new SparkRepository(db).AvailableAsync(cycle.Id, now, CancellationToken.None))
                .Should().Be(55);
        }
        finally
        {
            await CleanupAsync(cycle.Id, userIds);
        }
    }

    [VerificationDatabaseFact]
    public async Task CompleteAsync_ExpiredLeaseForSameOperationCannotConfirmNewLease()
    {
        var user = NewUser();
        var clock = NewClock();
        var cycle = OpenCycle(clock, 10);
        await using var db = VerificationTestSupport.Open();
        db.Users.Add(user);
        db.SparkCycles.Add(cycle);
        db.SparkGrants.Add(new SparkGrant(user.Id, cycle.Id, SparkGrantKind.Initial, 1, clock.GetUtcNow()));
        await db.SaveChangesAsync();
        try
        {
            var operationId = Guid.NewGuid();
            var resultId = Guid.NewGuid();
            var reservations = new SparkReservationService(new SparkRepository(db), new UnitOfWork(db), clock,
                Options.Create(new SparkOptions { ReservationTimeoutMinutes = 1 }));
            var first = await reservations.ReserveAsync(user.Id, operationId, SparkOperationKind.CreateGame, null,
                CancellationToken.None);
            clock.Advance(TimeSpan.FromMinutes(2));
            var second = await reservations.ReserveAsync(user.Id, operationId, SparkOperationKind.CreateGame, null,
                CancellationToken.None);
            first.State.Should().Be(SparkReservationState.Reserved);
            second.State.Should().Be(SparkReservationState.Reserved);
            second.LeaseId.Should().NotBeNull();
            (second.LeaseId != first.LeaseId).Should().BeTrue();

            // Gameplay stages its result in the request context before finalizing.
            var game = new Game(user.Id, "Spark campaign") { Id = resultId };
            game.SetCreatedDate(clock.GetUtcNow());
            db.Games.Add(game);
            var persistedByOldOwner = false;
            var stale = await reservations.CompleteAsync(user.Id, operationId, first.LeaseId!.Value, resultId, _ =>
            {
                persistedByOldOwner = true;
                return Task.CompletedTask;
            }, CancellationToken.None);

            stale.Should().BeFalse();
            persistedByOldOwner.Should().BeFalse();
            await reservations.ReleaseAsync(user.Id, operationId, first.LeaseId.Value, CancellationToken.None);
            (await db.Games.AnyAsync(item => item.Id == resultId)).Should().BeFalse();

            db.Games.Add(new Game(user.Id, "Spark campaign") { Id = resultId, CreatedAt = game.CreatedAt, UpdatedAt = game.UpdatedAt });
            (await reservations.CompleteAsync(user.Id, operationId, second.LeaseId!.Value, resultId,
                _ => Task.CompletedTask, CancellationToken.None)).Should().BeTrue();
            (await db.Games.AnyAsync(item => item.Id == resultId)).Should().BeTrue();
            var replay = await reservations.ReserveAsync(user.Id, operationId, SparkOperationKind.CreateGame, null,
                CancellationToken.None);
            replay.State.Should().Be(SparkReservationState.Replayed);
            replay.ResultId.Should().Be(resultId);
            (await db.SparkGrants.SingleAsync(grant => grant.UserId == user.Id)).Consumed.Should().Be(1);
        }
        finally
        {
            await CleanupAsync(cycle.Id, [user.Id]);
        }
    }

    private static SparkAllocationService Allocation(EIContext db, TimeProvider clock) =>
        new(new SparkRepository(db), new UnitOfWork(db), clock, Options.Create(new SparkOptions()));

    private static TestClock NewClock() => new(DateTimeOffset.UtcNow.AddYears(5));

    private static SparkCycle OpenCycle(TimeProvider clock, int capacity)
    {
        var now = clock.GetUtcNow();
        var cycle = new SparkCycle(now.AddDays(-15), now.AddDays(15), capacity);
        cycle.SetState(SparkCycleState.Open);
        return cycle;
    }

    private static User NewUser()
    {
        var email = $"{Guid.NewGuid():N}@spark.test";
        return new User(email, "hash", UserRole.CommonUser)
        {
            Id = Guid.NewGuid(),
            NormalizedEmail = email.ToUpperInvariant(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static async Task CleanupAsync(Guid cycleId, Guid[] userIds)
    {
        await using var db = VerificationTestSupport.Open();
        await db.SparkGrants.Where(grant => grant.CycleId == cycleId).ExecuteDeleteAsync();
        await db.SparkQueueRequests.Where(request => userIds.Contains(request.UserId)).ExecuteDeleteAsync();
        await db.SparkCycles.Where(cycle => cycle.Id == cycleId).ExecuteDeleteAsync();
        await db.Games.Where(game => userIds.Contains(game.OwnerUserId)).ExecuteDeleteAsync();
        await db.Users.Where(user => userIds.Contains(user.Id)).ExecuteDeleteAsync();
    }

    private sealed class TestClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan elapsed) => _now += elapsed;
    }
}
