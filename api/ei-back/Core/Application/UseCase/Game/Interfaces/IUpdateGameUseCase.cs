using ei_back.Core.Application.UseCase.Game.Dtos;

namespace ei_back.Core.Application.UseCase.Game.Interfaces
{
    public interface IUpdateGameUseCase
    {
        Task<GameDtoResponse> Handler(Guid gameId, UpdateGameDtoRequest request, string userName, CancellationToken cancellationToken);
    }
}
