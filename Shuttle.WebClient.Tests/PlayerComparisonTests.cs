using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Api.Client;
using Shuttle.Models.Players;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;
using Shuttle.WebClient.Models;
using Shuttle.WebClient.Models.Options;
using Shuttle.WebClient.Pages.Players;
using Shuttle.WebClient.Services;
using Shuttle.WebClient.Testing;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Render tests for the standalone <see cref="PlayerComparison"/> page. The base
/// <see cref="WebClientTestContext"/> seeds attribute-free players, so this context overrides the
/// in-memory client with skater/goaltender cards that actually carry attributes (the only way the
/// page charts anything), and supplies a no-JS <see cref="IShuttleOptionsStorage"/>.
/// The <c>ids</c> query is bound directly through the <see cref="PlayerComparison.Ids"/> parameter.
/// </summary>
public class PlayerComparisonTests : WebClientTestContext {
    private const int GoaltenderId = 1050;

    public PlayerComparisonTests() {
        // Last registration wins, so this replaces the seed client for both the page and the
        // PlayerDirectoryService-backed autocomplete.
        Services.AddSingleton<IShuttlePlayerClient>(new InMemoryShuttlePlayerClient(TestPlayers()));
        Services.AddSingleton<IShuttleOptionsStorage>(new FakeOptionsStorage());
    }

    private static IReadOnlyList<PlayerCard> TestPlayers() {
        var cards = new List<PlayerCard>();
        for (var i = 0; i < 7; i++) {
            cards.Add(SkaterCard(1001 + i, $"Skater {(char)('A' + i)}", 8 + i));
        }

        cards.Add(GoaltenderCard(GoaltenderId, "Gordie Goalie"));
        return cards;
    }

    private static PlayerCard SkaterCard(int id, string name, int fill) => BaseCard(id, name) with {
        Position = PlayerPosition.Center,
        Attributes = new SkaterAttributes(
            fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill,
            fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill),
    };

    private static PlayerCard GoaltenderCard(int id, string name) => BaseCard(id, name) with {
        Position = PlayerPosition.Goalie,
        Attributes = new GoaltenderAttributes(
            10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10),
    };

    private static PlayerCard BaseCard(int id, string name) => new() {
        PlayerId = id,
        UserId = id,
        Username = name.Replace(" ", string.Empty).ToLowerInvariant(),
        Name = name,
        Status = PlayerStatus.Active,
        Position = PlayerPosition.Center,
        Handedness = PlayerHandedness.Left,
        TotalTpe = 1000,
        AppliedTpe = 1000,
        BankedTpe = 0,
        BankBalance = 0,
    };

    // A SupplyParameterFromQuery parameter must be supplied through the NavigationManager, not as a
    // component parameter, so navigate to the compare URL first and then render.
    private IRenderedComponent<PlayerComparison> RenderCompare(params int[] ids) {
        var url = ids.Length == 0 ? Routes.Players.Compare : Routes.Players.CompareWith(ids);
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo(url);
        return Render<PlayerComparison>();
    }

    [Fact]
    public void Empty_selection_prompts_the_user_to_add_players() {
        var cut = RenderCompare();

        cut.WaitForState(() => cut.Markup.Contains("Add two or more players"));
        Assert.Contains("Add two or more players", cut.Markup);
    }

    [Fact]
    public void Two_skaters_render_the_skater_comparison_with_a_chip_per_player() {
        var cut = RenderCompare(1001, 1002);

        cut.WaitForState(() => cut.Markup.Contains("Skater attributes"));
        Assert.Contains("Skater attributes", cut.Markup);
        Assert.Contains("Skater A", cut.Markup);
        Assert.Contains("Skater B", cut.Markup);
        // Each chip links to the player's profile.
        Assert.Contains(Routes.Players.Player(1001), cut.Markup);
        Assert.Contains(Routes.Players.Player(1002), cut.Markup);
    }

    [Fact]
    public void Unknown_ids_surface_a_not_found_warning_without_crashing() {
        var cut = RenderCompare(1001, 9999);

        cut.WaitForState(() => cut.Markup.Contains("couldn't be found"));
        Assert.Contains("couldn't be found", cut.Markup);
        Assert.Contains("9999", cut.Markup);
        // The valid player still charts.
        Assert.Contains("Skater attributes", cut.Markup);
    }

    [Fact]
    public void Mixing_a_goaltender_into_a_skater_comparison_excludes_the_goaltender() {
        var cut = RenderCompare(1001, GoaltenderId);

        cut.WaitForState(() => cut.Markup.Contains("Skater attributes"));
        Assert.Contains("aren't charted", cut.Markup);
        Assert.Contains("Goaltender — excluded", cut.Markup);
        // The skater still drives a skater comparison.
        Assert.Contains("Skater attributes", cut.Markup);
    }

    [Fact]
    public void More_than_six_charted_players_shows_the_soft_cap_hint() {
        var cut = RenderCompare(1001, 1002, 1003, 1004, 1005, 1006, 1007);

        cut.WaitForState(() => cut.Markup.Contains("Skater attributes"));
        Assert.Contains("easier to read with", cut.Markup);
    }

    [Fact]
    public void At_the_soft_cap_the_add_box_is_disabled() {
        var cut = RenderCompare(1001, 1002, 1003, 1004, 1005, 1006);

        cut.WaitForState(() => cut.Markup.Contains("Skater attributes"));
        Assert.Contains("holds up to", cut.Markup);
    }

    [Fact]
    public void Removing_a_player_navigates_to_the_url_without_that_player() {
        var cut = RenderCompare(1001, 1002);
        cut.WaitForState(() => cut.Markup.Contains("Skater attributes"));

        // The first remove button belongs to the first chip (Skater A / 1001).
        var removeButtons = cut.FindAll("fluent-button[title^='Remove']");
        Assert.NotEmpty(removeButtons);
        removeButtons[0].Click();

        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        Assert.EndsWith(Routes.Players.CompareWith(new[] { 1002 }), nav.Uri);
    }

    [Fact]
    public void Clear_all_navigates_back_to_the_bare_compare_route() {
        var cut = RenderCompare(1001, 1002);
        cut.WaitForState(() => cut.Markup.Contains("Skater attributes"));

        cut.Find("fluent-button:contains('Clear all')").Click();

        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        Assert.EndsWith(Routes.Players.Compare, nav.Uri);
    }

    private sealed class FakeOptionsStorage : IShuttleOptionsStorage {
        public ShuttleOptions CurrentOptions => ShuttleOptions.Default;
        public event Action<ShuttleOptions>? OptionsChanged { add { } remove { } }

        public Task<ShuttleOptions> LoadOptions(bool forceLoad, CancellationToken token = default) =>
            Task.FromResult(CurrentOptions);

        public Task SaveOptions(ShuttleOptions options, CancellationToken token = default) =>
            Task.CompletedTask;
    }
}
