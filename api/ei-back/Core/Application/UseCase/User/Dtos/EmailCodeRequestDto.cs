using System.ComponentModel.DataAnnotations;

namespace ei_back.Core.Application.UseCase.User.Dtos;

public class EmailCodeRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}