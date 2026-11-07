using AutoMapper;
using ei_back.Core.Application.Interfaces;
using ei_back.Core.Application.Service.Play;
using ei_back.Core.Application.Service.Play.Interfaces;
using ei_back.Core.Application.UseCase.Play.Dtos;
using ei_back.Core.Application.UseCase.Play.Interfaces;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Core.Application.Repository;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using Tiktoken;
using System.Runtime.CompilerServices;

namespace ei_back.Core.Application.UseCase.Play
{
    public class NewUserPlayUseCase : INewUserPlayUseCase
    {
        private readonly IMapper _mapper;
        private readonly IGameRepository _gameRepository;
        private readonly IPlayRepository _playRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NewUserPlayUseCase> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly int _limitTokens;
        private readonly IGenAi _genAi;
        private readonly IPlayAnalyzerService _playAnalyzerService;
        private readonly IInitialMasterPlayService _initialMasterPlayService;

        public NewUserPlayUseCase(
            IMapper mapper,
            IGameRepository gameRepository,
            IPlayRepository playRepository,
            IUnitOfWork unitOfWork,
            ILogger<NewUserPlayUseCase> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            IGenAi genAi, 
            IPlayAnalyzerService playAnalyzerService,
            IInitialMasterPlayService initialMasterPlayService)
        {
            _mapper = mapper;
            _gameRepository = gameRepository;
            _playRepository = playRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _genAi = genAi;
            _playAnalyzerService = playAnalyzerService;
            _initialMasterPlayService = initialMasterPlayService;

            var errorMessage = "Verify if all the string connections was registered properly in the appsettings.";

            var limitTokens = configuration["PlayOptions:LimitTokens"] ?? "";
            if (!int.TryParse(limitTokens, out _limitTokens))
                throw new InternalServerErrorException(errorMessage);
        }

