using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Quarters");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "ScoreEvents",
                newName: "DateRegister");

            migrationBuilder.RenameIndex(
                name: "IX_ScoreEvents_CreatedUtc",
                table: "ScoreEvents",
                newName: "IX_ScoreEvents_DateRegister");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "Players",
                newName: "DateRegister");

            migrationBuilder.RenameColumn(
                name: "StartTimeUtc",
                table: "Matches",
                newName: "StartMatch");

            migrationBuilder.RenameColumn(
                name: "QuarterEndsAtUtc",
                table: "Matches",
                newName: "PeriodEnd");

            migrationBuilder.RenameColumn(
                name: "CurrentQuarter",
                table: "Matches",
                newName: "Period");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "Matches",
                newName: "DateMatch");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "Fouls",
                newName: "DateRegister");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DateRegister",
                table: "ScoreEvents",
                newName: "CreatedUtc");

            migrationBuilder.RenameIndex(
                name: "IX_ScoreEvents_DateRegister",
                table: "ScoreEvents",
                newName: "IX_ScoreEvents_CreatedUtc");

            migrationBuilder.RenameColumn(
                name: "DateRegister",
                table: "Players",
                newName: "CreatedUtc");

            migrationBuilder.RenameColumn(
                name: "StartMatch",
                table: "Matches",
                newName: "StartTimeUtc");

            migrationBuilder.RenameColumn(
                name: "PeriodEnd",
                table: "Matches",
                newName: "QuarterEndsAtUtc");

            migrationBuilder.RenameColumn(
                name: "Period",
                table: "Matches",
                newName: "CurrentQuarter");

            migrationBuilder.RenameColumn(
                name: "DateMatch",
                table: "Matches",
                newName: "CreatedUtc");

            migrationBuilder.RenameColumn(
                name: "DateRegister",
                table: "Fouls",
                newName: "CreatedUtc");

            migrationBuilder.CreateTable(
                name: "Quarters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    AwayScoreAtEnd = table.Column<int>(type: "int", nullable: true),
                    EndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HomeScoreAtEnd = table.Column<int>(type: "int", nullable: true),
                    Number = table.Column<byte>(type: "tinyint", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quarters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quarters_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quarters_MatchId_Number",
                table: "Quarters",
                columns: new[] { "MatchId", "Number" },
                unique: true);
        }
    }
}
