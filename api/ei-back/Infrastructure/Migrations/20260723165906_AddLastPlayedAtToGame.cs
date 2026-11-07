using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ei_back.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLastPlayedAtToGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_played_at",
                table: "games",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_played_at",
                table: "games");
        }
    }
}
