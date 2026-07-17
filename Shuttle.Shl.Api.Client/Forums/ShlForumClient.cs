using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Microsoft.Extensions.Logging;
using StreamRegex.Extensions;
using StreamRegex.Extensions.RegexExtensions;

namespace Shuttle.Shl.Api.Client.Forums;

public partial class ShlForumClient : IShlForumClient {
    private readonly ILogger<ShlForumClient> logger;
    private readonly HttpClient httpClient;

    private const string BaseUrl = "https://simulationhockey.com/";

    private const int MaxPageSize = 1024 * 1024; // 1MiB
    
    public ShlForumClient(ILogger<ShlForumClient> logger, HttpClient httpClient) {
        this.logger = logger;
        this.httpClient = httpClient;
    }

    private Uri GetMemberProfilePageUri(int userId) {
        return new(BaseUrl + $"/member.php?action=profile&user={userId}");
    }

    [GeneratedRegex(@"Discord: (?<username>[a-z0-9_.]{2,32})", RegexOptions.IgnoreCase)]
    public static partial Regex DiscordName();

    public async Task<string?> GetDiscordUsername(int userId, CancellationToken token = default) {
        var pageUri = GetMemberProfilePageUri(userId);
        logger.LogInformation("Getting profile page for user  {userId}", userId);
        using var resp = await httpClient.GetAsync(pageUri, HttpCompletionOption.ResponseHeadersRead, token);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(token);
        byte[]? buffer = null;
        try {
            buffer = ArrayPool<byte>.Shared.Rent(MaxPageSize);
            using var streamBuffer = new MemoryStream(buffer, 0, MaxPageSize);
            await stream.CopyToAsync(streamBuffer, token);
            var match = await DiscordName()
                .GetFirstMatchAsync(
                    streamBuffer
                );

            if (!match.Success) {
                logger.LogInformation("No match found for user {userId}", userId);
                return null;
            }

            var contents = match.Value;

            if (contents is null) {
                logger.LogInformation("No match found for user {userId}", userId);
                logger.LogWarning("Stream regex returned null string on good match");
                return null;
            }

            var reMatch = DiscordName().Match(contents);
            if (!reMatch.Success) {
                logger.LogInformation("No match found for user {userId}", userId);
                logger.LogWarning("Stream regex returned bad result on good match");
                return null;
            }

            logger.LogInformation("Successfully found match");
            return reMatch.Groups["username"].Value;
        } finally {
            if (buffer is not null) {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
