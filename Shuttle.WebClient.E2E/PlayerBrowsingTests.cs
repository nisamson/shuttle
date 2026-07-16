using Microsoft.Playwright;
using Shuttle.WebClient.Testing;

namespace Shuttle.WebClient.E2E;

/// <summary>
/// Playwright smoke tests that drive the real rendered WebClient in its offline fake-backend
/// run mode. No Azure sign-in and no live <c>Shuttle.Api</c> are required: the app serves the
/// deterministic <see cref="Testing.SeedData"/> players and a fake authenticated user.
/// </summary>
/// <remarks>
/// Requires Playwright browsers to be installed once:
/// <c>pwsh Shuttle.WebClient.E2E/bin/Debug/net10.0/playwright.ps1 install chromium</c>.
/// If Chromium is unavailable the browser launch throws and the test fails with Playwright's
/// install instructions.
/// </remarks>
[Collection(WebAppCollection.Name)]
public sealed class PlayerBrowsingTests : IAsyncLifetime
{
    private readonly WebAppFixture app;
    private IPlaywright? playwright;
    private IBrowser? browser;

    public PlayerBrowsingTests(WebAppFixture app) => this.app = app;

    public async ValueTask InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (browser is not null)
        {
            await browser.DisposeAsync();
        }

        playwright?.Dispose();
    }

    private async Task<IPage> NewPageAsync()
    {
        var context = await browser!.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = app.BaseUrl,
        });
        return await context.NewPageAsync();
    }

    [Fact]
    public async Task Players_page_lists_seeded_players()
    {
        var page = await NewPageAsync();
        await page.GotoAsync("/players", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // The first seeded player (SeedData is ordered by name); proves the fake backend served
        // data offline. Sourced from the shared seed so the assertion tracks the data.
        var firstPlayer = SeedData.Players()[0];
        await page.GetByText(firstPlayer.Name).First.WaitForAsync(new LocatorWaitForOptions
        {
            Timeout = 30_000,
        });

        Assert.Contains("Players", await page.TitleAsync());
    }

    [Fact]
    public async Task Player_profile_is_reachable_directly()
    {
        var page = await NewPageAsync();
        var firstPlayer = SeedData.Players()[0];
        await page.GotoAsync($"/players/{firstPlayer.PlayerId}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        await page.GetByText(firstPlayer.Name).First.WaitForAsync(new LocatorWaitForOptions
        {
            Timeout = 30_000,
        });
    }

    [Fact]
    public async Task Fake_user_appears_signed_in()
    {
        var page = await NewPageAsync();
        await page.GotoAsync("/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // The fake auth provider signs in a deterministic admin user, so ShuttleNavMenu renders its
        // authorized "Logout" item (and the role-gated "Admin" category) rather than the anonymous
        // "Login" item — proving auth works with no Entra / MSAL round trip. FluentUI emits the label
        // into hidden tooltip/aria nodes too, so wait for the element to be attached (present in the
        // DOM) rather than visible, then assert on presence counts.
        var logout = page.GetByText("Logout");
        await logout.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = 30_000,
        });

        Assert.True(await logout.CountAsync() > 0, "Expected an authorized 'Logout' nav item.");
        Assert.Equal(0, await page.GetByText("Login", new PageGetByTextOptions { Exact = true }).CountAsync());
    }
}
