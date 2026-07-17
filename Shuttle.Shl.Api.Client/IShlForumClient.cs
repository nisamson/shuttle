namespace Shuttle.Shl.Api.Client;

public interface IShlForumClient {
    public Task<string?> GetDiscordUsername(int userId, CancellationToken token = default);
}
