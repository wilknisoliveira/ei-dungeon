using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Auth.Interfaces;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.Utils;
using ei_back.Core.Domain.DomainExceptions;
using ei_back.Core.Domain.Enums;
using ei_back.Core.Application.UseCase.GoogleAuth.Dtos;
using ei_back.Core.Application.UseCase.GoogleAuth.Interfaces;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Core.Application.UseCase.GoogleAuth;

public class GoogleSigninUseCase(IUserRepository userRepository,
    IExternalIdentityRepository externalIdentityRepository, IVerificationCodeRepository verificationCodeRepository,
    IGoogleIdentityVerifier identityVerifier, IApplicationSessionService sessionService, IGoogleAuthFlowService authFlowService) : IGoogleSigninUseCase
{
    public async Task<TokenDtoReponse> Handler(GoogleAuthRequestDto request, string browserSecret,
        CancellationToken cancellationToken)
    {
        var identity = await identityVerifier.VerifyAsync(request.Credential, cancellationToken);
        return await authFlowService.ExecuteAsync(async ct =>
        {
            await authFlowService.ConsumeAsync(request.ChallengeId, browserSecret, identity, GoogleAuthPurpose.SignIn, null, ct);
            await verificationCodeRepository.AcquireLockAsync("google:" + identity.Subject, ct);
            var link = await externalIdentityRepository.FindBySubjectAsync(identity.Subject, ct);
            if (link != null)
                return sessionService.Create(await authFlowService.GetVerifiedUserAsync(link.UserId, ct));
            if (!identity.HasAuthoritativeEmail || !System.Net.Mail.MailAddress.TryCreate(identity.Email, out _))
                throw Error("google_registration_required");
            var normalized = EmailNormalizer.Normalize(identity.Email!);
            await verificationCodeRepository.AcquireLockAsync(normalized, ct);
            if (await userRepository.FindByEmail(normalized, ct) != null)
                throw Error("google_link_required");
            var now = DateTimeOffset.UtcNow;
            var user = new UserEntity(identity.Email!.Trim(), null, UserRole.CommonUser)
            {
                Id = Guid.NewGuid(), NormalizedEmail = normalized, EmailVerifiedAt = now,
                CreatedAt = now, UpdatedAt = now
            };
            await userRepository.CreateAsync(user, ct);
            authFlowService.AddLink(user.Id, identity.Subject);
            return sessionService.Create(user);
        }, cancellationToken);
    }

    private static GoogleAuthenticationException Error(string code) => new(code);
}
