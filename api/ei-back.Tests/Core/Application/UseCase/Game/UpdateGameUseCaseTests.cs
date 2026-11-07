using AutoMapper;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.Game;
using ei_back.Core.Application.UseCase.Game.Dtos;
using ei_back.Core.Application.UseCase.Game.Interfaces;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Tests.Core.Application.UseCase.Game
{
    public class UpdateGameUseCaseTests
    {
        private readonly IGameRepository _gameRepository;
        private readonly IMapper _mapper;
        private readonly IUpdateGameUseCase _useCase;

        public UpdateGameUseCaseTests()
        {
            _gameRepository = A.Fake<IGameRepository>();
            _mapper = A.Fake<IMapper>();
            _useCase = new UpdateGameUseCase(_mapper, _gameRepository);
        }

        [Fact]
        public async Task Handler_WithValidRequest_UpdatesGameSuccessfully()
        {
            var gameId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var user = new UserEntity("testuser", "Test User", "test@test.com", "hash", UserRole.CommonUser);
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty(nameof(ei_back.Core.Domain.Entity.Base.Id))!.SetValue(user, userId);

            var game = new ei_back.Core.Domain.Entity.Game(user, "Old Name");
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty(nameof(ei_back.Core.Domain.Entity.Base.Id))!.SetValue(game, gameId);

            var request = new UpdateGameDtoRequest
            {
                Name = "New Name",
                GameLanguage = GameLanguage.Spanish
            };

            var responseDto = new GameDtoResponse
            {
                Id = gameId,
                Name = "New Name",
                OwnerUserId = userId,
                GameLanguage = GameLanguage.Spanish,
                GameStatus = GameStatus.Active
            };

            A.CallTo(() => _gameRepository.GetGameByIdAndOwnerUserName(gameId, "testuser", CancellationToken.None))
                .Returns(Task.FromResult(game));
            A.CallTo(() => _mapper.Map<GameDtoResponse>(game))
                .Returns(responseDto);

            var result = await _useCase.Handler(gameId, request, "testuser", CancellationToken.None);

            result.Should().NotBeNull();
            result.Name.Should().Be("New Name");
            result.GameLanguage.Should().Be(GameLanguage.Spanish);
        }

        [Fact]
        public async Task Handler_WithPartialRequest_UpdatesOnlyProvidedFields()
        {
            var gameId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var user = new UserEntity("testuser", "Test User", "test@test.com", "hash", UserRole.CommonUser);
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty(nameof(ei_back.Core.Domain.Entity.Base.Id))!.SetValue(user, userId);

            var game = new ei_back.Core.Domain.Entity.Game(user, "Original Name");
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty(nameof(ei_back.Core.Domain.Entity.Base.Id))!.SetValue(game, gameId);

            var request = new UpdateGameDtoRequest
            {
                Name = null,
                GameLanguage = GameLanguage.English
            };

            var responseDto = new GameDtoResponse
            {
                Id = gameId,
                Name = "Original Name",
                OwnerUserId = userId,
                GameLanguage = GameLanguage.English,
                GameStatus = GameStatus.Active
            };

            A.CallTo(() => _gameRepository.GetGameByIdAndOwnerUserName(gameId, "testuser", CancellationToken.None))
                .Returns(Task.FromResult(game));
            A.CallTo(() => _mapper.Map<GameDtoResponse>(game))
                .Returns(responseDto);

            var result = await _useCase.Handler(gameId, request, "testuser", CancellationToken.None);

            result.Should().NotBeNull();
            result.GameLanguage.Should().Be(GameLanguage.English);
        }

        [Fact]
        public async Task Handler_WithNullFields_NoChangesApplied()
        {
            var gameId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var user = new UserEntity("testuser", "Test User", "test@test.com", "hash", UserRole.CommonUser);
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty(nameof(ei_back.Core.Domain.Entity.Base.Id))!.SetValue(user, userId);

            var game = new ei_back.Core.Domain.Entity.Game(user, "Existing Name");
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty(nameof(ei_back.Core.Domain.Entity.Base.Id))!.SetValue(game, gameId);

            var request = new UpdateGameDtoRequest
            {
                Name = null,
                GameLanguage = null
            };

            var responseDto = new GameDtoResponse
            {
                Id = gameId,
                Name = "Existing Name",
                OwnerUserId = userId,
                GameStatus = GameStatus.Active
            };

            A.CallTo(() => _gameRepository.GetGameByIdAndOwnerUserName(gameId, "testuser", CancellationToken.None))
                .Returns(Task.FromResult(game));
            A.CallTo(() => _mapper.Map<GameDtoResponse>(game))
                .Returns(responseDto);

            var result = await _useCase.Handler(gameId, request, "testuser", CancellationToken.None);

            result.Should().NotBeNull();
            result.Name.Should().Be("Existing Name");
        }

        [Fact]
        public async Task Handler_WhenGameNotFound_ThrowsNotFoundException()
        {
            var gameId = Guid.NewGuid();
            var request = new UpdateGameDtoRequest { Name = "New Name" };

            A.CallTo(() => _gameRepository.GetGameByIdAndOwnerUserName(gameId, "unknown", CancellationToken.None))
                .Returns(Task.FromResult<ei_back.Core.Domain.Entity.Game?>(null));

            await _useCase.Invoking(x => x.Handler(gameId, request, "unknown", CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }
    }
}
