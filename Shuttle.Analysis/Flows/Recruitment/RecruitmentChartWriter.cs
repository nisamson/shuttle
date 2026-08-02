using System.Globalization;
using ScottPlot;
using Shuttle.EFCore.Recruitment;

namespace Shuttle.Analysis.Flows.Recruitment;

/// <summary>
/// Renders recruitment bar graphs with ScottPlot: top-N recruiters by recruited members and by total
/// career TPE, plus a per-category breakdown. Output is PNG by default or SVG when requested.
/// </summary>
public static class RecruitmentChartWriter {

    /// <summary>A single labeled bar value.</summary>
    public readonly record struct BarItem(string Label, double Value);

    private const int Width = 1000;

    /// <summary>
    /// Renders a horizontal bar chart of the top <paramref name="top"/> items (already ordered) to
    /// <paramref name="path"/>. The largest value is drawn at the top.
    /// </summary>
    public static void RenderHorizontalBars(
        FileInfo path,
        string title,
        string valueAxisLabel,
        IReadOnlyList<BarItem> items,
        Color color
    ) {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(items);

        Plot plot = new();
        plot.Title(title);
        plot.XLabel(valueAxisLabel);

        var count = items.Count;
        var bars = new List<Bar>(count);
        var ticks = new List<Tick>(count);
        for (var i = 0; i < count; i++) {
            // Draw the first (largest) item at the top: highest position renders uppermost.
            var position = count - 1 - i;
            bars.Add(new Bar {
                Position = position,
                Value = items[i].Value,
                Orientation = Orientation.Horizontal,
                FillColor = color,
            });
            ticks.Add(new Tick(position, items[i].Label));
        }

        plot.Add.Bars(bars);
        plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericManual([.. ticks]);
        plot.Axes.Left.MajorTickStyle.Length = 0;
        plot.HideGrid();
        plot.Axes.Margins(left: 0, right: 0.15);

        Save(plot, path, height: Math.Max(300, count * 28 + 120));
    }

    /// <summary>Renders the per-category breakdown (recruited members per category), one colored bar each.</summary>
    public static void RenderCategoryBreakdown(FileInfo path, IReadOnlyList<RecruiterCategoryCount> summary) {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(summary);

        Plot plot = new();
        plot.Title("Recruited members by category");
        plot.YLabel("Recruited members");

        var ordered = summary.OrderByDescending(c => c.RecruitedUsers).ToList();
        var bars = new List<Bar>(ordered.Count);
        var ticks = new List<Tick>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++) {
            bars.Add(new Bar {
                Position = i,
                Value = ordered[i].RecruitedUsers,
                FillColor = CategoryColor(ordered[i].Category),
            });
            ticks.Add(new Tick(i, ordered[i].Category.ToString()));
        }

        plot.Add.Bars(bars);
        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual([.. ticks]);
        plot.Axes.Bottom.MajorTickStyle.Length = 0;
        plot.HideGrid();
        plot.Axes.Margins(bottom: 0);

        Save(plot, path, height: 500);
    }

    private static void Save(Plot plot, FileInfo path, int height) {
        path.Directory?.Create();
        var ext = path.Extension.TrimStart('.').ToLowerInvariant();
        if (ext == "svg") {
            plot.SaveSvg(path.FullName, Width, height);
        } else {
            plot.SavePng(path.FullName, Width, height);
        }
    }

    private static Color CategoryColor(RecruiterCategory category) => category switch {
        RecruiterCategory.Player => Colors.SteelBlue,
        RecruiterCategory.External => Colors.Orange,
        RecruiterCategory.Self => Colors.MediumSeaGreen,
        RecruiterCategory.None => Colors.Gray,
        _ => Colors.SteelBlue,
    };

    /// <summary>Formats a career-TPE value for an axis label.</summary>
    public static string FormatTpe(long tpe) => tpe.ToString("N0", CultureInfo.InvariantCulture);
}
