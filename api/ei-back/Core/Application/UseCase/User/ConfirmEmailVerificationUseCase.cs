using ei_back.Core.Application.Service.Verification.Interfaces;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.UseCase.User;

public class ConfirmEmailVerificationUseCase(IVerificationFlowService verificationFlowService) : IConfirmEmailVerificationUseCase
{
    public Task<CodeConfirmationResult> Handler(ConfirmEmailVerificationRequestDto request, CancellationToken cancellationToken = default) =>
        verificationFlowService.ConfirmAsync(request.Email, request.Code, VerificationCodePurpose.EmailVerification, user =>
        {
            user.EmailVerifiedAt = DateTimeOffset.UtcNow;
            return Task.CompletedTask;
        }, cancellationToken);
}