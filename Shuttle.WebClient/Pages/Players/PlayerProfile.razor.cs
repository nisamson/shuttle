using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Plotly.Blazor;
using Plotly.Blazor.Traces;
using Plotly.Blazor.Traces.ScatterPolarLib;
using Refit;
using Shuttle.Api.Client;
using Shuttle.Models.Players;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;
using Shuttle.WebClient.Models;
using Shuttle.WebClient.Models.Options;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Pages.Players;

public partial class PlayerProfile : ComponentBase, IDisposable {
    [Parameter] public int PlayerId { get; set; }

    [Inject] private IShuttlePlayerClient PlayerClient { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IShuttleOptionsStorage OptionsStorage { get; set; } = null!;

    private bool darkMode;

    private void GoToSearch() => Navigation.NavigateTo(Routes.Players.Root);

    protected override void OnInitialized() {
        darkMode = OptionsStorage.CurrentOptions.DarkMode;
        OptionsStorage.OptionsChanged += OnOptionsChanged;
    }

    private void OnOptionsChanged(ShuttleOptions options) {
        if (options.DarkMode == darkMode) {
            return;
        }

        darkMode = options.DarkMode;
        RethemeCharts();
        InvokeAsync(StateHasChanged);
    }

    private void RethemeCharts() {
        if (card?.Attributes is not null) {
            BuildCharts(card);
        }

        needsRedraw = true;
    }

    public void Dispose() => OptionsStorage.OptionsChanged -= OnOptionsChanged;

    private PlayerCard? card;
    private bool loading;
    private bool notFound;
    private string? error;
    private string? jsonDataUrl;
    private string? jsonFileName;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
        WriteIndented = true,
    };

    private readonly Config chartConfig = new() { Responsive = true };
    private readonly List<AttributeChart> attributeCharts = new();
    private AttributeChart? combinedChart;
    private bool groupedView = true;
    private bool needsRedraw;

    protected override async Task OnParametersSetAsync() => await LoadAsync();

