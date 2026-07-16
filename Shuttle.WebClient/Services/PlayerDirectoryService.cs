using System.Text.Json;
using Microsoft.JSInterop;
using Shuttle.Api.Client;
using Shuttle.Models.Players;

namespace Shuttle.WebClient.Services;

/// <summary>
/// Client-side player "directory" used to power name/username autocomplete without a server round
/// trip per keystroke. The full slim directory is fetched from the API <em>once</em> and cached both
/// in memory and in <c>localStorage</c> (with a TTL), then filtered locally.
/// </summary>
public interface IPlayerDirectoryService {
    /// <summary>
    /// Ensures the directory is loaded (from <c>localStorage</c> if fresh, otherwise the API) and
    /// cached. Safe to call repeatedly and concurrently; the fetch happens at most once.
    /// </summary>
    Task EnsureLoadedAsync(CancellationToken token = default);

    /// <summary>
    /// Returns up to <paramref name="limit"/> players whose name or username matches
    /// <paramref name="term"/>, case-insensitively. Prefix matches rank ahead of substring matches,
    /// then alphabetical by name. An empty <paramref name="term"/> returns the first
    /// <paramref name="limit"/> players by name.
    /// </summary>
    Task<IReadOnlyList<PlayerSuggestion>> Search(string? term, int limit = 10, CancellationToken token = default);
}

/// <inheritdoc cref="IPlayerDirectoryService"/>
public sealed class PlayerDirectoryService : IPlayerDirectoryService, IDisposable {
    private const string StorageKey = "shuttle-player-directory";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(6);

    private readonly IShuttlePlayerClient client;
    private readonly IJSRuntime js;
    private readonly ILogger<PlayerDirectoryService> logger;
    private readonly SemaphoreSlim loadLock = new(1, 1);

    private IReadOnlyList<PlayerSuggestion>? cache;

    public PlayerDirectoryService(
        IShuttlePlayerClient client,
        IJSRuntime js,
        ILogger<PlayerDirectoryService> logger) {
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

    public async Task<IReadOnlyList<PlayerSuggestion>> Search(
        string? term,
        int limit = 10,
        CancellationToken token = default) {
        await EnsureLoadedAsync(token);

        var players = cache ?? [];
        if (limit <= 0 || players.Count == 0) {
            return [];
        }

        var trimmed = term?.Trim();
        if (string.IsNullOrEmpty(trimmed)) {
            return players.Take(limit).ToList();
        }

        return players
            .Select(p => (player: p, rank: MatchRank(p, trimmed)))
            .Where(x => x.rank >= 0)
            .OrderBy(x => x.rank)
            .ThenBy(x => x.player.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(x => x.player)
            .ToList();
    }

    // Lower rank = better match. 0/1 = name/username prefix; 2/3 = name/username substring; -1 = none.
    private static int MatchRank(PlayerSuggestion p, string term) {
        var nameIdx = p.Name.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        var userIdx = p.Username.IndexOf(term, StringComparison.OrdinalIgnoreCase);

        if (nameIdx == 0) {
            return 0;
        }

        if (userIdx == 0) {
            return 1;
        }

        if (nameIdx > 0) {
            return 2;
        }

        if (userIdx > 0) {
            return 3;
        }

        return -1;
    }

    private async Task<IReadOnlyList<PlayerSuggestion>?> LoadFromStorageAsync(CancellationToken token) {
        try {
            var raw = await js.InvokeAsync<string?>("localStorage.getItem", token, StorageKey);
            if (string.IsNullOrEmpty(raw)) {
                return null;
            }

            var stored = JsonSerializer.Deserialize<CachedDirectory>(raw, ShuttleApiClientExtensions.JsonSerializerOptions);
            if (stored?.Players is null || DateTimeOffset.UtcNow - stored.FetchedAt > CacheTtl) {
                return null;
            }

            logger.LogDebug("Loaded {Count} player suggestions from local storage", stored.Players.Count);
            return stored.Players;
        } catch (Exception ex) {
            logger.LogWarning(ex, "Failed to read player directory from local storage; will refetch");
            return null;
        }
    }

    private async Task<IReadOnlyList<PlayerSuggestion>> FetchAndStoreAsync(CancellationToken token) {
        var players = await client.GetPlayerSuggestions(token);
        logger.LogDebug("Fetched {Count} player suggestions from API", players.Count);

        try {
            var payload = JsonSerializer.Serialize(
                new CachedDirectory(DateTimeOffset.UtcNow, players),
                ShuttleApiClientExtensions.JsonSerializerOptions);
            await js.InvokeVoidAsync("localStorage.setItem", token, StorageKey, payload);
        } catch (Exception ex) {
            logger.LogWarning(ex, "Failed to persist player directory to local storage");
        }

        return players;
    }

    public void Dispose() => loadLock.Dispose();

    private sealed record CachedDirectory(DateTimeOffset FetchedAt, IReadOnlyList<PlayerSuggestion> Players);
}
