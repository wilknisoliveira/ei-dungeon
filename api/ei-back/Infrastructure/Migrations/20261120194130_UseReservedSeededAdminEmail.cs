using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ei_back.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UseReservedSeededAdminEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE users
                SET email = 'admin@ei-dungeon.invalid',
                    normalized_email = 'ADMIN@EI-DUNGEON.INVALID'
                WHERE id = '7d9ff283-6174-40a6-a317-f32a4a0620d0'
                  AND UPPER(BTRIM(email)) = 'ADMIN@GMAIL.COM'
                  AND normalized_email = 'ADMIN@GMAIL.COM';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE users
                SET email = 'admin@gmail.com',
                    normalized_email = 'ADMIN@GMAIL.COM'
                WHERE id = '7d9ff283-6174-40a6-a317-f32a4a0620d0'
                  AND email = 'admin@ei-dungeon.invalid'
                  AND normalized_email = 'ADMIN@EI-DUNGEON.INVALID';
                """);
        }
    }
}
