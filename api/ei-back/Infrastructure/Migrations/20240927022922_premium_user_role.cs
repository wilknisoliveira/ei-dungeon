using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ei_back.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class premium_user_role : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "created_at", "description", "name", "updated_at" },
                values: new object[,]
                {
                    { new Guid("03c54894-e384-499d-a271-59451471af56"), new DateTime(2024, 3, 30, 23, 43, 3, 919, DateTimeKind.Unspecified).AddTicks(950), "", "PremiumUser", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("03c54894-e384-499d-a271-59451471af56"));
        }
    }
}