        public async IAsyncEnumerable<StreamPlayDtoResponse> Handler(
            PlayDtoRequest playDtoRequest, 
            string userName, 
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            List<Domain.Entity.Play> plays = [];
             
            var game = await _gameRepository.GetGameByIdAndOwnerUserName(playDtoRequest.GameId, userName, cancellationToken);
            if (game == null)
            {
                yield return new StreamPlayDtoResponse 
                { 
                    EventType = EventType.Error, 
                    Content = $"No game found with id {playDtoRequest.GameId} to user name {userName}." 
                };

                yield break;
            }

            if (!game.GameStatus.Equals(GameStatus.Active))
            {
                yield return new StreamPlayDtoResponse 
                { 
                    EventType = EventType.Error, 
                    Content = $"Game '{game.Id}' is not active."
                };

                yield break;
            }

            var lastSummary = await _playRepository.GetLastPlayByPlayTypeAndGameId(game.Id, PlayType.Summary, cancellationToken);
            if (lastSummary != null)
            {
                List<Domain.Entity.Play> nextPlays = await _playRepository.GetPlayWhereCreatedAtIsUpperThan(game.Id, lastSummary.CreatedAt, cancellationToken);

                var numberOfPlaysRequiredForContext = 4;
                if (nextPlays.Count < numberOfPlaysRequiredForContext)
                {
                    var missingPlaysNumber = numberOfPlaysRequiredForContext - nextPlays.Count;
                    
                    List<Domain.Entity.Play> previousPlays = (await _playRepository
                        .GetLastNBeforeDate(game.Id, missingPlaysNumber, lastSummary.CreatedAt, cancellationToken))
                        .OrderBy(x => x.CreatedAt).ToList();
                    plays.AddRange(previousPlays);
                }
                plays.Add(lastSummary);
                plays.AddRange(nextPlays);
            }
            else
            {
                List<Domain.Entity.Play> allGamePlays = await _playRepository.GetAllByGameId(game.Id, cancellationToken);
                plays.AddRange(allGamePlays);
            }

            yield return new StreamPlayDtoResponse
            {
                EventType = EventType.Start,
            };
            var changedItems = 0;

            if (plays.Count == 0)
            {
                await foreach (var chunk in _initialMasterPlayService
                    .ExecuteStreamingAsync(game, cancellationToken)
                    .WithCancellation(cancellationToken))
                {
                if (chunk.EventType == AIStreamEventType.Error)
                {
                    _logger.LogError("Initial master play streaming error: {Content}", chunk.Content);
                    yield return new StreamPlayDtoResponse
                    {
                        EventType = EventType.Error,
                        Content = chunk.Content
                    };
                    yield break;
                }
                yield return new StreamPlayDtoResponse
                {
                    EventType = EventType.Chunk,
                    Content = chunk.Content,
                };
            }

            game.SetLastPlayedAt(DateTimeOffset.UtcNow);

                changedItems = await _unitOfWork.CommitAsync(cancellationToken);
                if (changedItems == 0)
                {
                    yield return new StreamPlayDtoResponse
                    {
                        EventType = EventType.Error,
                        Content = "Something went wrong while attempting to create the user play."
                    };
                    yield break;
                }

                yield break;
            }

            var newPlay = new Domain.Entity.Play(game, PlayType.Protagonist, playDtoRequest.Prompt);
            _ = await _playRepository.CreateAsync(newPlay, cancellationToken) ??
                throw new InternalServerErrorException($"Something went wrong while attempting to create the play");
            plays.Add(newPlay);
            
            AnalyzerDtoResponse analyzerDtoResponse = await _playAnalyzerService.Handler(
                plays, 
                game, 
                cancellationToken);
            if (analyzerDtoResponse.Result == AnalyzerResult.PlayerDied)
            {
                game.KillPlayer();
            }
            
            var completedMasterResponse = "";

            await foreach (var chunk in 
                StreamGenerateMasterPlay(plays, game, analyzerDtoResponse, cancellationToken)
                .WithCancellation(cancellationToken))
            {
                if (chunk.EventType == AIStreamEventType.Error)
                {
                    _logger.LogError("Master play generation streaming error: {Content}", chunk.Content);
                    yield return new StreamPlayDtoResponse
                    {
                        EventType = EventType.Error,
                        Content = chunk.Content
                    };
                    yield break;
                } 
                else
                {
                    completedMasterResponse += chunk.Content;
                    yield return new StreamPlayDtoResponse
                    {
                        EventType = EventType.Chunk,
                        Content = chunk.Content,
                    };
                }
            }

            var masterPlay = new Domain.Entity.Play(game, PlayType.GameMaster, completedMasterResponse);

            _ = await _playRepository.CreateAsync(masterPlay, cancellationToken) ??
                throw new InternalServerErrorException($"Something went wrong while attempting to create the master play");
            plays.Add(masterPlay);

            game.SetLastPlayedAt(DateTimeOffset.UtcNow);

            changedItems = await _unitOfWork.CommitAsync(cancellationToken);
            if (changedItems == 0)
            {
                yield return new StreamPlayDtoResponse 
                { 
                    EventType = EventType.Error, 
                    Content = "Something went wrong while attempting to create the user play."
                };

                yield break;
            }

            int numberOfTokens = CountTokensFromPlays(plays);

            if (numberOfTokens > _limitTokens)
            {
                var timeSpan = TimeSpan.FromMinutes(2);
                var cancellationTokenTask = new CancellationTokenSource(timeSpan);

                _ = Task.Run(async () =>
                {
                    using var cancellationTokenService = new CancellationTokenSource(timeSpan);

                    using var summaryScope = _serviceProvider.CreateScope();
                    var newGeneratePlaysSummaryService = summaryScope.ServiceProvider.GetRequiredService<IGeneratePlaysSummaryService>();
                        
                    await newGeneratePlaysSummaryService.Handler(game.Id, plays, cancellationTokenService.Token);
                }, cancellationTokenTask.Token);
            }
        }

