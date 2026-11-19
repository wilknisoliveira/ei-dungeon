using ei_back.Core.Application.UseCase.User.Dtos;
namespace ei_back.Core.Application.UseCase.User.Interfaces;

public interface ISignInUseCase
{
    Task<TokenDtoReponse?> Handler(LoginDtoRequest request, CancellationToken cancellationToken = default);
}
