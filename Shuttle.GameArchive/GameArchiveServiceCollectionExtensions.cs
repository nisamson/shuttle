using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Shuttle.GameArchive;

public static class GameArchiveServiceCollectionExtensions {
    public static IServiceCollection AddGameArchive(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment,
        bool runOnce = false) {
        services.Configure<GameArchiveOptions>(configuration.GetSection(GameArchiveOptions.SectionName));
        if (!runOnce && !configuration.GetValue<bool>($"{GameArchiveOptions.SectionName}:Enabled"))
            return services;

        var vaultUri = configuration[$"{GameArchiveOptions.SectionName}:VaultUri"]
            ?? configuration.GetConnectionString("archivevault")
            ?? configuration.GetConnectionString("archive-vault")
            ?? throw new InvalidOperationException("Archive Key Vault URI must be configured when the archive is enabled.");
        services.AddSingleton(new SecretClient(
            new Uri(vaultUri),
            new DefaultAzureCredential(new DefaultAzureCredentialOptions {
                DisableInstanceDiscovery = environment.IsDevelopment(),
            }),
            new SecretClientOptions {
                DisableChallengeResourceVerification = environment.IsDevelopment(),
            }));
        services.AddSingleton<IGameArchiveCredentialProvider, KeyVaultGameArchiveCredentialProvider>();
        services.AddHttpClient<GameArchiveCrawler>(client => client.Timeout = TimeSpan.FromMinutes(5))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddScoped<GameArchiveSynchronizer>();
        return services;
    }
}
