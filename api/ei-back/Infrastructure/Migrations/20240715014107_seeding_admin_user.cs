using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ei_back.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class seeding_admin_user : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "created_at", "email", "full_name", "password", "refresh_token", "refresh_token_expiry_time", "updated_at", "user_name" },
                values: new object[] { new Guid("7d9ff283-6174-40a6-a317-f32a4a0620d0"), new DateTime(2024, 7, 14, 22, 41, 6, 874, DateTimeKind.Local).AddTicks(4028), "admin@gmail.com", "admin", "24-0B-E5-18-FA-BD-27-24-DD-B6-F0-4E-EB-1D-A5-96-74-48-D7-E8-31-C0-8C-8F-A8-22-80-9F-74-C7-20-A9", null, null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("7d9ff283-6174-40a6-a317-f32a4a0620d0"));
        }
    }
}
