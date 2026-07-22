using System.Reflection;
using System.Text.RegularExpressions;
using Plotly.Blazor;
using Plotly.Blazor.Traces;
using Plotly.Blazor.Traces.ScatterPolarLib;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.WebClient.Services;

/// <summary>
/// A single attribute chart (radar or bar) together with the pieces needed to render and re-theme
/// it in place: the current <see cref="Layout"/>, a <see cref="LayoutFactory"/> that produces a
/// freshly themed layout for the light/dark option, the trace <see cref="Data"/>, and the captured
/// <see cref="Chart"/> ref. Shared by the player profile (single series) and the comparison page
/// (one series per player overlaid on the same axes).
/// </summary>
public sealed class AttributeChart {
    public required string Title { get; init; }
    public Plotly.Blazor.Layout Layout { get; set; } = PlayerAttributeCharts.BuildLayout(false);

    /// <summary>
    /// Builds a fresh, themed layout for this chart. Used to re-theme in place (preserving the
    /// captured <see cref="Chart"/> ref and <see cref="Data"/>) when the light/dark option changes.
    /// </summary>
    public Func<bool, Plotly.Blazor.Layout>? LayoutFactory { get; init; }

    public IList<ITrace> Data { get; init; } = new List<ITrace>();
    public PlotlyChart? Chart { get; set; }
}

/// <summary>
/// The set of attribute charts for a player (or a comparison of players): the "all attributes"
/// <see cref="Combined"/> chart plus one <see cref="Categories"/> chart per attribute category.
/// </summary>
public sealed record AttributeChartSet(AttributeChart? Combined, IReadOnlyList<AttributeChart> Categories) {
    public static readonly AttributeChartSet Empty = new(null, Array.Empty<AttributeChart>());
}

/// <summary>A named series of attribute values contributed to an overlaid comparison chart.</summary>
public sealed record PlayerAttributeSeries(string Name, PlayerAttributes Attributes);

/// <summary>
/// Builds the radar/bar attribute charts shared by the player profile and comparison pages.
/// Attributes that are identical for every player (and so carry no comparative signal) are excluded,
/// categories with fewer than three axes fall back to a bar chart, and light/dark theming is applied
/// via the returned charts' <see cref="AttributeChart.LayoutFactory"/>.
/// </summary>
public static class PlayerAttributeCharts {
    /// <summary>True when the attributes describe a goaltender rather than a skater.</summary>
    public static bool IsGoaltender(PlayerAttributes attributes) => attributes is GoaltenderAttributes;

    /// <summary>Builds the combined + per-category charts for a single player.</summary>
    public static AttributeChartSet Build(PlayerAttributes attributes, bool dark) =>
        BuildOverlay(new[] { new PlayerAttributeSeries(string.Empty, attributes) }, dark);

    /// <summary>
    /// Builds the combined + per-category charts for one or more players overlaid on shared axes.
    /// All players are assumed to be the same type (skater or goaltender); the category set is taken
    /// from the first player. Each player contributes one trace per chart, named after the player.
    /// </summary>
    public static AttributeChartSet BuildOverlay(IReadOnlyList<PlayerAttributeSeries> players, bool dark) {
        if (players.Count == 0) {
            return AttributeChartSet.Empty;
        }

        var resolved = players
            .Select(p => new { p.Name, Values = GetAttributeValues(p.Attributes) })
            .ToList();

        var categories = IsGoaltender(players[0].Attributes) ? GoaltenderCategories : SkaterCategories;

        // The combined chart uses every (non-excluded) attribute in the first player's order.
        var allNames = resolved[0].Values.Select(v => v.Name).ToArray();
        var combined = CreateChart(
            "All attributes",
            resolved.Select(r => new ChartSeries(r.Name, SelectPoints(r.Values, allNames))).ToList(),
            dark);

        var categoryCharts = new List<AttributeChart>();
        foreach (var (title, propertyNames) in categories) {
            var series = resolved
                .Select(r => new ChartSeries(r.Name, SelectPoints(r.Values, propertyNames)))
                .ToList();

            var chart = CreateChart(title, series, dark);
            if (chart is not null) {
                categoryCharts.Add(chart);
            }
        }

        return new AttributeChartSet(combined, categoryCharts);
    }

