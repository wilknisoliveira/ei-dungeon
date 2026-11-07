using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ei_back.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class game_info_index_type_value : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_game_infos_value",
                table: "game_infos");

            migrationBuilder.CreateIndex(
                name: "IX_game_infos_type_value",
                table: "game_infos",
                columns: new[] { "type", "value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_game_infos_type_value",
                table: "game_infos");

            migrationBuilder.CreateIndex(
                name: "IX_game_infos_value",
                table: "game_infos",
                column: "value",
                unique: true);
        }
    }
}
