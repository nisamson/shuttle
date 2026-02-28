using Microsoft.Data.Sqlite;
using Microsoft.Extensions.FileProviders;
using Quartz;
using SHLAnalytics.WebApp.Services.IO;

namespace SHLAnalytics.WebApp.Services.Jobs;

public static class JobsExtensions {
    
    private static string GetQuartzInit() {
        var assembly = typeof(JobsExtensions).Assembly;
        var provider = new EmbeddedFileProvider(assembly, typeof(JobsExtensions).Namespace);
        using var stream = provider.GetFileInfo("quartz_sqlite_init.sql").CreateReadStream();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string GetQuartzConnectionString(string dbDirectory) {
        var dbPath = Path.Combine(dbDirectory, "quartz.db");
        return $"Data Source={dbPath}";
    }

    public static async Task EnsureQuartzDatabaseExists(this IServiceProvider serviceProvider) {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IIoConfiguration>();
        var location = await context.EnsureFileStorageLocation();
        
        var connectionString = GetQuartzConnectionString(location);
        await using var connection = new SqliteConnection(connectionString);
        connection.Open();
        {
            await using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = """
            SELECT EXISTS(SELECT name FROM sqlite_master WHERE type='table' AND name='QRTZ_JOB_DETAILS');
            """;
            var exists = await checkCommand.ExecuteScalarAsync() as long? == 1;
            if (exists) {
                return;
            }
        }
        
        var initSql = GetQuartzInit();
        await using var initCommand = connection.CreateCommand();
        initCommand.CommandText = initSql;
        await initCommand.ExecuteNonQueryAsync();
    }
    
    public static void AddJobs(this IServiceCollection services, string dbPath) {
        services.AddQuartz(c => {
            c.UsePersistentStore(opt => {
                opt.UseMicrosoftSQLite(GetQuartzConnectionString(dbPath));
                opt.UseProperties = true;
                opt.UseSystemTextJsonSerializer();
            });
        });
        services.AddQuartzHostedService(c => {
                c.AwaitApplicationStarted = true;
                c.StartDelay = TimeSpan.FromSeconds(5);
            }
        );
    }
}
