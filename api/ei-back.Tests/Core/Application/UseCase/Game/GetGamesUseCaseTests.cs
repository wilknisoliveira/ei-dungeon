using AutoMapper;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.Game;
using ei_back.Core.Application.UseCase.Game.Dtos;
using ei_back.Core.Application.UseCase.Game.Interfaces;
using ei_back.Core.Application.Utils;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using GameEntity = ei_back.Core.Domain.Entity.Game;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Tests.Core.Application.UseCase.Game
{
    public class GetGamesUseCaseTests
    {
        private readonly IGameRepository _gameRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IGetGamesUseCase _useCase;

        public GetGamesUseCaseTests()
        {
            _gameRepository = A.Fake<IGameRepository>();
            _userRepository = A.Fake<IUserRepository>();
            _mapper = A.Fake<IMapper>();
            _useCase = new GetGamesUseCase(_mapper, _gameRepository, _userRepository);
        }

        [Fact]
        public async Task Handler_ReturnsPagedSearchWithMappedGames()
        {
            var userId = Guid.NewGuid();
            var user = new UserEntity("testuser", "Test User", "test@test.com", "hash", ei_back.Core.Domain.Enums.UserRole.CommonUser);
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty(nameof(ei_back.Core.Domain.Entity.Base.Id))!.SetValue(user, userId);

            var games = new List<GameEntity>
            {
                new(userId, "Game 1"),
                new(userId, "Game 2")
            };

            var dtos = games.Select(g => new GameDtoResponse
            {
                Id = Guid.NewGuid(),
                Name = g.Name,
                OwnerUserId = userId,
                GameStatus = ei_back.Core.Domain.Enums.GameStatus.Active
            }).ToList();

            A.CallTo(() => _userRepository.FindByUserName("testuser", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));
            A.CallTo(() => _gameRepository.FindWithPagedSearchAsync("asc", 10, 0, userId, CancellationToken.None))
                .Returns(Task.FromResult(games));
            A.CallTo(() => _gameRepository.GetCountAsync(userId, CancellationToken.None))
                .Returns(Task.FromResult(2));
            A.CallTo(() => _mapper.Map<GameDtoResponse>(games[0]))
                .Returns(dtos[0]);
            A.CallTo(() => _mapper.Map<GameDtoResponse>(games[1]))
                .Returns(dtos[1]);

            var result = await _useCase.Handler("asc", 10, 1, "testuser", CancellationToken.None);

            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(10);
            result.TotalResults.Should().Be(2);
            result.SortDirection.Should().Be("asc");
            result.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handler_WhenUserNotFound_ThrowsNotFoundException()
        {
            A.CallTo(() => _userRepository.FindByUserName("unknown", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(null));

            await _useCase.Invoking(x => x.Handler("asc", 10, 1, "unknown", CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }
    }
}
