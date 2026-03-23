using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttle.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class DbInit : Migration
    {
        private static string LoadQuartzSql() {
            return LoadSqlFromResource("Shuttle.EFCore.Migrations.SqlScript.quartz.sql");
        }
        
        private static string LoadRolesUpSql() {
            return LoadSqlFromResource("Shuttle.EFCore.Migrations.SqlScript.roles_up.sql");
        }
        
        private static string LoadRolesDownSql() {
            return LoadSqlFromResource("Shuttle.EFCore.Migrations.SqlScript.roles_down.sql");
        }
        
        private static string LoadSqlFromResource(string resourceName) {
            var assembly = typeof(DbInit).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) {
                throw new InvalidOperationException($"Resource '{resourceName}' not found.");
            }
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.EnsureSchema("qrtz");
            migrationBuilder.Sql(LoadQuartzSql());
            migrationBuilder.Sql(LoadRolesUpSql());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(LoadRolesDownSql());
            migrationBuilder.DropSchema("qrtz");
        }
    }
}
