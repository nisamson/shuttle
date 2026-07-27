using Microsoft.Playwright;
using Shuttle.WebClient.Testing;

namespace Shuttle.WebClient.E2E;

/// <summary>
/// Playwright regression tests guarding that internal links perform Blazor client-side
/// navigation rather than a full document reload.
/// </summary>
/// <remarks>
/// A full page reload re-downloads and restarts the WASM app, wiping any window-scoped state.
/// These tests plant a sentinel on <c>window</c>, click an internal link, and assert the
/// sentinel survives — which only holds when the router intercepts the click (SPA navigation).
/// This is the regression guard for FluentUI issue #5022: <c>FluentLink</c> used for internal
/// navigation triggers a full reload, so internal links must render as plain anchors.
/// Requires Playwright browsers (see <see cref="PlayerBrowsingTests"/>).
/// </remarks>
[Collection(WebAppCollection.Name)]
public sealed class NavigationTests : IAsyncLifetime
{
    private const string SentinelScript = "() => { window.__spaSentinel = 'kept'; }";
    private const string ReadSentinelScript = "() => window.__spaSentinel";

    private readonly WebAppFixture app;
    private IPlaywright? playwright;
    private IBrowser? browser;

    public NavigationTests(WebAppFixture app) => this.app = app;

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
    public async Task Clicking_a_player_name_navigates_client_side_without_full_reload()
    {
        var page = await NewPageAsync();
        await page.GotoAsync("/players", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var firstPlayer = SeedData.Players()[0];
        var link = page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = firstPlayer.Name }).First;
        await link.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });

        // Plant a window-scoped sentinel. A client-side (SPA) navigation preserves it; a full
        // document reload restarts the app and wipes it.
        await page.EvaluateAsync(SentinelScript);

        await link.ClickAsync();

        await page.WaitForURLAsync($"**/players/{firstPlayer.PlayerId}",
            new PageWaitForURLOptions { Timeout = 30_000 });
        await page.GetByText(firstPlayer.Name).First.WaitForAsync(new LocatorWaitForOptions
        {
            Timeout = 30_000,
        });

        var sentinel = await page.EvaluateAsync<string?>(ReadSentinelScript);
        Assert.Equal("kept", sentinel);
    }
}
