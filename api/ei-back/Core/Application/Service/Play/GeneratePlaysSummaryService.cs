using ei_back.Core.Application.Interfaces;
using ei_back.Core.Application.Service.Play.Interfaces;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Core.Application.Repository;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ei_back.Core.Application.Service.Play
{
    public class GeneratePlaysSummaryService : IGeneratePlaysSummaryService
    {
        private readonly ILogger<GeneratePlaysSummaryService> _logger;
        private readonly IPlayRepository _playRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenAi _genAi;
        private readonly IGameRepository _gameRepository;
        private readonly int _summaryMaxTokens;

        public GeneratePlaysSummaryService(
            ILogger<GeneratePlaysSummaryService> logger,
            IUnitOfWork unitOfWork,
            IPlayRepository playRepository,
            IGenAi genAi, IGameRepository gameRepository,
            IConfiguration configuration)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _playRepository = playRepository;
            _genAi = genAi;
            _gameRepository = gameRepository;

            var errorMessage = "Verify if SummaryMaxTokens was set in the PlayOptions section of appsettings.";
            var summaryMaxTokens = configuration["PlayOptions:SummaryMaxTokens"] ?? "";
            if (!int.TryParse(summaryMaxTokens, out _summaryMaxTokens))
                throw new InternalServerErrorException(errorMessage);
        }

        public async Task Handler(
            Guid gameId, 
            List<Domain.Entity.Play> plays, 
            CancellationToken cancellationToken)
        {
            var game = await _gameRepository.FindByIdAsync(gameId, cancellationToken: cancellationToken);

            // It's necessary instantiate the Play at the beginning to ensure the createdAt date
            var newPlay = new Domain.Entity.Play(game, PlayType.Summary, "");
            
            List<AiPromptRequest> promptList = [];
            var lastSystemPlay = plays.FirstOrDefault(x => x.PlayType.Equals(PlayType.Summary));
            if (lastSystemPlay != null)
                promptList.Add(new AiPromptRequest(AiRole.Assistant, lastSystemPlay.Response));
            
            var systemPrompt = $"<player-info>\n{game.InfoToString()}\n</player-info> \n\n " +
                               $"{GetAssistantPersonality()}\n" +
                               $"<language>\n{LanguageInstructionHelper.GetLanguageInstruction(game.GameLanguage)}\n</language>\n";
            promptList.Add(new AiPromptRequest(AiRole.System, systemPrompt));
            
            var lastPlays = "# Last Plays\n";
            foreach (var play in plays.Where(x => !x.PlayType.Equals(PlayType.Summary)))
            {
                if (play.PlayType.Equals(PlayType.GameMaster))
                    lastPlays += $"## Master Table: \n";
                else
                    lastPlays += $"## {game.ProtagonistName}(player): \n";

                lastPlays += play.Response + "\n\n";
            }

            var userPrompt = $"<plays>\n{lastPlays}\n</plays> \n\n {PromptCommand()}";
            promptList.Add(new AiPromptRequest(AiRole.User, userPrompt));

            var iaResponse = "";
            try
            {
                iaResponse = await _genAi.GetResponse(promptList, _summaryMaxTokens, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("Something went wrong while attempting to generate the summary: {ex}", ex);
                return;
            }

            if (iaResponse.IsNullOrEmpty())
            {
                _logger.LogError("Something went wrong while attempting to generate the summary.");
                return;
            }

            newPlay.SetResponse(iaResponse);

            _ = await _playRepository.CreateAsync(newPlay, cancellationToken) ??
                throw new InternalServerErrorException($"Something went wrong while attempting to create the master play");

            var changedItems = await _unitOfWork.CommitAsync(cancellationToken);
            if (changedItems == 0)
            {
                _logger.LogError("Something went wrong while attempting to save the summary.");
            }
        }

        private static string GetAssistantPersonality()
        {
            return "You are observing a tabletop RPG match and are responsible for creating match summaries.";
        }

        private static string PromptCommand()
        {
            const int minOutputTokens = 500;
            // LLMs better understand characters instead of tokens. So it's why we convert it by inference.
            return $"Create a summary of all the information provided, including the recent plays located " +
                   $"within the <play> tags. The generated summary must have at least {minOutputTokens * 4} characters. " +
                   $"The text to be generated will later be used by a generative AI as a database " +
                   $"for generating new summaries, meaning the language and synthesis used should be directed " +
                   $"toward AI reading. Don't worry about saving tokens, prioritize recording the story " +
                   $"and important details.";
        }
    }
}
