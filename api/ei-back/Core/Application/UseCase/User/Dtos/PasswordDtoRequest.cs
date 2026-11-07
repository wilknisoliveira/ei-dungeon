using System.ComponentModel.DataAnnotations;

namespace ei_back.Core.Application.UseCase.User.Dtos
{
    public record PasswordDtoRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 4)]
        public required string CurrentPassword { get; set; }
        [Required]
        [StringLength(50, MinimumLength = 4)]
        public required string NewPassword { get; set; }
    }
}
