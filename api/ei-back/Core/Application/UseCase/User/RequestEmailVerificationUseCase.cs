using ei_back.Core.Application.Service.Verification.Interfaces;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.UseCase.User;

public class RequestEmailVerificationUseCase(IVerificationFlowService verificationFlowService) : IRequestEmailVerificationUseCase
{
    public Task Handler(EmailCodeRequestDto request, CancellationToken cancellationToken = default) =>
        verificationFlowService.RequestAsync(request.Email, VerificationCodePurpose.EmailVerification, cancellationToken);
}