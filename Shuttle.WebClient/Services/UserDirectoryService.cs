using System.Text.Json;
using Microsoft.JSInterop;
using Shuttle.Api.Client;
using Shuttle.Models.Users;

namespace Shuttle.WebClient.Services;

/// <summary>
/// Client-side user "directory" used to power username autocomplete without a server round trip per
/// keystroke. The full slim directory is fetched from the API <em>once</em> and cached both in
/// memory and in <c>localStorage</c> (with a TTL), then filtered locally.
/// </summary>
public interface IUserDirectoryService {
    /// <summary>
    /// Ensures the directory is loaded (from <c>localStorage</c> if fresh, otherwise the API) and
    /// cached. Safe to call repeatedly and concurrently; the fetch happens at most once.
    /// </summary>
    Task EnsureLoadedAsync(CancellationToken token = default);

    /// <summary>
    /// Returns up to <paramref name="limit"/> users whose username matches <paramref name="term"/>,
    /// case-insensitively. Prefix matches rank ahead of substring matches, then alphabetical by
    /// username. An empty <paramref name="term"/> returns the first <paramref name="limit"/> users.
    /// </summary>
    Task<IReadOnlyList<UserSuggestion>> Search(string? term, int limit = 10, CancellationToken token = default);
}

/// <inheritdoc cref="IUserDirectoryService"/>
public sealed class UserDirectoryService : IUserDirectoryService, IDisposable {
    private const string StorageKey = "shuttle-user-directory";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(6);

    private readonly IShuttleUserClient client;
    private readonly IJSRuntime js;
    private readonly ILogger<UserDirectoryService> logger;
    private readonly SemaphoreSlim loadLock = new(1, 1);

    private IReadOnlyList<UserSuggestion>? cache;

    public UserDirectoryService(
        IShuttleUserClient client,
        IJSRuntime js,
        ILogger<UserDirectoryService> logger) {
        this.client = client;
        this.js = js;
        this.logger = logger;
    }

    public async Task EnsureLoadedAsync(CancellationToken token = default) {
        if (cache is not null) {
            return;
        }

        await loadLock.WaitAsync(token);
        try {
            if (cache is not null) {
                return;
            }

            cache = await LoadFromStorageAsync(token) ?? await FetchAndStoreAsync(token);
        } finally {
            loadLock.Release();
        }
    }

    public async Task<IReadOnlyList<UserSuggestion>> Search(
        string? term,
        int limit = 10,
        CancellationToken token = default) {
        await EnsureLoadedAsync(token);

        var directory = cache ?? [];
        if (limit <= 0 || directory.Count == 0) {
            return [];
        }

        var trimmed = term?.Trim();
        if (string.IsNullOrEmpty(trimmed)) {
            return directory.Take(limit).ToList();
        }

        return directory
            .Select(u => (user: u, rank: MatchRank(u, trimmed)))
            .Where(x => x.rank >= 0)
            .OrderBy(x => x.rank)
            .ThenBy(x => x.user.Username, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(x => x.user)
            .ToList();
    }

    // Lower rank = better match. 0 = username prefix; 1 = username substring; -1 = none.
    private static int MatchRank(UserSuggestion u, string term) {
        var idx = u.Username.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        return idx switch {
            0 => 0,
            > 0 => 1,
            _ => -1,
        };
    }

    private async Task<IReadOnlyList<UserSuggestion>?> LoadFromStorageAsync(CancellationToken token) {
        try {
            var raw = await js.InvokeAsync<string?>("localStorage.getItem", token, StorageKey);
            if (string.IsNullOrEmpty(raw)) {
                return null;
            }

            var stored = JsonSerializer.Deserialize<CachedDirectory>(raw, ShuttleApiClientExtensions.JsonSerializerOptions);
            if (stored?.Users is null || DateTimeOffset.UtcNow - stored.FetchedAt > CacheTtl) {
                return null;
            }

            logger.LogDebug("Loaded {Count} user suggestions from local storage", stored.Users.Count);
            return stored.Users;
        } catch (Exception ex) {
            logger.LogWarning(ex, "Failed to read user directory from local storage; will refetch");
            return null;
        }
    }

    private async Task<IReadOnlyList<UserSuggestion>> FetchAndStoreAsync(CancellationToken token) {
        var directory = await client.GetUserSuggestions(token);
        logger.LogDebug("Fetched {Count} user suggestions from API", directory.Count);

        try {
            var payload = JsonSerializer.Serialize(
                new CachedDirectory(DateTimeOffset.UtcNow, directory),
                ShuttleApiClientExtensions.JsonSerializerOptions);
            await js.InvokeVoidAsync("localStorage.setItem", token, StorageKey, payload);
        } catch (Exception ex) {
            logger.LogWarning(ex, "Failed to persist user directory to local storage");
        }

        return directory;
    }

    public void Dispose() => loadLock.Dispose();

    private sealed record CachedDirectory(DateTimeOffset FetchedAt, IReadOnlyList<UserSuggestion> Users);
}
