using System.ComponentModel.DataAnnotations;

namespace ei_back.Core.Application.UseCase.Role.Dtos
{
    public class RoleDto
    {
        [Required]
        [StringLength(10, MinimumLength = 2)]
        public string Name { get; set; }
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string? Description { get; set; }
    }
}
