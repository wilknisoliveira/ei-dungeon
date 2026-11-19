using System.ComponentModel.DataAnnotations;

namespace ei_back.Core.Application.UseCase.User.Dtos
{
    public record LoginDtoRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 4)]
        public required string Password { get; set; }
        [Required]
        [EmailAddress]
        public required string Email { get; set; }
    }
}
