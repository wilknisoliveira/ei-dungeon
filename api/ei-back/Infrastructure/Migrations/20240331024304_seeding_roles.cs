using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ei_back.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class seeding_roles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "created_at", "description", "name", "updated_at" },
                values: new object[,]
                {
                    { new Guid("11841ca4-6c2b-4bdf-b14f-265fc7307717"), new DateTime(2024, 3, 30, 23, 43, 3, 919, DateTimeKind.Local).AddTicks(965), "", "CommonUser", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { new Guid("440e090b-1245-4cfe-bb62-b22a676ab441"), new DateTime(2024, 3, 30, 23, 43, 3, 919, DateTimeKind.Local).AddTicks(955), "", "Admin", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("11841ca4-6c2b-4bdf-b14f-265fc7307717"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("440e090b-1245-4cfe-bb62-b22a676ab441"));
        }
    }
}
