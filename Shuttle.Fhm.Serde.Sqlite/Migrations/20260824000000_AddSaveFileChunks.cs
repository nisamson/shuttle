using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttle.Fhm.Serde.Sqlite.Migrations;

/// <inheritdoc />
[DbContext(typeof(FhmSaveSqliteContext))]
[Migration("20260824000000_AddSaveFileChunks")]
public partial class AddSaveFileChunks : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SaveFileChunks",
            columns: table => new
            {
                RelativePath = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                Content = table.Column<byte[]>(type: "BLOB", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SaveFileChunks", value => new { value.RelativePath, value.Ordinal });
                table.ForeignKey(
                    name: "FK_SaveFileChunks_SaveFiles_RelativePath",
                    column: value => value.RelativePath,
                    principalTable: "SaveFiles",
                    principalColumn: "RelativePath",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SaveFileChunks");
    }
}
