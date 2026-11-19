using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.Game;
using ei_back.Core.Application.UseCase.Game.Dtos;
using ei_back.Core.Application.UseCase.Game.Interfaces;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using GameEntity = ei_back.Core.Domain.Entity.Game;

namespace ei_back.Tests.Core.Application.UseCase.Game
{
    public class GetGameByIdAndUserUseCaseTests
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGetGameByIdAndUserUseCase _useCase;

        public GetGameByIdAndUserUseCaseTests()
        {
            _gameRepository = A.Fake<IGameRepository>();
            _useCase = new GetGameByIdAndUserUseCase(_gameRepository);
        }

        [Fact]
        public async Task Handler_ReturnsGameWhenFound()
        {
            var gameId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var game = new GameEntity(userId, "Test Game");
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty(nameof(ei_back.Core.Domain.Entity.Base.Id))!.SetValue(game, gameId);

            A.CallTo(() => _gameRepository.GetGameByIdAndOwnerId(gameId, Guid.Parse("11111111-1111-1111-1111-111111111111"), CancellationToken.None))
                .Returns(Task.FromResult<GameEntity?>(game));

            var result = await _useCase.Handler(gameId, Guid.Parse("11111111-1111-1111-1111-111111111111"), CancellationToken.None);

            result.Should().NotBeNull();
            result.Id.Should().Be(gameId);
            result.Name.Should().Be("Test Game");
            result.OwnerUserId.Should().Be(userId);
        }

        [Fact]
        public async Task Handler_WhenGameNotFound_ThrowsNotFoundException()
        {
            var gameId = Guid.NewGuid();

            A.CallTo(() => _gameRepository.GetGameByIdAndOwnerId(gameId, Guid.Parse("22222222-2222-2222-2222-222222222222"), CancellationToken.None))
                .Returns(Task.FromResult<GameEntity?>(null));

            await _useCase.Invoking(x => x.Handler(gameId, Guid.Parse("22222222-2222-2222-2222-222222222222"), CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }
    }
}
