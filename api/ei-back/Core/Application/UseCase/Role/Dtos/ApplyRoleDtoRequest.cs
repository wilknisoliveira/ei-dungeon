using ei_back.Core.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ei_back.Core.Application.UseCase.Role.Dtos
{
    public record ApplyRoleDtoRequest
    {
        [Required]
        public required Guid Id { get; set; }
        [Required]
        public required UserRole role { get; set; }
    }
}
