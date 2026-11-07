using AutoMapper;
using ei_back.Core.Application.Interfaces;
using ei_back.Core.Application.Service.Play.Interfaces;
using ei_back.Core.Application.UseCase.Play;
using ei_back.Core.Application.UseCase.Play.Dtos;
using ei_back.Core.Application.UseCase.Play.Interfaces;
using ei_back.Core.Domain.Enums;
using ei_back.Core.Application.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PlayEntity = ei_back.Core.Domain.Entity.Play;

namespace ei_back.Tests.Core.Application.UseCase.Play
{
    public class NewUserPlayUseCaseTests
    {
        private readonly IGameRepository _gameRepository;
        private readonly IPlayRepository _playRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenAi _genAi;
        private readonly IPlayAnalyzerService _playAnalyzerService;
        private readonly IInitialMasterPlayService _initialMasterPlayService;
        private readonly INewUserPlayUseCase _useCase;

        public NewUserPlayUseCaseTests()
        {
            _gameRepository = A.Fake<IGameRepository>();
            _playRepository = A.Fake<IPlayRepository>();
            _unitOfWork = A.Fake<IUnitOfWork>();
            _genAi = A.Fake<IGenAi>();
            _playAnalyzerService = A.Fake<IPlayAnalyzerService>();
            _initialMasterPlayService = A.Fake<IInitialMasterPlayService>();
            var mapper = A.Fake<IMapper>();
            var logger = A.Fake<ILogger<NewUserPlayUseCase>>();
            var serviceProvider = A.Fake<IServiceProvider>();
            var configuration = A.Fake<IConfiguration>();

            A.CallTo(() => configuration["PlayOptions:LimitTokens"]).Returns("10000");

            _useCase = new NewUserPlayUseCase(
                mapper,
                _gameRepository,
                _playRepository,
                _unitOfWork,
                logger,
                serviceProvider,
                configuration,
                _genAi,
                _playAnalyzerService,
                _initialMasterPlayService);
        }

        [Fact]
        public async Task FirstPlay_WhenGameHasNoPlays_ShouldStreamInitialMasterPlay()
        {
            var gameId = Guid.NewGuid();
            var request = new PlayDtoRequest { GameId = gameId, Prompt = "I want to start playing" };
            var userName = "testuser";
            var cancellationToken = CancellationToken.None;

            var game = CreateGameWithPlayers(gameId);

            A.CallTo(() => _gameRepository.GetGameByIdAndOwnerUserName(gameId, userName, cancellationToken))
                .Returns(Task.FromResult(game));
            A.CallTo(() => _playRepository
                .GetLastPlayByPlayTypeAndGameId(gameId, PlayType.Summary, cancellationToken))
                .Returns(Task.FromResult<PlayEntity?>(null));
            A.CallTo(() => _playRepository.GetAllByGameId(gameId, cancellationToken))
                .Returns(Task.FromResult<List<PlayEntity>>([]));

            var serviceChunks = GenerateServiceChunks("Welcome to the adventure...", false);
            A.CallTo(() => _initialMasterPlayService
                .ExecuteStreamingAsync(game, cancellationToken))
                .Returns(serviceChunks);

            A.CallTo(() => _unitOfWork.CommitAsync(cancellationToken))
                .Returns(Task.FromResult(2));

            var results = new List<StreamPlayDtoResponse>();
            await foreach (var chunk in _useCase.Handler(request, userName, cancellationToken))
            {
                results.Add(chunk);
            }

            results.Should().NotBeEmpty();
            results[0].EventType.Should().Be(EventType.Start);
            results.Any(x => x.EventType == EventType.Chunk).Should().BeTrue();
            results.Any(x => x.EventType == EventType.Error).Should().BeFalse();
        }

        [Fact]
        public async Task FirstPlay_WhenAiFails_ShouldYieldErrorAndNotPersist()
        {
            var gameId = Guid.NewGuid();
            var request = new PlayDtoRequest { GameId = gameId, Prompt = "Start" };
            var userName = "testuser";
            var cancellationToken = CancellationToken.None;

            var game = CreateGameWithPlayers(gameId);

            A.CallTo(() => _gameRepository.GetGameByIdAndOwnerUserName(gameId, userName, cancellationToken))
                .Returns(Task.FromResult(game));
            A.CallTo(() => _playRepository
                .GetLastPlayByPlayTypeAndGameId(gameId, PlayType.Summary, cancellationToken))
                .Returns(Task.FromResult<PlayEntity?>(null));
            A.CallTo(() => _playRepository.GetAllByGameId(gameId, cancellationToken))
                .Returns(Task.FromResult<List<PlayEntity>>([]));

            var serviceChunks = GenerateServiceChunks("AI error", true);
            A.CallTo(() => _initialMasterPlayService
                .ExecuteStreamingAsync(game, cancellationToken))
                .Returns(serviceChunks);

            var results = new List<StreamPlayDtoResponse>();
            await foreach (var chunk in _useCase.Handler(request, userName, cancellationToken))
            {
                results.Add(chunk);
            }

            results.Any(x => x.EventType == EventType.Error).Should().BeTrue();
        }

