using System.ComponentModel.DataAnnotations;
using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.UseCase.Game.Dtos
{
    public record GameDtoRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 4)]
        public required string ProtagonistName { get; set; }
        [Required]
        [StringLength(2000, MinimumLength = 4)]
        public required string ProtagonistDescription { get; set; }
        [Required]
        [StringLength(20, MinimumLength = 2)]
        public required string Name { get; set; }
        [Required]
        public CharacterRace ProtagonistRace { get; set; }
        [Required]
        public GameLanguage GameLanguage { get; set; }
        [Required]
        public SkillsDtoRequest Skills { get; set; }
        
    }

    public record SkillsDtoRequest
    {
        [Range(8, 18)]
        public int Strength { get; set; }
        [Range(8, 18)]
        public int Dexterity { get; set; }
        [Range(8, 18)]
        public int Intelligence { get; set; }
        [Range(8, 18)]
        public int Constitution { get; set; }
        [Range(8, 18)]
        public int Charisma { get; set; }
        [Range(8, 18)]
        public int Wisdom { get; set; }
    }
}
