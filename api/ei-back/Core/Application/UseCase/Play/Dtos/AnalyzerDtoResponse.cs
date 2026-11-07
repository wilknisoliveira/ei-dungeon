using System.Text.Json.Serialization;
using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.UseCase.Play.Dtos;

public record AnalyzerDtoResponse(
    [property: JsonPropertyName("result")] AnalyzerResult Result,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("skill")] Skill? Skill,
    [property: JsonPropertyName("difficultyClass")] int? DifficultyClass);

public enum AnalyzerResult
{
    Ok,
    InvalidPlay,
    RollDice,
    ClarificationNeeded,
    PlayerDied
}