    private async Task LoadAsync() {
        loading = true;
        notFound = false;
        error = null;
        card = null;
        attributeCharts.Clear();
        combinedChart = null;
        jsonDataUrl = null;
        jsonFileName = null;

        try {
            card = await PlayerClient.GetPlayer(PlayerId);
            if (card is null) {
                notFound = true;
            } else {
                BuildDownload(card);
                if (card.Attributes is not null) {
                    BuildCharts(card);
                    needsRedraw = true;
                }
            }
        } catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
            notFound = true;
        } catch (Exception ex) {
            error = ex.Message;
        } finally {
            loading = false;
        }
    }

    private void OnViewChanged() => needsRedraw = true;

    private void BuildDownload(PlayerCard c) {
        var json = JsonSerializer.Serialize(c, JsonOptions);
        jsonDataUrl = "data:application/json;charset=utf-8," + Uri.EscapeDataString(json);
        jsonFileName = $"player-{c.PlayerId}.json";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (!needsRedraw) {
            return;
        }

        needsRedraw = false;

        // Only redraw the charts for the currently visible view; the hidden view's
        // components have been disposed, so their captured refs must not be touched.
        var visible = groupedView
            ? (IEnumerable<AttributeChart>)attributeCharts
            : combinedChart is null ? Array.Empty<AttributeChart>() : new[] { combinedChart };
        foreach (var group in visible) {
            if (group.Chart is null) {
                continue;
            }

            try {
                await group.Chart.React();
            } catch (ObjectDisposedException) {
                // The chart was disposed mid-toggle; the new view will redraw itself.
            }
        }
    }

    private void BuildCharts(PlayerCard c) {
        var attributes = c.Attributes!;
        var values = GetAttributeValues(attributes);
        var byName = values.ToDictionary(v => v.Name, StringComparer.Ordinal);
        var categories = attributes is GoaltenderAttributes ? GoaltenderCategories : SkaterCategories;

        attributeCharts.Clear();
        combinedChart = CreateChart("All attributes", values, darkMode);

        foreach (var (title, propertyNames) in categories) {
            var points = propertyNames
                .Where(byName.ContainsKey)
                .Select(name => byName[name])
                .ToList();

            var chart = CreateChart(title, points, darkMode);
            if (chart is not null) {
                attributeCharts.Add(chart);
            }
        }
    }

    private static AttributeChart? CreateChart(string title, IReadOnlyList<(string Name, string Label, int Value)> points, bool dark) {
        if (points.Count == 0) {
            return null;
        }

        // With fewer than three axes a radar collapses into a line, so use a bar chart instead.
        if (points.Count < 3) {
            return new AttributeChart {
                Title = title,
                Layout = BuildBarLayout(dark),
                Data = new List<ITrace> {
                    new Bar {
                        X = points.Select(p => (object)p.Label).ToList(),
                        Y = points.Select(p => (object)p.Value).ToList(),
                        Name = title,
                    },
                },
            };
        }

        var labels = points.Select(p => (object)p.Label).ToList();
        var magnitudes = points.Select(p => (object)p.Value).ToList();

        // Repeat the first point so the radar polygon closes.
        labels.Add(points[0].Label);
        magnitudes.Add(points[0].Value);

        return new AttributeChart {
            Title = title,
            Layout = BuildLayout(dark),
            Data = new List<ITrace> {
                new ScatterPolar {
                    R = magnitudes,
                    Theta = labels,
                    Fill = FillEnum.ToSelf,
                    Mode = ModeFlag.Lines | ModeFlag.Markers,
                    Name = title,
                },
            },
        };
    }

    private string? ExcludedAttributeLabels => card?.Attributes switch {
        SkaterAttributes => ExcludedSkaterAttributeLabels,
        GoaltenderAttributes => ExcludedGoaltenderAttributeLabels,
        _ => null,
    };

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

    private static readonly string ExcludedSkaterAttributeLabels =
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

    private static readonly string ExcludedGoaltenderAttributeLabels =
        string.Join(", ", ExcludedGoaltenderAttributeNames.Select(SplitPascalCase));

    private sealed class AttributeChart {
        public required string Title { get; init; }
        public Plotly.Blazor.Layout Layout { get; set; } = BuildLayout(false);
        public IList<ITrace> Data { get; init; } = new List<ITrace>();
        public PlotlyChart? Chart { get; set; }
    }

    // Backgrounds are transparent so the chart inherits the (themed) page background;
    // only the font, axis, and grid colors are switched for light/dark.
    private static Plotly.Blazor.Layout BuildLayout(bool dark) {
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
    private static Plotly.Blazor.Layout BuildBarLayout(bool dark) {
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

    private static string SplitPascalCase(string name) =>
        Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])", " ");

    private string PositionText => card?.Position.ToShortString() ?? string.Empty;

    private static readonly CultureInfo UsdCulture = CreateUsdCulture();

    private static CultureInfo CreateUsdCulture() {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.CurrencySymbol = "$";
        culture.NumberFormat.CurrencyPositivePattern = 0;
        culture.NumberFormat.CurrencyNegativePattern = 1;
        return culture;
    }

    private string HandednessText => card is null ? string.Empty : $"Shoots {card.Handedness.ToValueString()}";

    private BadgeColor StatusColor => card?.Status switch {
        PlayerStatus.Active => BadgeColor.Success,
        PlayerStatus.Retired => BadgeColor.Subtle,
        PlayerStatus.Pending => BadgeColor.Warning,
        PlayerStatus.Denied => BadgeColor.Danger,
        _ => BadgeColor.Subtle,
    };

    private string StatusText => card?.Status switch {
        PlayerStatus.Active => "Active",
        PlayerStatus.Retired => "Retired",
        PlayerStatus.Pending => "Pending",
        PlayerStatus.Denied => "Denied",
        _ => card?.Status.ToString() ?? string.Empty,
    };

    private string StatusTooltip => card?.Status switch {
        PlayerStatus.Active => "Current, non-retired player.",
        PlayerStatus.Retired => "This player has retired.",
        PlayerStatus.Pending => "Player creation is pending approval.",
        PlayerStatus.Denied => "Player creation was denied.",
        _ => string.Empty,
    };

    private IEnumerable<StatRow> Progression {
        get {
            if (card is null) {
                yield break;
            }

            yield return new StatRow("Total TPE", card.TotalTpe.ToString("N0", CultureInfo.InvariantCulture));
            yield return new StatRow("Applied TPE", card.AppliedTpe.ToString("N0", CultureInfo.InvariantCulture));

            var bankedNegative = card.BankedTpe < 0;
            yield return new StatRow(
                "Banked TPE",
                card.BankedTpe.ToString("N0", CultureInfo.InvariantCulture),
                Negative: bankedNegative,
                Tooltip: bankedNegative ? "Regression penalties have not been applied yet." : null,
                AnchorId: bankedNegative ? "banked-tpe-value" : null);

            yield return new StatRow("Bank balance", card.BankBalance.ToString("C0", UsdCulture));
        }
    }

    private sealed record StatRow(string Label, string Value, bool Negative = false, string? Tooltip = null, string? AnchorId = null);

    private IEnumerable<(string Label, string Value)> TeamInfo {
        get {
            if (card is null) {
                yield break;
            }

            yield return ("Current league", card.CurrentLeague?.Abbreviation ?? "—");
            yield return ("Current team", card.CurrentTeamId?.ToString() ?? "—");
            yield return ("SHL rights", card.ShlRightsTeamId?.ToString() ?? "—");
            yield return ("SMJHL rights", card.SmjhlRightsTeamId?.ToString() ?? "—");
            yield return ("Draft season", card.DraftSeason?.ToString() ?? "—");
        }
    }

    private IEnumerable<(string Label, string Value)> Details {
        get {
            if (card is null) {
                yield break;
            }

            yield return ("Handedness", card.Handedness.ToString());
            yield return ("Height", card.Height?.ToString() ?? "—");
            yield return ("Weight", card.Weight is int w ? $"{w} lbs" : "—");
            yield return ("Birthplace", string.IsNullOrWhiteSpace(card.Birthplace) ? "—" : card.Birthplace);
            yield return ("Nation", string.IsNullOrWhiteSpace(card.IihfNation) ? "—" : card.IihfNation);
            yield return ("Created", card.CreationDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            if (card.RetirementDate is DateTime retired) {
                yield return ("Retired", retired.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }
        }
    }
}