    /// <summary>The excluded (no-signal) attribute labels for the given player type, for a hint.</summary>
    public static string ExcludedAttributeLabels(PlayerAttributes attributes) =>
        IsGoaltender(attributes) ? ExcludedGoaltenderAttributeLabelsText : ExcludedSkaterAttributeLabelsText;

    private sealed record ChartSeries(string Name, IReadOnlyList<(string Label, int Value)> Points);

    private static IReadOnlyList<(string Label, int Value)> SelectPoints(
        IReadOnlyList<(string Name, string Label, int Value)> values, IReadOnlyList<string> names) {
        var byName = values.ToDictionary(v => v.Name, StringComparer.Ordinal);
        return names
            .Where(byName.ContainsKey)
            .Select(name => (byName[name].Label, byName[name].Value))
            .ToList();
    }

    private static AttributeChart? CreateChart(string title, IReadOnlyList<ChartSeries> series, bool dark) {
        if (series.Count == 0 || series[0].Points.Count == 0) {
            return null;
        }

        string TraceName(ChartSeries s) => string.IsNullOrEmpty(s.Name) ? title : s.Name;

        // With fewer than three axes a radar collapses into a line, so use a bar chart instead.
        if (series[0].Points.Count < 3) {
            return new AttributeChart {
                Title = title,
                Layout = BuildBarLayout(dark),
                LayoutFactory = BuildBarLayout,
                Data = series
                    .Select(s => (ITrace)new Bar {
                        X = s.Points.Select(p => (object)p.Label).ToList(),
                        Y = s.Points.Select(p => (object)p.Value).ToList(),
                        Name = TraceName(s),
                    })
                    .ToList(),
            };
        }

        return new AttributeChart {
            Title = title,
            Layout = BuildLayout(dark),
            LayoutFactory = BuildLayout,
            Data = series
                .Select(s => {
                    var labels = s.Points.Select(p => (object)p.Label).ToList();
                    var magnitudes = s.Points.Select(p => (object)p.Value).ToList();

                    // Repeat the first point so the radar polygon closes.
                    labels.Add(s.Points[0].Label);
                    magnitudes.Add(s.Points[0].Value);

                    return (ITrace)new ScatterPolar {
                        R = magnitudes,
                        Theta = labels,
                        Fill = FillEnum.ToSelf,
                        Mode = ModeFlag.Lines | ModeFlag.Markers,
                        Name = TraceName(s),
                    };
                })
                .ToList(),
        };
    }

