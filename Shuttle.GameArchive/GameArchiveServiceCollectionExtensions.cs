using Azure.Core;
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
        var uri = new Uri(vaultUri);
        var emulator = configuration.GetSection($"{GameArchiveOptions.SectionName}:Emulator");
        services.AddSingleton(new SecretClient(
            uri,
            CreateVaultCredential(emulator, uri, environment),
            new SecretClientOptions {
                DisableChallengeResourceVerification = emulator.Exists(),
            }));
        services.AddSingleton<IGameArchiveCredentialProvider, KeyVaultGameArchiveCredentialProvider>();
        services.AddHttpClient<GameArchiveCrawler>(client => client.Timeout = TimeSpan.FromMinutes(5))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddScoped<GameArchiveSynchronizer>();
        return services;
    }

    private static TokenCredential CreateVaultCredential(
        IConfigurationSection emulator, Uri vaultUri, IHostEnvironment environment) {
        if (!emulator.Exists())
            return new DefaultAzureCredential(new DefaultAzureCredentialOptions {
                DisableInstanceDiscovery = environment.IsDevelopment(),
            });

        if (!environment.IsDevelopment() || !IsLocalHttpsEndpoint(vaultUri))
            throw new InvalidOperationException("Archive emulator credentials require Development and a loopback HTTPS vault.");

        string Required(string name) {
            var value = emulator[name];
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Archive emulator setting {name} must be configured.");
            return value;
        }

        if (!Uri.TryCreate(Required("AZURE_AUTHORITY_HOST"), UriKind.Absolute, out var authority)
            || !IsLocalHttpsEndpoint(authority))
            throw new InvalidOperationException("Archive emulator authority must be a loopback HTTPS endpoint.");

        return new ClientSecretCredential(
            Required("AZURE_TENANT_ID"), Required("AZURE_CLIENT_ID"), Required("AZURE_CLIENT_SECRET"),
            new ClientSecretCredentialOptions {
                AuthorityHost = authority,
                DisableInstanceDiscovery = true,
            });
    }

    private static bool IsLocalHttpsEndpoint(Uri uri) =>
        uri.IsAbsoluteUri && uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttps
        && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
}
