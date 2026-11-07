using ei_back.Core.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ei_back.Core.Application.UseCase.GameInfo.Dtos
{
    public record GameInfoDto
    {
        [Required]
        public required InfoType Type {  get; set; }
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public required string Value { get; set; }
    }
}
