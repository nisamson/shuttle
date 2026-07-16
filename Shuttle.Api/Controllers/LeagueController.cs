using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Shuttle.EFCore;
using Shuttle.Models.Leagues;
using Shuttle.Shl.Api.Models.Common;

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
    private readonly ShlDbContext db;
    private readonly ILogger<LeagueController> logger;

    public LeagueController(ShlDbContext db, ILogger<LeagueController> logger) {
        this.db = db;
        this.logger = logger;
    }

    /// <summary>
    /// Returns every season for which data exists, as a mapping from league abbreviation
    /// (e.g. "SHL", "SMJHL") to the list of available seasons ordered newest first.
    /// </summary>
    [HttpGet("seasons")]
    [ProducesResponseType<IReadOnlyList<LeagueSeasons>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LeagueSeasons>>> GetSeasons(
        CancellationToken cancellationToken) {
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

        SetCacheHeaders();

        return Ok(result);
    }

    /// <summary>
    /// Returns the most recent season for which data exists, as a mapping from league
    /// abbreviation (e.g. "SHL", "SMJHL") to that league's latest season.
    /// </summary>
    [HttpGet("seasons/current")]
    [ProducesResponseType<IReadOnlyList<LeagueCurrentSeason>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LeagueCurrentSeason>>> GetCurrentSeasons(
        CancellationToken cancellationToken) {
        var seasons = await db.Seasons
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var result = seasons
            .GroupBy(ls => ls.KnownLeague.Abbreviation)
            .Select(g => new LeagueCurrentSeason(g.Key, g.Max(ls => ls.Season)))
            .OrderBy(ls => ls.League)
            .ToList();

        SetCacheHeaders();

        return Ok(result);
    }

    private void SetCacheHeaders() {
        Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue {
            Public = true,
            MaxAge = TimeSpan.FromHours(1),
        };
    }
}
