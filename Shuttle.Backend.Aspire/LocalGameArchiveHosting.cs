using Aspire.Hosting.Azure;
using AzureKeyVaultEmulator.Aspire.Hosting;

internal static class LocalGameArchiveHosting {
    public const string GitRemote = "http://127.0.0.1:3000/shuttle-bot/shl-games-archive.git";
    public const string SecretName = "shl-games-archive-git-password";

    public static IResourceBuilder<ExecutableResource> Add(
        IDistributedApplicationBuilder builder, IResourceBuilder<AzureKeyVaultResource> vault) {
        vault.RunAsEmulator().SeedWithSecret(SecretName, "testtest");
        var gitea = builder.AddContainer("archive-gitea", "gitea/gitea", "1.24.6")
            .WithContainerName("shuttle-archive-gitea")
            .WithVolume("shuttle-games-archive-gitea-data", "/data")
            .WithEnvironment("GITEA__security__INSTALL_LOCK", "true")
            .WithEnvironment("GITEA__service__DISABLE_REGISTRATION", "true")
            .WithEnvironment("GITEA__repository__DEFAULT_BRANCH", "main")
            .WithEnvironment("GITEA__server__ROOT_URL", "http://127.0.0.1:3000/")
            .WithEndpoint(port: 3000, targetPort: 3000, scheme: "http", name: "http", isProxied: false)
            .WithEndpoint("http", endpoint => endpoint.TargetHost = "127.0.0.1")
            .WithHttpHealthCheck("/api/healthz");

        return builder.AddExecutable("archive-gitea-bootstrap", "dotnet", AppContext.BaseDirectory,
                Path.Combine(AppContext.BaseDirectory, "Shuttle.Backend.Aspire.dll"), "--bootstrap-gitea")
            .WithEnvironment("GITEA_URL", gitea.GetEndpoint("http"))
            .WithEnvironment("GITEA_CONTAINER_NAME", "shuttle-archive-gitea")
            .WaitFor(gitea);
    }
}
