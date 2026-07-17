using Shuttle.Api.Client;
using Shuttle.Models.Leagues;

namespace Shuttle.WebClient.Testing;

/// <summary>
/// In-memory <see cref="IShuttleLeagueClient"/> that serves <see cref="SeedData.Teams"/> without any
/// HTTP, backend, or Azure dependency. Mirrors the server's
/// <c>GET /leagues/{league}/teams/{teamId}</c> semantics closely enough that the WebClient behaves
/// identically against it: a team is matched by league abbreviation and id, returning the most
/// recent seeded season (the seed models a single current season).
/// </summary>
public sealed class InMemoryShuttleLeagueClient : IShuttleLeagueClient {
    private readonly IReadOnlyList<TeamCard> teams;

    /// <summary>Creates a client backed by the default <see cref="SeedData.Teams"/>.</summary>
    public InMemoryShuttleLeagueClient()
        : this(SeedData.Teams()) {
    }

    /// <summary>Creates a client backed by a caller-supplied team set (useful for focused tests).</summary>
    public InMemoryShuttleLeagueClient(IReadOnlyList<TeamCard> teams) {
        this.teams = teams;
    }

    public Task<TeamCard?> GetTeam(
        string league,
        int teamId,
        int? season = null,
        CancellationToken token = default) {
        var matches = teams
            .Where(t => string.Equals(t.League, league, StringComparison.OrdinalIgnoreCase)
                        && t.TeamId == teamId);

        if (season is { } requestedSeason) {
            matches = matches.Where(t => t.Season == requestedSeason);
        }

        var team = matches
            .OrderByDescending(t => t.Season)
            .FirstOrDefault();

        return Task.FromResult(team);
    }
}
