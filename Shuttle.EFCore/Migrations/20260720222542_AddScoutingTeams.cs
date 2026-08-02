using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttle.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddScoutingTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScoutingTeams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoutingTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoutingTeams_ShuttleUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "ShuttleUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScoutingBoards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoutingTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DraftSeason = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoutingBoards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoutingBoards_ScoutingTeams_ScoutingTeamId",
                        column: x => x.ScoutingTeamId,
                        principalTable: "ScoutingTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScoutingTeamMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoutingTeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShuttleUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoutingTeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoutingTeamMembers_ScoutingTeams_ScoutingTeamId",
                        column: x => x.ScoutingTeamId,
                        principalTable: "ScoutingTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScoutingTeamMembers_ShuttleUsers_ShuttleUserId",
                        column: x => x.ShuttleUserId,
                        principalTable: "ShuttleUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScoutingBoardEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoutingBoardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoutingBoardEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoutingBoardEntries_ScoutingBoards_ScoutingBoardId",
                        column: x => x.ScoutingBoardId,
                        principalTable: "ScoutingBoards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScoutingComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoutingBoardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoutingBoardEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EditedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoutingComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoutingComments_ScoutingBoardEntries_ScoutingBoardEntryId",
                        column: x => x.ScoutingBoardEntryId,
                        principalTable: "ScoutingBoardEntries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ScoutingComments_ScoutingBoards_ScoutingBoardId",
                        column: x => x.ScoutingBoardId,
                        principalTable: "ScoutingBoards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScoutingComments_ShuttleUsers_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "ShuttleUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScoutingBoardEntries_ScoutingBoardId_PlayerId",
                table: "ScoutingBoardEntries",
                columns: new[] { "ScoutingBoardId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScoutingBoardEntries_ScoutingBoardId_Rank",
                table: "ScoutingBoardEntries",
                columns: new[] { "ScoutingBoardId", "Rank" });

            migrationBuilder.CreateIndex(
                name: "IX_ScoutingBoards_ScoutingTeamId",
                table: "ScoutingBoards",
                column: "ScoutingTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoutingComments_AuthorUserId",
                table: "ScoutingComments",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoutingComments_ScoutingBoardEntryId",
                table: "ScoutingComments",
                column: "ScoutingBoardEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoutingComments_ScoutingBoardId_ScoutingBoardEntryId_CreatedAt",
                table: "ScoutingComments",
                columns: new[] { "ScoutingBoardId", "ScoutingBoardEntryId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ScoutingTeamMembers_ScoutingTeamId_ShuttleUserId",
                table: "ScoutingTeamMembers",
                columns: new[] { "ScoutingTeamId", "ShuttleUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScoutingTeamMembers_ShuttleUserId",
                table: "ScoutingTeamMembers",
                column: "ShuttleUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoutingTeams_CreatedByUserId",
                table: "ScoutingTeams",
                column: "CreatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScoutingComments");

            migrationBuilder.DropTable(
                name: "ScoutingTeamMembers");

            migrationBuilder.DropTable(
                name: "ScoutingBoardEntries");

            migrationBuilder.DropTable(
                name: "ScoutingBoards");

            migrationBuilder.DropTable(
                name: "ScoutingTeams");
        }
    }
}
