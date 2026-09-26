using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttle.EFCore.Migrations;

[DbContext(typeof(ShlDbContext))]
[Migration("20260926030000_AddProjectedPeakTpe")]
public sealed class AddProjectedPeakTpe : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<double>(
            name: "ProjectedPeakTpe",
            table: "DevelopmentProjections",
            type: "float",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE projection
            SET ProjectedPeakTpe = peaks.ProjectedPeakTpe
            FROM DevelopmentProjections AS projection
            CROSS APPLY (
                SELECT MAX(TRY_CONVERT(float, JSON_VALUE(point.[value], '$.p50'))) AS ProjectedPeakTpe
                FROM OPENJSON(projection.PayloadJson, '$.projectedPoints') AS point
            ) AS peaks
            WHERE projection.PayloadJson IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            name: "ProjectedPeakTpe",
            table: "DevelopmentProjections");
    }
}
