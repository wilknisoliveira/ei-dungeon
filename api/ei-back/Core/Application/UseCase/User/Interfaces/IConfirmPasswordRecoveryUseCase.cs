using ei_back.Core.Application.UseCase.User.Dtos;
namespace ei_back.Core.Application.UseCase.User.Interfaces;
public interface IConfirmPasswordRecoveryUseCase
{
    Task<CodeConfirmationResult> Handler(ConfirmPasswordRecoveryRequestDto request, CancellationToken cancellationToken = default);
}