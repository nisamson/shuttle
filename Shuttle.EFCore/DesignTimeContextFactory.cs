using Microsoft.EntityFrameworkCore.Design;
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
        return new ShlDbContext(optionsBuilder.Options, NullLogger<ShlDbContext>.Instance);
    }
}