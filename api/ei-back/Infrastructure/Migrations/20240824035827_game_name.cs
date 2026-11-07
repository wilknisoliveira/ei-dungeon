using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ei_back.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class game_name : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "games",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name",
                table: "games");
        }
    }
}
