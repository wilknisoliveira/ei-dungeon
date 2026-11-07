using ei_back.Core.Application.Interfaces;
using ei_back.Core.Application.Service.Play;
using ei_back.Core.Application.UseCase.Play.Dtos;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Core.Application.Repository;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using Microsoft.Extensions.Logging;
using PlayEntity = ei_back.Core.Domain.Entity.Play;

namespace ei_back.Tests.Core.Application.Service.Play
{
    public class UpsertWorldInfoServiceTests
    {
        private readonly IGenAi _genAi;
        private readonly IGameRepository _gameRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UpsertWorldInfoService _service;

        public UpsertWorldInfoServiceTests()
        {
            _genAi = A.Fake<IGenAi>();
            _gameRepository = A.Fake<IGameRepository>();
            _unitOfWork = A.Fake<IUnitOfWork>();
            var logger = A.Fake<ILogger<UpsertWorldInfoService>>();
            _service = new UpsertWorldInfoService(logger, _unitOfWork, _genAi, _gameRepository);
        }

        [Fact]
        public async Task Handler_WhenGeneratingWorldInfo_ReturnsWorldInfo()
        {
            var worldInfoJson = "{\"CampaignStyle\":\"Epic\",\"MagicLevel\":\"High\"}";
            A.CallTo(() => _genAi.GetResponse<WorldInfoDtoResponse>(
                A<List<AiPromptRequest>>._, A<int>._, A<CancellationToken>._))
                .Returns(Task.FromResult(worldInfoJson));

            var result = await _service.Handler("Hero - Human - Brave", GameLanguage.English, CancellationToken.None);

            result.Should().Be(worldInfoJson);
        }

        [Fact]
        public async Task Handler_WhenGeneratingWorldInfo_AndNoContent_ThrowsBadGatewayException()
        {
            A.CallTo(() => _genAi.GetResponse<WorldInfoDtoResponse>(
                A<List<AiPromptRequest>>._, A<int>._, A<CancellationToken>._))
                .Returns(Task.FromResult<string?>(null));

            await _service.Invoking(x => x.Handler("Hero - Human", GameLanguage.English, CancellationToken.None))
                .Should().ThrowAsync<BadGatewayException>();
        }

        [Fact]
        public async Task Handler_WhenGeneratingWorldInfo_WithPortugueseLanguage_IncludesPortugueseInstruction()
        {
            var worldInfoJson = "{\"CampaignStyle\":\"Dark\"}";
            List<AiPromptRequest>? capturedPrompts = null;

            A.CallTo(() => _genAi.GetResponse<WorldInfoDtoResponse>(
                A<List<AiPromptRequest>>._, A<int>._, A<CancellationToken>._))
                .Invokes(call => capturedPrompts = call.GetArgument<List<AiPromptRequest>>(0))
                .Returns(Task.FromResult(worldInfoJson));

            await _service.Handler("Hero - Human", GameLanguage.Portuguese, CancellationToken.None);

            capturedPrompts.Should().NotBeNull();
            var systemPrompt = capturedPrompts!.First(x => x.Role == AiRole.System).Content;
            systemPrompt.Should().Contain("<language>");
            systemPrompt.Should().Contain("português brasileiro");
        }

        [Fact]
        public async Task Handler_WhenUpdatingWorldInfo_UpdatesGame()
        {
            var gameId = Guid.NewGuid();
            var game = CreateGameWithPlayers(gameId, GameLanguage.English);

            var play = new PlayEntity(game, PlayType.Protagonist, "I explore the forest");
            var plays = new List<PlayEntity> { play };

            var updatedWorldInfo = "{\"CampaignStyle\":\"Exploration\"}";
            A.CallTo(() => _gameRepository.FindByIdAsync(gameId, A<CancellationToken>._))
                .Returns(Task.FromResult(game));
            A.CallTo(() => _genAi.GetResponse<WorldInfoDtoResponse>(
                A<List<AiPromptRequest>>._, A<int>._, A<CancellationToken>._))
                .Returns(Task.FromResult(updatedWorldInfo));
            A.CallTo(() => _unitOfWork.CommitAsync(A<CancellationToken>._))
                .Returns(Task.FromResult(1));

            await _service.Handler(gameId, plays, CancellationToken.None);

            game.WorldInfo.Should().Be(updatedWorldInfo);
            A.CallTo(() => _gameRepository.Update(game)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _unitOfWork.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task Handler_WhenUpdatingWorldInfo_AndAiFails_LogsAndReturns()
        {
            var gameId = Guid.NewGuid();
            var game = CreateGameWithPlayers(gameId);

            var play = new PlayEntity(game, PlayType.Protagonist, "I look around");
            var plays = new List<PlayEntity> { play };

            A.CallTo(() => _gameRepository.FindByIdAsync(gameId, A<CancellationToken>._))
                .Returns(Task.FromResult(game));
            A.CallTo(() => _genAi.GetResponse<WorldInfoDtoResponse>(
                A<List<AiPromptRequest>>._, A<int>._, A<CancellationToken>._))
                .Throws(new Exception("AI error"));

            await _service.Handler(gameId, plays, CancellationToken.None);

            A.CallTo(() => _unitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        [Fact]
        public async Task Handler_WhenUpdatingWorldInfo_AndNoContent_LogsAndReturns()
        {
            var gameId = Guid.NewGuid();
            var game = CreateGameWithPlayers(gameId);

            var play = new PlayEntity(game, PlayType.Protagonist, "I open the door");
            var plays = new List<PlayEntity> { play };

            A.CallTo(() => _gameRepository.FindByIdAsync(gameId, A<CancellationToken>._))
                .Returns(Task.FromResult(game));
            A.CallTo(() => _genAi.GetResponse<WorldInfoDtoResponse>(
                A<List<AiPromptRequest>>._, A<int>._, A<CancellationToken>._))
                .Returns(Task.FromResult<string?>(null));

            await _service.Handler(gameId, plays, CancellationToken.None);

            A.CallTo(() => _unitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
        }

        private static Game CreateGameWithPlayers(Guid gameId, GameLanguage language = GameLanguage.English)
        {
            var game = new Game("Test Game");
            typeof(Base).GetProperty(nameof(Base.Id))!.SetValue(game, gameId);
            game.SetWorldInfo("A fantasy world");
            game.SetGameLanguage(language);
            game.SetProtagonistInfo("Hero", "Brave adventurer", CharacterRace.Human);

            return game;
        }
    }
}
