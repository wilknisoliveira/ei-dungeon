using ei_back.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ei_back.Infrastructure.Context.Map
{
    public class PlayMap : BaseMap<Play>
    {
        public PlayMap() : base("plays") { }

        public override void Configure(EntityTypeBuilder<Play> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.GameId).HasColumnName("game_id").IsRequired();
            builder.Property(x => x.PlayType)
                .HasColumnName("play_type")
                .HasConversion<string>();
            builder.Property(x => x.Response).HasColumnName("response").IsRequired();

            builder.HasOne(x=> x.Game)
                .WithMany(x => x.Plays)
                .HasForeignKey(x => x.GameId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
