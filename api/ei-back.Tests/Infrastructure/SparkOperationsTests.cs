using AutoMapper;
using ei_back.Core.Application.Interfaces;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Play.Interfaces;
using ei_back.Core.Application.Service.Sparks;
using ei_back.Core.Application.UseCase.Game;
using ei_back.Core.Application.UseCase.Game.Dtos;
using ei_back.Core.Application.UseCase.Play;
using ei_back.Core.Application.UseCase.Play.Dtos;
using ei_back.Core.Application.UseCase.Sparks;
using ei_back.Core.Domain.DomainExceptions;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ei_back.Tests.Infrastructure;

public class SparkOperationsTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly IGameRepository _games = A.Fake<IGameRepository>();
    private readonly IUpsertWorldInfoService _world = A.Fake<IUpsertWorldInfoService>();
    private readonly SparkGrant _grant;
    private readonly SparkReservationService _reservations;
    private readonly CreateGameUseCase _create;

    public SparkOperationsTests()
    {
        var mapper = A.Fake<IMapper>();
        var userRepository = A.Fake<IUserRepository>();
        var sparkRepository = A.Fake<ISparkRepository>();
        var unitOfWork = A.Fake<IUnitOfWork>();
        var user = new User("free@example.invalid", "hash", UserRole.CommonUser) { Id = _userId };
        _grant = new SparkGrant(_userId, Guid.NewGuid(), SparkGrantKind.Initial, 1, DateTimeOffset.UtcNow);
        _reservations = new SparkReservationService(sparkRepository, unitOfWork, TimeProvider.System, Options.Create(new SparkOptions()));
        _create = new CreateGameUseCase(mapper, _games, userRepository, A.Fake<IGenAi>(), _world,
            _reservations);

        A.CallTo(() => userRepository.FindByIdAsync(_userId, A<CancellationToken>._)).Returns(user);
        A.CallTo(() => sparkRepository.UserExistsAsync(_userId, A<CancellationToken>._)).Returns(true);
        A.CallTo(() => sparkRepository.UserGrantsAsync(_userId, A<CancellationToken>._)).Returns(new List<SparkGrant> { _grant });
        A.CallTo(() => _world.Handler(A<string>._, A<GameLanguage>._, A<CancellationToken>._)).Returns("A quiet village.");
        A.CallTo(() => mapper.Map<GameDtoResponse>(A<Game>._))
            .ReturnsLazily((object source) => new GameDtoResponse { Id = ((Game)source).Id });

        A.CallTo(() => unitOfWork.ExecuteInTransactionAsync(A<Func<CancellationToken, Task<SparkReservationResult>>>._, A<CancellationToken>._))
            .ReturnsLazily((Func<CancellationToken, Task<SparkReservationResult>> action, CancellationToken ct) => action(ct));
        A.CallTo(() => unitOfWork.ExecuteInTransactionAsync(A<Func<CancellationToken, Task<bool>>>._, A<CancellationToken>._))
            .ReturnsLazily((Func<CancellationToken, Task<bool>> action, CancellationToken ct) => action(ct));
    }

    [Fact]
    public async Task CreateCommittedAdventure_ReplayReturnsSameGameWithoutAnotherDebitOrGeneration()
    {
        Game? saved = null;
        A.CallTo(() => _games.CreateAsync(A<Game>._, A<CancellationToken>._))
            .ReturnsLazily((Game game, CancellationToken _) => Task.FromResult(saved = game));
        A.CallTo(() => _games.GetGameByIdAndOwnerId(A<Guid>._, _userId, A<CancellationToken>._))
            .ReturnsLazily(() => Task.FromResult(saved));
        var request = NewGameRequest();

        var first = await _create.Handler(request, _userId, CancellationToken.None);
        var replay = await _create.Handler(request, _userId, CancellationToken.None);

        first.Id.Should().NotBeEmpty().And.Be(replay.Id);
        _grant.Consumed.Should().Be(1);
        _grant.ReservationId.Should().BeNull();
        A.CallTo(() => _world.Handler(A<string>._, A<GameLanguage>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _games.CreateAsync(A<Game>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task CreateAdventure_WhenGenerationFails_ReleasesSpark()
    {
        A.CallTo(() => _world.Handler(A<string>._, A<GameLanguage>._, A<CancellationToken>._))
            .ThrowsAsync(new BadGatewayException("No response."));

        await _create.Invoking(useCase => useCase.Handler(NewGameRequest(), _userId, CancellationToken.None))
            .Should().ThrowAsync<BadGatewayException>();

        _grant.Consumed.Should().Be(0);
        _grant.ReservationId.Should().BeNull();
        A.CallTo(() => _games.CreateAsync(A<Game>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task CreateAdventure_WhenOperationIdIsMissing_BlocksBeforeAi()
    {
        var request = NewGameRequest() with { OperationId = null };

        var failure = await _create.Invoking(useCase => useCase.Handler(request, _userId, CancellationToken.None))
            .Should().ThrowAsync<SparkOperationException>();

        failure.Which.Code.Should().Be("sparks_invalid_operation");
        A.CallTo(() => _world.Handler(A<string>._, A<GameLanguage>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Theory]
    [InlineData(false, 15)]
    [InlineData(true, 21)]
    public async Task Availability_UsesSavedInitialGrantOrConfiguredDefault(bool hasSavedSettings, int expected)
    {
        var repository = A.Fake<ISparkRepository>();
        var settings = hasSavedSettings ? new SparkSettings(820, 21, 15, 0.0061m, 10, 3) : null;
        A.CallTo(() => repository.GetSettingsAsync(A<CancellationToken>._)).Returns(settings);
        var availability = new GetSparkAvailabilityUseCase(repository, Options.Create(new SparkOptions()));

        var result = await availability.Handler(CancellationToken.None);

        result.InitialGrant.Should().Be(expected);
    }

    [Theory]
    [InlineData("You reach a village.", EventType.Committed, 1)]
    [InlineData(" ", EventType.Error, 0)]
    public async Task OpenScene_ChargesOnlyForACompleteResponse(string narration, EventType terminalEvent, int charges)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["PlayOptions:LimitTokens"] = "10000" }).Build();
        var plays = A.Fake<IPlayRepository>();
        var unitOfWork = A.Fake<IUnitOfWork>();
        var initialPlay = A.Fake<IInitialMasterPlayService>();
        var game = new Game(_userId, "A new adventure") { Id = Guid.NewGuid() };
        A.CallTo(() => _games.CheckIfExistGameByUser(game.Id, _userId, A<CancellationToken>._)).Returns(true);
        A.CallTo(() => _games.GetGameByIdAndOwnerId(game.Id, _userId, A<CancellationToken>._)).Returns(game);
        A.CallTo(() => plays.GetLastPlayByPlayTypeAndGameId(game.Id, PlayType.Summary, A<CancellationToken>._))
            .Returns(Task.FromResult<Play?>(null));
        A.CallTo(() => plays.GetAllByGameId(game.Id, A<CancellationToken>._)).Returns(new List<Play>());
        A.CallTo(() => initialPlay.ExecuteStreamingAsync(game, A<CancellationToken>._)).Returns(GenerateOpening(narration));
        var useCase = new NewUserPlayUseCase(A.Fake<IMapper>(), _games, plays, unitOfWork,
            NullLogger<NewUserPlayUseCase>.Instance, A.Fake<IServiceProvider>(), configuration, A.Fake<IGenAi>(),
            A.Fake<IPlayAnalyzerService>(), initialPlay, _reservations);
        var request = new PlayDtoRequest
        {
            UsesSparks = true, Opening = true, OperationId = Guid.NewGuid(), GameId = game.Id, Prompt = "Open scene"
        };

        var events = new List<EventType>();
        await foreach (var chunk in useCase.Handler(request, _userId, CancellationToken.None))
            events.Add(chunk.EventType);

        events.Should().Equal(EventType.Start, EventType.Chunk, terminalEvent);
        _grant.Consumed.Should().Be(charges);
        _grant.ReservationId.Should().BeNull();
        A.CallTo(() => unitOfWork.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    private static async IAsyncEnumerable<StreamAIDtoResponse> GenerateOpening(string narration)
    {
        await Task.Yield();
        yield return new StreamAIDtoResponse { EventType = AIStreamEventType.Chunk, Content = narration };
    }

    private static GameDtoRequest NewGameRequest() => new()
    {
        UsesSparks = true,
        OperationId = Guid.NewGuid(),
        Name = "A new adventure",
        ProtagonistName = "Hero",
        ProtagonistDescription = "A brave hero",
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
}
