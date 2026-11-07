using ei_back.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ei_back.Infrastructure.Context.Map
{
    public class RefreshTokenMap : BaseMap<RefreshToken>
    {
        public RefreshTokenMap() : base("refresh_tokens") { }

        public override void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            base.Configure(builder);

            builder.HasIndex(x => x.TokenHash).IsUnique();
            builder.Property(x => x.TokenHash).HasColumnName("token_hash");
            builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        }
    }
}
