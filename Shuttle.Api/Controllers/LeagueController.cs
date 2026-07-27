using System.Drawing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shuttle.Api.Services;
using Shuttle.EFCore;
using Shuttle.Models.Leagues;
using Shuttle.Shl.Api.Models.Common;
using TeamEntity = Shuttle.EFCore.Entities.Team;

namespace Shuttle.Api.Controllers;

/// <summary>
/// Public, unauthenticated read access to league metadata, such as which seasons of data are
/// available for each league.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("leagues")]
[Route("league")]
public class LeagueController : ControllerBase {
    private static readonly TimeSpan CacheMaxAge = TimeSpan.FromHours(1);

    private readonly ShlDbContext db;
    private readonly IDatabaseFreshnessProvider freshness;
    private readonly ILogger<LeagueController> logger;

    public LeagueController(
        ShlDbContext db,
        IDatabaseFreshnessProvider freshness,
        ILogger<LeagueController> logger) {
        this.db = db;
        this.freshness = freshness;
        this.logger = logger;
    }

    /// <summary>
    /// Returns every season for which data exists, as a mapping from league abbreviation
    /// (e.g. "SHL", "SMJHL") to the list of available seasons ordered newest first.
    /// </summary>
    [HttpGet("seasons")]
    [ProducesResponseType<IReadOnlyList<LeagueSeasons>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<ActionResult<IReadOnlyList<LeagueSeasons>>> GetSeasons(
        CancellationToken cancellationToken) {
        var lastUpdated = await freshness.GetLastUpdatedAsync(cancellationToken);

        var seasons = await db.Seasons
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var result = seasons
            .GroupBy(ls => ls.KnownLeague.Abbreviation)
            .Select(g => new LeagueSeasons(
                g.Key,
                g.Select(ls => ls.Season)
                    .Distinct()
                    .OrderByDescending(s => s)
                    .ToList()))
            .OrderBy(ls => ls.League)
            .ToList();

        return this.DbVersionedOk(result, lastUpdated, CacheMaxAge);
    }

    /// <summary>
    /// Returns the most recent season for which data exists, as a mapping from league
    /// abbreviation (e.g. "SHL", "SMJHL") to that league's latest season.
    /// </summary>
    [HttpGet("seasons/current")]
    [ProducesResponseType<IReadOnlyList<LeagueCurrentSeason>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<ActionResult<IReadOnlyList<LeagueCurrentSeason>>> GetCurrentSeasons(
        CancellationToken cancellationToken) {
        var lastUpdated = await freshness.GetLastUpdatedAsync(cancellationToken);

        var seasons = await db.Seasons
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var result = seasons
            .GroupBy(ls => ls.KnownLeague.Abbreviation)
            .Select(g => new LeagueCurrentSeason(g.Key, g.Max(ls => ls.Season)))
            .OrderBy(ls => ls.League)
            .ToList();

        return this.DbVersionedOk(result, lastUpdated, CacheMaxAge);
    }

    /// <summary>
    /// Returns a single team's identity and branding within <paramref name="league"/>. When
    /// <paramref name="season"/> is supplied the team is looked up for that exact season; otherwise
    /// the most recent season on file is returned (the team's current branding). Responds
    /// <c>404 Not Found</c> when the league abbreviation is unknown or no matching team exists.
    /// </summary>
    /// <param name="league">The league abbreviation from the route (e.g. "SHL", "SMJHL").</param>
    /// <param name="teamId">The team id within that league.</param>
    /// <param name="season">Optional season; defaults to the team's most recent season.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    [HttpGet("{league}/teams/{teamId:int}")]
    [ProducesResponseType<TeamCard>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamCard>> GetTeam(
        string league,
        int teamId,
        [FromQuery] int? season,
        CancellationToken cancellationToken) {
        if (!KnownLeague.TryFromAbbreviation(league, out var knownLeague)) {
            return NotFound();
        }

        var lastUpdated = await freshness.GetLastUpdatedAsync(cancellationToken);

        var leagueId = knownLeague.Id;
        var query = db.Teams
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Where(t => t.LeagueId == leagueId && t.TeamId == teamId);

        if (season is { } requestedSeason) {
            query = query.Where(t => t.Season == requestedSeason);
        }

        var team = await query
            .OrderByDescending(t => t.Season)
            .FirstOrDefaultAsync(cancellationToken);

        if (team is null) {
            return NotFound();
        }

        return this.DbVersionedOk(ToTeamCard(team, knownLeague), lastUpdated, CacheMaxAge);
    }

    private static TeamCard ToTeamCard(TeamEntity team, KnownLeague league) =>
        new() {
            TeamId = team.TeamId,
            Season = team.Season,
            League = league.Abbreviation,
            LeagueId = team.LeagueId,
            Name = team.Name,
            Abbreviation = team.Abbreviation,
            Location = team.Location,
            PrimaryColor = ToHex(team.Colors.Primary),
            SecondaryColor = ToHex(team.Colors.Secondary),
            TextColor = team.Colors.Text is { } text ? ToHex(text) : null,
        };

    private static string ToHex(Color color) =>
        $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
