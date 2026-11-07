using System.ComponentModel.DataAnnotations;

namespace ei_back.Core.Application.UseCase.User.Dtos
{
    public record UserDtoRequest
    {
        [Required]
        [StringLength(20, MinimumLength = 4)]
        public required string UserName { get; set; }
        [Required]
        [StringLength(50, MinimumLength = 4)]
        public required string FullName { get; set; }
        [Required]
        [StringLength(50, MinimumLength = 8)]
        public required string Password { get; set; }
        [Required]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public required string Email { get; set; }
    }
}
