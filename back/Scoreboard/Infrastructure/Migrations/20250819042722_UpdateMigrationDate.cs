using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMigrationDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "Teams",
                newName: "DateRegister");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DateRegister",
                table: "Teams",
                newName: "CreatedUtc");
        }
    }
}
