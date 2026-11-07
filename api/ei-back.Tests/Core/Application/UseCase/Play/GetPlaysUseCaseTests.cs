using AutoMapper;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.Play;
using ei_back.Core.Application.UseCase.Play.Dtos;
using ei_back.Core.Application.UseCase.Play.Interfaces;
using ei_back.Core.Application.Utils;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using PlayEntity = ei_back.Core.Domain.Entity.Play;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Tests.Core.Application.UseCase.Play
{
    public class GetPlaysUseCaseTests
    {
        private readonly IPlayRepository _playRepository;
        private readonly IGameRepository _gameRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IGetPlaysUseCase _useCase;

        public GetPlaysUseCaseTests()
        {
            _playRepository = A.Fake<IPlayRepository>();
            _gameRepository = A.Fake<IGameRepository>();
            _userRepository = A.Fake<IUserRepository>();
            _mapper = A.Fake<IMapper>();
            _useCase = new GetPlaysUseCase(_mapper, _playRepository, _gameRepository, _userRepository);
        }

        [Fact]
        public async Task Handler_ReturnsPagedSearchWithMappedPlays()
        {
            var userId = Guid.NewGuid();
            var gameId = Guid.NewGuid();
            var user = new UserEntity("testuser", "Test User", "test@test.com", "hash", ei_back.Core.Domain.Enums.UserRole.CommonUser);
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty(nameof(ei_back.Core.Domain.Entity.Base.Id))!.SetValue(user, userId);

            var game = new ei_back.Core.Domain.Entity.Game(gameId, "Test Game");
            var plays = new List<PlayEntity>
            {
                new(game, PlayType.Protagonist, "First play"),
                new(game, PlayType.GameMaster, "Second play")
            };

            var playDtos = plays.Select(p => new PlayDtoResponse
            {
                Id = Guid.NewGuid(),
                PlayType = p.PlayType,
                Response = p.Response,
                CreatedAt = p.CreatedAt
            }).ToList();

            A.CallTo(() => _userRepository.FindByUserName("testuser", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));
            A.CallTo(() => _gameRepository.CheckIfExistGameByUser(gameId, userId, CancellationToken.None))
                .Returns(Task.FromResult(true));
            A.CallTo(() => _playRepository.GetPlaysByGameAndSizeButSummaryPlay(gameId, 10, 0, "asc", CancellationToken.None))
                .Returns(Task.FromResult(plays));
            A.CallTo(() => _playRepository.CountPlaysByGameButSummaryPlay(gameId, CancellationToken.None))
                .Returns(Task.FromResult(2));

            A.CallTo(() => _mapper.Map<PlayDtoResponse>(plays[0]))
                .Returns(playDtos[0]);
            A.CallTo(() => _mapper.Map<PlayDtoResponse>(plays[1]))
                .Returns(playDtos[1]);

            var result = await _useCase.Handler(gameId, "asc", 10, 1, "testuser", CancellationToken.None);

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

            await _useCase.Invoking(x => x.Handler(Guid.NewGuid(), "asc", 10, 1, "unknown", CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handler_WhenGameNotOwnedByUser_ThrowsNotFoundException()
        {
            var userId = Guid.NewGuid();
            var gameId = Guid.NewGuid();
            var user = new UserEntity("testuser", "Test User", "test@test.com", "hash", ei_back.Core.Domain.Enums.UserRole.CommonUser);
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty(nameof(ei_back.Core.Domain.Entity.Base.Id))!.SetValue(user, userId);

            A.CallTo(() => _userRepository.FindByUserName("testuser", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));
            A.CallTo(() => _gameRepository.CheckIfExistGameByUser(gameId, userId, CancellationToken.None))
                .Returns(Task.FromResult(false));

            await _useCase.Invoking(x => x.Handler(gameId, "asc", 10, 1, "testuser", CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }
    }
}
