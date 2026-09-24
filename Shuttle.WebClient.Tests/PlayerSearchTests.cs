using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Models.Players;
using Shuttle.WebClient.Components.Players;
using Shuttle.WebClient.Pages.Players;

namespace Shuttle.WebClient.Tests;

/// <summary>Render tests for the player search page's URL-backed filter state.</summary>
public class PlayerSearchTests : WebClientTestContext {
    [Fact]
    public async Task Searching_with_multiple_filters_keeps_them_selected() {
        var cut = Render<PlayerSearch>();
        cut.WaitForState(() => cut.Markup.Contains("Players"));

        var filters = cut.FindComponent<PlayerSearchFilters>();
        await cut.InvokeAsync(() => filters.Instance.OnSearch.InvokeAsync(new PlayerSearchQuery {
            PlayerIds = [1001, 1002],
            Positions = ["C", "LW"],
        }));

        var navigation = Services.GetRequiredService<NavigationManager>();
        cut.WaitForAssertion(() => {
            Assert.Contains("PlayerIds=1001", navigation.Uri);
            Assert.Contains("PlayerIds=1002", navigation.Uri);
            Assert.Contains("pos=C", navigation.Uri);
            Assert.Contains("pos=LW", navigation.Uri);
            Assert.Contains("Aaron Frost", cut.Markup);
            Assert.Contains("Bella Ridge", cut.Markup);
            Assert.Contains("C", cut.Markup);
            Assert.Contains("LW", cut.Markup);
        });
    }
}
