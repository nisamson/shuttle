using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using AzureKeyVaultEmulator.Aspire.Hosting;

namespace Shuttle.Tests.Api;

public sealed class LocalGameArchiveHostingTests {
    [Fact]
    public async Task EmulatorCallbacksPreserveDeveloperCredentialsAndScopeArchiveCredentials() {
        var builder = DistributedApplication.CreateBuilder([]);
        var vault = builder.AddAzureKeyVault("archive-vault").RunAsEmulator();
        var consumer = builder.AddExecutable("api", "dotnet", ".")
            .WithEnvironment("AZURE_TENANT_ID", "developer-tenant")
            .WithEnvironment("AZURE_CLIENT_ID", "developer-client")
            .WithEnvironment("AZURE_CLIENT_SECRET", "developer-test-secret")
            .WithEnvironment("AZURE_AUTHORITY_HOST", "https://login.microsoftonline.com/")
            .WithArchiveVaultEmulatorCredentials(vault);
        var endpoint = vault.Resource.Annotations.OfType<EndpointAnnotation>()
            .Single(annotation => annotation.UriScheme == "https");
        endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "127.0.0.1", 7443);
        var variables = new Dictionary<string, object>();
        var context = new EnvironmentCallbackContext(
            builder.ExecutionContext, consumer.Resource, variables, TestContext.Current.CancellationToken);

        foreach (var callback in consumer.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
            await callback.Callback(context);

        Assert.Equal("developer-tenant", variables["AZURE_TENANT_ID"]);
        Assert.Equal("developer-client", variables["AZURE_CLIENT_ID"]);
        Assert.Equal("developer-test-secret", variables["AZURE_CLIENT_SECRET"]);
        Assert.Equal("https://login.microsoftonline.com/", variables["AZURE_AUTHORITY_HOST"]);
        foreach (var name in new[] { "AZURE_TENANT_ID", "AZURE_CLIENT_ID", "AZURE_CLIENT_SECRET", "AZURE_AUTHORITY_HOST" })
            Assert.Contains($"Archive__Emulator__{name}", variables.Keys);
        Assert.Equal("https://127.0.0.1:7443", variables["Archive__Emulator__AZURE_AUTHORITY_HOST"]);
    }
}
