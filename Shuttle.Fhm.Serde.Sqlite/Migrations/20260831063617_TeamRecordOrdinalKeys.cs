using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttle.Fhm.Serde.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class TeamRecordOrdinalKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("PRAGMA foreign_keys = OFF;", suppressTransaction: true);

            migrationBuilder.DropForeignKey(
                name: "FK_Personnel_Teams_TeamId",
                table: "Personnel");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Teams_TeamId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_StoredLines_Teams_TeamId",
                table: "StoredLines");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamActiveLineSlots_Teams_TeamId",
                table: "TeamActiveLineSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Teams_AffiliateParentId",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Teams_AffiliateParentId2",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamTactics_Teams_TeamId",
                table: "TeamTactics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Teams",
                table: "Teams");

            migrationBuilder.RenameColumn(
                name: "TeamId",
                table: "TeamTactics",
                newName: "TeamRecordOrdinal");

            migrationBuilder.RenameColumn(
                name: "AffiliateParentId2",
                table: "Teams",
                newName: "SecondaryAffiliateParentRecordOrdinal");

            migrationBuilder.RenameColumn(
                name: "AffiliateParentId",
                table: "Teams",
                newName: "AffiliateParentRecordOrdinal");

            migrationBuilder.RenameIndex(
                name: "IX_Teams_AffiliateParentId2",
                table: "Teams",
                newName: "IX_Teams_SecondaryAffiliateParentRecordOrdinal");

            migrationBuilder.RenameIndex(
                name: "IX_Teams_AffiliateParentId",
                table: "Teams",
                newName: "IX_Teams_AffiliateParentRecordOrdinal");

            migrationBuilder.RenameColumn(
                name: "TeamId",
                table: "TeamActiveLineSlots",
                newName: "TeamRecordOrdinal");

            migrationBuilder.RenameColumn(
                name: "TeamId",
                table: "StoredLines",
                newName: "TeamRecordOrdinal");

            migrationBuilder.RenameIndex(
                name: "IX_StoredLines_TeamId",
                table: "StoredLines",
                newName: "IX_StoredLines_TeamRecordOrdinal");

            migrationBuilder.RenameColumn(
                name: "TeamId",
                table: "Players",
                newName: "TeamRecordOrdinal");

            migrationBuilder.RenameIndex(
                name: "IX_Players_TeamId",
                table: "Players",
                newName: "IX_Players_TeamRecordOrdinal");

            migrationBuilder.RenameColumn(
                name: "TeamId",
                table: "Personnel",
                newName: "TeamRecordOrdinal");

            migrationBuilder.RenameIndex(
                name: "IX_Personnel_TeamId",
                table: "Personnel",
                newName: "IX_Personnel_TeamRecordOrdinal");

            migrationBuilder.Sql(
                """
                UPDATE "Personnel"
                SET "TeamRecordOrdinal" = (
                    SELECT "RecordOrdinal"
                    FROM "Teams" AS "Principal"
                    WHERE "Principal"."TeamId" = "Personnel"."TeamRecordOrdinal")
                WHERE "TeamRecordOrdinal" IS NOT NULL;

                UPDATE "Players"
                SET "TeamRecordOrdinal" = (
                    SELECT "RecordOrdinal"
                    FROM "Teams" AS "Principal"
                    WHERE "Principal"."TeamId" = "Players"."TeamRecordOrdinal")
                WHERE "TeamRecordOrdinal" IS NOT NULL;

                UPDATE "StoredLines"
                SET "TeamRecordOrdinal" = (
                    SELECT "RecordOrdinal"
                    FROM "Teams" AS "Principal"
                    WHERE "Principal"."TeamId" = "StoredLines"."TeamRecordOrdinal")
                WHERE "TeamRecordOrdinal" IS NOT NULL;

                UPDATE "TeamActiveLineSlots"
                SET "TeamRecordOrdinal" = (
                    SELECT "RecordOrdinal"
                    FROM "Teams" AS "Principal"
                    WHERE "Principal"."TeamId" = "TeamActiveLineSlots"."TeamRecordOrdinal");

                UPDATE "TeamTactics"
                SET "TeamRecordOrdinal" = (
                    SELECT "RecordOrdinal"
                    FROM "Teams" AS "Principal"
                    WHERE "Principal"."TeamId" = "TeamTactics"."TeamRecordOrdinal");

                UPDATE "Teams"
                SET "AffiliateParentRecordOrdinal" = (
                    SELECT "RecordOrdinal"
                    FROM "Teams" AS "Principal"
                    WHERE "Principal"."TeamId" = "Teams"."AffiliateParentRecordOrdinal")
                WHERE "AffiliateParentRecordOrdinal" IS NOT NULL;

                UPDATE "Teams"
                SET "SecondaryAffiliateParentRecordOrdinal" = (
                    SELECT "RecordOrdinal"
                    FROM "Teams" AS "Principal"
                    WHERE "Principal"."TeamId" = "Teams"."SecondaryAffiliateParentRecordOrdinal")
                WHERE "SecondaryAffiliateParentRecordOrdinal" IS NOT NULL;
                """);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Teams",
                table: "Teams",
                column: "RecordOrdinal");

            migrationBuilder.AddForeignKey(
                name: "FK_Personnel_Teams_TeamRecordOrdinal",
                table: "Personnel",
                column: "TeamRecordOrdinal",
                principalTable: "Teams",
                principalColumn: "RecordOrdinal",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Teams_TeamRecordOrdinal",
                table: "Players",
                column: "TeamRecordOrdinal",
                principalTable: "Teams",
                principalColumn: "RecordOrdinal",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StoredLines_Teams_TeamRecordOrdinal",
                table: "StoredLines",
                column: "TeamRecordOrdinal",
                principalTable: "Teams",
                principalColumn: "RecordOrdinal",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamActiveLineSlots_Teams_TeamRecordOrdinal",
                table: "TeamActiveLineSlots",
                column: "TeamRecordOrdinal",
                principalTable: "Teams",
                principalColumn: "RecordOrdinal",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Teams_AffiliateParentRecordOrdinal",
                table: "Teams",
                column: "AffiliateParentRecordOrdinal",
                principalTable: "Teams",
                principalColumn: "RecordOrdinal",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Teams_SecondaryAffiliateParentRecordOrdinal",
                table: "Teams",
                column: "SecondaryAffiliateParentRecordOrdinal",
                principalTable: "Teams",
                principalColumn: "RecordOrdinal",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamTactics_Teams_TeamRecordOrdinal",
                table: "TeamTactics",
                column: "TeamRecordOrdinal",
                principalTable: "Teams",
                principalColumn: "RecordOrdinal",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql("PRAGMA foreign_keys = ON;", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Personnel_Teams_TeamRecordOrdinal",
                table: "Personnel");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Teams_TeamRecordOrdinal",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_StoredLines_Teams_TeamRecordOrdinal",
                table: "StoredLines");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamActiveLineSlots_Teams_TeamRecordOrdinal",
                table: "TeamActiveLineSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Teams_AffiliateParentRecordOrdinal",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Teams_SecondaryAffiliateParentRecordOrdinal",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamTactics_Teams_TeamRecordOrdinal",
                table: "TeamTactics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Teams",
                table: "Teams");

            migrationBuilder.RenameColumn(
                name: "TeamRecordOrdinal",
                table: "TeamTactics",
                newName: "TeamId");

            migrationBuilder.RenameColumn(
                name: "SecondaryAffiliateParentRecordOrdinal",
                table: "Teams",
                newName: "AffiliateParentId2");

            migrationBuilder.RenameColumn(
                name: "AffiliateParentRecordOrdinal",
                table: "Teams",
                newName: "AffiliateParentId");

            migrationBuilder.RenameIndex(
                name: "IX_Teams_SecondaryAffiliateParentRecordOrdinal",
                table: "Teams",
                newName: "IX_Teams_AffiliateParentId2");

            migrationBuilder.RenameIndex(
                name: "IX_Teams_AffiliateParentRecordOrdinal",
                table: "Teams",
                newName: "IX_Teams_AffiliateParentId");

            migrationBuilder.RenameColumn(
                name: "TeamRecordOrdinal",
                table: "TeamActiveLineSlots",
                newName: "TeamId");

            migrationBuilder.RenameColumn(
                name: "TeamRecordOrdinal",
                table: "StoredLines",
                newName: "TeamId");

            migrationBuilder.RenameIndex(
                name: "IX_StoredLines_TeamRecordOrdinal",
                table: "StoredLines",
                newName: "IX_StoredLines_TeamId");

            migrationBuilder.RenameColumn(
                name: "TeamRecordOrdinal",
                table: "Players",
                newName: "TeamId");

            migrationBuilder.RenameIndex(
                name: "IX_Players_TeamRecordOrdinal",
                table: "Players",
                newName: "IX_Players_TeamId");

            migrationBuilder.RenameColumn(
                name: "TeamRecordOrdinal",
                table: "Personnel",
                newName: "TeamId");

            migrationBuilder.RenameIndex(
                name: "IX_Personnel_TeamRecordOrdinal",
                table: "Personnel",
                newName: "IX_Personnel_TeamId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Teams",
                table: "Teams",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_RecordOrdinal",
                table: "Teams",
                column: "RecordOrdinal",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Personnel_Teams_TeamId",
                table: "Personnel",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "TeamId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Teams_TeamId",
                table: "Players",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "TeamId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StoredLines_Teams_TeamId",
                table: "StoredLines",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "TeamId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamActiveLineSlots_Teams_TeamId",
                table: "TeamActiveLineSlots",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "TeamId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Teams_AffiliateParentId",
                table: "Teams",
                column: "AffiliateParentId",
                principalTable: "Teams",
                principalColumn: "TeamId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Teams_AffiliateParentId2",
                table: "Teams",
                column: "AffiliateParentId2",
                principalTable: "Teams",
                principalColumn: "TeamId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamTactics_Teams_TeamId",
                table: "TeamTactics",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "TeamId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
