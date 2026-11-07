using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Context;
using ei_back.Infrastructure.Context.Repository;
using Microsoft.EntityFrameworkCore;

namespace ei_back.Tests.Infrastructure;

public class UnitOfWorkTransactionTests
{
    private static User NewUser() => new(Guid.NewGuid().ToString("N"), "Transaction Test", "test@example.com", "hash", UserRole.CommonUser)
    {
        Id = Guid.NewGuid(), NormalizedEmail = $"{Guid.NewGuid():N}@EXAMPLE.COM",
        CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
    };

    [VerificationDatabaseFact]
    public async Task Success_SavesAutomatically_ReturnsResult_AndAllowsLaterNormalCommit()
    {
        await using var db = VerificationTestSupport.Open();
        using var unitOfWork = new UnitOfWork(db);
        var user = NewUser();
        var result = await unitOfWork.ExecuteInTransactionAsync(ct =>
        {
            db.Users.Add(user);
            return Task.FromResult(42);
        });
        result.Should().Be(42);
        await using var check = VerificationTestSupport.Open();
        (await check.Users.AnyAsync(x => x.Id == user.Id)).Should().BeTrue();
        user.FullName = "Updated";
        await unitOfWork.CommitAsync();
        (await check.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id)).FullName.Should().Be("Updated");
    }

    [VerificationDatabaseFact]
    public async Task NestedTransactionsAndManualCommitOrRollback_AreRejected_AndOuterChangesRolledBack()
    {
        foreach (var operation in new[] { "nested", "other-unit", "commit", "commit-async", "rollback" })
        {
            await using var db = VerificationTestSupport.Open();
            using var unitOfWork = new UnitOfWork(db);
            var user = NewUser();
            Func<Task> act = () => unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                db.Users.Add(user);
                switch (operation)
                {
                    case "nested": await unitOfWork.ExecuteInTransactionAsync(_ => Task.FromResult(0)); break;
                    case "other-unit": await new UnitOfWork(db).ExecuteInTransactionAsync(_ => Task.FromResult(0)); break;
                    case "commit": unitOfWork.Commit(); break;
                    case "commit-async": await unitOfWork.CommitAsync(ct); break;
                    case "rollback": unitOfWork.Rollback(); break;
                }
                return 0;
            });
            await act.Should().ThrowAsync<InvalidOperationException>();
            db.ChangeTracker.Entries().Should().BeEmpty();
            db.Database.CurrentTransaction.Should().BeNull();
            await unitOfWork.CommitAsync();
            (await db.Users.AnyAsync(x => x.Id == user.Id)).Should().BeFalse();
        }
    }

    [VerificationDatabaseFact]
    public async Task Cancellation_RollsBackEvenAfterDatabaseWrite_AndClearsTracking()
    {
        await using var db = VerificationTestSupport.Open();
        using var unitOfWork = new UnitOfWork(db);
        using var cancellation = new CancellationTokenSource();
        var user = NewUser();
        Func<Task> act = () => unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            db.Users.Add(user);
            await db.SaveChangesAsync(ct); // Simulate a repository write inside the transaction.
            cancellation.Cancel();
            return true;
        }, cancellation.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
        db.ChangeTracker.Entries().Should().BeEmpty();
        db.Database.CurrentTransaction.Should().BeNull();
        (await db.Users.AnyAsync(x => x.Id == user.Id)).Should().BeFalse();
    }

    [VerificationDatabaseFact]
    public async Task UniquenessConflict_UsesGenericPersistenceError_AndClearsFailedChanges()
    {
        await using var db = VerificationTestSupport.Open();
        using var unitOfWork = new UnitOfWork(db);
        var first = NewUser();
        db.Users.Add(first);
        await unitOfWork.CommitAsync();
        var duplicate = NewUser();
        duplicate.NormalizedEmail = first.NormalizedEmail;
        Func<Task> save = () => unitOfWork.ExecuteInTransactionAsync(ct =>
        {
            db.Users.Add(duplicate);
            return Task.FromResult(true);
        });
        await save.Should().ThrowAsync<ei_back.Core.Application.Repository.PersistenceConflictException>();
        db.ChangeTracker.Entries().Should().BeEmpty();
        (await db.Users.CountAsync(x => x.NormalizedEmail == first.NormalizedEmail)).Should().Be(1);
    }
    [VerificationDatabaseFact]
    public async Task ExistingTransaction_IsNotTakenOver_AndVerificationLockRequiresTransaction()
    {
        await using var db = VerificationTestSupport.Open();
        using var unitOfWork = new UnitOfWork(db);
        Func<Task> acquire = () => new VerificationCodeRepository(db).AcquireLockAsync("TEST@EXAMPLE.COM");
        await acquire.Should().ThrowAsync<InvalidOperationException>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        Func<Task> nested = () => unitOfWork.ExecuteInTransactionAsync(_ => Task.FromResult(1));
        await nested.Should().ThrowAsync<InvalidOperationException>();
        db.Database.CurrentTransaction.Should().BeSameAs(transaction);
        await transaction.RollbackAsync();
    }
}
