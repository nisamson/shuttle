using Bunit;
using Bunit.TestDoubles;
using Shuttle.WebClient.Layout;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Auth-gated rendering tests for <see cref="LoginDisplay"/>. Uses bUnit's test authorization to
/// flip between signed-in and anonymous states with no MSAL / Entra dependency.
/// </summary>
public class LoginDisplayTests : WebClientTestContext {
    [Fact]
    public void Shows_user_and_logout_when_authenticated() {
        var auth = this.AddAuthorization();
        auth.SetAuthorized("Test Scout");

        var cut = Render<LoginDisplay>();

        Assert.Contains("Log out", cut.Markup);
    }

    [Fact]
    public void Shows_login_link_when_anonymous() {
        this.AddAuthorization(); // defaults to not-authorized

        var cut = Render<LoginDisplay>();

        Assert.Contains("Log in", cut.Markup);
        Assert.DoesNotContain("Log out", cut.Markup);
    }
}
