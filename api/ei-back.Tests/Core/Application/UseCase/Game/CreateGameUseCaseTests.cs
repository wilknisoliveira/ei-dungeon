using AutoMapper;
using ei_back.Core.Application.Interfaces;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Play.Interfaces;
using ei_back.Core.Application.UseCase.Game;
using ei_back.Core.Application.UseCase.Game.Dtos;
using ei_back.Core.Application.UseCase.Game.Interfaces;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Tests.Core.Application.UseCase.Game
{
    public class CreateGameUseCaseTests
    {
        private readonly IGameRepository _gameRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IGenAi _genAi;
        private readonly IUpsertWorldInfoService _upsertWorldInfoService;
        private readonly ICreateGameUseCase _useCase;

        public CreateGameUseCaseTests()
        {
            _gameRepository = A.Fake<IGameRepository>();
            _userRepository = A.Fake<IUserRepository>();
            _mapper = A.Fake<IMapper>();
            _genAi = A.Fake<IGenAi>();
            _upsertWorldInfoService = A.Fake<IUpsertWorldInfoService>();
            _useCase = new CreateGameUseCase(_mapper, _gameRepository, _userRepository, _genAi, _upsertWorldInfoService);
        }

        [Fact]
        public async Task Handler_CreatesGameSuccessfully()
        {
            var userId = Guid.NewGuid();
            var user = new UserEntity("testuser", "Test User", "test@test.com", "hash", UserRole.CommonUser);
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty(nameof(ei_back.Core.Domain.Entity.Base.Id))!.SetValue(user, userId);

            var request = new GameDtoRequest
            {
                ProtagonistName = "Hero",
                ProtagonistDescription = "A brave hero",
                Name = "My Campaign",
                ProtagonistRace = CharacterRace.Human,
                Skills = new SkillsDtoRequest
                {
                    Strength = 13,
                    Dexterity = 13,
                    Intelligence = 13,
                    Constitution = 13,
                    Charisma = 13,
                    Wisdom = 13
                }
            };

            var createdGame = new ei_back.Core.Domain.Entity.Game(user, request.Name);

            var responseDto = new GameDtoResponse
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                OwnerUserId = userId,
                GameStatus = GameStatus.Active
            };

            A.CallTo(() => _userRepository.FindByUserName("testuser", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));
            A.CallTo(() => _upsertWorldInfoService.Handler(A<string>._, A<GameLanguage>._, CancellationToken.None))
                .Returns(Task.FromResult("World info text"));
            A.CallTo(() => _gameRepository.CreateAsync(A<ei_back.Core.Domain.Entity.Game>._, CancellationToken.None))
                .Returns(Task.FromResult(createdGame));
            A.CallTo(() => _mapper.Map<GameDtoResponse>(createdGame))
                .Returns(responseDto);

            var result = await _useCase.Handler(request, "testuser", CancellationToken.None);

            result.Should().NotBeNull();
            result.Name.Should().Be(request.Name);
            result.OwnerUserId.Should().Be(userId);
        }

        [Fact]
        public async Task Handler_WhenUserNotFound_ThrowsNotFoundException()
        {
            var request = new GameDtoRequest
            {
                ProtagonistName = "Hero",
                ProtagonistDescription = "A brave hero",
                Name = "My Campaign",
                ProtagonistRace = CharacterRace.Human,
                Skills = new SkillsDtoRequest()
            };

            A.CallTo(() => _userRepository.FindByUserName("unknown", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(null));

            await _useCase.Invoking(x => x.Handler(request, "unknown", CancellationToken.None))
                .Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handler_WhenSkillPointsInvalid_ThrowsBadRequestException()
        {
            var userId = Guid.NewGuid();
            var user = new UserEntity("testuser", "Test User", "test@test.com", "hash", UserRole.CommonUser);
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty(nameof(ei_back.Core.Domain.Entity.Base.Id))!.SetValue(user, userId);

            var request = new GameDtoRequest
            {
                ProtagonistName = "Hero",
                ProtagonistDescription = "A brave hero",
                Name = "My Campaign",
                ProtagonistRace = CharacterRace.Human,
                Skills = new SkillsDtoRequest
                {
                    Strength = 8,
                    Dexterity = 8,
                    Intelligence = 8,
                    Constitution = 8,
                    Charisma = 8,
                    Wisdom = 8
                }
            };

            A.CallTo(() => _userRepository.FindByUserName("testuser", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));

            await _useCase.Invoking(x => x.Handler(request, "testuser", CancellationToken.None))
                .Should().ThrowAsync<BadRequestException>();
        }
    }
}
