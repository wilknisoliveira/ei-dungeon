using ei_back.Core.Application.Interfaces;
using ei_back.Core.Application.Service.Play.Interfaces;
using ei_back.Core.Application.UseCase.Play.Dtos;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using Microsoft.IdentityModel.Tokens;

namespace ei_back.Core.Application.Service.Play;

public class PlayAnalyzerService(ILogger<PlayAnalyzerService> logger, IGenAi genAi) : IPlayAnalyzerService
{
    private readonly ILogger<PlayAnalyzerService> _logger = logger;
    private readonly IGenAi _genAi = genAi;

    public async Task<AnalyzerDtoResponse> Handler(
        List<Domain.Entity.Play> plays, 
        Domain.Entity.Game game, 
        CancellationToken cancellationToken)
    {
        var systemPrompt = $"<master-instruction>\n{GetAssistantPersonality()}\n</master-instruction>\n" +
                           $"<player-info>\n{game.InfoToString()}\n</player-info>\n" + 
                           $"<world-info>\n{game.WorldInfo}\n</world-info>\n" +
                           $"<language>\n{LanguageInstructionHelper.GetLanguageInstruction(game.GameLanguage)}\n</language>\n";
        
        var newPlay = plays.Where(x => x.PlayType.Equals(PlayType.Protagonist))
            .OrderByDescending(x => x.CreatedAt).First();
        
        var playsWithoutTheLastPlay = plays.Where(x => 
            !x.PlayType.Equals(PlayType.Protagonist) && !x.CreatedAt.Equals(newPlay.CreatedAt))
            .OrderByDescending(x => x.CreatedAt);

        var lastPlays = "<last-plays>\n";
        foreach (var play in playsWithoutTheLastPlay)
        {
            lastPlays += play.PlayType switch
            {
                PlayType.Summary => $"# Summary: \n",
                PlayType.GameMaster => $"# Master Table: \n",
                _ => $"# {game.ProtagonistName}(player): \n"
            };

            lastPlays += play.Response + "\n\n";
        }
        lastPlays += "\n</last-plays>\n";

        systemPrompt += lastPlays;

        var userPrompt = $"{GetUserPrompt()}\n<user-play># {game.ProtagonistName}(player):\n{newPlay.Response}</user-play>\n>";

        List<AiPromptRequest> promptList = [
            new(AiRole.System, systemPrompt),
            new(AiRole.User, userPrompt),];
        
        try
        {
            return await _genAi.GetStructureResponse<AnalyzerDtoResponse>(promptList, 50, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError("Something went wrong while attempting to analyse the new play: {ex}", ex);
            throw new BadGatewayException("Something went wrong while attempting to analyse the new play.");
        }
    }

    private string GetAssistantPersonality()
    {
        return
            "You are responsible for analyzing the user's play in a Dungeons & Dragons match. Your response" +
            " must correspond to one of the following options:\n" +
            "- Ok: Valid play, plausible, follows the rules and narrative logic;\n" +
            "- InvalidPlay: Play that tries to break fundamental rules, such as controlling NPCs, directly " +
            "defining consequences, altering the world without permission, assuming unrealistic powers, " +
            "bypassing narrative coherence;\n" +
            "- RollDice: Possible play, but with risk and uncertainty. Requires dice rolling;\n" +
            "- ClarificationNeeded: When the play is ambiguous or incomplete. Ex: \"I try to hide\" <- where? " +
            "how? from whom? | \"I attack him\" <- which weapon? which target? | \"I look for useful things\" <- where? " +
            "how do you search?\n" +
            "- PlayerDied: When the player's play will lead the character to death, either by self-sacrifice " +
            "or due to the circumstances of the story.\n\n" +
            "Besides responding with the result, explain the reason for your analysis. Ex:\n" +
            "{'Result': 'InvalidPlay', 'Reason': 'It is impossible for the character to travel between cities " +
            "in just 5 minutes.'}\n\n" +
            "If the choice is RollDice, the response must also indicate the skill needed for the action and " +
            "the difficulty class value. Ex:\n" +
            "{'result': 'RollDice', 'reason': 'An attack on the enemy under these circumstances is a risky " +
            "play', 'skill': 'Strength', 'difficultyClass': 15}\n\n" +
            "Below is a reference to help choose the skill:" +
            "Physical strength -> Strength\n" +
            "Precision, reflexes, stealth -> Dexterity\n" +
            "Body endurance -> Constitution\n" +
            "Logic, knowledge -> Intelligence\n" +
            "Perception, intuition -> Wisdom\n" +
            "Social interaction -> Charisma\n\n" +
            "Below is a reference to help choose the DC (difficultyClass) value:" +
            "5 -> Very easy -> Trivial tasks, almost impossible to fail\n" +
            "10 -> Easy -> Light challenges, most characters can handle.\n" +
            "12 -> Moderate -> Requires some skill.\n" +
            "15 -> Medium -> Significant challenge, heroes sometimes succeed.\n" +
            "18 -> Hard -> Requires high skill, planning or luck.\n" +
            "20 -> Very hard -> Rare success.\n" +
            "25 -> Extremely hard -> Something extraordinary.\n";
    }

    private string GetUserPrompt()
    {
        return "Analyze the user's play below in <user-play>, considering the world information in <world-info>, the summary of " +
               "previous plays in '# Summary', as well as the recent plays in <last-plays>.";
    }
}
