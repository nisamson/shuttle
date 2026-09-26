using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Shuttle.Models.Players;
using Shuttle.WebClient.Models;
using Shuttle.WebClient.Models.Options;
using Shuttle.WebClient.Pages.Players;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Tests;

public sealed class PlayerProfileTests : WebClientTestContext {
    public PlayerProfileTests() {
        Services.AddSingleton<IShuttleOptionsStorage>(new FakeOptionsStorage());
    }

    [Fact]
    public async Task Development_tab_loads_projection_cone_and_working_compare_link() {
        var root = Render(Profile(1001));
        var cut = root.FindComponent<PlayerProfile>();
        root.WaitForState(() => root.Markup.Contains("Aaron Frost"));

        SetActiveDevelopmentTab(cut);

        root.WaitForState(() => GetDevelopmentChart(cut)?.Data.Count == 4);

        var result = GetProjectionResult(cut);
        Assert.NotNull(result?.Projection);
        var similar = result!.Projection!.SimilarPlayers[0];
        Assert.NotEmpty(similar.Name);
        var compareUrl = Routes.Players.CompareWith([1001, similar.PlayerId]);
        var compareWith = typeof(PlayerProfile)
            .GetMethod("CompareWith", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await cut.InvokeAsync(() => compareWith.Invoke(cut.Instance, [similar.PlayerId]));
        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith(compareUrl, navigation.Uri);
        Assert.Single(GetTimelineChart(cut)!.Data);
        Assert.Contains("Development projection", root.Markup);
        Assert.Contains("Current tier:", root.Markup);
        Assert.Contains("Potential tier:", root.Markup);
        Assert.Contains("certainty", root.Markup);
    }

    [Fact]
    public void Tab_query_value_opens_and_loads_the_requested_tab() {
        var root = Render(Profile(1001, "development"));
        var cut = root.FindComponent<PlayerProfile>();

        root.WaitForState(() => GetDevelopmentChart(cut)?.Data.Count == 4);

        Assert.Equal("development", GetActiveTabId(cut));
    }

    [Fact]
    public void Changing_tabs_updates_the_url() {
        var root = Render(Profile(1001));
        var cut = root.FindComponent<PlayerProfile>();
        root.WaitForState(() => root.Markup.Contains("Aaron Frost"));

        SetActiveDevelopmentTab(cut);

        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.Contains("tab=development", navigation.Uri);
    }

    private static RenderFragment Profile(int playerId, string? tab = null) => builder => {
        builder.OpenComponent<FluentTooltipProvider>(0);
        builder.CloseComponent();
        builder.OpenComponent<PlayerProfile>(1);
        builder.AddAttribute(2, nameof(PlayerProfile.PlayerId), playerId);
        if (tab is not null) {
            builder.AddAttribute(3, nameof(PlayerProfile.Tab), tab);
        }
        builder.CloseComponent();
    };

    private static string GetActiveTabId(IRenderedComponent<PlayerProfile> cut) =>
        (string)typeof(PlayerProfile)
            .GetField("activeTabId", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(cut.Instance)!;

    private static AttributeChart? GetDevelopmentChart(IRenderedComponent<PlayerProfile> cut) =>
        (AttributeChart?)typeof(PlayerProfile)
            .GetField("developmentChart", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(cut.Instance);

    private static AttributeChart? GetTimelineChart(IRenderedComponent<PlayerProfile> cut) =>
        (AttributeChart?)typeof(PlayerProfile)
            .GetField("timelineChart", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(cut.Instance);

    private static PlayerDevelopmentProjectionResult? GetProjectionResult(
        IRenderedComponent<PlayerProfile> cut) =>
        (PlayerDevelopmentProjectionResult?)typeof(PlayerProfile)
            .GetField("projectionResult", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(cut.Instance);

    private static void SetActiveDevelopmentTab(IRenderedComponent<PlayerProfile> cut) {
        var type = typeof(PlayerProfile);
        var developmentTabId = (string)type
            .GetField("DevelopmentTabId", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;
        type.GetField("activeTabId", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(cut.Instance, developmentTabId);

        var onTabChanged = type.GetMethod("OnTabChangedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        cut.InvokeAsync(() => (Task)onTabChanged.Invoke(cut.Instance, null)!).GetAwaiter().GetResult();
        var stateHasChanged = typeof(ComponentBase)
            .GetMethod("StateHasChanged", BindingFlags.NonPublic | BindingFlags.Instance)!;
        cut.InvokeAsync(() => stateHasChanged.Invoke(cut.Instance, null)).GetAwaiter().GetResult();
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
