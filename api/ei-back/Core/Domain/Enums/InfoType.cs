using System.ComponentModel;

namespace ei_back.Core.Domain.Enums
{
    public enum InfoType : short
    {
        [Description("CharacterName")] CharacterName = 0,
        [Description("CharacterRace")] CharacterRace = 1,
        [Description("CharacterClass")] CharacterClass = 2,
        [Description("GameSystem")] GameSystem = 3
    }
}
