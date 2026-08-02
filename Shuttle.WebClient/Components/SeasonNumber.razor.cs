using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Shuttle.WebClient.Components;

public partial class SeasonNumber
{
    /// <summary>
    /// The season number to display. Rendered as "S{Season}" (e.g. S87).
    /// </summary>
    [Parameter]
    public int? Season { get; set; }

    /// <summary>
    /// Text rendered when <see cref="Season"/> is <c>null</c>.
    /// </summary>
    [Parameter]
    public string NullText { get; set; } = "—";

    /// <summary>
    /// Formats a season number as a string (e.g. "S87") for plain-text contexts.
    /// Returns <paramref name="nullText"/> when <paramref name="season"/> is <c>null</c>.
    /// </summary>
    public static string Format(int? season, string nullText = "—") =>
        season is { } s ? $"S{s.ToString(CultureInfo.InvariantCulture)}" : nullText;
}
