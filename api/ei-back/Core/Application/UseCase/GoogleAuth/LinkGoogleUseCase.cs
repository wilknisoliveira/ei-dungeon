using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Auth.Interfaces;
using ei_back.Core.Application.Utils;
using ei_back.Core.Domain.DomainExceptions;
using ei_back.Core.Domain.Enums;
using ei_back.Core.Application.UseCase.GoogleAuth.Dtos;
using ei_back.Core.Application.UseCase.GoogleAuth.Interfaces;

namespace ei_back.Core.Application.UseCase.GoogleAuth;

public class LinkGoogleUseCase(IExternalIdentityRepository externalIdentityRepository,
    IVerificationCodeRepository verificationCodeRepository, IGoogleIdentityVerifier identityVerifier,
    IGoogleAuthFlowService authFlowService) : ILinkGoogleUseCase
{
    public async Task Handler(Guid userId, GoogleAuthRequestDto request, string browserSecret,
        CancellationToken cancellationToken)
    {
        var identity = await identityVerifier.VerifyAsync(request.Credential, cancellationToken);
        await authFlowService.ExecuteAsync(async ct =>
        {
            await authFlowService.ConsumeAsync(request.ChallengeId, browserSecret, identity, GoogleAuthPurpose.Link, userId, ct);
            await verificationCodeRepository.AcquireLockAsync("google:" + identity.Subject, ct);
            var user = await authFlowService.GetVerifiedUserAsync(userId, ct);
            await verificationCodeRepository.AcquireLockAsync(user.NormalizedEmail, ct);
            if (!identity.EmailVerified || string.IsNullOrWhiteSpace(identity.Email) ||
                EmailNormalizer.Normalize(identity.Email) != user.NormalizedEmail)
                throw Error("google_email_mismatch");
            var bySubject = await externalIdentityRepository.FindBySubjectAsync(identity.Subject, ct);
            var byUser = await externalIdentityRepository.FindByUserAsync(userId, ct);
            if (bySubject?.UserId == userId) return true;
            if (bySubject != null || byUser != null) throw Error("google_link_conflict");
            authFlowService.AddLink(userId, identity.Subject);
            return true;
        }, cancellationToken);
    }

    private static GoogleAuthenticationException Error(string code) => new(code);
}
