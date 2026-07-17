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

            // Prefer the team's explicit text color; otherwise fall back to the secondary color,
            // which is also used for the outline.
            var text = string.IsNullOrWhiteSpace(Team.TextColor)
                ? Team.SecondaryColor
                : Team.TextColor;

            return $"background-color:{Team.PrimaryColor};color:{text};" +
                   $"border:1px solid {Team.SecondaryColor};";
        }
    }
}
