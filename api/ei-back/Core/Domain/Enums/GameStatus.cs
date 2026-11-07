using System.ComponentModel;

namespace ei_back.Core.Domain.Enums;

public enum GameStatus : short
{
    [Description("Active")] Active = 0,
    [Description("PlayerDied")] PlayerDied = 1,
}