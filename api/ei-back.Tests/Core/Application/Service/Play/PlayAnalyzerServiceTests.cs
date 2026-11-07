using ei_back.Core.Application.Interfaces;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Play;
using ei_back.Core.Application.UseCase.Play.Dtos;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using Microsoft.Extensions.Logging;
using PlayEntity = ei_back.Core.Domain.Entity.Play;

namespace ei_back.Tests.Core.Application.Service.Play
{
    public class PlayAnalyzerServiceTests
    {
        private readonly IGenAi _genAi;
        private readonly PlayAnalyzerService _service;

        public PlayAnalyzerServiceTests()
        {
            _genAi = A.Fake<IGenAi>();
            var logger = A.Fake<ILogger<PlayAnalyzerService>>();
            _service = new PlayAnalyzerService(logger, _genAi);
        }

        [Fact]
        public async Task Handler_WhenValidPlay_ReturnsAnalyzerResponse()
        {
            var gameId = Guid.NewGuid();
            var game = CreateGameWithPlayers(gameId);

            var userPlay = new PlayEntity(game, PlayType.Protagonist, "I search the room");
            var masterPlay = new PlayEntity(game, PlayType.GameMaster, "The room is dark");

            var plays = new List<PlayEntity> { masterPlay, userPlay };

            var expectedResponse = new AnalyzerDtoResponse(AnalyzerResult.Ok, "valid play", null, null);

            A.CallTo(() => _genAi.GetStructureResponse<AnalyzerDtoResponse>(
                A<List<AiPromptRequest>>._, A<int>._, A<CancellationToken>._))
                .Returns(Task.FromResult(expectedResponse));

            var result = await _service.Handler(plays, game, CancellationToken.None);

            result.Should().NotBeNull();
            result.Result.Should().Be(AnalyzerResult.Ok);
            result.Reason.Should().Be("valid play");
        }

        [Fact]
        public async Task Handler_WhenGenAiThrows_ThrowsBadGatewayException()
        {
            var gameId = Guid.NewGuid();
            var game = CreateGameWithPlayers(gameId);

            var userPlay = new PlayEntity(game, PlayType.Protagonist, "I attack");
            var plays = new List<PlayEntity> { userPlay };

            A.CallTo(() => _genAi.GetStructureResponse<AnalyzerDtoResponse>(
                A<List<AiPromptRequest>>._, A<int>._, A<CancellationToken>._))
                .Throws(new Exception("AI service error"));

            await _service.Invoking(x => x.Handler(plays, game, CancellationToken.None))
                .Should().ThrowAsync<BadGatewayException>();
        }

        [Fact]
        public async Task Handler_WithLanguageTag_IncludesLanguageInstructionInSystemPrompt()
        {
            var gameId = Guid.NewGuid();
            var game = CreateGameWithPlayers(gameId, GameLanguage.Portuguese);

            var userPlay = new PlayEntity(game, PlayType.Protagonist, "I look around");
            var plays = new List<PlayEntity> { userPlay };

            var expectedResponse = new AnalyzerDtoResponse(AnalyzerResult.Ok, "ok", null, null);
            List<AiPromptRequest>? capturedPrompts = null;

            A.CallTo(() => _genAi.GetStructureResponse<AnalyzerDtoResponse>(
                A<List<AiPromptRequest>>._, A<int>._, A<CancellationToken>._))
                .Invokes(call => capturedPrompts = call.GetArgument<List<AiPromptRequest>>(0))
                .Returns(Task.FromResult(expectedResponse));

            await _service.Handler(plays, game, CancellationToken.None);

            capturedPrompts.Should().NotBeNull();
            var systemPrompt = capturedPrompts!.First(x => x.Role == AiRole.System).Content;
            systemPrompt.Should().Contain("<language>");
            systemPrompt.Should().Contain("português brasileiro");
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
