using ei_back.Core.Application.UseCase.User.Dtos;
namespace ei_back.Core.Application.UseCase.User.Interfaces;

public interface IRefreshTokenUseCase
{
    Task<TokenDtoReponse> Handler(RefreshTokenDtoRequest request, CancellationToken cancellationToken = default);
}
