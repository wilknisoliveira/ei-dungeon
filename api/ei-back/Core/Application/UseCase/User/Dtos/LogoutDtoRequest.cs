using System.ComponentModel.DataAnnotations;

namespace ei_back.Core.Application.UseCase.User.Dtos
{
    public record LogoutDtoRequest
    {
        [StringLength(256, MinimumLength = 1)]
        public required string? RefreshToken { get; set; }
    }
}
