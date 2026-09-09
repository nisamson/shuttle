using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttle.Fhm.Serde.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class PreserveUnresolvedPersonnelNameReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Personnel_Names_FirstNameNameId",
                table: "Personnel");

            migrationBuilder.DropForeignKey(
                name: "FK_Personnel_Names_NicknameNameId",
                table: "Personnel");

            migrationBuilder.DropForeignKey(
                name: "FK_Personnel_Names_SurnameNameId",
                table: "Personnel");

            migrationBuilder.DropIndex(
                name: "IX_Personnel_FirstNameNameId",
                table: "Personnel");

            migrationBuilder.DropIndex(
                name: "IX_Personnel_NicknameNameId",
                table: "Personnel");

            migrationBuilder.DropIndex(
                name: "IX_Personnel_SurnameNameId",
                table: "Personnel");

            migrationBuilder.AddColumn<int>(
                name: "FirstNameLookupNameId",
                table: "Personnel",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NicknameLookupNameId",
                table: "Personnel",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SurnameLookupNameId",
                table: "Personnel",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_FirstNameLookupNameId",
                table: "Personnel",
                column: "FirstNameLookupNameId");

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_NicknameLookupNameId",
                table: "Personnel",
                column: "NicknameLookupNameId");

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_SurnameLookupNameId",
                table: "Personnel",
                column: "SurnameLookupNameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Personnel_Names_FirstNameLookupNameId",
                table: "Personnel",
                column: "FirstNameLookupNameId",
                principalTable: "Names",
                principalColumn: "NameId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Personnel_Names_NicknameLookupNameId",
                table: "Personnel",
                column: "NicknameLookupNameId",
                principalTable: "Names",
                principalColumn: "NameId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Personnel_Names_SurnameLookupNameId",
                table: "Personnel",
                column: "SurnameLookupNameId",
                principalTable: "Names",
                principalColumn: "NameId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Personnel_Names_FirstNameLookupNameId",
                table: "Personnel");

            migrationBuilder.DropForeignKey(
                name: "FK_Personnel_Names_NicknameLookupNameId",
                table: "Personnel");

            migrationBuilder.DropForeignKey(
                name: "FK_Personnel_Names_SurnameLookupNameId",
                table: "Personnel");

            migrationBuilder.DropIndex(
                name: "IX_Personnel_FirstNameLookupNameId",
                table: "Personnel");

            migrationBuilder.DropIndex(
                name: "IX_Personnel_NicknameLookupNameId",
                table: "Personnel");

            migrationBuilder.DropIndex(
                name: "IX_Personnel_SurnameLookupNameId",
                table: "Personnel");

            migrationBuilder.DropColumn(
                name: "FirstNameLookupNameId",
                table: "Personnel");

            migrationBuilder.DropColumn(
                name: "NicknameLookupNameId",
                table: "Personnel");

            migrationBuilder.DropColumn(
                name: "SurnameLookupNameId",
                table: "Personnel");

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_FirstNameNameId",
                table: "Personnel",
                column: "FirstNameNameId");

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_NicknameNameId",
                table: "Personnel",
                column: "NicknameNameId");

            migrationBuilder.CreateIndex(
                name: "IX_Personnel_SurnameNameId",
                table: "Personnel",
                column: "SurnameNameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Personnel_Names_FirstNameNameId",
                table: "Personnel",
                column: "FirstNameNameId",
                principalTable: "Names",
                principalColumn: "NameId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Personnel_Names_NicknameNameId",
                table: "Personnel",
                column: "NicknameNameId",
                principalTable: "Names",
                principalColumn: "NameId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Personnel_Names_SurnameNameId",
                table: "Personnel",
                column: "SurnameNameId",
                principalTable: "Names",
                principalColumn: "NameId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
