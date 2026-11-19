using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.Game.Interfaces;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;

namespace ei_back.Core.Application.UseCase.Game;

public class DeleteGameUseCase(IGameRepository gameRepository) : IDeleteGameUseCase
{
    private readonly IGameRepository _gameRepository = gameRepository;

    public async Task Handler(Guid gameId, Guid userId, CancellationToken cancellationToken)
    {
        Domain.Entity.Game game = await _gameRepository
            .GetGameByIdAndOwnerId(gameId, userId, cancellationToken)??
            throw new NotFoundException($"No game with id '{gameId}' found to user ID {userId}.");

        _gameRepository.Delete(gameId);
    }
}