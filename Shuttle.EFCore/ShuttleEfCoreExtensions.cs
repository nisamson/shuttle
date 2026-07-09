using System.Reflection;
using LinqToDB;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using Shuttle.EFCore.Resilience;

namespace Shuttle.EFCore;

public static class ShuttleEfCoreExtensions {

    internal static T AddShuttleDatabase<T>(
        this T builder,
        out DbContextOptionsBuilder<ShlDbContext> options,
        string? databasePath = null
    ) where T : IHostApplicationBuilder {
        var shuttleConnStr = GetConnectionString(databasePath);
        var connectionString = shuttleConnStr.ConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<ShlDbContext>();
        builder.PrepareShuttleOptionsBuilder(optionsBuilder, connectionString);
        options = optionsBuilder;
        builder.Services.AddSingleton<IConnectionStringProvider<ShlDbContext>>(shuttleConnStr);
        builder.Services.AddDbContext<ShlDbContext>(options => {
                builder.PrepareShuttleOptionsBuilder(options, connectionString);
            }
        );
        builder.ConfigureEfCoreLogging();
        return builder;
    }

    private static DbContextOptionsBuilder PrepareShuttleOptionsBuilder<T>(this T builder, DbContextOptionsBuilder optionsBuilder, string connectionString) where T : IHostApplicationBuilder {
        optionsBuilder.UseSqlite(connectionString)
            .EnableDetailedErrors(builder.Environment.IsDevelopment());
        optionsBuilder.UseLinqToDB(ldb => { ldb.AddCustomOptions(o => o.UseSQLite(connectionString, SQLiteProvider.Microsoft)); });
        return optionsBuilder;
    }

    public static ShuttleConnectionString GetConnectionString(
        string? databasePath = null
    ) {
        databasePath ??= Environment.GetEnvironmentVariable(ShuttleEfCoreConstants.DatabasePathKey);
        if (string.IsNullOrWhiteSpace(databasePath)) {
            databasePath = ShuttleEfCoreConstants.DefaultDatabaseFileName;
        }

        var connectionStringBuilder = new SqliteConnectionStringBuilder {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true,
            Pooling = true,
        };
        return new(connectionStringBuilder.ToString());
    }

    private static IHostApplicationBuilder ConfigureEfCoreLogging(this IHostApplicationBuilder host) {
        host.Services.AddOpenTelemetry()
            .WithTracing(t => {
                    t.AddSource(ActivitySources.ShuttleEfCore.Name);
                    t.AddEntityFrameworkCoreInstrumentation();
                }
            );
        return host;
    }

    public static T AddShuttleDatabase<T>(this T builder, string? databasePath = null)
        where T : IHostApplicationBuilder {
        return AddShuttleDatabase(builder, out _, databasePath);
    }

    public static async Task EnsureShuttleDatabaseConnectivity(this IHost host, CancellationToken cancellationToken = default) {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShlDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        await dbContext.ConfigureSqlitePragmas(cancellationToken);
        await dbContext.EnsureTemporalHistory(cancellationToken);
    }

    internal static async Task ConfigureSqlitePragmas(this ShlDbContext dbContext, CancellationToken cancellationToken = default) {
        // WAL is persisted in the database file; busy_timeout is per-connection but harmless to
        // (re)apply here to reduce "database is locked" errors from concurrent updater jobs.
        // recursive_triggers stays OFF (the default) so the ValidFrom bump inside the temporal
        // update trigger does not re-fire the trigger.
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=30000;", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA recursive_triggers=OFF;", cancellationToken);
    }

    /// <summary>
    /// Applies the official Quartz.NET SQLite schema on a fresh database. The upstream script
    /// contains DROP statements, so it is only executed when the Quartz tables are absent to
    /// avoid discarding persisted scheduler state on restart.
    /// </summary>
    public static async Task EnsureQuartzSchema(this IHost host, CancellationToken cancellationToken = default) {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShlDbContext>();
        var tableCount = await dbContext.Database
            .SqlQueryRaw<long>("SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = 'QRTZ_LOCKS'")
            .SingleAsync(cancellationToken);
        if (tableCount > 0) {
            return;
        }

        var script = ReadEmbeddedScript("Shuttle.EFCore.Migrations.SqlScripts.quartz_sqlite.sql");
        await dbContext.Database.ExecuteSqlRawAsync(script, cancellationToken);
    }

    private static string ReadEmbeddedScript(string resourceName) {
        var assembly = typeof(ShuttleEfCoreExtensions).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
