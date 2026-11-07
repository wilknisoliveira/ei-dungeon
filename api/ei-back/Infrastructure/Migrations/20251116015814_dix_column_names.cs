using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ei_back.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class dix_column_names : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WorldInfo",
                table: "games",
                newName: "world_info");

            migrationBuilder.RenameColumn(
                name: "GameStatus",
                table: "games",
                newName: "game_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "world_info",
                table: "games",
                newName: "WorldInfo");

            migrationBuilder.RenameColumn(
                name: "game_status",
                table: "games",
                newName: "GameStatus");
        }
    }
}
