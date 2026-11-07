using ei_back.Core.Application.UseCase.Game.Dtos;

namespace ei_back.Core.Application.UseCase.Game.Interfaces
{
    public interface ICreateGameUseCase
    {
        Task<GameDtoResponse> Handler(GameDtoRequest gameDtoRequest, string userName, CancellationToken cancellationToken);
    }
}
