using System.Text.Json.Serialization;

namespace Shuttle.Shl.Api.Models.Index.V1;

public record GameResult(
    int? GameId,
    int Season,
    int League,
    [property: JsonConverter(typeof(ShlDateConverter))]
    DateOnly Date,
    int HomeTeam,
    int AwayTeam,
    int HomeScore,
    int AwayScore,
    string GameType,
    [property: JsonConverter(typeof(IntBoolJsonConverter))]
    bool Played,
    [property: JsonConverter(typeof(IntBoolJsonConverter))]
    bool Overtime,
    [property: JsonConverter(typeof(IntBoolJsonConverter))]
    bool Shootout,
    /// <summary>
    /// A 12-digit numeric identifier that encodes the game's key attributes. For seasons
    /// prior to 65 the data is unreliable, and the <see cref="GameType"/> field in
    /// particular may be missing; the slug is the authoritative source for those fields.
    /// <para>
    /// Layout (fixed width, zero-padded, left to right):
    /// <c>SS L T DDMM HH AA</c>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><c>SS</c> (2 digits) — season number, e.g. <c>65</c>.</description></item>
    ///   <item><description><c>L</c> (1 digit) — league id: 0 = SHL, 1 = SMJHL, 2 = IIHF, 3 = WJC (see <c>KnownLeague</c>).</description></item>
    ///   <item><description><c>T</c> (1 digit) — game type: 1 = Pre-Season, 2 = Regular Season, 3 = Playoffs (see <c>GameType</c>).</description></item>
    ///   <item><description><c>DDMM</c> (4 digits) — the in-game date at time of play, <b>day first, then month</b> (not month/day).</description></item>
    ///   <item><description><c>HH</c> (2 digits) — home team number within the league.</description></item>
    ///   <item><description><c>AA</c> (2 digits) — away team number within the league.</description></item>
    /// </list>
    /// <para>
    /// Example: <c>650211100407</c> = season 65, league 0 (SHL), type 2 (Regular Season),
    /// in-game date the 11th of month 10 (October 11), home team 4, away team 7.
    /// </para>
    /// <para>
    /// Verified against every schedule game across seasons 65, 70 and 75 for all four
    /// leagues and all three game types (0 mismatches over 4,909 games).
    /// </para>
    /// </summary>
    string Slug
) {
    public string ToString(string homeTeamName, string awayTeamName) =>
        $"{awayTeamName} @ {homeTeamName} on {Date}: {AwayScore}-{HomeScore} {(Overtime ? "OT" : Shootout ? "SO" : "")}";
}
