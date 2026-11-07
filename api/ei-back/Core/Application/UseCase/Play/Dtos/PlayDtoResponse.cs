using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.UseCase.Play.Dtos
{
    public record PlayDtoResponse
    {
        public Guid Id { get; set; }
        public PlayType PlayType { get; set; }
        public string Response { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