        [Fact]
        public async Task FirstPlay_WhenCommitFails_ShouldYieldError()
        {
            var gameId = Guid.NewGuid();
            var request = new PlayDtoRequest { GameId = gameId, Prompt = "Start" };
            var userName = "testuser";
            var cancellationToken = CancellationToken.None;

            var game = CreateGameWithPlayers(gameId);

            A.CallTo(() => _gameRepository.GetGameByIdAndOwnerUserName(gameId, userName, cancellationToken))
                .Returns(Task.FromResult(game));
            A.CallTo(() => _playRepository
                .GetLastPlayByPlayTypeAndGameId(gameId, PlayType.Summary, cancellationToken))
                .Returns(Task.FromResult<PlayEntity?>(null));
            A.CallTo(() => _playRepository.GetAllByGameId(gameId, cancellationToken))
                .Returns(Task.FromResult<List<PlayEntity>>([]));

            var serviceChunks = GenerateServiceChunks("Intro text", false);
            A.CallTo(() => _initialMasterPlayService
                .ExecuteStreamingAsync(game, cancellationToken))
                .Returns(serviceChunks);

            A.CallTo(() => _unitOfWork.CommitAsync(cancellationToken))
                .Returns(Task.FromResult(0));

            var results = new List<StreamPlayDtoResponse>();
            await foreach (var chunk in _useCase.Handler(request, userName, cancellationToken))
            {
                results.Add(chunk);
            }

            results.Any(x => x.EventType == EventType.Error).Should().BeTrue();
        }

        [Fact]
        public async Task SubsequentPlay_WhenGameHasPlays_ShouldUseExistingFlow()
        {
            var gameId = Guid.NewGuid();
            var request = new PlayDtoRequest { GameId = gameId, Prompt = "I attack the goblin" };
            var userName = "testuser";
            var cancellationToken = CancellationToken.None;

            var game = CreateGameWithPlayers(gameId);
            var systemSummary = new PlayEntity(game, PlayType.Summary, "summary");

            A.CallTo(() => _gameRepository.GetGameByIdAndOwnerUserName(gameId, userName, cancellationToken))
                .Returns(Task.FromResult(game));
            A.CallTo(() => _playRepository
                .GetLastPlayByPlayTypeAndGameId(gameId, PlayType.Summary, cancellationToken))
                .Returns(Task.FromResult<PlayEntity?>(systemSummary));
            A.CallTo(() => _playRepository.GetPlayWhereCreatedAtIsUpperThan(gameId, systemSummary.CreatedAt, cancellationToken))
                .Returns(Task.FromResult<List<PlayEntity>>([
                    new PlayEntity(game, PlayType.Protagonist, "user action")
                ]));
            A.CallTo(() => _playRepository.GetLastNBeforeDate(gameId, 3, systemSummary.CreatedAt, cancellationToken))
                .Returns(Task.FromResult<List<PlayEntity>>([]));

            A.CallTo(() => _playRepository.CreateAsync(A<PlayEntity>._, cancellationToken))
                .Returns(Task.FromResult(A.Fake<PlayEntity>()));

            A.CallTo(() => _playAnalyzerService.Handler(A<List<PlayEntity>>._, game, cancellationToken))
                .Returns(Task.FromResult(new AnalyzerDtoResponse(AnalyzerResult.Ok, "valid", null, null)));

            var aiChunks = GenerateServiceChunks("The goblin dodges!", false);
            A.CallTo(() => _genAi.StreamGetResponse(A<List<AiPromptRequest>>._, A<int>._, cancellationToken))
                .Returns(aiChunks);

            A.CallTo(() => _unitOfWork.CommitAsync(cancellationToken))
                .Returns(Task.FromResult(2));

            var results = new List<StreamPlayDtoResponse>();
            await foreach (var chunk in _useCase.Handler(request, userName, cancellationToken))
            {
                results.Add(chunk);
            }

            results.Should().NotBeEmpty();
            results[0].EventType.Should().Be(EventType.Start);
            results.Any(x => x.EventType == EventType.Chunk).Should().BeTrue();
            results.Any(x => x.EventType == EventType.Error).Should().BeFalse();
        }

        private static ei_back.Core.Domain.Entity.Game CreateGameWithPlayers(Guid gameId)
        {
            var game = new ei_back.Core.Domain.Entity.Game("Test Game");
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty(nameof(ei_back.Core.Domain.Entity.Base.Id))!
                .SetValue(game, gameId);
            game.SetWorldInfo("A fantasy world");
            game.SetProtagonistInfo("Hero", "Brave adventurer", CharacterRace.Human);

            return game;
        }

        private static async IAsyncEnumerable<StreamAIDtoResponse> GenerateServiceChunks(string content, bool isError)
        {
            if (isError)
            {
                yield return new StreamAIDtoResponse
                {
                    EventType = AIStreamEventType.Error,
                    Content = "AI service error"
                };
                yield break;
            }

            var words = content.Split(' ');
            foreach (var word in words)
            {
                yield return new StreamAIDtoResponse
                {
                    EventType = AIStreamEventType.Chunk,
                    Content = word + " "
                };
                await Task.CompletedTask;
            }
        }
    }
}
