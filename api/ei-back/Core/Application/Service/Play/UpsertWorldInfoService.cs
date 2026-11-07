using ei_back.Core.Application.Interfaces;
using ei_back.Core.Application.Service.Play.Interfaces;
using ei_back.Core.Application.UseCase.Play.Dtos;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Core.Application.Repository;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using Microsoft.IdentityModel.Tokens;

namespace ei_back.Core.Application.Service.Play;

public class UpsertWorldInfoService(
    ILogger<UpsertWorldInfoService> logger,
    IUnitOfWork unitOfWork,
    IGenAi genAi,
    IGameRepository gameRepository)
    : IUpsertWorldInfoService
{
    private readonly ILogger<UpsertWorldInfoService> _logger = logger;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGenAi _genAi = genAi;
    private readonly IGameRepository _gameRepository = gameRepository;

    public async Task<string> Handler(string playerInfo, GameLanguage language, CancellationToken cancellationToken)
    {
        var systemPrompt = GetMasterPersonality();
        systemPrompt += "\n\n<player>\n" + playerInfo + "\n" + @"<\/player>" + "\n";
        systemPrompt += $"\n{GetResponseDetailsPrompt()}";
        systemPrompt += $"\n<language>\n{LanguageInstructionHelper.GetLanguageInstruction(language)}\n</language>\n";

        List<AiPromptRequest> promptList =
        [
            new(AiRole.System, systemPrompt),
            new(AiRole.User, GetWorldInfoGenerationPrompt())
        ];
            
        var iaResponse = await _genAi.GetResponse<WorldInfoDtoResponse>(promptList, 2000, cancellationToken);
            
        if (iaResponse.IsNullOrEmpty())
            throw new BadGatewayException("No content was returned by the gateway");
            
        return iaResponse;
    }

    public async Task Handler(Guid gameId, List<Domain.Entity.Play> plays, CancellationToken cancellationToken)
    {
        var game = await _gameRepository.FindByIdAsync(gameId, cancellationToken: cancellationToken);
        
        var systemPrompt = $"{GetMasterPersonality()} \n {GetAdditionalUpdatePrompt()}";
        systemPrompt += "\n\n<player>\n" + game.InfoToString() + "\n" + @"<\/player>" + "\n";
        systemPrompt += $"\n<language>\n{LanguageInstructionHelper.GetLanguageInstruction(game.GameLanguage)}\n</language>\n";
        
        var assistantPrompt = $"<world-info>\n{game.WorldInfo}\n</world-info>";
        
        var lastPlays = "# Last Plays\n";
        foreach (var play in plays.Where(x => !x.PlayType.Equals(PlayType.Summary)))
        {
            if (play.PlayType.Equals(PlayType.GameMaster))
                lastPlays += $"## Master Table: \n";
            else
                lastPlays += $"## {game.ProtagonistName}(player): \n";

            lastPlays += play.Response + "\n\n";
        }
        
        var userPrompt = $"<plays>\n{lastPlays}\n</plays> \n\n {GetWorldInfoUpdatePrompt()}";

        List<AiPromptRequest> promptList =
        [
            new(AiRole.System, systemPrompt),
            new(AiRole.Assistant, assistantPrompt),
            new(AiRole.User, userPrompt)
        ];
            
        
        var iaResponse = "";
        try
        {
            iaResponse = await _genAi.GetResponse<WorldInfoDtoResponse>(
                promptList, 
                2000, 
                cancellationToken);

        }
        catch (Exception ex)
        {
            _logger.LogError("Something went wrong while attempting to update the world-info: {ex}", ex);
            return;
        }
        
        if (iaResponse.IsNullOrEmpty())
        {
            _logger.LogError("Something went wrong while attempting to generate the world-info.");
            return;
        }
        
        game.SetWorldInfo(iaResponse);
        
        _ = _gameRepository.Update(game);
        
        var changedItems = await _unitOfWork.CommitAsync(cancellationToken);
        if (changedItems == 0)
        {
            _logger.LogError("Something went wrong while attempting to save the world-info.");
        }
    }

    private static string GetMasterPersonality()
    {
        return "You are a tabletop RPG master in a Dungeons & Dragons campaign. " +
               "You like to prepare campaigns without a script, only with world building, " +
               "using the motto 'Create worlds, not stories'.";
    }

    private static string GetAdditionalUpdatePrompt()
    {
        return "You are responsible for keeping the current state of an RPG world updated. \n" +
               "Analyze <plays> sent by the user and update only what is necessary in <world-info>.\n" +
               "Preserve everything that is correct and current. Do not rewrite the world.\n";
    }

    private static string GetWorldInfoGenerationPrompt()
    {
        return "Create a world for a Dungeons & Dragons campaign. It must be an original fantasy world, " +
               "cohesive and detailed. Avoid obvious clichés.";
    }

    private static string GetWorldInfoUpdatePrompt()
    {
        return "Carefully analyze the information within the <plays> tags and compare it with the content " +
               "existing in <world-info>. \nUpdate only the parts of <world-info> that are outdated " +
               "or that need to reflect new information \nfrom <plays>. \nKeep all other " +
               "<world-info> data unchanged, preserving its coherence and narrative consistency. \nDo not " +
               "rewrite the entire world — only modify what is necessary so that <world-info> remains updated " +
               "and faithful to recent events.\n If <world-info> is empty, create the world completely from scratch.";
    }

    private static string GetResponseDetailsPrompt()
    {
        return "Below is an explanation of how the world should be created. \n" +
               "Where it indicates MIN-x, it means that at least 'x' objects must be generated for the array." +
               "Ex: MIN-3 - must generate at least 3 objects; MIN-5 - must generate at least 5 objects. \n\n" +
               "# CampaignStyle: enum ['Epic', 'Dark', 'Exploration', 'Politic', 'Mystery', 'Horror']\n" +
               "# MagicLevel: enum ['High', 'Medium', 'Low']\n" +
               "# SocietalEntities: MIN-3\n" +
               "## SocietalType: enum ['Faction', 'Alliance', 'Guild', 'Kingdom', 'Priest', 'Organization', 'Peoples']\n" +
               "## Name: name of the society\n" +
               "## Background: History, goals, internal motivations, conflicts, enemies and impact on the world.\n" +
               "# NPCs: MIN-5\n" +
               "## Name: NPC name\n" +
               "## Background: Brief history, social role, origin and remarkable life events.\n" +
               "## Race: NPC race (human, elf, dwarf, tiefling etc.).\n" +
               "## Profession: Current occupation or role in the world (mage, merchant, spy, king, researcher etc.).\n" +
               "## Personality: Notable personality traits: behavior, vices, virtues, fears and goals.\n" +
               "# Events: MIN-5 List of relevant events that occurred in the world recently or in the past that " +
               "influence the narrative. Can be wars, disasters, discoveries, murders, magical apparitions, prophecies etc.\n" +
               "# Consequences: MIN-5 Predict outcomes for possible player actions\n" +
               "## Action: Action that may happen in the future\n" +
               "## Consequence: Consequence if the player performs the action.\n" +
               "# Locations: MIN-5 Significant world locations, can be cities, ruins, territories, establishments etc...\n" +
               "## Name: location name\n" +
               "## Background: General description, location history, reputation and importance.\n" +
               "## MainPoints: MIN-3 Points of interest within the location (landmarks, important areas, structures, dangers).\n" +
               "## CurrentEvents: MIN-3 What is happening at the location at the moment: conflicts, problems, rumors, crises, opportunities.\n" +
               "# Treasures: MIN-3 Treasures, relics or important and unique artifacts within the world.\n" +
               "## Name: Treasure name.\n" +
               "## Background: Origin, legend or history behind the item.\n" +
               "## Location: Where the treasure can currently be found.\n" +
               "## Properties: Powers, effects, utilities or curses associated with the item.\n";
    }
}