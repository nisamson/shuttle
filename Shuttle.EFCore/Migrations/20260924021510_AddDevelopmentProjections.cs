using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttle.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddDevelopmentProjections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DevelopmentProjectionRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlgorithmVersion = table.Column<int>(type: "int", nullable: false),
                    DataAsOf = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevelopmentProjectionRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DevelopmentProjections",
                columns: table => new
                {
                    RunId = table.Column<long>(type: "bigint", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevelopmentProjections", x => new { x.RunId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_DevelopmentProjections_DevelopmentProjectionRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "DevelopmentProjectionRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DevelopmentProjectionRuns_AlgorithmVersion_DataAsOf",
                table: "DevelopmentProjectionRuns",
                columns: new[] { "AlgorithmVersion", "DataAsOf" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DevelopmentProjectionRuns_AlgorithmVersion_PublishedAt",
                table: "DevelopmentProjectionRuns",
                columns: new[] { "AlgorithmVersion", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DevelopmentProjections_PlayerId",
                table: "DevelopmentProjections",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DevelopmentProjections");

            migrationBuilder.DropTable(
                name: "DevelopmentProjectionRuns");
        }
    }
}
