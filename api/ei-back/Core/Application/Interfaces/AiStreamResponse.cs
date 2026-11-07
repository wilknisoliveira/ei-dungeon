using System.ComponentModel;

namespace ei_back.Core.Application.Interfaces;

public record StreamAIDtoResponse
{
    public AIStreamEventType EventType { get; set; } = AIStreamEventType.Chunk;
    public string Content { get; set; } = string.Empty;
}

public enum AIStreamEventType : short
{
    [Description("chunk")] Chunk = 1,
    [Description("error")] Error = 2
}