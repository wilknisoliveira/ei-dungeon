using ei_back.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace ei_back.Infrastructure.Context.Map;

public class ExternalIdentityMap() : BaseMap<ExternalIdentity>("external_identities")
{
    public override void Configure(EntityTypeBuilder<ExternalIdentity> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(32);
        builder.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(255);
        builder.HasIndex(x => new { x.Provider, x.Subject }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Provider }).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
