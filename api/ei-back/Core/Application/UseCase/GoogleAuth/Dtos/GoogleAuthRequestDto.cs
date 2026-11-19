using System.ComponentModel.DataAnnotations;
namespace ei_back.Core.Application.UseCase.GoogleAuth.Dtos;

public class GoogleAuthRequestDto
{
    [Required] public Guid ChallengeId { get; set; }
    [Required, StringLength(16384)] public string Credential { get; set; } = string.Empty;
}
