using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scoreboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class aaaaaaaaaaa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRunning",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "PeriodEnd",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "RemainingSeconds",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "StartMatch",
                table: "Matches");

            migrationBuilder.CreateIndex(
                name: "IX_TeamWins_TeamId",
                table: "TeamWins",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeamWins_TeamId",
                table: "TeamWins");

            migrationBuilder.AddColumn<bool>(
                name: "IsRunning",
                table: "Matches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PeriodEnd",
                table: "Matches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemainingSeconds",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartMatch",
                table: "Matches",
                type: "datetime2",
                nullable: true);
        }
    }
}
