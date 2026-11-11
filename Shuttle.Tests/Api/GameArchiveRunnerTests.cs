using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shuttle.GameArchive;
using Shuttle.GameArchive.Runner;

namespace Shuttle.Tests.Api;

public sealed class GameArchiveRunnerTests {
    [Theory]
    [InlineData("http://127.0.0.1:3000/bot/archive.git", "https://localhost:7443/")]
    [InlineData("https://localhost:3000/bot/archive.git", "http://127.0.0.1:7443/")]
    [InlineData("http://[::1]:3000/bot/archive.git", "https://[::1]:7443/")]
    public void AcceptsOnlyLocalDestinations(string remote, string vault) {
        ArchiveRunner.ValidateConfiguration(Configuration(remote, vault), new TestEnvironment());
    }

    [Theory]
    [InlineData("https://github.com/shuttle-shl/shl-games-archive.git")]
    [InlineData("file:///C:/archive.git")]
    [InlineData("ssh://localhost/archive.git")]
    [InlineData("http://bot:secret@localhost:3000/archive.git")]
    [InlineData("http://localhost:3000/archive.git?token=secret")]
    [InlineData("http://localhost:3000/archive.git#fragment")]
    [InlineData("not-a-url")]
    [InlineData(null)]
    public void RejectsUnsafeGitDestination(string? remote) {
        Assert.Throws<InvalidOperationException>(() =>
            ArchiveRunner.ValidateConfiguration(Configuration(remote), new TestEnvironment()));
    }

