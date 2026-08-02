using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttle.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class FixKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Divisions_DivisionId_Season_LeagueId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_DivisionId_Season_LeagueId",
                table: "Teams");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Divisions_DivisionId_Season_LeagueId",
                table: "Divisions");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Divisions_ConferenceId_DivisionId_Season_LeagueId",
                table: "Divisions",
                columns: new[] { "ConferenceId", "DivisionId", "Season", "LeagueId" });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ConferenceId_DivisionId_Season_LeagueId",
                table: "Teams",
                columns: new[] { "ConferenceId", "DivisionId", "Season", "LeagueId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Divisions_ConferenceId_DivisionId_Season_LeagueId",
                table: "Teams",
                columns: new[] { "ConferenceId", "DivisionId", "Season", "LeagueId" },
                principalTable: "Divisions",
                principalColumns: new[] { "ConferenceId", "DivisionId", "Season", "LeagueId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Divisions_ConferenceId_DivisionId_Season_LeagueId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_ConferenceId_DivisionId_Season_LeagueId",
                table: "Teams");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Divisions_ConferenceId_DivisionId_Season_LeagueId",
                table: "Divisions");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Divisions_DivisionId_Season_LeagueId",
                table: "Divisions",
                columns: new[] { "DivisionId", "Season", "LeagueId" });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_DivisionId_Season_LeagueId",
                table: "Teams",
                columns: new[] { "DivisionId", "Season", "LeagueId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Divisions_DivisionId_Season_LeagueId",
                table: "Teams",
                columns: new[] { "DivisionId", "Season", "LeagueId" },
                principalTable: "Divisions",
                principalColumns: new[] { "DivisionId", "Season", "LeagueId" });
        }
    }
}
