using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.Analysis;

/// <summary>
/// Parses a comma-separated list of shorthand position codes into a set of
/// <see cref="PlayerPosition"/> values used to filter the player export.
/// </summary>
/// <remarks>
/// Accepts the single codes understood by <see cref="PositionExtensions.TryFromString"/>
/// (<c>G, C, LW, RW, LD, RD</c>) plus two case-insensitive group aliases that expand to
/// multiple positions: <c>F</c> ⇒ all forwards (<c>C, LW, RW</c>) and <c>D</c> ⇒ all
/// defensemen (<c>LD, RD</c>). Tokens may be freely mixed and combined (e.g. <c>F,G</c>).
/// </remarks>
public static class PositionFilter {

    /// <summary>All valid tokens, for error messages.</summary>
    public const string ValidTokens = "G, C, LW, RW, LD, RD, F (forwards), D (defense)";

    private static readonly IReadOnlyDictionary<string, PlayerPosition[]> GroupAliases =
        new Dictionary<string, PlayerPosition[]>(StringComparer.OrdinalIgnoreCase) {
            ["f"] = [PlayerPosition.Center, PlayerPosition.LeftWing, PlayerPosition.RightWing],
            ["d"] = [PlayerPosition.LeftDefense, PlayerPosition.RightDefense],
        };

    /// <summary>
    /// Parses <paramref name="spec"/> into a de-duplicated set of positions.
    /// </summary>
    /// <param name="spec">
    /// The comma-separated position spec, or <c>null</c>/whitespace for no filter.
    /// </param>
    /// <param name="positions">
    /// On success, the parsed positions, or <c>null</c> when no filter was requested.
    /// </param>
    /// <param name="error">On failure, a human-readable description of the problem.</param>
    /// <returns><c>true</c> if parsing succeeded (including the no-filter case).</returns>
    public static bool TryParse(
        string? spec,
        out IReadOnlySet<PlayerPosition>? positions,
        out string? error
    ) {
        positions = null;
        error = null;

        if (string.IsNullOrWhiteSpace(spec)) {
            return true;
        }

        var result = new HashSet<PlayerPosition>();
        var invalid = new List<string>();

        foreach (var raw in spec.Split(',')) {
            var token = raw.Trim();
            if (token.Length == 0) {
                continue;
            }

            if (GroupAliases.TryGetValue(token, out var expanded)) {
                foreach (var position in expanded) {
                    result.Add(position);
                }
            } else if (PositionExtensions.TryFromString(token, out var single)) {
                result.Add(single);
            } else {
                invalid.Add(token);
            }
        }

        if (invalid.Count > 0) {
            error = $"Invalid position token(s): {string.Join(", ", invalid)}. Valid values: {ValidTokens}.";
            return false;
        }

        positions = result;
        return true;
    }
}
