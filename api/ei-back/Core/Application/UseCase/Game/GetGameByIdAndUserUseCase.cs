using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.Game.Dtos;
using ei_back.Core.Application.UseCase.Game.Interfaces;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;

namespace ei_back.Core.Application.UseCase.Game;

public class GetGameByIdAndUserUseCase(IGameRepository gameRepository) : IGetGameByIdAndUserUseCase
{
    private readonly IGameRepository _gameRepository = gameRepository;

    public async Task<GameDtoResponse> Handler(Guid gameId, string userName, CancellationToken cancellationToken)
    {
        Domain.Entity.Game game = await _gameRepository.GetGameByIdAndOwnerUserName(gameId, userName, cancellationToken) ??
                   throw new NotFoundException($"No game with id '{gameId}' found to user name {userName}.");

        return new GameDtoResponse
        {
            GameStatus = game.GameStatus,
            Id = game.Id,
            Name = game.Name,
            OwnerUserId = game.OwnerUserId,
            GameLanguage = game.GameLanguage,
            ProtagonistName = game.ProtagonistName,
            LastPlayedAt = game.LastPlayedAt
        };
    }
}