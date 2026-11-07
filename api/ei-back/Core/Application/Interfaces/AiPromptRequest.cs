using System.ComponentModel;

namespace ei_back.Core.Application.Interfaces;
public class AiPromptRequest(AiRole role, string content)
{
    public AiRole Role { get; set; } = role;
    public string Content { get; set; } = content;
}

public enum AiRole : short
{
    [Description("system")] System = 0,
    [Description("assistant")] Assistant = 1,
    [Description("user")] User = 2
}

