using System.ComponentModel;

namespace ei_back.Core.Domain.Enums;

public enum PlayType
{
    [Description("Protagonist")] Protagonist,
    [Description("GameMaster")] GameMaster,
    [Description("Summary")] Summary
}
