using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttle.Fhm.Serde.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class PreserveRawPersonnelBirthDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BirthDay",
                table: "Personnel",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BirthMonth",
                table: "Personnel",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BirthYear",
                table: "Personnel",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE Personnel
                SET BirthYear = CAST(strftime('%Y', BirthDate) AS INTEGER),
                    BirthMonth = CAST(strftime('%m', BirthDate) AS INTEGER),
                    BirthDay = CAST(strftime('%d', BirthDate) AS INTEGER);
                """);

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "Personnel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirthDay",
                table: "Personnel");

            migrationBuilder.DropColumn(
                name: "BirthMonth",
                table: "Personnel");

            migrationBuilder.DropColumn(
                name: "BirthYear",
                table: "Personnel");

            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                table: "Personnel",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }
    }
}
