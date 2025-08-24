using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveTimerStateIntoMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimerStates");

            migrationBuilder.AddColumn<int>(
                name: "CurrentQuarter",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRunning",
                table: "Matches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "QuarterEndsAtUtc",
                table: "Matches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemainingSeconds",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentQuarter",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "IsRunning",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "QuarterEndsAtUtc",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "RemainingSeconds",
                table: "Matches");

            migrationBuilder.CreateTable(
                name: "TimerStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    CurrentQuarter = table.Column<int>(type: "int", nullable: false),
                    IsRunning = table.Column<bool>(type: "bit", nullable: false),
                    LastChangedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QuarterEndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemainingSeconds = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimerStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimerStates_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimerStates_MatchId",
                table: "TimerStates",
                column: "MatchId",
                unique: true);
        }
    }
}
