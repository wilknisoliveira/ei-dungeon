using System.ComponentModel.DataAnnotations;
using ei_back.Core.Domain.Enums;
namespace ei_back.Core.Application.UseCase.GoogleAuth.Dtos;

public class GoogleChallengeRequestDto
{
    [EnumDataType(typeof(GoogleAuthPurpose))] public GoogleAuthPurpose Purpose { get; set; }
}
