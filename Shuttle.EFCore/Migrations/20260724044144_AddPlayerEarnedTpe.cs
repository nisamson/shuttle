using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttle.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerEarnedTpe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerEarnedTpe",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Season = table.Column<int>(type: "int", nullable: false),
                    EarnedTpe = table.Column<int>(type: "int", nullable: false),
                    Regression = table.Column<int>(type: "int", nullable: false),
                    ActivityCheck = table.Column<int>(type: "int", nullable: false),
                    Training = table.Column<int>(type: "int", nullable: false),
                    TrainingCamp = table.Column<int>(type: "int", nullable: false),
                    Coaching = table.Column<int>(type: "int", nullable: false),
                    Pt = table.Column<int>(type: "int", nullable: false),
                    Fantasy = table.Column<int>(type: "int", nullable: false),
                    Recruitment = table.Column<int>(type: "int", nullable: false),
                    Correction = table.Column<int>(type: "int", nullable: false),
                    Other = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerEarnedTpe", x => new { x.PlayerId, x.Season });
                    table.ForeignKey(
                        name: "FK_PlayerEarnedTpe_PlayerInformation_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerInformation",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerEarnedTpe");
        }
    }
}
