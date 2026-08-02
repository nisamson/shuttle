namespace Shuttle.Models.Leagues;

/// <summary>
/// The seasons of data available for a single league, returned by <c>GET /seasons</c>.
/// </summary>
/// <param name="League">The league abbreviation (e.g. "SHL", "SMJHL").</param>
/// <param name="Seasons">The available seasons for the league, ordered newest first.</param>
public record LeagueSeasons(string League, IReadOnlyList<int> Seasons);

/// <summary>
/// The most recent season of data available for a single league, returned by
/// <c>GET /seasons/current</c>.
/// </summary>
/// <param name="League">The league abbreviation (e.g. "SHL", "SMJHL").</param>
/// <param name="Season">The league's most recent available season.</param>
public record LeagueCurrentSeason(string League, int Season);
