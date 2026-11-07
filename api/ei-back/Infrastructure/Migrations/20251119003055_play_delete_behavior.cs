using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ei_back.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class play_delete_behavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_plays_players_player_id",
                table: "plays");

            migrationBuilder.RenameColumn(
                name: "promt",
                table: "plays",
                newName: "prompt");

            migrationBuilder.AddForeignKey(
                name: "FK_plays_players_player_id",
                table: "plays",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_plays_players_player_id",
                table: "plays");

            migrationBuilder.RenameColumn(
                name: "prompt",
                table: "plays",
                newName: "promt");

            migrationBuilder.AddForeignKey(
                name: "FK_plays_players_player_id",
                table: "plays",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id");
        }
    }
}
