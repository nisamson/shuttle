using Microsoft.Playwright;
using Shuttle.WebClient.Testing;

namespace Shuttle.WebClient.E2E;

/// <summary>
/// Playwright smoke tests for the standalone player comparison page, driven against the real
/// rendered WebClient in its offline fake-backend run mode. The shared <see cref="Testing.SeedData"/>
/// players carry no in-game attributes, so the page charts nothing; these tests assert the route is
/// reachable, the URL selection is honoured (a chip per requested player), and the empty-attribute
/// path degrades gracefully rather than crashing.
/// </summary>
[Collection(WebAppCollection.Name)]
public sealed class PlayerComparisonTests : IAsyncLifetime
{
    private readonly WebAppFixture app;
    private IPlaywright? playwright;
    private IBrowser? browser;

    public PlayerComparisonTests(WebAppFixture app) => this.app = app;

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
    public async Task Comparison_page_is_reachable_and_honours_the_url_selection()
    {
        var page = await NewPageAsync();
        var players = SeedData.Players();
        var first = players[0];
        var second = players[1];

        await page.GotoAsync($"/players/compare?ids={first.PlayerId},{second.PlayerId}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // A chip is rendered per requested player, proving the ?ids= query was parsed and resolved
        // through the offline directory/client.
        await page.GetByText(first.Name).First.WaitForAsync(new LocatorWaitForOptions
        {
            Timeout = 30_000,
        });

        Assert.True(await page.GetByText(second.Name).CountAsync() > 0,
            "Expected a chip for the second requested player.");
        Assert.Contains("Compare", await page.TitleAsync());
    }

    [Fact]
    public async Task Empty_comparison_prompts_the_user_to_add_players()
    {
        var page = await NewPageAsync();

        await page.GotoAsync("/players/compare",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        await page.GetByText("Add two or more players").First.WaitForAsync(new LocatorWaitForOptions
        {
            Timeout = 30_000,
        });
    }
}
