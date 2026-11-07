using System.ComponentModel.DataAnnotations;

namespace ei_back.Core.Application.UseCase.User.Dtos;

public class ConfirmPasswordRecoveryRequestDto : ConfirmEmailVerificationRequestDto
{
    [Required, StringLength(50, MinimumLength = 4)]
    public string NewPassword { get; set; } = string.Empty;
}