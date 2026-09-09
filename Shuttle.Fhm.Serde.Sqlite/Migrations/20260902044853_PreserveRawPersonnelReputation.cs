using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttle.Fhm.Serde.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class PreserveRawPersonnelReputation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Personnel_Reputation",
                table: "Personnel");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Personnel_Reputation",
                table: "Personnel",
                sql: "Reputation BETWEEN 0 AND 65535");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Personnel_Reputation",
                table: "Personnel");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Personnel_Reputation",
                table: "Personnel",
                sql: "Reputation BETWEEN 0 AND 100");
        }
    }
}
