using Refit;
using Shuttle.Models.Leagues;

namespace Shuttle.Api.Client;

/// <summary>
/// Typed Refit client for the Shuttle backend API (<c>Shuttle.Api</c>) league/team endpoints. The
/// base address is supplied at registration time (see
/// <see cref="ShuttleApiClientExtensions.AddShuttleLeagueClient"/>).
/// </summary>
public interface IShuttleLeagueClient {
    /// <summary>
    /// Fetches a single team's identity and branding within <paramref name="league"/>. When
    /// <paramref name="season"/> is omitted the team's most recent season (its current branding) is
    /// returned. Returns <see langword="null"/> when the league or team is unknown (HTTP 404).
    /// </summary>
    /// <param name="league">The league abbreviation (e.g. "SHL", "SMJHL").</param>
    /// <param name="teamId">The team id within that league.</param>
    /// <param name="season">Optional season; defaults to the team's most recent season.</param>
    /// <param name="token">A cancellation token.</param>
    [Get("/leagues/{league}/teams/{teamId}")]
    Task<TeamCard?> GetTeam(string league, int teamId, [Query] int? season = null, CancellationToken token = default);
}
