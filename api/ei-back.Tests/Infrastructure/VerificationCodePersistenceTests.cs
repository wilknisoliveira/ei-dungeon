using ei_back.Core.Application.Service.Verification;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Context;
using ei_back.Infrastructure.Context.Repository;
using Microsoft.EntityFrameworkCore;

namespace ei_back.Tests.Infrastructure;

public class VerificationDatabaseFactAttribute : FactAttribute
{
    public VerificationDatabaseFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VERIFICATION_TEST_CONNECTION")))
            Skip = "Set VERIFICATION_TEST_CONNECTION to a migrated isolated PostgreSQL database.";
    }
}

public class VerificationCodePersistenceTests
{
    private static EIContext Open() => new(new DbContextOptionsBuilder<EIContext>()
        .UseNpgsql(Environment.GetEnvironmentVariable("VERIFICATION_TEST_CONNECTION")).Options);

    [VerificationDatabaseFact]
    public async Task ConcurrentIssuanceAndConfirmation_RespectCooldownAndSingleUse()
    {
        var email = $"{Guid.NewGuid():N}@verification.test".ToUpperInvariant();
        var user = new User("test_" + Guid.NewGuid().ToString("N"), "Test", email, "hash", UserRole.CommonUser)
            { Id = Guid.NewGuid(), NormalizedEmail = email, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        await using (var setup = Open()) { setup.Users.Add(user); await setup.SaveChangesAsync(); }
        var service = new VerificationCodeService();
        var issued = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            await using var db = Open();
            var repository = new VerificationCodeRepository(db);
            return await new UnitOfWork(db).ExecuteInTransactionAsync(async ct =>
            {
                await new VerificationCodeRepository(db).AcquireLockAsync(email, ct);
                var now = DateTimeOffset.UtcNow;
                var history = await repository.GetHistoryAsync(user.Id, VerificationCodePurpose.EmailVerification, now.AddHours(-1));
                if (!service.CanIssue(history, now)) return false;
                repository.Add(service.Create(user.Id, email, VerificationCodePurpose.EmailVerification, now));
                return true;
            });
        }));
        issued.Count(x => x).Should().Be(1);
        var confirmations = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            await using var db = Open();
            return await new UnitOfWork(db).ExecuteInTransactionAsync(async ct =>
            {
                await new VerificationCodeRepository(db).AcquireLockAsync(email, ct);
                var code = await new VerificationCodeRepository(db).GetLatestAsync(user.Id, VerificationCodePurpose.EmailVerification);
                return service.Confirm(code!, user.Id, email, VerificationCodePurpose.EmailVerification, DateTimeOffset.UtcNow, true);
            });
        }));
        confirmations.Count(x => x).Should().Be(1);
    }

    [VerificationDatabaseFact]
    public async Task ConcurrentFailures_StayAtFive_AndUniqueEmailIsEnforced()
    {
        var email = $"{Guid.NewGuid():N}@verification.test".ToUpperInvariant();
        var user = new User(Guid.NewGuid().ToString("N"), "Test", email, "hash", UserRole.CommonUser)
            { Id = Guid.NewGuid(), NormalizedEmail = email, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        var service = new VerificationCodeService();
        await using (var setup = Open())
        {
            setup.Users.Add(user);
            setup.VerificationCodes.Add(service.Create(user.Id, email, VerificationCodePurpose.PasswordRecovery, DateTimeOffset.UtcNow));
            await setup.SaveChangesAsync();
        }
        await Task.WhenAll(Enumerable.Range(0, 12).Select(async _ =>
        {
            await using var db = Open();
            await new UnitOfWork(db).ExecuteInTransactionAsync(async ct =>
            {
                await new VerificationCodeRepository(db).AcquireLockAsync(email, ct);
                var code = await new VerificationCodeRepository(db).GetLatestAsync(user.Id, VerificationCodePurpose.PasswordRecovery);
                return service.Confirm(code!, user.Id, email, code!.Purpose, DateTimeOffset.UtcNow, false);
            });
        }));
        await using var check = Open();
        var saved = await check.VerificationCodes.SingleAsync(x => x.UserId == user.Id);
        saved.FailedAttempts.Should().Be(5);
        saved.InvalidatedAt.Should().NotBeNull();
        check.Users.Add(new User(Guid.NewGuid().ToString("N"), "Other", email.ToLowerInvariant(), "hash", UserRole.CommonUser)
            { Id = Guid.NewGuid(), NormalizedEmail = email });
        Func<Task> save = () => check.SaveChangesAsync();
        await save.Should().ThrowAsync<DbUpdateException>();
    }
}