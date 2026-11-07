using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ei_back.Infrastructure.Migrations
{
    public partial class MarkSeededAdminEmailVerified : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The provisioned admin intentionally has a fictitious email address.
            migrationBuilder.Sql("""
                UPDATE users
                SET email_verified_at = TIMESTAMPTZ '2026-11-02 00:00:00+00'
                WHERE id = '7d9ff283-6174-40a6-a317-f32a4a0620d0'
                  AND email_verified_at IS NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE users
                SET email_verified_at = NULL
                WHERE id = '7d9ff283-6174-40a6-a317-f32a4a0620d0'
                  AND email_verified_at = TIMESTAMPTZ '2026-11-02 00:00:00+00';
                """);
        }
    }
}
