using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Domain.Entity
{
    public class GameInfo : Base
    {
        public InfoType Type { get; private set; }
        public string Value { get; private set; }

        public GameInfo(InfoType type, string value)
        {
            Type = type;
            Value = value;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
