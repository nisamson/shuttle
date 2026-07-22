using Plotly.Blazor.Traces;
using Shuttle.Models.Players;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Unit tests for the shared <see cref="PlayerAttributeCharts"/> builder used by both the player
/// profile and the comparison page. These are pure (no bUnit/JS) and assert the overlay semantics:
/// one trace per player, radar vs. bar selection by axis count, the closing radar point, the
/// category set per player type, and single-player naming.
/// </summary>
public class PlayerAttributeChartsTests {
    private static SkaterAttributes Skater(int fill = 10) =>
        new(fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill,
            fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill);

    private static GoaltenderAttributes Goaltender(int fill = 10) =>
        new(fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill, fill,
            fill, fill, fill, fill);

    [Fact]
    public void IsGoaltender_distinguishes_the_two_attribute_types() {
        Assert.False(PlayerAttributeCharts.IsGoaltender(Skater()));
        Assert.True(PlayerAttributeCharts.IsGoaltender(Goaltender()));
    }

    [Fact]
    public void BuildOverlay_with_no_players_returns_empty() {
        var set = PlayerAttributeCharts.BuildOverlay(Array.Empty<PlayerAttributeSeries>(), dark: false);

        Assert.Null(set.Combined);
        Assert.Empty(set.Categories);
    }

    [Fact]
    public void BuildOverlay_creates_one_trace_per_skater_named_after_the_player() {
        var series = new[] {
            new PlayerAttributeSeries("Aaron Frost", Skater(8)),
            new PlayerAttributeSeries("Bella Ice", Skater(14)),
        };

        var set = PlayerAttributeCharts.BuildOverlay(series, dark: false);

        Assert.NotNull(set.Combined);
        Assert.Equal(2, set.Combined!.Data.Count);
        var names = set.Combined.Data.Select(TraceName).ToArray();
        Assert.Contains("Aaron Frost", names);
        Assert.Contains("Bella Ice", names);

        // Every category chart also carries one trace per player.
        Assert.All(set.Categories, c => Assert.Equal(2, c.Data.Count));
    }

    [Fact]
    public void BuildOverlay_uses_the_skater_category_set() {
        var set = PlayerAttributeCharts.BuildOverlay(
            new[] { new PlayerAttributeSeries("A", Skater()) }, dark: false);

        var titles = set.Categories.Select(c => c.Title).ToArray();
        Assert.Equal(new[] { "Offense", "Defense", "Physical", "Mental" }, titles);
    }

    [Fact]
    public void BuildOverlay_uses_the_goaltender_category_set() {
        var set = PlayerAttributeCharts.BuildOverlay(
            new[] { new PlayerAttributeSeries("G", Goaltender()) }, dark: false);

        var titles = set.Categories.Select(c => c.Title).ToArray();
        Assert.Equal(new[] { "Technique", "Athleticism & puck", "Mental" }, titles);
    }

    [Fact]
    public void BuildOverlay_renders_large_categories_as_radar_and_small_ones_as_bar() {
        var set = PlayerAttributeCharts.BuildOverlay(
            new[] { new PlayerAttributeSeries("A", Skater()) }, dark: false);

        // Offense has 8 axes -> radar (ScatterPolar); Mental has 2 axes -> bar.
        var offense = set.Categories.Single(c => c.Title == "Offense");
        var mental = set.Categories.Single(c => c.Title == "Mental");

        Assert.All(offense.Data, t => Assert.IsType<ScatterPolar>(t));
        Assert.All(mental.Data, t => Assert.IsType<Bar>(t));
    }

    [Fact]
    public void BuildOverlay_closes_the_radar_polygon_by_repeating_the_first_point() {
        var set = PlayerAttributeCharts.BuildOverlay(
            new[] { new PlayerAttributeSeries("A", Skater()) }, dark: false);

        var offense = (ScatterPolar)set.Categories.Single(c => c.Title == "Offense").Data[0];

        // 8 offensive axes, plus one repeated point to close the polygon.
        Assert.Equal(9, offense.R!.Count);
        Assert.Equal(9, offense.Theta!.Count);
        Assert.Equal(offense.R[0], offense.R[^1]);
        Assert.Equal(offense.Theta[0], offense.Theta[^1]);
    }

    [Fact]
    public void Build_single_player_names_the_trace_after_the_chart_title() {
        var set = PlayerAttributeCharts.Build(Skater(), dark: false);

        Assert.NotNull(set.Combined);
        Assert.Single(set.Combined!.Data);
        Assert.Equal("All attributes", TraceName(set.Combined.Data[0]));
    }

    [Fact]
    public void BuildTimelineOverlay_overlays_one_series_per_player_with_points() {
        var start = new DateTime(2021, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var series = new List<(string, IReadOnlyList<TpeTimelinePoint>)> {
            ("Alice", Points(start, (0, 10), (30, 40))),
            ("Bob", Points(start.AddDays(5), (0, 5), (30, 25))),
            ("Empty", Array.Empty<TpeTimelinePoint>()),
        };

        var chart = PlayerAttributeCharts.BuildTimelineOverlay(series, align: false, dark: false);

        Assert.NotNull(chart);
        // The empty series is dropped; the two with points are overlaid as Scatter traces.
        var scatters = chart!.Data.OfType<Scatter>().ToList();
        Assert.Equal(2, scatters.Count);
        Assert.Equal(new[] { "Alice", "Bob" }, scatters.Select(s => s.Name));
    }

    [Fact]
    public void BuildTimelineOverlay_returns_null_when_no_player_has_points() {
        var series = new List<(string, IReadOnlyList<TpeTimelinePoint>)> {
            ("Empty", Array.Empty<TpeTimelinePoint>()),
        };

        Assert.Null(PlayerAttributeCharts.BuildTimelineOverlay(series, align: false, dark: false));
    }

    [Fact]
    public void BuildTimelineOverlay_align_shifts_every_series_to_a_common_start() {
        var start = new DateTime(2021, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var series = new List<(string, IReadOnlyList<TpeTimelinePoint>)> {
            ("Alice", Points(start, (0, 10), (30, 40))),
            ("Bob", Points(start.AddDays(5), (0, 5), (30, 25))),
        };

        var unaligned = PlayerAttributeCharts.BuildTimelineOverlay(series, align: false, dark: false)!;
        var starts = unaligned.Data.OfType<Scatter>().Select(s => (DateTime)s.X![0]).Distinct().ToList();
        Assert.Equal(2, starts.Count);

        var aligned = PlayerAttributeCharts.BuildTimelineOverlay(series, align: true, dark: false)!;
        var alignedStarts = aligned.Data.OfType<Scatter>().Select(s => (DateTime)s.X![0]).Distinct().ToList();
        Assert.Single(alignedStarts);
        Assert.Equal(start, alignedStarts[0]);
        // Alignment shifts origins only; each series keeps its own span (30 days here).
        var bob = aligned.Data.OfType<Scatter>().Single(s => s.Name == "Bob");
        Assert.Equal(start.AddDays(30), (DateTime)bob.X![^1]);
    }

    private static IReadOnlyList<TpeTimelinePoint> Points(
        DateTime start, params (int DayOffset, int TotalTpe)[] points) =>
        points.Select(p => new TpeTimelinePoint {
            TaskDate = start.AddDays(p.DayOffset),
            TotalTpe = p.TotalTpe,
        }).ToList();

    private static string TraceName(Plotly.Blazor.ITrace trace) => trace switch {
        ScatterPolar s => s.Name!,
        Bar b => b.Name!,
        _ => throw new InvalidOperationException($"Unexpected trace type {trace.GetType().Name}"),
    };
}
