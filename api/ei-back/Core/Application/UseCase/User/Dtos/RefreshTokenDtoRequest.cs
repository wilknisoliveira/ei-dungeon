using System.ComponentModel.DataAnnotations;

namespace ei_back.Core.Application.UseCase.User.Dtos
{
    public record RefreshTokenDtoRequest
    {
        [Required]
        [StringLength(4096, MinimumLength = 10)]
        public required string AccessToken { get; set; }

        [Required]
        [StringLength(256, MinimumLength = 1)]
        public required string RefreshToken { get; set; }
    }
}
