using System.ComponentModel;

namespace ei_back.Core.Application.UseCase.Play.Dtos;

public record StreamPlayDtoResponse
{
    public EventType EventType { get; set; } = EventType.Start;
    public string Content { get; set; } = string.Empty;
}

public enum EventType : short
{
    [Description("start")] Start = 0,
    [Description("chunk")] Chunk = 1,
    [Description("end")] End = 2,
    [Description("error")] Error = 3
}