        private async IAsyncEnumerable<StreamAIDtoResponse> StreamGenerateMasterPlay(
            List<Domain.Entity.Play> plays, 
            Domain.Entity.Game game,
            AnalyzerDtoResponse analyzerDtoResponse,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var systemPrompt = $"<master-instruction>\n{MasterPlayCommand()}\n</master-instruction>\n" +
                               $"<player-info>\n{game.InfoToString()}\n</player-info>\n" + 
                               $"<world-info>\n{game.WorldInfo}\n</world-info>\n" +
                               $"<language>\n{LanguageInstructionHelper.GetLanguageInstruction(game.GameLanguage)}\n</language>\n";
            
            List<AiPromptRequest> promptList = [];

            foreach (var play in plays)
            {
                switch (play.PlayType)
                {
                    case PlayType.Summary:
                        systemPrompt += $"\n<summary>\n{play.Response}\n</summary>";
                        break;
                    case PlayType.Protagonist:
                        promptList.Add(new AiPromptRequest(AiRole.User, play.Response));
                        break;
                    case PlayType.GameMaster:
                        promptList.Add(new AiPromptRequest(AiRole.Assistant, play.Response));
                        break;
                }
            }

            var maxOutputTokens = 400;
            var analysis = "<analysis>\n";
            switch (analyzerDtoResponse.Result)
            {
                case AnalyzerResult.InvalidPlay:
                    analysis += $"The player's new play is invalid for the following reason: {analyzerDtoResponse.Reason}\n" +
                                $"Deny the player's play, explain the reason and give them valid options.";
                    maxOutputTokens = 100;
                    break;
                case AnalyzerResult.RollDice:
                {
                    var random = new Random();
                    // Dice d20
                    var dicesResult = random.Next(1, 21);
                    var skill = analyzerDtoResponse.Skill ?? Skill.Intelligence;
                    var modifier = game.GetModifier(skill);
                    var result = (dicesResult + modifier) >= dicesResult ? "SUCCESS" : "FAIL";

                    analysis += $"The new play is critical for the following reason: {analyzerDtoResponse.Reason}\n\n" +
                                $"The beginning of your response as master should be similar to this:\n" +
                                $"'Your play is critical because [explain the reason here...]. Therefore it is necessary to roll " +
                                $"a d20 die!\n" +
                                $"For this, a difficulty class of " +
                                $"{analyzerDtoResponse.DifficultyClass ?? 12} will be required and you may use the {skill} skill.\n" +
                                $"Rolling the die... The result was {dicesResult}!\n" +
                                $"For the {skill} skill your modifier is {modifier}.\n" +
                                $"So your roll was - {result}! - '\n\n" +
                                $"In your following narration, consider the dice result to dictate the outcome " +
                                $"of the play.";
                    break;
                }
                case AnalyzerResult.ClarificationNeeded:
                    analysis += $"The player's new play is insufficient for the following reason: " +
                                $"{analyzerDtoResponse.Reason}\n\n" +
                                $"Request more information about the player's action or play in order to clarify their " +
                                $"intentions.";
                    maxOutputTokens = 100;
                    break;
                case AnalyzerResult.PlayerDied:
                    analysis += $"The player's character died for the following reason: {analyzerDtoResponse.Reason}\n\n" +
                                $"End the game with a conclusion that matches the game's mood and the necessary " +
                                $"narrative coherence.\n" +
                                $"At the end, thank the player and invite them to start a new game.";
                    break;
                default:
                    analysis += "Everything is fine with the player's play. You may proceed normally.";
                    break;
            }
            analysis += "\n</analysis>";
            systemPrompt += analysis;

            promptList.Insert(0, new AiPromptRequest(AiRole.System, systemPrompt));

            await foreach (var chunk in _genAi
                .StreamGetResponse(promptList, maxOutputTokens, cancellationToken)
                .WithCancellation(cancellationToken))
            {
                if (chunk.EventType == AIStreamEventType.Error)
                {
                    _logger.LogError("LLM streaming error in StreamGenerateMasterPlay: {Content}", chunk.Content);
                    yield return chunk;
                    yield break;
                }
                yield return chunk;
            }
        }

        private static string MasterPlayCommand()
        {
            return $"You are a tabletop master (Master table) in a Dungeons & Dragons RPG game. " +
                   $"Your role is to drive the story, develop the plot, interpret " +
                   $"NPCs, make the game always engaging and exciting, as well as any other actions " +
                   $"related to being a Table Master.\n" +
                   $"IMPORTANT: As the Table Master, you must NEVER play the role of the Player! " +
                   $"You must also NEVER dictate the Player's actions!;\n\n" +
                   $"You have some important information that is essential for your role as Table Master:\n" +
                   $"- In <world-info> are all the information you as master created about the game world. " +
                   $"Use this information strategically to drive the story" +
                   $"- In <summary> you will find a brief summary of previous matches.\n" +
                   $"- In <analysis> is an analysis you previously made about the user's new play." +
                   $"It contains the result of the play and how you as master should proceed.\n\n" +
                   $"Now, proceed with the next Table Master guidance!";
        }

        private int CountTokensFromPlays(List<Domain.Entity.Play> plays)
        {
            // TikToken doesn't have support to the gemini models. Therefore, is used an OpenAi model to get
            // a similar result
            const string model = "gpt-4";
            Encoder? encoder = null;
            try
            {
                encoder = ModelToEncoder.For(model);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Something went wrong while attempting to create a encoder for {model} model.";
                _logger.LogError(errorMessage + " Error: " + ex);
                throw new InternalServerErrorException(errorMessage);
            }

            var prompts = "";
                
            plays.ForEach(x => prompts += x.Response + " ");

            return encoder.CountTokens(prompts);
        }
    }
}
