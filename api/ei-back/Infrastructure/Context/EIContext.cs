using ei_back.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace ei_back.Infrastructure.Context
{
    public class EIContext : DbContext
    {
        public EIContext() { }

        public EIContext(DbContextOptions<EIContext> options) : base(options) { }

        public DbSet<VerificationCode> VerificationCodes { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Play> Plays { get; set; }
        public DbSet<GameInfo> GameInfos { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

            /*
            //User admin seeding
            var adminUser = new UserEntity(
                "admin",
                "admin",
                "admin@gmail.com",
                "24-0B-E5-18-FA-BD-27-24-DD-B6-F0-4E-EB-1D-A5-96-74-48-D7-E8-31-C0-8C-8F-A8-22-80-9F-74-C7-20-A9",
                0)
            {
                Id = Guid.Parse("7d9ff283-6174-40a6-a317-f32a4a0620d0"),
                CreatedAt = DateTimeOffset.Parse("2024-07-14 22:41:06.874402")
            };

            modelBuilder.Entity<UserEntity>().HasData(adminUser);
            */
        }
    }
}
