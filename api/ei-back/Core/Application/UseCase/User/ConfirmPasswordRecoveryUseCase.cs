using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Encryption.Interfaces;
using ei_back.Core.Application.Service.Verification.Interfaces;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Domain.Enums;
namespace ei_back.Core.Application.UseCase.User;
public class ConfirmPasswordRecoveryUseCase(IVerificationFlowService verificationFlowService,
        IEncryptionService encryptionService,
        IRefreshTokenRepository refreshTokenRepository) : IConfirmPasswordRecoveryUseCase
{
    public Task<CodeConfirmationResult> Handler(ConfirmPasswordRecoveryRequestDto request, CancellationToken cancellationToken = default) =>
        verificationFlowService.ConfirmAsync(request.Email, request.Code, VerificationCodePurpose.PasswordRecovery, user =>
        {
            user.Password = encryptionService.ComputeBcryptHash(request.NewPassword);
            refreshTokenRepository.DeleteByUserId(user.Id);
            return Task.CompletedTask;
        }, cancellationToken);
}