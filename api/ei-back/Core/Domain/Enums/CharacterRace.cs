using System.ComponentModel;

namespace ei_back.Core.Domain.Enums;

public enum CharacterRace
{
    [Description("Human")] Human,
    [Description("Elf")] Elf,
    [Description("Dwarf")] Dwarf,
    [Description("HalfElf")] HalfElf,
    [Description("Halfling")] Halfling,
    [Description("Tiefling")] Tiefling,
    [Description("Dragonborn")] Dragonborn,
    [Description("HalfOrc")] HalfOrc,
    [Description("Gnome")] Gnome
}