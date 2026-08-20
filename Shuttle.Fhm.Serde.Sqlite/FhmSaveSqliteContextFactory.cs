using Microsoft.EntityFrameworkCore.Design;

namespace Shuttle.Fhm.Serde.Sqlite;

/// <summary>Provides design-time context creation for the SQLite schema migrations.</summary>
public sealed class FhmSaveSqliteContextFactory : IDesignTimeDbContextFactory<FhmSaveSqliteContext>
{
    /// <inheritdoc />
    public FhmSaveSqliteContext CreateDbContext(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var databasePath = args.FirstOrDefault() ?? Path.Combine(Path.GetTempPath(), "fhm-save.sqlite");
        return new FhmSaveSqliteContext(FhmSaveSqliteContext.CreateOptions(databasePath));
    }
}
