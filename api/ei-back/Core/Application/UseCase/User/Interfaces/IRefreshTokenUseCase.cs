using ei_back.Core.Application.UseCase.User.Dtos;

namespace ei_back.Core.Application.UseCase.User.Interfaces
{
    public interface IRefreshTokenUseCase
    {
        TokenDtoReponse Handler(RefreshTokenDtoRequest request);
    }
}
