using ei_back.Core.Application.Service.Email;
using System.Text.RegularExpressions;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Email.Interfaces;
using ei_back.Core.Application.Service.Encryption;
using ei_back.Core.Application.UseCase.User;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Context.Repository;
using ei_back.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Tests.Core.Application.UseCase.User;

public class PasswordRecoveryUseCaseTests
{
    private static UserEntity User(bool verified = true) => new(Guid.NewGuid().ToString("N"), "Recovery Test",
        $"{Guid.NewGuid():N}@example.com", BCrypt.Net.BCrypt.HashPassword("old-password"), UserRole.CommonUser)
    {
        Id = Guid.NewGuid(), EmailVerifiedAt = verified ? DateTimeOffset.UtcNow : null,
        CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
    };

    [VerificationDatabaseFact]
    public async Task Recovery_ChangesPasswordConsumesCodeAndRevokesOnlyOwnSessions()
    {
        var user = User();
        var other = User();
        foreach (var item in new[] { user, other }) item.NormalizedEmail = item.Email.ToUpperInvariant();
        await using (var db = VerificationTestSupport.Open())
        {
            db.Users.AddRange(user, other);
            foreach (var owner in new[] { user, user, other }) db.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(), UserId = owner.Id, TokenHash = Guid.NewGuid().ToString("N"),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(1), CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }
        string plain = "";
        var sender = VerificationTestSupport.Capture((_, code) => plain = code);
        await using (var db = VerificationTestSupport.Open())
            await new RequestPasswordRecoveryUseCase(VerificationTestSupport.Flow(db, sender)).Handler(new() { Email = user.Email });
        await using (var db = VerificationTestSupport.Open())
        {
            var flow = VerificationTestSupport.Flow(db, sender);
            // An email-verification confirmation cannot redeem a recovery code.
            (await new ConfirmEmailVerificationUseCase(flow).Handler(new() { Email = user.Email, Code = plain })).Should().Be(CodeConfirmationResult.Invalid);
            var confirm = new ConfirmPasswordRecoveryUseCase(flow, new EncryptionService(), new RefreshTokenRepository(db));
            (await confirm.Handler(new() { Email = user.Email, Code = "wrong", NewPassword = "new-password" })).Should().Be(CodeConfirmationResult.Invalid);
        }
        await using (var db = VerificationTestSupport.Open())
        {
            (await db.VerificationCodes.SingleAsync(x => x.UserId == user.Id)).FailedAttempts.Should().Be(1);
            var confirm = new ConfirmPasswordRecoveryUseCase(VerificationTestSupport.Flow(db, sender), new EncryptionService(), new RefreshTokenRepository(db));
            (await confirm.Handler(new() { Email = user.Email.ToUpperInvariant(), Code = plain, NewPassword = "new-password" })).Should().Be(CodeConfirmationResult.Confirmed);
        }
        await using (var db = VerificationTestSupport.Open())
        {
            var saved = await db.Users.SingleAsync(x => x.Id == user.Id);
            BCrypt.Net.BCrypt.Verify("new-password", saved.Password).Should().BeTrue();
            BCrypt.Net.BCrypt.Verify("old-password", saved.Password).Should().BeFalse();
            (await db.RefreshTokens.CountAsync(x => x.UserId == user.Id)).Should().Be(0);
            (await db.RefreshTokens.CountAsync(x => x.UserId == other.Id)).Should().Be(1);
            var confirm = new ConfirmPasswordRecoveryUseCase(VerificationTestSupport.Flow(db, sender), new EncryptionService(), new RefreshTokenRepository(db));
            (await confirm.Handler(new() { Email = user.Email, Code = plain, NewPassword = "another-password" })).Should().Be(CodeConfirmationResult.Invalid);
        }
    }

    [VerificationDatabaseFact]
    public async Task Recovery_ApplicationFailureRollsBackPasswordAndConsumption()
    {
        var user = User();
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        await using (var db = VerificationTestSupport.Open()) { db.Users.Add(user); await db.SaveChangesAsync(); }
        string plain = "";
        var sender = VerificationTestSupport.Capture((_, code) => plain = code);
        await using (var db = VerificationTestSupport.Open())
            await new RequestPasswordRecoveryUseCase(VerificationTestSupport.Flow(db, sender)).Handler(new() { Email = user.Email });
        await using (var db = VerificationTestSupport.Open())
        {
            var tokens = A.Fake<IRefreshTokenRepository>();
            A.CallTo(() => tokens.DeleteByUserId(user.Id)).Throws(new InvalidOperationException("Simulated failure"));
            var confirm = new ConfirmPasswordRecoveryUseCase(VerificationTestSupport.Flow(db, sender), new EncryptionService(), tokens);
            Func<Task> action = () => confirm.Handler(new() { Email = user.Email, Code = plain, NewPassword = "new-password" });
            await action.Should().ThrowAsync<InvalidOperationException>();
        }
        await using (var db = VerificationTestSupport.Open())
        {
            (await db.Users.SingleAsync(x => x.Id == user.Id)).Password.Should().Be(user.Password);
            (await db.VerificationCodes.SingleAsync(x => x.UserId == user.Id)).ConsumedAt.Should().BeNull();
        }
    }

    [VerificationDatabaseFact]
    public async Task Recovery_UnverifiedAndUnknownEmails_DoNotSendCodes()
    {
        var user = User(false);
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        await using var db = VerificationTestSupport.Open();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var sender = A.Fake<IEmailSender>();
        var request = new RequestPasswordRecoveryUseCase(VerificationTestSupport.Flow(db, sender));
        await request.Handler(new() { Email = user.Email });
        await request.Handler(new() { Email = $"{Guid.NewGuid():N}@example.com" });
        (await db.VerificationCodes.CountAsync(x => x.UserId == user.Id)).Should().Be(0);
        A.CallTo(() => sender.SendAsync(A<OutgoingEmail>._, A<CancellationToken>._)).MustNotHaveHappened();
    }
}