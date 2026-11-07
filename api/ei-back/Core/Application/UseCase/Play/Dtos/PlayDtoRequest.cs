using System.ComponentModel.DataAnnotations;

namespace ei_back.Core.Application.UseCase.Play.Dtos
{
    public record PlayDtoRequest
    {
        [Required]
        public required Guid GameId { get; set; }
        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public required string Prompt { get; set; }
    }
}
