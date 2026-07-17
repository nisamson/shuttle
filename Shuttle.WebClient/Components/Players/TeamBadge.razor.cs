using System.Globalization;
using System.Drawing;
using AccessibleColors;
using Microsoft.AspNetCore.Components;
using Shuttle.Models.Leagues;

namespace Shuttle.WebClient.Components.Players;

/// <summary>
/// Renders a team as a colored badge showing the team abbreviation in the team's own colors, with
/// the full team name surfaced as a tooltip. When <see cref="Team"/> has not been resolved the raw
/// <see cref="TeamId"/> is shown as a plain outline badge; when neither is available an em dash is
/// rendered.
/// </summary>
public partial class TeamBadge : ComponentBase {
    /// <summary>The resolved team to render, or <see langword="null"/> when unavailable.</summary>
    [Parameter] public TeamCard? Team { get; set; }

    /// <summary>
    /// The team id, used as a fallback label when <see cref="Team"/> could not be resolved.
    /// </summary>
    [Parameter] public int? TeamId { get; set; }

    // A stable, HTML-id-safe anchor so each badge's tooltip targets its own element.
    private readonly string AnchorId = $"team-badge-{Guid.NewGuid():N}";

    private string Tooltip => Team is null
        ? string.Empty
        : $"{Team.Name} · {Team.League}";

    private string BadgeStyle {
        get {
            if (Team is null) {
                return string.Empty;
            }

            return $"background-color:{Team.PrimaryColor};color:{ResolveTextColor(Team)};" +
                   $"border:1px solid {Team.SecondaryColor};";
        }
    }

    // Picks the badge's text color: an explicit team text color always wins. Otherwise the
    // secondary color is used, but only when it is legible against the primary background —
    // when the secondary color would be low-contrast we fall back to accessible black/white.
    private static string ResolveTextColor(TeamCard team) {
        if (!string.IsNullOrWhiteSpace(team.TextColor)) {
            return team.TextColor;
        }

        if (TryParseHex(team.PrimaryColor, out var background)
            && TryParseHex(team.SecondaryColor, out var secondary)) {
            return WcagContrastColor.IsCompliant(background, secondary)
                ? team.SecondaryColor
                : ToHex(background.GetContrastColor());
        }

        return team.SecondaryColor;
    }

    private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static bool TryParseHex(string hex, out Color color) {
        color = Color.Black;
        var value = hex.AsSpan().TrimStart('#');
        if (value.Length != 6) {
            return false;
        }

        if (int.TryParse(value[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)
            && int.TryParse(value[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)
            && int.TryParse(value[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b)) {
            color = Color.FromArgb(r, g, b);
            return true;
        }

        return false;
    }
}
