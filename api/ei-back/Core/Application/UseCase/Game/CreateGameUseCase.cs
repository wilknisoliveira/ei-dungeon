using AutoMapper;
using ei_back.Core.Application.Interfaces;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Play.Interfaces;
using ei_back.Core.Application.UseCase.Game.Dtos;
using ei_back.Core.Application.UseCase.Game.Interfaces;
using ei_back.Core.Domain.DomainExceptions.Game;
using ei_back.Core.Domain.Entity;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;

namespace ei_back.Core.Application.UseCase.Game
{
    public class CreateGameUseCase : ICreateGameUseCase
    {
        private readonly IMapper _mapper;
        private readonly IGameRepository _gameRepository;
        private readonly IUserRepository _userRepository;
        private readonly IGenAi _genAi;
        private readonly IUpsertWorldInfoService _upsertWorldInfoService;

        public CreateGameUseCase(
            IMapper mapper,
            IGameRepository gameRepository,
            IUserRepository userRepository,
            IGenAi genAi, 
            IUpsertWorldInfoService upsertWorldInfoService)
        {
            _mapper = mapper;
            _gameRepository = gameRepository;
            _userRepository = userRepository;
            _genAi = genAi;
            _upsertWorldInfoService = upsertWorldInfoService;
        }

        public async Task<GameDtoResponse> Handler(GameDtoRequest gameDtoRequest, string userName, CancellationToken cancellationToken)
        {
            var user = await _userRepository.FindByUserName(userName) ??
                throw new NotFoundException($"No user found to user name {userName}.");

            var game = new Domain.Entity.Game(user, gameDtoRequest.Name);
            
            game.SetOwnerUser(user);

            game.SetProtagonistInfo(
                gameDtoRequest.ProtagonistName,
                gameDtoRequest.ProtagonistDescription,
                gameDtoRequest.ProtagonistRace);

            try
            {
                game.SetSkillPoints(
                    gameDtoRequest.Skills.Strength,
                    gameDtoRequest.Skills.Dexterity,
                    gameDtoRequest.Skills.Intelligence, 
                    gameDtoRequest.Skills.Constitution,
                    gameDtoRequest.Skills.Charisma,
                    gameDtoRequest.Skills.Wisdom);
            }
            catch (AttributePointsNotValidException ex)
            {
                throw new BadRequestException(ex.Message);
            }

            game.SetGameLanguage(gameDtoRequest.GameLanguage);
            game.SetWorldInfo(await _upsertWorldInfoService.Handler(game.InfoToString(), gameDtoRequest.GameLanguage, cancellationToken));

            game.SetCreatedDate(DateTimeOffset.UtcNow);
            var gameResponse = await _gameRepository.CreateAsync(game, cancellationToken);

            return _mapper.Map<GameDtoResponse>(gameResponse);
        }
    }
}
