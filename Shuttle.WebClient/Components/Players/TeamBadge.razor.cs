using System.Globalization;
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

            var text = string.IsNullOrWhiteSpace(Team.TextColor)
                ? ContrastingText(Team.PrimaryColor)
                : Team.TextColor;

            return $"background-color:{Team.PrimaryColor};color:{text};" +
                   $"border:1px solid {Team.SecondaryColor};";
        }
    }

    // Picks black or white for legibility against the given background using the standard
    // luminance heuristic; falls back to white if the color can't be parsed.
    private static string ContrastingText(string hex) =>
        TryParseHex(hex, out var r, out var g, out var b) && (0.299 * r + 0.587 * g + 0.114 * b) > 150
            ? "#000000"
            : "#FFFFFF";

    private static bool TryParseHex(string hex, out int r, out int g, out int b) {
        r = g = b = 0;
        var value = hex.TrimStart('#');
        if (value.Length != 6) {
            return false;
        }

        return int.TryParse(value.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r)
               && int.TryParse(value.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)
               && int.TryParse(value.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
    }
}