    private static IReadOnlyList<(string Name, string Label, int Value)> GetAttributeValues(PlayerAttributes attributes) =>
        attributes.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(int) && p.GetIndexParameters().Length == 0)
            .Where(p => attributes is not SkaterAttributes || !ExcludedSkaterAttributes.Contains(p.Name))
            .Where(p => attributes is not GoaltenderAttributes || !ExcludedGoaltenderAttributes.Contains(p.Name))
            .Select(p => (Name: p.Name, Label: SplitPascalCase(p.Name), Value: (int)p.GetValue(attributes)!))
            .ToList();

    private static readonly IReadOnlyList<(string Title, string[] Attributes)> SkaterCategories = new (string, string[])[] {
        ("Offense", new[] {
            nameof(SkaterAttributes.Screening), nameof(SkaterAttributes.GettingOpen), nameof(SkaterAttributes.Passing),
            nameof(SkaterAttributes.Puckhandling), nameof(SkaterAttributes.ShootingAccuracy), nameof(SkaterAttributes.ShootingRange),
            nameof(SkaterAttributes.OffensiveRead), nameof(SkaterAttributes.Faceoffs),
        }),
        ("Defense", new[] {
            nameof(SkaterAttributes.Checking), nameof(SkaterAttributes.Stickchecking), nameof(SkaterAttributes.Hitting),
            nameof(SkaterAttributes.Positioning), nameof(SkaterAttributes.ShotBlocking), nameof(SkaterAttributes.DefensiveRead),
        }),
        ("Physical", new[] {
            nameof(SkaterAttributes.Acceleration), nameof(SkaterAttributes.Agility), nameof(SkaterAttributes.Balance),
            nameof(SkaterAttributes.Speed), nameof(SkaterAttributes.Stamina), nameof(SkaterAttributes.Strength),
            nameof(SkaterAttributes.Fighting),
        }),
        ("Mental", new[] {
            nameof(SkaterAttributes.Aggression), nameof(SkaterAttributes.Bravery),
        }),
    };

    // Skater mental attributes we deliberately exclude from the attribute charts because
    // they are identical for every player and therefore carry no comparative signal.
    private static readonly string[] ExcludedSkaterAttributeNames = {
        nameof(SkaterAttributes.Determination), nameof(SkaterAttributes.TeamPlayer),
        nameof(SkaterAttributes.Leadership), nameof(SkaterAttributes.Temperament),
        nameof(SkaterAttributes.Professionalism),
    };

    private static readonly HashSet<string> ExcludedSkaterAttributes =
        new(ExcludedSkaterAttributeNames, StringComparer.Ordinal);

    private static readonly string ExcludedSkaterAttributeLabelsText =
        string.Join(", ", ExcludedSkaterAttributeNames.Select(SplitPascalCase));

    private static readonly IReadOnlyList<(string Title, string[] Attributes)> GoaltenderCategories = new (string, string[])[] {
        ("Technique", new[] {
            nameof(GoaltenderAttributes.Reflexes), nameof(GoaltenderAttributes.Positioning), nameof(GoaltenderAttributes.Blocker),
            nameof(GoaltenderAttributes.Glove), nameof(GoaltenderAttributes.LowShots), nameof(GoaltenderAttributes.Recovery),
            nameof(GoaltenderAttributes.Rebound), nameof(GoaltenderAttributes.PokeCheck),
        }),
        ("Athleticism & puck", new[] {
            nameof(GoaltenderAttributes.Skating), nameof(GoaltenderAttributes.Stamina), nameof(GoaltenderAttributes.Passing),
            nameof(GoaltenderAttributes.Puckhandling),
        }),
        ("Mental", new[] {
            nameof(GoaltenderAttributes.MentalToughness),
        }),
    };

    // Goaltender mental attributes we deliberately exclude from the attribute charts because
    // they are identical for every player and therefore carry no comparative signal.
    private static readonly string[] ExcludedGoaltenderAttributeNames = {
        nameof(GoaltenderAttributes.Determination), nameof(GoaltenderAttributes.TeamPlayer),
        nameof(GoaltenderAttributes.Leadership), nameof(GoaltenderAttributes.Professionalism),
        nameof(GoaltenderAttributes.Aggression),
    };

    private static readonly HashSet<string> ExcludedGoaltenderAttributes =
        new(ExcludedGoaltenderAttributeNames, StringComparer.Ordinal);

    private static readonly string ExcludedGoaltenderAttributeLabelsText =
        string.Join(", ", ExcludedGoaltenderAttributeNames.Select(SplitPascalCase));

    private static string SplitPascalCase(string name) =>
        Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])", " ");

    // Backgrounds are transparent so the chart inherits the (themed) page background;
    // only the font, axis, and grid colors are switched for light/dark.
    public static Plotly.Blazor.Layout BuildLayout(bool dark) {
        const string transparent = "rgba(0,0,0,0)";
        var foreground = dark ? "#e6e6e6" : "#242424";
        var grid = dark ? "rgba(255,255,255,0.16)" : "rgba(0,0,0,0.16)";

        return new Plotly.Blazor.Layout {
            AutoSize = true,
            PaperBgColor = transparent,
            PlotBgColor = transparent,
            Font = new Plotly.Blazor.LayoutLib.Font { Color = foreground },
            Polar = new List<Plotly.Blazor.LayoutLib.Polar> {
                new() {
                    BgColor = transparent,
                    RadialAxis = new Plotly.Blazor.LayoutLib.PolarLib.RadialAxis {
                        AutoRange = Plotly.Blazor.LayoutLib.PolarLib.RadialAxisLib.AutoRangeEnum.False,
                        Range = new List<object> { 0, 20 },
                        Color = foreground,
                        GridColor = grid,
                    },
                    AngularAxis = new Plotly.Blazor.LayoutLib.PolarLib.AngularAxis {
                        Color = foreground,
                        GridColor = grid,
                    },
                },
            },
        };
    }

    // Themed cartesian layout for the small-N categories rendered as bar charts.
    public static Plotly.Blazor.Layout BuildBarLayout(bool dark) {
        const string transparent = "rgba(0,0,0,0)";
        var foreground = dark ? "#e6e6e6" : "#242424";
        var grid = dark ? "rgba(255,255,255,0.16)" : "rgba(0,0,0,0.16)";

        return new Plotly.Blazor.Layout {
            AutoSize = true,
            PaperBgColor = transparent,
            PlotBgColor = transparent,
            Font = new Plotly.Blazor.LayoutLib.Font { Color = foreground },
            XAxis = new List<Plotly.Blazor.LayoutLib.XAxis> {
                new() { Color = foreground, GridColor = grid },
            },
            YAxis = new List<Plotly.Blazor.LayoutLib.YAxis> {
                new() {
                    AutoRange = Plotly.Blazor.LayoutLib.YAxisLib.AutoRangeEnum.False,
                    Range = new List<object> { 0, 20 },
                    Color = foreground,
                    GridColor = grid,
                },
            },
        };
    }

    // Themed cartesian layout for the TPE timeline: a date x-axis with an auto-ranged TPE y-axis.
    // Shared by the player profile (single series) and the comparison page (one series per player).
    public static Plotly.Blazor.Layout BuildTimelineLayout(bool dark) {
        const string transparent = "rgba(0,0,0,0)";
        var foreground = dark ? "#e6e6e6" : "#242424";
        var grid = dark ? "rgba(255,255,255,0.16)" : "rgba(0,0,0,0.16)";

        return new Plotly.Blazor.Layout {
            AutoSize = true,
            PaperBgColor = transparent,
            PlotBgColor = transparent,
            Font = new Plotly.Blazor.LayoutLib.Font { Color = foreground },
            XAxis = new List<Plotly.Blazor.LayoutLib.XAxis> {
                new() {
                    Type = Plotly.Blazor.LayoutLib.XAxisLib.TypeEnum.Date,
                    Color = foreground,
                    GridColor = grid,
                },
            },
            YAxis = new List<Plotly.Blazor.LayoutLib.YAxis> {
                new() { Color = foreground, GridColor = grid },
            },
        };
    }

    /// <summary>
    /// Builds the "Total TPE" timeline chart, overlaying one step-line series per player. When
    /// <paramref name="align"/> is set, every series is shifted so its first point lands on the
    /// earliest player's start date, aligning origins while preserving each player's own pace.
    /// Series with no points are dropped; returns <c>null</c> when nothing can be charted.
    /// </summary>
    public static AttributeChart? BuildTimelineOverlay(
        IReadOnlyList<(string Name, IReadOnlyList<Shuttle.Models.Players.TpeTimelinePoint> Points)> series,
        bool align, bool dark) {
        var valid = series.Where(s => s.Points is { Count: > 0 }).ToList();
        if (valid.Count == 0) {
            return null;
        }

        var earliestStart = valid.Min(s => s.Points[0].TaskDate);

        var data = new List<ITrace>();
        foreach (var (name, points) in valid) {
            var offset = align ? earliestStart - points[0].TaskDate : TimeSpan.Zero;
            data.Add(new Plotly.Blazor.Traces.Scatter {
                X = points.Select(p => (object)p.TaskDate.Add(offset)).ToList(),
                Y = points.Select(p => (object)p.TotalTpe).ToList(),
                Mode = Plotly.Blazor.Traces.ScatterLib.ModeFlag.Lines | Plotly.Blazor.Traces.ScatterLib.ModeFlag.Markers,
                // A step ("hv") line reflects that the cumulative total holds until the next event.
                Line = new Plotly.Blazor.Traces.ScatterLib.Line {
                    Shape = Plotly.Blazor.Traces.ScatterLib.LineLib.ShapeEnum.Hv,
                },
                Name = name,
            });
        }

        return new AttributeChart {
            Title = "Total TPE",
            Layout = BuildTimelineLayout(dark),
            LayoutFactory = BuildTimelineLayout,
            Data = data,
        };
    }
}
