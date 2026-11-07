using System.ComponentModel;

namespace ei_back.Core.Domain.Enums
{
    public enum GameLanguage : short
    {
        [Description("Portuguese")] Portuguese = 0,
        [Description("English")] English = 1,
        [Description("Spanish")] Spanish = 2
    }
}
