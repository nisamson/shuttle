using Microsoft.Extensions.Logging.Abstractions;
using Shuttle.Api.Client;
using Shuttle.Models.Players;
using Shuttle.Models.Users;
using Shuttle.WebClient.Services;
using Shuttle.WebClient.Testing;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Tests for <see cref="CurrentUserService"/>: it hits <c>GET /users/me</c> to initialize the
/// caller's account on login, caches per identity, invalidates on sign-out, and re-fetches when a
/// different user signs in.
/// </summary>
public class CurrentUserServiceTests {
    private static CurrentUserService Create(
        CountingUserClient client, FakeAuthenticationStateProvider auth) =>
        new(client, auth, NullLogger<CurrentUserService>.Instance);

    private static FakeAuthenticationStateProvider Auth(bool authenticated, string userId = "user-1") =>
        new(new FakeAuthOptions {
            IsAuthenticated = authenticated,
            UserId = userId,
            Roles = new List<string>(),
        });

    [Fact]
    public async Task EnsureInitializedAsync_hits_users_me_for_a_signed_in_caller() {
        var client = new CountingUserClient();
        var service = Create(client, Auth(authenticated: true));

        await service.EnsureInitializedAsync();

        Assert.Equal(1, client.Calls);
    }

    [Fact]
    public async Task EnsureInitializedAsync_does_not_call_the_api_when_anonymous() {
        var client = new CountingUserClient();
        var service = Create(client, Auth(authenticated: false));

        await service.EnsureInitializedAsync();

        Assert.Equal(0, client.Calls);
    }

    [Fact]
    public async Task GetAsync_fetches_once_and_caches_for_the_same_identity() {
        var client = new CountingUserClient();
        var service = Create(client, Auth(authenticated: true));

        var first = await service.GetAsync();
        var second = await service.GetAsync();

        Assert.NotNull(first);
        Assert.Same(first, second);
        Assert.Equal(1, client.Calls);
    }

    [Fact]
    public async Task Signing_out_clears_the_cached_account() {
        var client = new CountingUserClient();
        var auth = Auth(authenticated: true);
        var service = Create(client, auth);

        Assert.NotNull(await service.GetAsync());

        auth.SetUser(o => o.IsAuthenticated = false);

        Assert.Null(await service.GetAsync());
        Assert.Equal(1, client.Calls);
    }

    [Fact]
    public async Task A_new_identity_triggers_a_fresh_fetch() {
        var client = new CountingUserClient();
        var auth = Auth(authenticated: true, userId: "user-1");
        var service = Create(client, auth);

        await service.GetAsync();

        auth.SetUser(o => o.UserId = "user-2");

        var reloaded = await service.GetAsync();
        Assert.NotNull(reloaded);
        Assert.Equal(2, client.Calls);
    }

    [Fact]
    public void Signing_in_during_the_session_initializes_the_account_via_the_auth_event() {
        var client = new CountingUserClient();
        var auth = Auth(authenticated: false);
        // Subscribes to AuthenticationStateChanged in its constructor.
        _ = Create(client, auth);
        Assert.Equal(0, client.Calls);

        auth.SetUser(o => o.IsAuthenticated = true);

        Assert.Equal(1, client.Calls);
    }

    /// <summary>An <see cref="IShuttleUserClient"/> that counts <c>GET /users/me</c> calls.</summary>
    private sealed class CountingUserClient : IShuttleUserClient {
        public int Calls { get; private set; }

        public Task<CurrentUser> GetCurrentUser(CancellationToken token = default) {
            Calls++;
            return Task.FromResult(new CurrentUser { Id = Guid.NewGuid(), Username = "tester" });
        }

        public Task<UserCard?> GetUser(string userIdOrName, bool players = false, CancellationToken token = default) =>
            throw new NotSupportedException();

        public Task<PagedResult<UserCard>> SearchUsers(UserSearchQuery query, CancellationToken token = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<UserSuggestion>> GetUserSuggestions(CancellationToken token = default) =>
            throw new NotSupportedException();

        public Task<CurrentUser> UpdateCurrentUser(UpdateCurrentUserRequest request, CancellationToken token = default) =>
            throw new NotSupportedException();
    }
}
