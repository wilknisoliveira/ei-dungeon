using System.Security.Cryptography;
using System.Text;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Auth;
using ei_back.Core.Application.Service.Auth.Interfaces;
using ei_back.Core.Application.Service.Encryption;
using ei_back.Tests.Core.Application.UseCase.User;
using ei_back.Core.Application.UseCase.GoogleAuth;
using ei_back.Core.Application.UseCase.GoogleAuth.Dtos;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Tests.Core.Application.UseCase.GoogleAuth;

internal sealed class GoogleAuthUseCaseTestContext
{
    public readonly IUserRepository Users = A.Fake<IUserRepository>();
    public readonly IExternalIdentityRepository Identities = A.Fake<IExternalIdentityRepository>();
    public readonly IGoogleAuthChallengeRepository Challenges = A.Fake<IGoogleAuthChallengeRepository>();
    public readonly IGoogleIdentityVerifier Verifier = A.Fake<IGoogleIdentityVerifier>();
    public readonly IApplicationSessionService Sessions = A.Fake<IApplicationSessionService>();
    public readonly IssueGoogleChallengeUseCase Issue;
    public readonly GoogleSigninUseCase Signin;
    public readonly LinkGoogleUseCase Link;
    public readonly CreatePasswordUseCase CreatePassword;
    public readonly GetCurrentAccountUseCase GetAccount;
    public readonly GoogleAuthRequestDto Request = new() { ChallengeId = Guid.NewGuid(), Credential = "credential" };
    public readonly UserEntity User = new("player@gmail.com", null, UserRole.CommonUser)
    {
        Id = Guid.NewGuid(), NormalizedEmail = "PLAYER@GMAIL.COM", EmailVerifiedAt = DateTimeOffset.UtcNow
    };

    public GoogleAuthUseCaseTestContext()
    {
        var locks = A.Fake<IVerificationCodeRepository>();
        var unitOfWork = new TestUnitOfWork();
        var flow = new GoogleAuthFlowService(Users, Identities, Challenges, unitOfWork);
        Issue = new(Verifier, flow, Challenges, unitOfWork);
        Signin = new(Users, Identities, locks, Verifier, Sessions, flow);
        Link = new(Identities, locks, Verifier, flow);
        CreatePassword = new(Users, Identities, locks, Verifier, new EncryptionService(), flow);
        GetAccount = new(Identities, flow);
        A.CallTo(() => Verifier.IsConfigured).Returns(true);
        A.CallTo(() => Users.FindByIdAsync(User.Id, A<CancellationToken>._)).Returns(User);
        A.CallTo(() => Users.ReloadAsync(User.Id, A<CancellationToken>._)).Returns(User);
        A.CallTo(() => Users.FindByEmail(A<string>._, A<CancellationToken>._)).Returns((UserEntity?)null);
        A.CallTo(() => Identities.FindBySubjectAsync(A<string>._, A<CancellationToken>._)).Returns((ExternalIdentity?)null);
        A.CallTo(() => Identities.FindByUserAsync(A<Guid>._, A<CancellationToken>._)).Returns((ExternalIdentity?)null);
        A.CallTo(() => Challenges.ConsumeAsync(Request.ChallengeId, A<DateTimeOffset>._, A<CancellationToken>._)).Returns(true);
        ConfigureAttempt(GoogleAuthPurpose.SignIn, null);
        SetIdentity("player@gmail.com", true, null);
    }

    public void ConfigureAttempt(GoogleAuthPurpose purpose, Guid? userId)
    {
        A.CallTo(() => Challenges.FindAsync(Request.ChallengeId, A<CancellationToken>._)).Returns(new GoogleAuthChallenge
        {
            Id = Request.ChallengeId, Purpose = purpose, UserId = userId,
            BrowserSecretHash = Hash("browser"), NonceHash = Hash("nonce"), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
        });
    }
    public void SetIdentity(string? email, bool verified, string? domain, string nonce = "nonce") =>
        A.CallTo(() => Verifier.VerifyAsync(Request.Credential, A<CancellationToken>._))
            .Returns(new GoogleIdentity("subject", email, verified, domain, nonce));
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

}
