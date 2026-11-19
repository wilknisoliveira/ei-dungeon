using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Auth;
using ei_back.Core.Application.Service.Auth.Interfaces;
using ei_back.Core.Application.Service.Encryption;
using ei_back.Core.Application.UseCase.GoogleAuth;
using ei_back.Core.Application.UseCase.GoogleAuth.Dtos;
using ei_back.Core.Domain.DomainExceptions;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Context;
using ei_back.Infrastructure.Context.Repository;
using ei_back.Infrastructure.Token;
using Microsoft.EntityFrameworkCore;

namespace ei_back.Tests.Infrastructure;

public class GoogleIdentityPersistenceTests
{
    private static (IssueGoogleChallengeUseCase Issue, GoogleSigninUseCase Signin, CreatePasswordUseCase CreatePassword) Build(EIContext db, string subject, string email)
    {
        var verifier = A.Fake<IGoogleIdentityVerifier>();
        A.CallTo(() => verifier.IsConfigured).Returns(true);
        A.CallTo(() => verifier.VerifyAsync(A<string>._, A<CancellationToken>._))
            .ReturnsLazily((string credential, CancellationToken ct) => Task.FromResult(new GoogleIdentity(subject, email, true, null, credential)));
        var encryption = new EncryptionService();
        var tokenConfiguration = new TokenConfiguration
        {
            Issuer = "test", Audience = "test", Secret = new string('s', 64), Minutes = 15, DaysToExpiry = 7
        };
        var users = new UserRepository(db);
        var identities = new ExternalIdentityRepository(db);
        var challenges = new GoogleAuthChallengeRepository(db);
        var locks = new VerificationCodeRepository(db);
        var unitOfWork = new UnitOfWork(db);
        var flow = new GoogleAuthFlowService(users, identities, challenges, unitOfWork);
        var sessions = new ApplicationSessionService(new TokenService(tokenConfiguration), new RefreshTokenRepository(db), encryption, tokenConfiguration);
        return (new(verifier, flow, challenges, unitOfWork),
            new(users, identities, locks, verifier, sessions, flow),
            new(users, identities, locks, verifier, encryption, flow));
    }

    [VerificationDatabaseFact]
    public async Task Register_ReplayAndReturningLogin_PreserveSingleAccountAndSessions()
    {
        var subject = Guid.NewGuid().ToString("N");
        var email = subject + "@gmail.com";
        await using var db = VerificationTestSupport.Open();
        var useCase = Build(db, subject, email);
        var challenge = await useCase.Issue.Handler(GoogleAuthPurpose.SignIn, null, "browser", CancellationToken.None);
        var request = new GoogleAuthRequestDto { ChallengeId = challenge.ChallengeId, Credential = challenge.Nonce };
        (await useCase.Signin.Handler(request, "browser", CancellationToken.None)).AccessToken.Should().NotBeNullOrEmpty();
        Func<Task> replay = () => useCase.Signin.Handler(request, "browser", CancellationToken.None);
        await replay.Should().ThrowAsync<GoogleAuthenticationException>();
        var user = await db.Users.SingleAsync(x => x.Email == email);
        var next = await useCase.Issue.Handler(GoogleAuthPurpose.SignIn, null, "browser", CancellationToken.None);
        await useCase.Signin.Handler(new() { ChallengeId = next.ChallengeId, Credential = next.Nonce }, "browser", CancellationToken.None);
        (await db.ExternalIdentities.CountAsync(x => x.Subject == subject)).Should().Be(1);
        (await db.RefreshTokens.CountAsync(x => x.UserId == user.Id)).Should().Be(2);
        user.Password.Should().BeNull();
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        (await db.ExternalIdentities.AnyAsync(x => x.Subject == subject)).Should().BeFalse();
    }

    [VerificationDatabaseFact]
    public async Task ConcurrentNewRegistration_SameSubject_CreatesOneAccount()
    {
        var subject = Guid.NewGuid().ToString("N");
        var email = subject + "@gmail.com";
        async Task Register()
        {
            await using var db = VerificationTestSupport.Open();
            var useCase = Build(db, subject, email);
            var challenge = await useCase.Issue.Handler(GoogleAuthPurpose.SignIn, null, "browser", CancellationToken.None);
            await useCase.Signin.Handler(new() { ChallengeId = challenge.ChallengeId, Credential = challenge.Nonce }, "browser", CancellationToken.None);
        }
        await Task.WhenAll(Register(), Register());
        await using var check = VerificationTestSupport.Open();
        (await check.Users.CountAsync(x => x.Email == email)).Should().Be(1);
        (await check.ExternalIdentities.CountAsync(x => x.Subject == subject)).Should().Be(1);
    }

    [VerificationDatabaseFact]
    public async Task ConcurrentFirstPassword_OnlyOneProofCanEstablishPassword()
    {
        var subject = Guid.NewGuid().ToString("N");
        var email = subject + "@gmail.com";
        Guid userId;
        await using (var db = VerificationTestSupport.Open())
        {
            var useCase = Build(db, subject, email);
            var challenge = await useCase.Issue.Handler(GoogleAuthPurpose.SignIn, null, "browser", CancellationToken.None);
            await useCase.Signin.Handler(new() { ChallengeId = challenge.ChallengeId, Credential = challenge.Nonce }, "browser", CancellationToken.None);
            userId = (await db.Users.SingleAsync(x => x.Email == email)).Id;
        }
        async Task<bool> Create(string password)
        {
            await using var db = VerificationTestSupport.Open();
            var useCase = Build(db, subject, email);
            var challenge = await useCase.Issue.Handler(GoogleAuthPurpose.CreatePassword, userId, "browser", CancellationToken.None);
            try
            {
                await useCase.CreatePassword.Handler(userId, new() { ChallengeId = challenge.ChallengeId, Credential = challenge.Nonce, NewPassword = password }, "browser", CancellationToken.None);
                return true;
            }
            catch (GoogleAuthenticationException error) when (error.Code == "password_already_exists") { return false; }
        }
        (await Task.WhenAll(Create("first-password"), Create("second-password"))).Count(success => success).Should().Be(1);
        await using var check = VerificationTestSupport.Open();
        var hash = (await check.Users.SingleAsync(x => x.Id == userId)).Password;
        (BCrypt.Net.BCrypt.Verify("first-password", hash) || BCrypt.Net.BCrypt.Verify("second-password", hash)).Should().BeTrue();
    }
}
