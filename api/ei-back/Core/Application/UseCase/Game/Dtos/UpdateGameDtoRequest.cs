using System.ComponentModel.DataAnnotations;
using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.UseCase.Game.Dtos
{
    public record UpdateGameDtoRequest
    {
        [StringLength(20, MinimumLength = 2)]
        public string? Name { get; set; }
        public GameLanguage? GameLanguage { get; set; }
    }
}
