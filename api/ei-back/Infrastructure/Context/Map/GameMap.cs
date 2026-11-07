using ei_back.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ei_back.Infrastructure.Context.Map
{
    public class GameMap : BaseMap<Game>
    {
        public GameMap() : base("games") { }

        public override void Configure(EntityTypeBuilder<Game> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Name).HasColumnName("name").IsRequired();
            builder.Property(x => x.OwnerUserId).HasColumnName("owner_user_id").IsRequired();
            builder.Property(x => x.WorldInfo).HasColumnName("world_info").IsRequired();
            builder.Property(x => x.GameLanguage).HasColumnName("game_language").IsRequired();
            builder.Property(x => x.GameStatus).HasColumnName("game_status").IsRequired();
            builder.Property(x => x.LastPlayedAt)
                .HasColumnName("last_played_at")
                .HasColumnType("timestamp with time zone");

            builder.Property(x => x.ProtagonistName).HasColumnName("protagonist_name").IsRequired();
            builder.Property(x => x.ProtagonistDescription).HasColumnName("protagonist_description").IsRequired();
            builder.Property(x => x.ProtagonistRace)
                .HasColumnName("protagonist_race")
                .HasConversion<string>();
            builder.Property(x => x.ProtagonistStrength).HasColumnName("protagonist_strength");
            builder.Property(x => x.ProtagonistDexterity).HasColumnName("protagonist_dexterity");
            builder.Property(x => x.ProtagonistIntelligence).HasColumnName("protagonist_intelligence");
            builder.Property(x => x.ProtagonistConstitution).HasColumnName("protagonist_constitution");
            builder.Property(x => x.ProtagonistCharisma).HasColumnName("protagonist_charisma");
            builder.Property(x => x.ProtagonistWisdom).HasColumnName("protagonist_wisdom");

            builder.HasOne(x => x.OwnerUser)
                .WithMany(x => x.Games)
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
