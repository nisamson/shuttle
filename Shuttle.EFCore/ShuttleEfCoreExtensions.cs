using System.Diagnostics;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Shuttle.EFCore;

public static class ShuttleEfCoreExtensions {

    internal static T AddShuttleDatabase<T>(
        this T builder,
        out DbContextOptionsBuilder<ShlDbContext> options,
        string? databaseName = null,
        string? dbUri = null
    ) where T : IHostApplicationBuilder {
        var shuttleConnStr = builder.GetConnectionString(databaseName, dbUri);
        var connectionString = shuttleConnStr.ConnectionString;

        DbContextOptionsBuilder<ShlDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(connectionString);
        options = optionsBuilder;
        builder.Services.AddSingleton(shuttleConnStr);
        builder.Services.AddDbContext<ShlDbContext>(options => {
                options.UseSqlServer(connectionString);
                options.UseLinqToDB(ldb => { ldb.AddCustomOptions(o => o.UseSqlServer(connectionString)); });
            }
        );
        return builder;
    }

    public static ShuttleConnectionString GetConnectionString<T>(
        this T builder,
        string? databaseName = null,
        string? dbHost = null
    ) where T : IHostApplicationBuilder {
        databaseName ??= Environment.GetEnvironmentVariable(ShuttleEfCoreConstants.DatabaseEnvironmentKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        dbHost ??= Environment.GetEnvironmentVariable(ShuttleEfCoreConstants.DatabaseHostKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(dbHost);

        var authMethod = SqlAuthenticationMethod.ActiveDirectoryDefault;
        if (builder.Environment.IsDevelopment()) {
            authMethod = SqlAuthenticationMethod.ActiveDirectoryInteractive;
        }
        var baseConnStr = BaseConnectionString(dbHost, databaseName, authMethod);
        var connectionStringBuilder = new SqlConnectionStringBuilder(baseConnStr) {
            Authentication = authMethod,
            PersistSecurityInfo = false,
            ConnectTimeout = 30,
            ConnectRetryCount = 5,
            ConnectRetryInterval = 10,
        };
        var connectionString = connectionStringBuilder.ToString();
        return new(connectionString);
    }

    private static string BaseConnectionString(string dbHost, string databaseName, SqlAuthenticationMethod authMethod) {
        var server = $"tcp:{dbHost},1433";
        var startingConStr =
            $"Server={server};Initial Catalog={databaseName};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        return startingConStr;
    }

    public static T AddShuttleDatabase<T>(this T builder, string? databaseName = null, string? dbUri = null)
        where T : IHostApplicationBuilder {
        return AddShuttleDatabase(builder, out _, databaseName, dbUri);
    }
}
