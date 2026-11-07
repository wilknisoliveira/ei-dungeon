using System.ComponentModel;

namespace ei_back.Core.Application.UseCase.Play.Dtos;

public record WorldInfoDtoResponse(
    CampaignStyle CampaignStyle,
    MagicLevel MagicLevel,
    List<SocietalEntity> SocietalEntities,
    List<Npc> Npcs,
    List<string> Events,
    List<Consequence> Consequences,
    List<Location> Locations,
    List<Treasures> Treasures);

public record SocietalEntity(
    SocietalType SocietalType,
    string Name,
    string Background);

public record Npc(
    string Name,
    string Background,
    string Race,
    string Profession,
    string Personality);

public record Consequence(
    string Action,
    string Response);

public record Location(
    string Name,
    string Background,
    List<string> MainPoints,
    List<string> CurrentEvents);

public record Treasures(
    string Name,
    string Background,
    string Location,
    string Properties);

public enum CampaignStyle
{
    Epic,
    Dark,
    Exploration,
    Politic,
    Mystery,
    Horror,
}

public enum MagicLevel
{
    High,
    Medium,
    Low,
}

public enum SocietalType
{
    Faction,
    Alliance,
    Guild,
    Kingdom,
    Priest,
    Organization,
    Peoples,
}
