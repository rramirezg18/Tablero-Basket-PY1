using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel_AfterRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DateRegister",
                table: "Teams",
                newName: "CreatedUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "Teams",
                newName: "DateRegister");
        }
    }
}
