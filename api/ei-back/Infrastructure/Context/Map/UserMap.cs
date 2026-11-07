using ei_back.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ei_back.Infrastructure.Context.Map
{
    public class UserMap : BaseMap<User>
    {
        public UserMap() : base("users") { }

        public override void Configure(EntityTypeBuilder<User> builder)
        {
            base.Configure(builder);

            builder.HasIndex(x => x.UserName).IsUnique();
            builder.Property(x => x.UserName).HasColumnName("user_name");
            builder.Property(x => x.FullName).HasColumnName("full_name");
            builder.Property(x => x.Password).HasColumnName("password");
            builder.Property(x => x.Role).HasColumnName("role");
            builder.Property(x => x.Email).HasColumnName("email");
            builder.Property(x => x.NormalizedEmail).HasColumnName("normalized_email").IsRequired();
            builder.HasIndex(x => x.NormalizedEmail).IsUnique();
            builder.Property(x => x.EmailVerifiedAt).HasColumnName("email_verified_at");
        }
    }
}
