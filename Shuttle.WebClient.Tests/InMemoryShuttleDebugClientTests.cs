using Shuttle.WebClient.Testing;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Pure (no-render) tests for <see cref="InMemoryShuttleDebugClient"/> proving its server-roles view
/// reflects the (fake) caller's identity — mirroring the real <c>GET /debug/roles</c> endpoint.
/// </summary>
public class InMemoryShuttleDebugClientTests {
    [Fact]
    public async Task GetServerRoles_returns_the_authenticated_users_roles_sorted() {
        var provider = new FakeAuthenticationStateProvider(new FakeAuthOptions {
            IsAuthenticated = true,
            Roles = new List<string> { "Shuttle.Jobs.Admin", "Shuttle.Admin" },
        });
        var client = new InMemoryShuttleDebugClient(provider);

        var roles = await client.GetServerRoles();

        Assert.Equal(new[] { "Shuttle.Admin", "Shuttle.Jobs.Admin" }, roles);
    }

    [Fact]
    public async Task GetServerRoles_returns_empty_for_anonymous_callers() {
        var provider = new FakeAuthenticationStateProvider(new FakeAuthOptions { IsAuthenticated = false });
        var client = new InMemoryShuttleDebugClient(provider);

        Assert.Empty(await client.GetServerRoles());
    }

    [Fact]
    public async Task GetServerRoles_returns_empty_when_no_auth_provider() {
        var client = new InMemoryShuttleDebugClient();

        Assert.Empty(await client.GetServerRoles());
    }

    [Fact]
    public async Task GetDiscordUsername_returns_a_deterministic_username_for_a_valid_id() {
        var client = new InMemoryShuttleDebugClient();

        var result = await client.GetDiscordUsername(42);

        Assert.Equal(42, result.UserId);
        Assert.Equal("discorduser42", result.DiscordUsername);
    }

    [Fact]
    public async Task GetDiscordUsername_returns_null_username_for_a_non_positive_id() {
        var client = new InMemoryShuttleDebugClient();

        var result = await client.GetDiscordUsername(0);

        Assert.Equal(0, result.UserId);
        Assert.Null(result.DiscordUsername);
    }
}
