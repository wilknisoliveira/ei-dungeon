using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

namespace ei_back.Core.Application.UseCase.Play.Dtos
{
    public record PlayDtoRequest
    {
        public Guid? OperationId { get; set; }
        public bool? Opening { get; set; }
        [JsonIgnore]
        public bool UsesSparks { get; set; }

        [Required]
        public required Guid GameId { get; set; }
        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public required string Prompt { get; set; }
    }
}
