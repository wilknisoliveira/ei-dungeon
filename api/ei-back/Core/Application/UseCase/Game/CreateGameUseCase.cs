using AutoMapper;
using ei_back.Core.Application.Interfaces;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Play.Interfaces;
using ei_back.Core.Application.Service.Sparks;
using ei_back.Core.Application.UseCase.Game.Dtos;
using ei_back.Core.Application.UseCase.Game.Interfaces;
using ei_back.Core.Domain.DomainExceptions.Game;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
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
        private readonly SparkReservationService _sparkReservations;

        public CreateGameUseCase(
            IMapper mapper,
            IGameRepository gameRepository,
            IUserRepository userRepository,
            IGenAi genAi, 
            IUpsertWorldInfoService upsertWorldInfoService,
            SparkReservationService sparkReservations)
        {
            _mapper = mapper;
            _gameRepository = gameRepository;
            _userRepository = userRepository;
            _genAi = genAi;
            _upsertWorldInfoService = upsertWorldInfoService;
            _sparkReservations = sparkReservations;
        }

        public async Task<GameDtoResponse> Handler(GameDtoRequest gameDtoRequest, Guid userId, CancellationToken cancellationToken)
        {
            var user = await _userRepository.FindByIdAsync(userId, cancellationToken) ??
                throw new NotFoundException($"No user found to user ID {userId}.");

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

            if (gameDtoRequest.UsesSparks)
                return await CreateWithSparksAsync(gameDtoRequest, game, userId, cancellationToken);

            var gameResponse = await GenerateAndStageGameAsync(game, gameDtoRequest, cancellationToken);
            return _mapper.Map<GameDtoResponse>(gameResponse);
        }

        private async Task<GameDtoResponse> CreateWithSparksAsync(
            GameDtoRequest request, Domain.Entity.Game game, Guid userId, CancellationToken cancellationToken)
        {
            if (request.OperationId is not Guid operationId || operationId == Guid.Empty)
                throw SparkGameplayErrors.InvalidRequest();

            var reservation = await _sparkReservations.ReserveAsync(
                userId, operationId, SparkOperationKind.CreateGame, null, cancellationToken);
            if (reservation is { State: SparkReservationState.Replayed, ResultId: Guid createdId })
            {
                var existingGame = await _gameRepository.GetGameByIdAndOwnerId(createdId, userId, cancellationToken)
                    ?? throw new NotFoundException("The previously created game is no longer available.");
                return _mapper.Map<GameDtoResponse>(existingGame);
            }
            if (reservation.State != SparkReservationState.Reserved || reservation.LeaseId is not Guid leaseId)
                throw SparkGameplayErrors.FromReservation(reservation.State);

            var committed = false;
            try
            {
                game.Id = Guid.NewGuid();
                await GenerateAndStageGameAsync(game, request, cancellationToken);
                committed = await _sparkReservations.CompleteAsync(
                    userId, operationId, leaseId, game.Id, _ => Task.CompletedTask, cancellationToken);
                if (!committed)
                    throw SparkGameplayErrors.Conflict();

                return _mapper.Map<GameDtoResponse>(game);
            }
            finally
            {
                if (!committed)
                    await _sparkReservations.TryReleaseAsync(userId, operationId, leaseId);
            }
        }

        private async Task<Domain.Entity.Game> GenerateAndStageGameAsync(
            Domain.Entity.Game game, GameDtoRequest gameDtoRequest, CancellationToken cancellationToken)
        {
            var world = await _upsertWorldInfoService.Handler(game.InfoToString(), gameDtoRequest.GameLanguage, cancellationToken);
            if (gameDtoRequest.UsesSparks && string.IsNullOrWhiteSpace(world))
                throw new BadGatewayException(SparkGameplayErrors.EmptyResponse().Message);

            game.SetWorldInfo(world);

            game.SetCreatedDate(DateTimeOffset.UtcNow);
            return await _gameRepository.CreateAsync(game, cancellationToken);
        }
    }
}
