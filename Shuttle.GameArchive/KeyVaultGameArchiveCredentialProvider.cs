using Azure.Security.KeyVault.Secrets;

namespace Shuttle.GameArchive;

public interface IGameArchiveCredentialProvider {
    Task<string> GetPasswordAsync(string secretName, CancellationToken cancellationToken);
}

public sealed class KeyVaultGameArchiveCredentialProvider(SecretClient client) : IGameArchiveCredentialProvider {
    public async Task<string> GetPasswordAsync(string secretName, CancellationToken cancellationToken) {
        var response = await client.GetSecretAsync(secretName, cancellationToken: cancellationToken);
        return response.Value.Value;
    }
}