    [Theory]
    [InlineData("https://production.vault.azure.net/")]
    [InlineData("file:///C:/vault")]
    [InlineData("http://bot:secret@localhost:7443/")]
    [InlineData("https://localhost:7443/?token=secret")]
    [InlineData("https://localhost:7443/#fragment")]
    [InlineData("not-a-url")]
    [InlineData(null)]
    public void RejectsUnsafeVaultDestination(string? vault) {
        Assert.Throws<InvalidOperationException>(() =>
            ArchiveRunner.ValidateConfiguration(Configuration(vault: vault), new TestEnvironment()));
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void RejectsNonDevelopmentEnvironment(string environment) {
        Assert.Throws<InvalidOperationException>(() =>
            ArchiveRunner.ValidateConfiguration(Configuration(), new TestEnvironment { EnvironmentName = environment }));
    }

    [Theory]
    [InlineData("http://simulationhockey.com/games/")]
    [InlineData("https://simulationhockey.com/games")]
    [InlineData("https://user:secret@simulationhockey.com/games/")]
    [InlineData("https://simulationhockey.com/games/?filter=1")]
    [InlineData("file:///C:/games/")]
    public void RejectsUnsafeSourceBeforeRunning(string source) {
        Assert.Throws<InvalidOperationException>(() =>
            ArchiveRunner.ValidateConfiguration(Configuration(source: source), new TestEnvironment()));
    }

    [Theory]
    [InlineData("archivevault")]
    [InlineData("archive-vault")]
    public void AcceptsFixtureAndVaultConnectionReference(string connectionName) {
        var configuration = Configuration(vault: null, source: "http://localhost:8000/games/");
        configuration[$"ConnectionStrings:{connectionName}"] = "https://localhost:7443/";
        ArchiveRunner.ValidateConfiguration(configuration, new TestEnvironment());
    }

    [Fact]
    public async Task ConfigurationFailureExitsBeforeSynchronizing() {
        var calls = 0;
        var exit = await ArchiveRunner.RunOnceAsync(_ => {
            ArchiveRunner.ValidateConfiguration(Configuration("https://github.com/shuttle-shl/shl-games-archive.git"),
                new TestEnvironment());
            calls++;
            return Task.CompletedTask;
        }, NullLogger.Instance, TestContext.Current.CancellationToken);
        Assert.Equal(1, exit);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task SuccessfulRunExecutesExactlyOnceAndExitsZero() {
        var calls = 0;
        var exit = await ArchiveRunner.RunOnceAsync(_ => {
            calls++;
            return Task.CompletedTask;
        }, NullLogger.Instance, TestContext.Current.CancellationToken);
        Assert.Equal(0, exit);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task SynchronizationFailureExitsNonzero() {
        var exit = await ArchiveRunner.RunOnceAsync(_ => throw new IOException("Download failed"),
            NullLogger.Instance, TestContext.Current.CancellationToken);
        Assert.Equal(1, exit);
    }

    [Fact]
    public async Task CancellationDuringSynchronizationExits130() {
        var exit = await ArchiveRunner.RunOnceAsync(_ => throw new OperationCanceledException(),
            NullLogger.Instance, TestContext.Current.CancellationToken);
        Assert.Equal(130, exit);
    }

    [Fact]
    public async Task AlreadyCancelledRunDoesNotStart() {
        var calls = 0;
        var exit = await ArchiveRunner.RunOnceAsync(_ => {
            calls++;
            return Task.CompletedTask;
        }, NullLogger.Instance, new CancellationToken(true));
        Assert.Equal(130, exit);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void DisabledApiBindsOptionsWithoutRequiringVaultOrSyncServices() {
        var configuration = Configuration(vault: null);
        var services = new ServiceCollection().AddGameArchive(configuration, new TestEnvironment());
        using var provider = services.BuildServiceProvider();
        Assert.False(provider.GetRequiredService<IOptions<GameArchiveOptions>>().Value.Enabled);
        Assert.Null(provider.GetService<SecretClient>());
        Assert.Null(provider.GetService<GameArchiveSynchronizer>());
    }

    [Theory]
    [InlineData(false, "archivevault")]
    [InlineData(true, "archivevault")]
    [InlineData(false, "archive-vault")]
    [InlineData(true, "archive-vault")]
    public void EnabledApiAndOneShotRunnerShareRegistration(bool runOnce, string connectionName) {
        var configuration = Configuration(vault: null, source: "http://localhost:8000/games/");
        configuration["Archive:Enabled"] = runOnce ? "false" : "true";
        configuration[$"ConnectionStrings:{connectionName}"] = "https://localhost:7443/";
        var environment = new TestEnvironment();
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddLogging();
        services.AddGameArchive(configuration, environment, runOnce);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.Equal(new Uri("https://localhost:7443/"), provider.GetRequiredService<SecretClient>().VaultUri);
        Assert.IsType<KeyVaultGameArchiveCredentialProvider>(provider.GetRequiredService<IGameArchiveCredentialProvider>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<GameArchiveSynchronizer>());
        Assert.Equal("http://localhost:8000/games/", provider.GetRequiredService<IOptions<GameArchiveOptions>>().Value.SourceUrl);
        Assert.Equal(TimeSpan.FromMinutes(5), provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(GameArchiveCrawler)).Timeout);
        var handler = provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(nameof(GameArchiveCrawler));
        while (handler is DelegatingHandler delegating)
            handler = Assert.IsAssignableFrom<HttpMessageHandler>(delegating.InnerHandler);
        Assert.False(Assert.IsType<HttpClientHandler>(handler).AllowAutoRedirect);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EnabledRegistrationRequiresVault(bool runOnce) {
        var configuration = Configuration(vault: null);
        configuration["Archive:Enabled"] = runOnce ? "false" : "true";
        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddGameArchive(configuration, new TestEnvironment(), runOnce));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScopedEmulatorRegistrationLeavesProcessAzureCredentialsUnchanged(bool runOnce) {
        var configuration = EmulatorConfiguration();
        configuration["Archive:Enabled"] = runOnce ? "false" : "true";
        var names = new[] { "AZURE_TENANT_ID", "AZURE_CLIENT_ID", "AZURE_CLIENT_SECRET", "AZURE_AUTHORITY_HOST" };
        var original = names.Select(Environment.GetEnvironmentVariable).ToArray();

        var services = new ServiceCollection().AddGameArchive(configuration, new TestEnvironment(), runOnce);
        using var provider = services.BuildServiceProvider();
        Assert.Equal(new Uri("https://localhost:7443/"), provider.GetRequiredService<SecretClient>().VaultUri);
        Assert.True(original.SequenceEqual(names.Select(Environment.GetEnvironmentVariable)));
    }

    [Fact]
    public void DisabledApiDoesNotActivateEmulatorCredentials() {
        var services = new ServiceCollection().AddGameArchive(
            EmulatorConfiguration(), new TestEnvironment { EnvironmentName = Environments.Production });
        using var provider = services.BuildServiceProvider();
        Assert.Null(provider.GetService<SecretClient>());
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void ScopedEmulatorCredentialsAreRejectedOutsideDevelopment(string environment) {
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddGameArchive(
            EmulatorConfiguration(), new TestEnvironment { EnvironmentName = environment }, runOnce: true));
    }

    [Theory]
    [InlineData("http://localhost:7443/")]
    [InlineData("https://production.vault.azure.net/")]
    [InlineData("https://user:password@localhost:7443/")]
    [InlineData("https://localhost:7443/?token=not-a-real-secret")]
    [InlineData("https://localhost:7443/#fragment")]
    public void ScopedEmulatorCredentialsRequireLocalHttpsVault(string vault) {
        var configuration = EmulatorConfiguration();
        configuration["Archive:VaultUri"] = vault;
        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddGameArchive(configuration, new TestEnvironment(), runOnce: true));
    }

    [Theory]
    [InlineData("http://localhost:7443/")]
    [InlineData("https://login.microsoftonline.com/")]
    [InlineData("https://user:password@localhost:7443/")]
    [InlineData("https://localhost:7443/?token=not-a-real-secret")]
    [InlineData("https://localhost:7443/#fragment")]
    [InlineData("not-a-url")]
    public void ScopedEmulatorCredentialsRejectUnsafeAuthority(string authority) {
        var configuration = EmulatorConfiguration();
        configuration["Archive:Emulator:AZURE_AUTHORITY_HOST"] = authority;
        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddGameArchive(configuration, new TestEnvironment(), runOnce: true));
    }

    [Theory]
    [InlineData("AZURE_TENANT_ID", null)]
    [InlineData("AZURE_CLIENT_ID", null)]
    [InlineData("AZURE_CLIENT_SECRET", null)]
    [InlineData("AZURE_AUTHORITY_HOST", null)]
    [InlineData("AZURE_CLIENT_SECRET", " ")]
    public void PartialEmulatorCredentialsFailBeforeClientRegistration(string name, string? value) {
        var configuration = EmulatorConfiguration();
        configuration[$"Archive:Emulator:{name}"] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddGameArchive(configuration, new TestEnvironment(), runOnce: true));
        Assert.DoesNotContain(services, service => service.ServiceType == typeof(SecretClient));
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void RealVaultDoesNotRequireEmulatorCredentials(string environment) {
        var configuration = Configuration(vault: "https://shuttle-test.vault.azure.net/");
        var services = new ServiceCollection().AddGameArchive(
            configuration, new TestEnvironment { EnvironmentName = environment }, runOnce: true);
        using var provider = services.BuildServiceProvider();
        Assert.Equal(new Uri("https://shuttle-test.vault.azure.net/"),
            provider.GetRequiredService<SecretClient>().VaultUri);
    }

    private static IConfigurationRoot EmulatorConfiguration() {
        var configuration = Configuration();
        configuration["Archive:Emulator:AZURE_TENANT_ID"] = "00000000-0000-0000-0000-000000000001";
        configuration["Archive:Emulator:AZURE_CLIENT_ID"] = "00000000-0000-0000-0000-000000000002";
        configuration["Archive:Emulator:AZURE_CLIENT_SECRET"] = "test-only-emulator-secret";
        configuration["Archive:Emulator:AZURE_AUTHORITY_HOST"] = "https://localhost:7443/";
        return configuration;
    }

    private static IConfigurationRoot Configuration(
        string? remote = "http://localhost:3000/shuttle-bot/shl-games-archive.git",
        string? vault = "https://localhost:7443/",
        string? source = null) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Archive:RemoteUrl"] = remote,
            ["Archive:VaultUri"] = vault,
            ["Archive:SourceUrl"] = source ?? "https://simulationhockey.com/games/",
        }).Build();

    private sealed class TestEnvironment : IHostEnvironment {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "ArchiveRunnerTests";
        public string ContentRootPath { get; set; } = Environment.CurrentDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
