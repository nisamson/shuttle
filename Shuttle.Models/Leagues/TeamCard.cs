namespace Shuttle.Models.Leagues;

/// <summary>
/// Client-facing team display card returned by
/// <c>GET /leagues/{league}/teams/{teamId}</c>. Carries a team's identity and branding for the
/// season requested (or the most recent season on file when none is specified). Colors are
/// serialized as CSS-ready hex strings (e.g. <c>#1A2B3C</c>) rather than <c>System.Drawing.Color</c>
/// so the value deserializes on Blazor WebAssembly, where <c>System.Drawing.Common</c> is
/// unavailable.
/// </summary>
public record TeamCard {
    public required int TeamId { get; init; }
    public required int Season { get; init; }

    /// <summary>The league abbreviation (e.g. "SHL", "SMJHL").</summary>
    public required string League { get; init; }

    public required int LeagueId { get; init; }
    public required string Name { get; init; }
    public required string Abbreviation { get; init; }
    public required string Location { get; init; }

    /// <summary>Primary team color as a hex string (e.g. <c>#1A2B3C</c>).</summary>
    public required string PrimaryColor { get; init; }

    /// <summary>Secondary team color as a hex string (e.g. <c>#1A2B3C</c>).</summary>
    public required string SecondaryColor { get; init; }

    /// <summary>Preferred text/foreground color as a hex string, or <see langword="null"/> if unset.</summary>
    public string? TextColor { get; init; }
}
