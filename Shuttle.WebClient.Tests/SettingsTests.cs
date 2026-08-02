using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Api.Client;
using Shuttle.WebClient.Pages.Account;
using Shuttle.WebClient.Testing;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Rendering tests for the account <see cref="Settings"/> page. Uses the in-memory user client wired
/// to an authenticated fake identity so the page's <c>GET /users/me</c> load succeeds offline.
/// </summary>
public class SettingsTests : WebClientTestContext {
    private const string DefaultUserId = "00000000-0000-0000-0000-000000000042";

    public SettingsTests() {
        var authProvider = new FakeAuthenticationStateProvider(new FakeAuthOptions {
            IsAuthenticated = true,
            UserId = DefaultUserId,
        });
        // Override the anonymous client from the base context with an authenticated one so the
        // page's own /users/me call resolves an account.
        Services.AddSingleton<IShuttleUserClient>(new InMemoryShuttleUserClient(authProvider));
    }

    [Fact]
    public void Shows_the_current_account_id_and_username() {
        this.AddAuthorization().SetAuthorized("Test Scout");

        var cut = Render<Settings>();

        cut.WaitForState(() => cut.Markup.Contains("Save"));
        Assert.Contains(DefaultUserId, cut.Markup);
        // The default username is the object id's dashless "N" form.
        Assert.Contains(Guid.Parse(DefaultUserId).ToString("N"), cut.Markup);
    }
}
