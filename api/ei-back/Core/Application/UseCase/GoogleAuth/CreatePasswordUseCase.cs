using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Auth.Interfaces;
using ei_back.Core.Application.Service.Encryption.Interfaces;
using ei_back.Core.Domain.DomainExceptions;
using ei_back.Core.Domain.Enums;
using ei_back.Core.Application.UseCase.GoogleAuth.Dtos;
using ei_back.Core.Application.UseCase.GoogleAuth.Interfaces;

namespace ei_back.Core.Application.UseCase.GoogleAuth;

public class CreatePasswordUseCase(IUserRepository userRepository,
    IExternalIdentityRepository externalIdentityRepository, IVerificationCodeRepository verificationCodeRepository,
    IGoogleIdentityVerifier identityVerifier, IEncryptionService encryptionService, IGoogleAuthFlowService authFlowService) : ICreatePasswordUseCase
{
    public async Task Handler(Guid userId, CreatePasswordDtoRequest request, string browserSecret,
        CancellationToken cancellationToken)
    {
        var identity = await identityVerifier.VerifyAsync(request.Credential, cancellationToken);
        await authFlowService.ExecuteAsync(async ct =>
        {
            await authFlowService.ConsumeAsync(request.ChallengeId, browserSecret, identity, GoogleAuthPurpose.CreatePassword, userId, ct);
            var link = await externalIdentityRepository.FindBySubjectAsync(identity.Subject, ct);
            if (link?.UserId != userId) throw Error("google_invalid_credential");
            var user = await authFlowService.GetVerifiedUserAsync(userId, ct);
            await verificationCodeRepository.AcquireLockAsync(user.NormalizedEmail, ct);
            // Reload after the account lock to observe a concurrently created password.
            user = await userRepository.ReloadAsync(userId, ct) ?? throw Error("google_invalid_credential");
            if (user.Password != null) throw Error("password_already_exists");
            user.Password = encryptionService.ComputeBcryptHash(request.NewPassword);
            user.UpdatedAt = DateTimeOffset.UtcNow;
            return true;
        }, cancellationToken);
    }

    private static GoogleAuthenticationException Error(string code) => new(code);
}
