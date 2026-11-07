using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ei_back.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class delete_player_entity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_plays_players_player_id",
                table: "plays");

            migrationBuilder.DropTable(
                name: "players");

            migrationBuilder.DropIndex(
                name: "IX_plays_player_id",
                table: "plays");

            migrationBuilder.DropColumn(
                name: "player_id",
                table: "plays");

            migrationBuilder.RenameColumn(
                name: "prompt",
                table: "plays",
                newName: "response");

            migrationBuilder.AddColumn<string>(
                name: "play_type",
                table: "plays",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "protagonist_charisma",
                table: "games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "protagonist_constitution",
                table: "games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "protagonist_description",
                table: "games",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "protagonist_dexterity",
                table: "games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "protagonist_intelligence",
                table: "games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "protagonist_name",
                table: "games",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "protagonist_race",
                table: "games",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "protagonist_strength",
                table: "games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "protagonist_wisdom",
                table: "games",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "play_type",
                table: "plays");

            migrationBuilder.DropColumn(
                name: "protagonist_charisma",
                table: "games");

            migrationBuilder.DropColumn(
                name: "protagonist_constitution",
                table: "games");

            migrationBuilder.DropColumn(
                name: "protagonist_description",
                table: "games");

            migrationBuilder.DropColumn(
                name: "protagonist_dexterity",
                table: "games");

            migrationBuilder.DropColumn(
                name: "protagonist_intelligence",
                table: "games");

            migrationBuilder.DropColumn(
                name: "protagonist_name",
                table: "games");

            migrationBuilder.DropColumn(
                name: "protagonist_race",
                table: "games");

            migrationBuilder.DropColumn(
                name: "protagonist_strength",
                table: "games");

            migrationBuilder.DropColumn(
                name: "protagonist_wisdom",
                table: "games");

            migrationBuilder.RenameColumn(
                name: "response",
                table: "plays",
                newName: "prompt");

            migrationBuilder.AddColumn<Guid>(
                name: "player_id",
                table: "plays",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    charisma = table.Column<int>(type: "integer", nullable: false),
                    constitution = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    dexterity = table.Column<int>(type: "integer", nullable: false),
                    intelligence = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    race = table.Column<string>(type: "text", nullable: false),
                    strength = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<short>(type: "smallint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    wisdom = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_players", x => x.id);
                    table.ForeignKey(
                        name: "FK_players_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_plays_player_id",
                table: "plays",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_players_game_id",
                table: "players",
                column: "game_id");

            migrationBuilder.AddForeignKey(
                name: "FK_plays_players_player_id",
                table: "plays",
                column: "player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
