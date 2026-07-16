using System.Security.Claims;
using Shuttle.WebClient.Testing;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Tests for <see cref="FakeAuthenticationStateProvider"/> — the offline auth used by the
/// WebClient's fake-backend run mode — verifying it produces role-checkable identities.
/// </summary>
public class FakeAuthenticationStateProviderTests {
    [Fact]
    public async Task Authenticated_user_has_configured_name_and_roles() {
        var provider = new FakeAuthenticationStateProvider(new FakeAuthOptions {
            UserName = "Scout McScout",
            Roles = { "Shuttle.Admin" },
        });

        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal("Scout McScout", state.User.Identity!.Name);
        Assert.True(state.User.IsInRole("Shuttle.Admin"));
    }

    [Fact]
    public async Task Anonymous_option_produces_unauthenticated_user() {
        var provider = new FakeAuthenticationStateProvider(new FakeAuthOptions {
            IsAuthenticated = false,
        });

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated ?? false);
    }

    [Fact]
    public async Task Roles_use_the_apps_role_claim_type() {
        var provider = new FakeAuthenticationStateProvider(new FakeAuthOptions {
            Roles = { "Shuttle.Admin" },
        });

        var state = await provider.GetAuthenticationStateAsync();

        Assert.Contains(
            state.User.Claims,
            c => c.Type == FakeAuthenticationStateProvider.RoleClaimType && c.Value == "Shuttle.Admin");
    }
}
