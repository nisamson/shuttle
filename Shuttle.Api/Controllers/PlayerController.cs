using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shuttle.Api.Contracts;
using Shuttle.EFCore;
using Shuttle.Models.Players;

namespace Shuttle.Api.Controllers;

/// <summary>
/// Public, unauthenticated read access to player information, backing the WebClient player profile
/// page and embeddable player widgets.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("[controller]")]
public class PlayerController : ControllerBase {
    private readonly ShlDbContext db;
    private readonly ILogger<PlayerController> logger;

    public PlayerController(ShlDbContext db, ILogger<PlayerController> logger) {
        this.db = db;
        this.logger = logger;
    }

    /// <summary>
    /// Returns the "at a glance" <see cref="PlayerCard"/> for every player, ordered by name.
    /// </summary>
    [HttpGet("~/players")]
    [ProducesResponseType<IReadOnlyList<PlayerCard>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlayerCard>>> GetPlayers(CancellationToken cancellationToken) {
        var entities = await db.PlayerInformation
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var cards = entities
            .Select(e => new PlayerInformationFacet(e).ToPlayerCard())
            .ToList();

        return Ok(cards);
    }

    /// <summary>
    /// Returns the "at a glance" <see cref="PlayerCard"/> for the given player id, or 404 when no
    /// such player exists.
    /// </summary>
    [HttpGet("{playerId:int}")]
    [ProducesResponseType<PlayerCard>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerCard>> GetPlayer(int playerId, CancellationToken cancellationToken) {
        var entity = await db.PlayerInformation
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .FirstOrDefaultAsync(p => p.PlayerId == playerId, cancellationToken);

        if (entity is null) {
            logger.LogInformation("Player {PlayerId} not found", playerId);
            return NotFound();
        }

        return Ok(new PlayerInformationFacet(entity).ToPlayerCard());
    }
}
