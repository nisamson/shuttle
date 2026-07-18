using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Microsoft.Extensions.Logging;
using StreamRegex.Extensions;
using StreamRegex.Extensions.RegexExtensions;

namespace Shuttle.Shl.Api.Client.Forums;

/// <summary>
/// Default <see cref="IShlForumClient"/> implementation that scrapes a member's forum profile page.
/// The forum has no public API for profile fields, so this downloads the rendered HTML and matches
/// the "Discord: &lt;username&gt;" text with <see cref="DiscordName"/>. To keep memory bounded on
/// large pages, the response is read as a stream into a pooled, fixed-size buffer and matched with
/// the streaming regex extensions rather than materializing the whole document as a string.
/// </summary>
public partial class ShlForumClient : IShlForumClient {
    private readonly ILogger<ShlForumClient> logger;
    private readonly HttpClient httpClient;

    private const string BaseUrl = "https://simulationhockey.com/";

    // Upper bound on how much of a profile page we buffer and scan (1 MiB); pages larger than this
    // are truncated to this many bytes before matching.
    private const int MaxPageSize = 1024 * 1024; // 1MiB

    public ShlForumClient(ILogger<ShlForumClient> logger, HttpClient httpClient) {
        this.logger = logger;
        this.httpClient = httpClient;
    }

    /// <summary>Builds the absolute URL of a member's public forum profile page.</summary>
    private Uri GetMemberProfilePageUri(int userId) {
        return new(BaseUrl + $"/member.php?action=profile&user={userId}");
    }

    /// <summary>
    /// Matches the forum's "Discord: &lt;username&gt;" profile field, capturing the username (2-32
    /// characters limited to the Discord-allowed set of lowercase letters, digits, <c>_</c> and
    /// <c>.</c>) in the <c>username</c> group. Case-insensitive so the "Discord:" label matches
    /// regardless of casing.
    /// </summary>
    [GeneratedRegex(@"Discord: (?<username>[a-z0-9_.]{2,32})", RegexOptions.IgnoreCase)]
    public static partial Regex DiscordName();

    /// <inheritdoc />
    public async Task<string?> GetDiscordUsername(int userId, CancellationToken token = default) {
        var pageUri = GetMemberProfilePageUri(userId);
        logger.LogInformation("Getting profile page for user  {userId}", userId);
        using var resp = await httpClient.GetAsync(pageUri, HttpCompletionOption.ResponseHeadersRead, token);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(token);
        byte[]? buffer = null;
        try {
            // Read the page into a pooled, fixed-size buffer and scan it as a stream so we never
            // hold the whole (potentially large) HTML document in a managed string.
            buffer = ArrayPool<byte>.Shared.Rent(MaxPageSize);
            Array.Clear(buffer);
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

            // The streaming match only yields the matched text, not its capture groups, so re-run the
            // regex over that small matched substring to pull out the "username" group.
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
