using ei_back.Core.Domain.Enums;
using ei_back.Core.Domain.DomainExceptions.Game;
using ei_back.Infrastructure.Extensions;

namespace ei_back.Core.Domain.Entity
{
    public class Game : Base
    {
        public Game(Guid ownerUserId, string name)
        {
            OwnerUserId = ownerUserId;
            Name = name;
        }
        
        public Game(User ownerUser, string name)
        {
            SetOwnerUser(ownerUser);
            Name = name;
        }

        public Game(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
        public User OwnerUser { get; private set; }
        public Guid OwnerUserId { get; private set; }
        public string WorldInfo { get; private set; } = "";
        public GameLanguage GameLanguage { get; private set; } = GameLanguage.English;
        public GameStatus GameStatus { get; private set; } = GameStatus.Active;
        public DateTimeOffset? LastPlayedAt { get; private set; }

        public string ProtagonistName { get; private set; } = "";
        public string ProtagonistDescription { get; private set; } = "";
        public CharacterRace ProtagonistRace { get; private set; } = CharacterRace.Human;
        public int ProtagonistStrength { get; private set; } = 8;
        public int ProtagonistDexterity { get; private set; } = 8;
        public int ProtagonistIntelligence { get; private set; } = 8;
        public int ProtagonistConstitution { get; private set; } = 8;
        public int ProtagonistCharisma { get; private set; } = 8;
        public int ProtagonistWisdom { get; private set; } = 8;

        public List<Play> Plays { get; private set; } = [];

        public void SetOwnerUser(User user)
        {
            OwnerUser = user;
            OwnerUserId = user.Id;
        }

        public void SetName(string name)
        {
            Name = name;
        }

        public void SetGameLanguage(GameLanguage language)
        {
            GameLanguage = language;
        }

        public void SetWorldInfo(string worldInfo)
        {
            WorldInfo = worldInfo;
        }

        public void SetLastPlayedAt(DateTimeOffset lastPlayedAt)
        {
            LastPlayedAt = lastPlayedAt;
        }

        public void SetProtagonistInfo(
            string name,
            string description,
            CharacterRace race)
        {
            ProtagonistName = name;
            ProtagonistDescription = description;
            ProtagonistRace = race;
        }

        public void SetSkillPoints(
            int strength, 
            int dexterity, 
            int intelligence, 
            int constitution, 
            int charisma, 
            int wisdom)
        {
            var numberOfSkills = 6;
            var skillBase = 8;
            var maxOfAllocatedPoints = 30;
            var sumOfSkills = strength + dexterity + intelligence + constitution + charisma + wisdom;
            
            if (sumOfSkills != (maxOfAllocatedPoints + numberOfSkills * skillBase))
            {
                throw new AttributePointsNotValidException();
            }

            ProtagonistStrength = strength;
            ProtagonistDexterity = dexterity;
            ProtagonistIntelligence = intelligence;
            ProtagonistConstitution = constitution;
            ProtagonistCharisma = charisma;
            ProtagonistWisdom = wisdom;

            switch (ProtagonistRace)
            {
                case CharacterRace.Human:
                    ProtagonistStrength += 1;
                    ProtagonistDexterity += 1;
                    ProtagonistIntelligence += 1;
                    ProtagonistConstitution += 1;
                    ProtagonistCharisma += 1;
                    ProtagonistWisdom += 1;
                    break;
                case CharacterRace.Elf:
                    ProtagonistDexterity += 2;
                    break;
                case CharacterRace.Dwarf:
                    ProtagonistConstitution += 2;
                    break;
                case CharacterRace.HalfElf:
                    ProtagonistCharisma += 2;
                    ProtagonistWisdom += 1;
                    ProtagonistIntelligence += 1;
                    break;
                case CharacterRace.Halfling:
                    ProtagonistDexterity += 2;
                    break;
                case CharacterRace.Tiefling:
                    ProtagonistCharisma += 2;
                    ProtagonistIntelligence += 1;
                    break;
                case CharacterRace.Dragonborn:
                    ProtagonistStrength += 2;
                    ProtagonistCharisma += 1;
                    break;
                case CharacterRace.HalfOrc:
                    ProtagonistStrength += 2;
                    ProtagonistConstitution += 1;
                    break;
                case CharacterRace.Gnome:
                    ProtagonistIntelligence += 2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ProtagonistRace), ProtagonistRace, null);
            }
        }

        public int GetModifier(Skill skill)
        {
            var skillValue = skill switch
            {
                Skill.Strength => ProtagonistStrength,
                Skill.Dexterity => ProtagonistDexterity,
                Skill.Intelligence => ProtagonistIntelligence,
                Skill.Constitution => ProtagonistConstitution,
                Skill.Charisma => ProtagonistCharisma,
                Skill.Wisdom => ProtagonistWisdom,
                _ => throw new ArgumentOutOfRangeException(nameof(skill), skill, null)
            };
            
            return (int)((skillValue - 10) / 2.0);
        }

        public string InfoToString()
        {
            return $"PlayerName: {ProtagonistName} \n" +
                   $"PlayerDescription: {ProtagonistDescription}\n" +
                   $"Race: {ProtagonistRace.GetEnumDescription()}\n" +
                   "Skills:\n" +
                   $"- Strength: {ProtagonistStrength}\n" +
                   $"- Dexterity: {ProtagonistDexterity}\n" +
                   $"- Intelligence: {ProtagonistIntelligence}\n" +
                   $"- Constitution: {ProtagonistConstitution}\n" +
                   $"- Charisma: {ProtagonistCharisma}\n" +
                   $"- Wisdom: {ProtagonistWisdom}";
        }

        public void AddPlay(Play play)
        {
            Plays.Add(play);
        }

        public void KillPlayer()
        {
            GameStatus = GameStatus.PlayerDied;
        }
    }
}
