using System.ComponentModel.DataAnnotations;
namespace ei_back.Core.Application.UseCase.GoogleAuth.Dtos;

public class CreatePasswordDtoRequest : GoogleAuthRequestDto
{
    [Required, StringLength(50, MinimumLength = 8)] public string NewPassword { get; set; } = string.Empty;
}
