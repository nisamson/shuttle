using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Authorization;
using Refit;
using Shuttle.Api.Client;
using Shuttle.Models.Players;
using Shuttle.Models.Users;

namespace Shuttle.WebClient.Testing;

/// <summary>
/// In-memory <see cref="IShuttleUserClient"/> that serves <see cref="SeedData"/> users without any
/// HTTP, backend, or Azure dependency. Mirrors the server's <c>UserController</c> semantics closely
/// enough that the WebClient behaves identically against it: Discord names are only surfaced when the
/// (fake) caller is authenticated, and player cards follow the same null-vs-empty contract.
/// </summary>
public sealed partial class InMemoryShuttleUserClient : IShuttleUserClient {
    private const int MaxPageSize = 100;

    private readonly IReadOnlyList<PlayerCard> players;
    private readonly IReadOnlyList<SeedUser> users;
    private readonly AuthenticationStateProvider? authProvider;
    private CurrentUser? currentUser;

    /// <summary>Creates a client backed by the default <see cref="SeedData"/>.</summary>
    public InMemoryShuttleUserClient(AuthenticationStateProvider? authProvider = null)
        : this(SeedData.Players(), SeedData.Users(), authProvider) {
    }

    /// <summary>Creates a client backed by caller-supplied data (useful for focused tests).</summary>
    public InMemoryShuttleUserClient(
        IReadOnlyList<PlayerCard> players,
        IReadOnlyList<SeedUser> users,
        AuthenticationStateProvider? authProvider = null) {
        this.players = players;
        this.users = users;
        this.authProvider = authProvider;
    }

    public async Task<UserCard?> GetUser(
        string userIdOrName,
        bool players = false,
        CancellationToken token = default) {
        var includeDiscord = await IsAuthenticatedAsync();

        var user = int.TryParse(userIdOrName, out var id)
            ? users.FirstOrDefault(u => u.UserId == id)
            : users.FirstOrDefault(u => string.Equals(u.Username, userIdOrName, StringComparison.OrdinalIgnoreCase));

        if (user is null) {
            return null;
        }

        IReadOnlyList<PlayerCard>? playerCards = null;
        if (players) {
            playerCards = this.players
                .Where(p => p.UserId == user.UserId)
                .OrderByDescending(p => p.CreationDate)
                .ThenByDescending(p => p.PlayerId)
                .ToList();
        }

        return new UserCard {
            UserId = user.UserId,
            Username = user.Username,
            DiscordName = includeDiscord ? user.DiscordName : null,
            Players = playerCards,
        };
    }

    public async Task<PagedResult<UserCard>> SearchUsers(
        UserSearchQuery query,
        CancellationToken token = default) {
        var includeDiscord = await IsAuthenticatedAsync();
        var searchDiscord = includeDiscord && query.SearchDiscord;

        IEnumerable<SeedUser> filtered = users;
        if (!string.IsNullOrWhiteSpace(query.Text)) {
            var text = query.Text.Trim();
            filtered = filtered.Where(u =>
                u.Username.Contains(text, StringComparison.OrdinalIgnoreCase)
                || (searchDiscord && u.DiscordName is not null
                    && u.DiscordName.Contains(text, StringComparison.OrdinalIgnoreCase)));
        }

        var matched = filtered.ToList();
        var totalCount = matched.Count;

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var items = ApplySort(matched, query)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserCard {
                UserId = u.UserId,
                Username = u.Username,
                DiscordName = includeDiscord ? u.DiscordName : null,
                Players = null,
            })
            .ToList();

        return new PagedResult<UserCard> {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public Task<IReadOnlyList<UserSuggestion>> GetUserSuggestions(CancellationToken token = default) =>
        Task.FromResult<IReadOnlyList<UserSuggestion>>(
            users.Select(u => new UserSuggestion { UserId = u.UserId, Username = u.Username }).ToList());

    public async Task<CurrentUser> GetCurrentUser(CancellationToken token = default) {
        await EnsureAuthenticatedAsync();
        return currentUser ??= await BuildDefaultCurrentUserAsync();
    }

    public async Task<CurrentUser> UpdateCurrentUser(
        UpdateCurrentUserRequest request,
        CancellationToken token = default) {
        await EnsureAuthenticatedAsync();
        var existing = currentUser ??= await BuildDefaultCurrentUserAsync();

        var username = request.Username;
        if (!UsernameRegex().IsMatch(username)) {
            throw await ApiError(HttpMethod.Put, HttpStatusCode.BadRequest);
        }

        // Mirror the server's unique-username contract: reject a name already used by a seeded user.
        if (users.Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase))) {
            throw await ApiError(HttpMethod.Put, HttpStatusCode.Conflict);
        }

        currentUser = existing with { Username = username };
        return currentUser;
    }

    // Derives the caller's account from the (fake) identity: a stable id from the oid claim and a
    // default username of the id's "N" form, matching the server's get-or-create behaviour.
    private async Task<CurrentUser> BuildDefaultCurrentUserAsync() {
        var id = Guid.NewGuid();
        if (authProvider is not null) {
            var state = await authProvider.GetAuthenticationStateAsync();
            var oid = state.User.FindFirst("oid")?.Value;
            if (Guid.TryParse(oid, out var parsed)) {
                id = parsed;
            }
        }

        return new CurrentUser { Id = id, Username = id.ToString("N") };
    }

    private async Task EnsureAuthenticatedAsync() {
        if (!await IsAuthenticatedAsync()) {
            throw await ApiError(HttpMethod.Get, HttpStatusCode.Unauthorized);
        }
    }

    private static async Task<ApiException> ApiError(HttpMethod method, HttpStatusCode status) {
        using var request = new HttpRequestMessage(method, "/users/me");
        using var response = new HttpResponseMessage(status);
        return await ApiException.Create(request, method, response, new RefitSettings());
    }

    [GeneratedRegex("^[A-Za-z0-9._]{2,32}$")]
    private static partial Regex UsernameRegex();

    // UserId is a stable tiebreaker so paging is deterministic, matching the server.
    private static IEnumerable<SeedUser> ApplySort(IEnumerable<SeedUser> source, UserSearchQuery query) {
        var desc = query.SortDescending;

        return query.SortBy switch {
            UserSortField.UserId => OrderBy(source, u => u.UserId, desc),
            UserSortField.DiscordName => OrderBy(source, u => u.DiscordName ?? string.Empty, desc),
            _ => OrderBy(source, u => u.Username, desc),
        };
    }

    private static IEnumerable<SeedUser> OrderBy<TKey>(
        IEnumerable<SeedUser> source,
        Func<SeedUser, TKey> keySelector,
        bool descending) =>
        (descending ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector))
        .ThenBy(u => u.UserId);

    private async Task<bool> IsAuthenticatedAsync() {
        if (authProvider is null) {
            return false;
        }

        var state = await authProvider.GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated == true;
    }
}
