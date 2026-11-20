using ei_back.Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;

namespace ei_back.Tests.Infrastructure;

public class SeededAdminEmailMigrationTests
{
    [VerificationDatabaseFact]
    public async Task Upgrade_OriginalSeedOnly_PreservesCustomizedAccountsAndOtherFields()
    {
        await using var connection = new NpgsqlConnection(Environment.GetEnvironmentVariable("VERIFICATION_TEST_CONNECTION"));
        await connection.OpenAsync();
        // The temporary table shadows users only on this connection; application data is never modified.
        await using var setup = new NpgsqlCommand("""
            CREATE TEMP TABLE users (
                id uuid PRIMARY KEY, email text, normalized_email text, password text,
                role integer, email_verified_at timestamptz
            );
            INSERT INTO users VALUES
                ('7d9ff283-6174-40a6-a317-f32a4a0620d0', 'admin@gmail.com', 'ADMIN@GMAIL.COM',
                 'existing-hash', 0, TIMESTAMPTZ '2026-11-02 00:00:00+00'),
                ('00000000-0000-0000-0000-000000000001', 'admin@gmail.com', 'ADMIN@GMAIL.COM',
                 'other-hash', 1, NULL);
            """, connection);
        await setup.ExecuteNonQueryAsync();
        var migration = new UseReservedSeededAdminEmail();

        async Task ApplyAsync(IEnumerable<MigrationOperation> operations)
        {
            foreach (var operation in operations.Cast<SqlOperation>())
            {
                await using var command = new NpgsqlCommand(operation.Sql, connection);
                await command.ExecuteNonQueryAsync();
            }
        }

        await ApplyAsync(migration.UpOperations);
        await using (var query = new NpgsqlCommand("SELECT email, normalized_email, password, role, email_verified_at FROM users WHERE id = '7d9ff283-6174-40a6-a317-f32a4a0620d0'", connection))
        await using (var reader = await query.ExecuteReaderAsync())
        {
            (await reader.ReadAsync()).Should().BeTrue();
            reader.GetString(0).Should().Be("admin@ei-dungeon.invalid");
            reader.GetString(1).Should().Be("ADMIN@EI-DUNGEON.INVALID");
            reader.GetString(2).Should().Be("existing-hash");
            reader.GetInt32(3).Should().Be(0);
            reader.GetDateTime(4).Should().Be(new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc));
        }
        await using (var other = new NpgsqlCommand("SELECT email FROM users WHERE id = '00000000-0000-0000-0000-000000000001'", connection))
            (await other.ExecuteScalarAsync()).Should().Be("admin@gmail.com");

        await ApplyAsync(migration.DownOperations);
        await using (var original = new NpgsqlCommand("SELECT normalized_email FROM users WHERE id = '7d9ff283-6174-40a6-a317-f32a4a0620d0'", connection))
            (await original.ExecuteScalarAsync()).Should().Be("ADMIN@GMAIL.COM");

        await using (var customize = new NpgsqlCommand("UPDATE users SET email = 'owner@example.com', normalized_email = 'OWNER@EXAMPLE.COM' WHERE id = '7d9ff283-6174-40a6-a317-f32a4a0620d0'", connection))
            await customize.ExecuteNonQueryAsync();
        await ApplyAsync(migration.UpOperations);
        await ApplyAsync(migration.DownOperations);
        await using (var customized = new NpgsqlCommand("SELECT email FROM users WHERE id = '7d9ff283-6174-40a6-a317-f32a4a0620d0'", connection))
            (await customized.ExecuteScalarAsync()).Should().Be("owner@example.com");
    }
}
