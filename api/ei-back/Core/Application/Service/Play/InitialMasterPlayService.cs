using ei_back.Core.Application.Interfaces;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Play.Interfaces;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using System.Runtime.CompilerServices;

namespace ei_back.Core.Application.Service.Play
{
    public class InitialMasterPlayService : IInitialMasterPlayService
    {
        private readonly ILogger<InitialMasterPlayService> _logger;
        private readonly IGenAi _genAi;
        private readonly IPlayRepository _playRepository;

        public InitialMasterPlayService(
            ILogger<InitialMasterPlayService> logger,
            IGenAi genAi,
            IPlayRepository playRepository)
        {
            _logger = logger;
            _genAi = genAi;
            _playRepository = playRepository;
        }

        public async IAsyncEnumerable<StreamAIDtoResponse> ExecuteStreamingAsync(
            Domain.Entity.Game game,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var initialPrompt = BuildInitialMasterPrompt(game);
            var promptList = new List<AiPromptRequest>
            {
                new(AiRole.System, initialPrompt),
                new(AiRole.User, "Create an introduction for the game as if it were the beginning of the campaign.")
            };

            var completedResponse = "";
            await foreach (var chunk in _genAi
                .StreamGetResponse(promptList, 650, cancellationToken)
                .WithCancellation(cancellationToken))
            {
                if (chunk.EventType == AIStreamEventType.Error)
                {
                    _logger.LogError("LLM streaming error in initial master play: {Content}", chunk.Content);
                    yield return new StreamAIDtoResponse
                    {
                        EventType = AIStreamEventType.Error,
                        Content = chunk.Content
                    };
                    yield break;
                }
                completedResponse += chunk.Content;
                yield return new StreamAIDtoResponse
                {
                    EventType = AIStreamEventType.Chunk,
                    Content = chunk.Content,
                };
            }

            var masterPlay = new Domain.Entity.Play(game, PlayType.GameMaster, completedResponse);
            _ = await _playRepository.CreateAsync(masterPlay, cancellationToken) ??
                throw new InternalServerErrorException("Something went wrong while attempting to create the master play");
        }

        private static string BuildInitialMasterPrompt(Domain.Entity.Game game)
        {
            var playerInfo = game.InfoToString();
            return $"<guidance>\nYou are a tabletop RPG master in a Dungeons & Dragons campaign. " +
                   $"Remember that as the Table Master, you must NOT act as the player or dictate the player's actions. " +
                   $"The campaign player is described within the <player></player> tags.\n</guidance>\n" +
                   $"<player>\n{playerInfo}\n</player>\n" +
                   $"<world-info>\n{game.WorldInfo}\n</world-info>\n" +
                   $"<language>\n{LanguageInstructionHelper.GetLanguageInstruction(game.GameLanguage)}\n</language>\n";
        }
    }
}
