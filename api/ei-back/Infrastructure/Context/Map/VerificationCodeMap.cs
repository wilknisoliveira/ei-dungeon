using ei_back.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ei_back.Infrastructure.Context.Map;

public class VerificationCodeMap() : BaseMap<VerificationCode>("verification_codes")
{
    public override void Configure(EntityTypeBuilder<VerificationCode> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Email).HasColumnName("email").IsRequired();
        builder.Property(x => x.Purpose).HasColumnName("purpose");
        builder.Property(x => x.CodeHash).HasColumnName("code_hash").HasMaxLength(64).IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        builder.Property(x => x.FailedAttempts).HasColumnName("failed_attempts");
        builder.Property(x => x.ConsumedAt).HasColumnName("consumed_at");
        builder.Property(x => x.InvalidatedAt).HasColumnName("invalidated_at");
        builder.HasIndex(x => new { x.UserId, x.Purpose, x.CreatedAt });
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}