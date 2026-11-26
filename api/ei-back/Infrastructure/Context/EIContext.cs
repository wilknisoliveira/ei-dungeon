using ei_back.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace ei_back.Infrastructure.Context
{
    public class EIContext : DbContext
    {
        public EIContext() { }

        public EIContext(DbContextOptions<EIContext> options) : base(options) { }

        public DbSet<VerificationCode> VerificationCodes { get; set; }
        public DbSet<ExternalIdentity> ExternalIdentities { get; set; }
        public DbSet<GoogleAuthChallenge> GoogleAuthChallenges { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Play> Plays { get; set; }
        public DbSet<GameInfo> GameInfos { get; set; }
        public DbSet<SparkCycle> SparkCycles { get; set; }
        public DbSet<SparkGrant> SparkGrants { get; set; }
        public DbSet<SparkQueueRequest> SparkQueueRequests { get; set; }
        public DbSet<SparkSettings> SparkSettings { get; set; }
        public DbSet<SparkDailySnapshot> SparkDailySnapshots { get; set; }
        public DbSet<SparkCycleChange> SparkCycleChanges { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

        }
    }
}
