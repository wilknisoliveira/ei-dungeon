using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace ei_back.Tests.Infrastructure;

public class GoogleMigrationTests
{
    [VerificationDatabaseFact]
    public async Task Upgrade_ExistingAccountAndGame_PreserveIdsCredentialsAndOwnership()
    {
        var source = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("VERIFICATION_TEST_CONNECTION"));
        var databaseName = "google_upgrade_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(source.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin))
            await create.ExecuteNonQueryAsync();
        try
        {
            source.Database = databaseName;
            await using var db = new EIContext(new DbContextOptionsBuilder<EIContext>().UseNpgsql(source.ConnectionString).Options);
            await db.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\"");
            await db.GetService<IMigrator>().MigrateAsync("20261102183758_MarkSeededAdminEmailVerified");
            var id = Guid.NewGuid();
            var email = id.ToString("N") + "@example.com";
            var now = DateTimeOffset.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO users (id, user_name, full_name, email, normalized_email, password, role, email_verified_at, created_at, updated_at) VALUES ({id}, {"legacy-user"}, {"Old Name"}, {email}, {email.ToUpperInvariant()}, {"existing-hash"}, {(int)UserRole.PremiumUser}, {now}, {now}, {now})");
            var user = await db.Users.SingleAsync(x => x.Id == id);
            var game = new Game(user, "Existing adventure") { Id = Guid.NewGuid(), CreatedAt = now, UpdatedAt = now };
            db.Games.Add(game);
            await db.SaveChangesAsync();
            await db.Database.MigrateAsync();
            db.ChangeTracker.Clear();
            var migrated = await db.Users.SingleAsync(x => x.Id == id);
            migrated.Email.Should().Be(email);
            migrated.Password.Should().Be("existing-hash");
            migrated.Role.Should().Be(UserRole.PremiumUser);
            migrated.EmailVerifiedAt.Should().NotBeNull();
            (await db.Games.SingleAsync(x => x.Id == game.Id)).OwnerUserId.Should().Be(id);
            var count = await db.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM information_schema.columns WHERE table_name='users' AND column_name IN ('user_name','full_name')").SingleAsync();
            count.Should().Be(0);
        }
        finally
        {
            // The generated database name is confined to this test and contains only a fixed prefix plus hex.
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{databaseName}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
