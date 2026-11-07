using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.UseCase.Game.Dtos
{
    public record GameDtoResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ProtagonistName { get; set; }
        public Guid OwnerUserId { get; set; }
        public GameLanguage GameLanguage { get; set; }
        public GameStatus GameStatus { get; set; }
        public DateTimeOffset? LastPlayedAt { get; set; }
    }
}
