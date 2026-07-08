using System.Diagnostics;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
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
        string? databaseName = null,
        string? dbUri = null
    ) where T : IHostApplicationBuilder {
        var shuttleConnStr = GetConnectionString(databaseName, dbUri);
        var connectionString = shuttleConnStr.ConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<ShlDbContext>();
        builder.PrepareShuttleOptionsBuilder(optionsBuilder, connectionString);
        options = optionsBuilder;
        builder.Services.AddSingleton<IConnectionStringProvider<ShlDbContext>>(shuttleConnStr);
        builder.Services.AddDbContext<ShlDbContext>(options => {
                builder.PrepareShuttleOptionsBuilder(options, connectionString);
            }
        );
        builder.Services.AddScoped<IDbConnectionResilienceService<ShlDbContext>, ShlDbConnectionResilienceService>();
        builder.ConfigureEfCoreLogging();
        return builder;
    }

    private static DbContextOptionsBuilder PrepareShuttleOptionsBuilder<T>(this T builder, DbContextOptionsBuilder optionsBuilder, string connectionString) where T : IHostApplicationBuilder {
        optionsBuilder.UseAzureSql(connectionString)
            .EnableDetailedErrors(builder.Environment.IsDevelopment());
        optionsBuilder.UseLinqToDB(ldb => { ldb.AddCustomOptions(o => o.UseSqlServer(connectionString)); });
        return optionsBuilder;
    }

    public static ShuttleConnectionString GetConnectionString(
        string? databaseName = null,
        string? dbHost = null
    ) {
        databaseName ??= Environment.GetEnvironmentVariable(ShuttleEfCoreConstants.DatabaseEnvironmentKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        dbHost ??= Environment.GetEnvironmentVariable(ShuttleEfCoreConstants.DatabaseHostKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(dbHost);
        var baseConnStr = BaseConnectionString(dbHost, databaseName);
        var connectionStringBuilder = new SqlConnectionStringBuilder(baseConnStr) {
            Authentication = SqlAuthenticationMethod.ActiveDirectoryDefault,
            PersistSecurityInfo = false,
            ConnectTimeout = 30,
            ConnectRetryCount = 5,
            ConnectRetryInterval = 10,
        };
        var connectionString = connectionStringBuilder.ToString();
        return new(connectionString);
    }

    private static IHostApplicationBuilder ConfigureEfCoreLogging(this IHostApplicationBuilder host) {
        host.Services.AddOpenTelemetry()
            .WithTracing(t => {
                    t.AddSource(ActivitySources.ShuttleEfCore.Name);
                    t.AddEntityFrameworkCoreInstrumentation();
                    t.AddSqlClientInstrumentation();
                }
            );
        return host;
    }

    private static string BaseConnectionString(string dbHost, string databaseName) {
        var server = $"tcp:{dbHost},1433";
        var startingConStr =
            $"Server={server};Initial Catalog={databaseName};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        return startingConStr;
    }

    public static T AddShuttleDatabase<T>(this T builder, string? databaseName = null, string? dbUri = null)
        where T : IHostApplicationBuilder {
        return AddShuttleDatabase(builder, out _, databaseName, dbUri);
    }
    
    public static async Task EnsureShuttleDatabaseConnectivity(this IHost host, CancellationToken cancellationToken = default) {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbConnectionResilienceService<ShlDbContext>>();
        await dbContext.EnsureDbConnectivity(cancellationToken);
    }
}
