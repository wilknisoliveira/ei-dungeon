using ei_back.Core.Application.UseCase.User.Dtos;

namespace ei_back.Core.Application.UseCase.User.Interfaces;

public interface IConfirmEmailVerificationUseCase
{
    Task<CodeConfirmationResult> Handler(ConfirmEmailVerificationRequestDto request, CancellationToken cancellationToken = default);
}