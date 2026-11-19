using ei_back.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace ei_back.Infrastructure.Context.Map;

public class GoogleAuthChallengeMap() : BaseMap<GoogleAuthChallenge>("google_auth_challenges")
{
    public override void Configure(EntityTypeBuilder<GoogleAuthChallenge> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Purpose).HasColumnName("purpose");
        builder.Property(x => x.NonceHash).HasColumnName("nonce_hash").HasMaxLength(64);
        builder.Property(x => x.BrowserSecretHash).HasColumnName("browser_secret_hash").HasMaxLength(64);
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        builder.Property(x => x.ConsumedAt).HasColumnName("consumed_at");
        builder.HasIndex(x => x.ExpiresAt);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
