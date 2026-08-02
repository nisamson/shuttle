using System.Net;
using Refit;
using Shuttle.Models.Users;
using Shuttle.WebClient.Testing;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Pure (no-render) tests for <see cref="InMemoryShuttleUserClient"/> proving its
/// lookup/search/paging behaviour — and its authenticated-only Discord gating — match what the
/// WebClient expects from the real API.
/// </summary>
public class InMemoryShuttleUserClientTests {
    private static InMemoryShuttleUserClient Authenticated() =>
        new(new FakeAuthenticationStateProvider(new FakeAuthOptions { IsAuthenticated = true }));

    private static InMemoryShuttleUserClient Anonymous() =>
        new(new FakeAuthenticationStateProvider(new FakeAuthOptions { IsAuthenticated = false }));

    // 5001 has a Discord name in the seed; used to exercise gating.
    private const int UserWithDiscord = 5001;

    [Fact]
    public async Task GetUser_by_id_returns_card() {
        var user = await Anonymous().GetUser(UserWithDiscord.ToString());

        Assert.NotNull(user);
        Assert.Equal(UserWithDiscord, user!.UserId);
    }

    [Fact]
    public async Task GetUser_by_username_is_case_insensitive() {
        var byId = await Anonymous().GetUser(UserWithDiscord.ToString());
        var byName = await Anonymous().GetUser(byId!.Username.ToUpperInvariant());

        Assert.NotNull(byName);
        Assert.Equal(byId.UserId, byName!.UserId);
    }

    [Fact]
    public async Task GetUser_returns_null_for_unknown() {
        Assert.Null(await Anonymous().GetUser("-1"));
        Assert.Null(await Anonymous().GetUser("nobody-here"));
    }

    [Fact]
    public async Task GetUser_omits_players_when_not_requested() {
        var user = await Authenticated().GetUser(UserWithDiscord.ToString(), players: false);

        Assert.NotNull(user);
        Assert.Null(user!.Players);
    }

    [Fact]
    public async Task GetUser_includes_players_newest_first_when_requested() {
        var user = await Authenticated().GetUser(UserWithDiscord.ToString(), players: true);

        Assert.NotNull(user!.Players);
        Assert.NotEmpty(user.Players!);
        var dates = user.Players!.Select(p => p.CreationDate).ToList();
        Assert.Equal(dates.OrderByDescending(d => d), dates);
        // 5001 owns two players in the seed, the later of which is a recreate.
        Assert.Contains(user.Players!, p => p.Recreate);
    }

    [Fact]
    public async Task GetUser_gates_discord_by_authentication() {
        var signedIn = await Authenticated().GetUser(UserWithDiscord.ToString());
        var anon = await Anonymous().GetUser(UserWithDiscord.ToString());

        Assert.False(string.IsNullOrWhiteSpace(signedIn!.DiscordName));
        Assert.Null(anon!.DiscordName);
    }

    [Fact]
    public async Task SearchUsers_never_returns_discord_when_anonymous() {
        var result = await Anonymous().SearchUsers(new UserSearchQuery { PageSize = 100 });

        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, u => Assert.Null(u.DiscordName));
    }

    [Fact]
    public async Task SearchUsers_filters_by_username() {
        var all = await Anonymous().SearchUsers(new UserSearchQuery { PageSize = 100 });
        var target = all.Items[0].Username;

        var result = await Anonymous().SearchUsers(new UserSearchQuery { Text = target, PageSize = 100 });

        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, u =>
            Assert.Contains(target, u.Username, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchUsers_only_matches_discord_when_authenticated_and_opted_in() {
        // 5002's Discord name ("bella.ridge") is distinct from their username ("bridge"), so an
        // anonymous username-only search cannot coincidentally match on it.
        const int userId = 5002;
        var user = await Authenticated().GetUser(userId.ToString());
        var discord = user!.DiscordName!;

        var authed = await Authenticated().SearchUsers(new UserSearchQuery {
            Text = discord,
            SearchDiscord = true,
            PageSize = 100,
        });
        var anon = await Anonymous().SearchUsers(new UserSearchQuery {
            Text = discord,
            SearchDiscord = true,
            PageSize = 100,
        });

        Assert.Contains(authed.Items, u => u.UserId == userId);
        // Anonymous callers can't match on Discord, so the discord-only term finds nothing.
        Assert.DoesNotContain(anon.Items, u => u.UserId == userId);
    }

    [Fact]
    public async Task SearchUsers_sorts_by_user_id_descending() {
        var result = await Anonymous().SearchUsers(new UserSearchQuery {
            SortBy = UserSortField.UserId,
            SortDescending = true,
            PageSize = 100,
        });

        var ids = result.Items.Select(u => u.UserId).ToList();
        Assert.Equal(ids.OrderByDescending(i => i), ids);
    }

    [Fact]
    public async Task SearchUsers_paginates_and_clamps_page_size() {
        var all = await Anonymous().SearchUsers(new UserSearchQuery { PageSize = 100 });
        var firstPage = await Anonymous().SearchUsers(new UserSearchQuery { Page = 1, PageSize = 3 });
        var clamped = await Anonymous().SearchUsers(new UserSearchQuery { PageSize = 10_000 });

        Assert.Equal(3, firstPage.PageSize);
        Assert.Equal(all.TotalCount, firstPage.TotalCount);
        Assert.Equal(Math.Min(3, all.TotalCount), firstPage.Items.Count);
        Assert.Equal(100, clamped.PageSize);
    }

    [Fact]
    public async Task GetUserSuggestions_returns_directory() {
        var suggestions = await Anonymous().GetUserSuggestions();

        Assert.NotEmpty(suggestions);
        Assert.All(suggestions, s => Assert.False(string.IsNullOrWhiteSpace(s.Username)));
    }

    [Fact]
    public async Task GetCurrentUser_creates_account_keyed_to_the_callers_object_id() {
        var oid = Guid.Parse("00000000-0000-0000-0000-0000000000aa");
        var client = new InMemoryShuttleUserClient(
            new FakeAuthenticationStateProvider(new FakeAuthOptions {
                IsAuthenticated = true,
                UserId = oid.ToString(),
            }));

        var me = await client.GetCurrentUser();

        Assert.Equal(oid, me.Id);
        // Default username mirrors the server: the id's "N" (dashless) form.
        Assert.Equal(oid.ToString("N"), me.Username);
    }

    [Fact]
    public async Task GetCurrentUser_throws_401_for_anonymous_callers() {
        var ex = await Assert.ThrowsAsync<ApiException>(() => Anonymous().GetCurrentUser());

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateCurrentUser_applies_a_valid_new_username() {
        var client = Authenticated();

        var updated = await client.UpdateCurrentUser(new UpdateCurrentUserRequest { Username = "new_name.01" });

        Assert.Equal("new_name.01", updated.Username);
        Assert.Equal(updated.Username, (await client.GetCurrentUser()).Username);
    }

    [Fact]
    public async Task UpdateCurrentUser_rejects_an_invalid_username_with_400() {
        var ex = await Assert.ThrowsAsync<ApiException>(
            () => Authenticated().UpdateCurrentUser(new UpdateCurrentUserRequest { Username = "no spaces!" }));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateCurrentUser_rejects_a_taken_username_with_409() {
        var taken = (await Anonymous().GetUserSuggestions())[0].Username;

        var ex = await Assert.ThrowsAsync<ApiException>(
            () => Authenticated().UpdateCurrentUser(new UpdateCurrentUserRequest { Username = taken }));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
    }
}
