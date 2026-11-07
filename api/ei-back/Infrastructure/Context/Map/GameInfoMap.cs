using ei_back.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ei_back.Infrastructure.Context.Map
{
    public class GameInfoMap : BaseMap<GameInfo>
    {
        public GameInfoMap() : base("game_infos")
        {
        }

        public override void Configure(EntityTypeBuilder<GameInfo> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Type).HasColumnName("type").IsRequired();
            builder.Property(x => x.Value).HasColumnName("value").IsRequired();
            builder.HasIndex(x => new { x.Type, x.Value }).IsUnique();
        }
    }
}
