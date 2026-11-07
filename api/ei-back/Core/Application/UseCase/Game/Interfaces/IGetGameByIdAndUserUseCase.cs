using ei_back.Core.Application.UseCase.Game.Dtos;

namespace ei_back.Core.Application.UseCase.Game.Interfaces;

public interface IGetGameByIdAndUserUseCase
{
    Task<GameDtoResponse> Handler(Guid gameId, string userName, CancellationToken cancellationToken);
}