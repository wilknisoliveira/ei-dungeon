using AutoMapper;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.Game.Dtos;
using ei_back.Core.Application.UseCase.Game.Interfaces;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;

namespace ei_back.Core.Application.UseCase.Game
{
    public class UpdateGameUseCase : IUpdateGameUseCase
    {
        private readonly IMapper _mapper;
        private readonly IGameRepository _gameRepository;

        public UpdateGameUseCase(IMapper mapper, IGameRepository gameRepository)
        {
            _mapper = mapper;
            _gameRepository = gameRepository;
        }

        public async Task<GameDtoResponse> Handler(
            Guid gameId,
            UpdateGameDtoRequest request,
            string userName,
            CancellationToken cancellationToken)
        {
            var game = await _gameRepository.GetGameByIdAndOwnerUserName(gameId, userName, cancellationToken)
                ?? throw new NotFoundException($"No game found with id {gameId} for user {userName}.");

            if (request.Name is not null)
                game.SetName(request.Name);

            if (request.GameLanguage is not null)
                game.SetGameLanguage(request.GameLanguage.Value);

            game.SetUpdatedDate(DateTimeOffset.UtcNow);

            return _mapper.Map<GameDtoResponse>(game);
        }
    }
}
