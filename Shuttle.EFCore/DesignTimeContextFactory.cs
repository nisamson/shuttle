using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shuttle.EFCore;

public class DesignTimeContextFactory : IDesignTimeDbContextFactory<ShlDbContext> {
    public ShlDbContext CreateDbContext(string[] args) {
        dotenv.net.DotEnv.Load();
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings() {
            Args = args,
            EnvironmentName = Environments.Development
        });
        builder.AddShuttleDatabase(out var optionsBuilder);
        var connStr = ShuttleEfCoreExtensions.GetConnectionString();
        Console.WriteLine(connStr);
        var db = new ShlDbContext(optionsBuilder.Options, NullLogger<ShlDbContext>.Instance);
        db.Database.ExecuteSql($"SELECT 1"); // Ensure database is created and migrations are applied at design time.
        return db;
    }
}