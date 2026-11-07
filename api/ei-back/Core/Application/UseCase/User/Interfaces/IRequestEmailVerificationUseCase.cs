using ei_back.Core.Application.UseCase.User.Dtos;

namespace ei_back.Core.Application.UseCase.User.Interfaces;

public interface IRequestEmailVerificationUseCase
{
    Task Handler(EmailCodeRequestDto request, CancellationToken cancellationToken = default);
}