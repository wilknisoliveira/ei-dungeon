using ei_back.Core.Application.UseCase.User.Dtos;
namespace ei_back.Core.Application.UseCase.User.Interfaces;
public interface IRequestPasswordRecoveryUseCase
{
    Task Handler(EmailCodeRequestDto request, CancellationToken cancellationToken = default);
}