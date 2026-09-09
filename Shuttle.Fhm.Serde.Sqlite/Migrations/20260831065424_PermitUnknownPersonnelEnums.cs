using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttle.Fhm.Serde.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class PermitUnknownPersonnelEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Personnel_GoalieHandlingTendency",
                table: "Personnel");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Personnel_InnovationTendency",
                table: "Personnel");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Personnel_LineMatchingTendency",
                table: "Personnel");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Personnel_LoyaltyTendency",
                table: "Personnel");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Personnel_VeteranPreference",
                table: "Personnel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Personnel_GoalieHandlingTendency",
                table: "Personnel",
                sql: "GoalieHandlingTendency BETWEEN 0 AND 4");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Personnel_InnovationTendency",
                table: "Personnel",
                sql: "InnovationTendency BETWEEN 0 AND 4");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Personnel_LineMatchingTendency",
                table: "Personnel",
                sql: "LineMatchingTendency BETWEEN 0 AND 4");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Personnel_LoyaltyTendency",
                table: "Personnel",
                sql: "LoyaltyTendency BETWEEN 0 AND 4");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Personnel_VeteranPreference",
                table: "Personnel",
                sql: "VeteranPreference BETWEEN 0 AND 4");
        }
    }
}
