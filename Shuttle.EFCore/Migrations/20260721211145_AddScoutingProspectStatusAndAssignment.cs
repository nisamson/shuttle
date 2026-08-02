using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttle.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddScoutingProspectStatusAndAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AssignedAt",
                table: "ScoutingBoardEntries",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToUserId",
                table: "ScoutingBoardEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ScoutingBoardEntries",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.CreateIndex(
                name: "IX_ScoutingBoardEntries_AssignedToUserId",
                table: "ScoutingBoardEntries",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoutingBoardEntries_ScoutingBoardId_Status",
                table: "ScoutingBoardEntries",
                columns: new[] { "ScoutingBoardId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_ScoutingBoardEntries_ShuttleUsers_AssignedToUserId",
                table: "ScoutingBoardEntries",
                column: "AssignedToUserId",
                principalTable: "ShuttleUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScoutingBoardEntries_ShuttleUsers_AssignedToUserId",
                table: "ScoutingBoardEntries");

            migrationBuilder.DropIndex(
                name: "IX_ScoutingBoardEntries_AssignedToUserId",
                table: "ScoutingBoardEntries");

            migrationBuilder.DropIndex(
                name: "IX_ScoutingBoardEntries_ScoutingBoardId_Status",
                table: "ScoutingBoardEntries");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "ScoutingBoardEntries");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "ScoutingBoardEntries");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ScoutingBoardEntries");
        }
    }
}
