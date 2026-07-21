using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Shuttle.Api.Contracts;
using Shuttle.EFCore;
using Shuttle.EFCore.Entities.Portal;
using Shuttle.Models.Players;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.Api.Controllers;

/// <summary>
/// Public, unauthenticated read access to player information, backing the WebClient player profile
/// page and embeddable player widgets.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("players")]
[Route("player")]
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
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PlayerCard>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlayerCard>>> GetPlayers(CancellationToken cancellationToken) {
        var rows = await db.PlayerInformation
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .OrderBy(p => p.Name)
            .SelectCardRows(db.PlayerInformation)
            .ToListAsync(cancellationToken);

        return Ok(rows.ToPlayerCards());
    }

    /// <summary>
    /// Returns the slim <see cref="PlayerSuggestion"/> directory for every player, ordered by name.
    /// Backs client-side name/username autocomplete: the WebClient fetches this once and filters it
    /// locally. Marked cacheable since the directory only changes on the periodic DB update job.
    /// </summary>
    [HttpGet("suggestions")]
    [ProducesResponseType<IReadOnlyList<PlayerSuggestion>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlayerSuggestion>>> GetPlayerSuggestions(
        CancellationToken cancellationToken) {
        var suggestions = await db.PlayerInformation
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .OrderBy(p => p.Name)
            .Select(p => new PlayerSuggestion {
                PlayerId = p.PlayerId,
                Name = p.Name,
                Username = p.Username,
                Status = p.Status,
                Position = p.Position,
            })
            .ToListAsync(cancellationToken);

        Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue {
            Public = true,
            MaxAge = TimeSpan.FromHours(1),
        };

        return Ok(suggestions);
    }
    /// filtered, sorted, and paginated server-side.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType<PagedResult<PlayerCard>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PlayerCard>>> SearchPlayers(
        [FromQuery] PlayerSearchQuery query,
        CancellationToken cancellationToken) {
        var filtered = ApplyFilters(
            db.PlayerInformation.AsNoTracking().IgnoreAutoIncludes(),
            db.PlayerInformation,
            query);

        var totalCount = await filtered.CountAsync(cancellationToken);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var rows = await ApplySort(filtered, query)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .SelectCardRows(db.PlayerInformation)
            .ToListAsync(cancellationToken);

        var cards = rows.ToPlayerCards();

        return Ok(new PagedResult<PlayerCard> {
            Items = cards,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        });
    }

    private const int MaxPageSize = 100;

    private static IQueryable<PlayerInformation> ApplyFilters(
        IQueryable<PlayerInformation> source,
        IQueryable<PlayerInformation> allPlayers,
        PlayerSearchQuery query) {
        if (!string.IsNullOrWhiteSpace(query.Text)) {
            var text = query.Text.Trim();
            source = source.Where(p => p.Name.Contains(text) || p.Username.Contains(text));
        }

        if (query.Positions is { Count: > 0 }) {
            var positions = query.Positions
                .Select(code => PlayerPosition.TryFromString(code, out var pos) ? pos : (PlayerPosition?)null)
                .Where(pos => pos is not null)
                .Select(pos => pos!.Value)
                .Distinct()
                .ToList();

            if (positions.Count > 0) {
                source = source.Where(p => positions.Contains(p.Position));
            }
        }

        if (query.Statuses is { Count: > 0 }) {
            var statuses = query.Statuses.Distinct().ToList();
            source = source.Where(p => statuses.Contains(p.Status));
        }

        if (query.Leagues is { Count: > 0 }) {
            var leagues = query.Leagues.Distinct().ToList();
            source = source.Where(p => p.CurrentLeague != null && leagues.Contains(p.CurrentLeague.Value));
        }

        if (query.Handedness is { Count: > 0 }) {
            var handedness = query.Handedness.Distinct().ToList();
            source = source.Where(p => handedness.Contains(p.Handedness));
        }

        if (query.DraftSeason is { } draftSeason) {
            source = source.Where(p => p.DraftSeason == draftSeason);
        }

        if (!string.IsNullOrWhiteSpace(query.IihfNation)) {
            var nation = query.IihfNation.Trim();
            source = source.Where(p => p.IihfNation != null && p.IihfNation.Contains(nation));
        }

        if (query.Inactive is { } inactive) {
            source = source.Where(p => p.Inactive == inactive);
        }

        if (query.Suspended is { } suspended) {
            source = source.Where(p => p.IsSuspended == suspended);
        }

        if (query.Recreate is { } recreate) {
            // Recreate == a same-user player exists with an earlier creation (mirror of the
            // most-recent anti-join, comparing "earlier" instead of "later").
            source = source.Where(p => allPlayers.Any(o =>
                o.UserId == p.UserId
                && (o.CreationTime < p.CreationTime
                    || (o.CreationTime == p.CreationTime && o.PlayerId < p.PlayerId))) == recreate);
        }

        if (query.MinTotalTpe is { } minTpe) {
            source = source.Where(p => p.TotalTpe >= minTpe);
        }

        if (query.MaxTotalTpe is { } maxTpe) {
            source = source.Where(p => p.TotalTpe <= maxTpe);
        }

        if (query.MinBankBalance is { } minBank) {
            source = source.Where(p => p.BankBalance >= minBank);
        }

        if (query.MaxBankBalance is { } maxBank) {
            source = source.Where(p => p.BankBalance <= maxBank);
        }

        return source;
    }

    private static IOrderedQueryable<PlayerInformation> ApplySort(
        IQueryable<PlayerInformation> source,
        PlayerSearchQuery query) {
        var desc = query.SortDescending;

        // PlayerId is a stable tiebreaker so paging is deterministic.
        return query.SortBy switch {
            PlayerSortField.TotalTpe => source.OrderByField(p => p.TotalTpe, desc).ThenBy(p => p.PlayerId),
            PlayerSortField.DraftSeason => source.OrderByField(p => p.DraftSeason, desc).ThenBy(p => p.PlayerId),
            PlayerSortField.Position => source.OrderByField(p => p.Position, desc).ThenBy(p => p.PlayerId),
            PlayerSortField.Status => source.OrderByField(p => p.Status, desc).ThenBy(p => p.PlayerId),
            PlayerSortField.League => source.OrderByField(p => p.CurrentLeague, desc).ThenBy(p => p.PlayerId),
            PlayerSortField.Username => source.OrderByField(p => p.Username, desc).ThenBy(p => p.PlayerId),
            PlayerSortField.Created => source.OrderByField(p => p.CreationTime, desc).ThenBy(p => p.PlayerId),
            _ => source.OrderByField(p => p.Name, desc).ThenBy(p => p.PlayerId),
        };
    }

    /// <summary>
    /// Returns the "at a glance" <see cref="PlayerCard"/> for the given player id, or 404 when no
    /// such player exists.
    /// </summary>
    [HttpGet("{playerId:int}")]
    [ProducesResponseType<PlayerCard>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerCard>> GetPlayer(int playerId, CancellationToken cancellationToken) {
        var row = await db.PlayerInformation
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Where(p => p.PlayerId == playerId)
            .SelectCardRows(db.PlayerInformation)
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null) {
            logger.LogInformation("Player {PlayerId} not found", playerId);
            return NotFound();
        }

        return Ok(row.ToPlayerCard());
    }

    /// <summary>
    /// Returns the player's TPE timeline (cumulative total TPE over time), ordered chronologically.
    /// Responds 404 when no player with the given id exists; an existing player with no recorded
    /// events yields an empty list.
    /// </summary>
    [HttpGet("{playerId:int}/tpe-timeline")]
    [ProducesResponseType<IReadOnlyList<TpeTimelinePoint>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TpeTimelinePoint>>> GetPlayerTpeTimeline(
        int playerId,
        CancellationToken cancellationToken) {
        var playerExists = await db.PlayerInformation
            .AsNoTracking()
            .AnyAsync(p => p.PlayerId == playerId, cancellationToken);

        if (!playerExists) {
            logger.LogInformation("Player {PlayerId} not found", playerId);
            return NotFound();
        }

        var timeline = await db.TpeEvents
            .AsNoTracking()
            .Where(e => e.PlayerId == playerId)
            .OrderBy(e => e.TaskDate)
            .Select(e => new TpeTimelinePoint {
                TaskDate = e.TaskDate,
                TotalTpe = e.TotalTpe,
            })
            .ToListAsync(cancellationToken);

        return Ok(timeline);
    }

    private const int MaxResolveInputs = 200;

    private sealed record NameMatch(int PlayerId, string LoweredName);

    /// <summary>
    /// Resolves a batch of player ids and/or names to concrete players without mutating anything.
    /// Names are matched case-insensitively; a name matching more than one player is reported as
    /// <c>Ambiguous</c>, and unknown ids/names as <c>NotFound</c>. Uses the HTTP <c>QUERY</c> method
    /// (a safe, idempotent read that carries a request body). Backs the WebClient bulk-add preview.
    /// </summary>
    [AcceptVerbs("QUERY", Route = "resolve")]
    [ProducesResponseType<ResolvePlayersResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResolvePlayersResult>> ResolvePlayers(
        [FromBody] ResolvePlayersRequest request,
        CancellationToken cancellationToken) {
        var requestedIds = (request.PlayerIds ?? []).ToList();
        var requestedNames = (request.Names ?? [])
            .Select(n => n?.Trim() ?? string.Empty)
            .Where(n => n.Length > 0)
            .ToList();

        if (requestedIds.Count == 0 && requestedNames.Count == 0) {
            return BadRequest(new ProblemDetails {
                Detail = "Provide at least one player id or name to resolve.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        if (requestedIds.Count + requestedNames.Count > MaxResolveInputs) {
            return BadRequest(new ProblemDetails {
                Detail = $"Too many inputs; resolve at most {MaxResolveInputs} players at a time.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        var notFound = new List<string>();

        // Resolve names -> player ids. Matching is case-insensitive on the trimmed name; any name that
        // matches more than one player is ambiguous and cannot be resolved to a single player.
        var loweredNames = requestedNames.Select(n => n.ToLowerInvariant()).Distinct().ToList();
        var nameMatches = loweredNames.Count == 0
            ? []
            : await db.PlayerInformation
                .Where(p => loweredNames.Contains(p.Name.ToLower()))
                .Select(p => new NameMatch(p.PlayerId, p.Name.ToLower()))
                .ToListAsync(cancellationToken);

        var byLoweredName = nameMatches
            .GroupBy(m => m.LoweredName)
            .ToDictionary(g => g.Key, g => g.Select(m => m.PlayerId).Distinct().ToList());

        var ambiguous = new List<string>();
        var resolvedFromNames = new List<int>();
        var seenLoweredNames = new HashSet<string>();
        foreach (var name in requestedNames) {
            var lowered = name.ToLowerInvariant();
            if (!seenLoweredNames.Add(lowered)) {
                continue; // duplicate name within the request
            }

            if (!byLoweredName.TryGetValue(lowered, out var ids) || ids.Count == 0) {
                notFound.Add(name);
            } else if (ids.Count > 1) {
                ambiguous.Add(name);
            } else {
                resolvedFromNames.Add(ids[0]);
            }
        }

        // Fetch slim summaries for every candidate id (requested + name-resolved) in one round trip.
        var candidateIds = requestedIds.Concat(resolvedFromNames).Distinct().ToList();
        var summaries = candidateIds.Count == 0
            ? new Dictionary<int, ResolvedPlayer>()
            : (await db.PlayerInformation
                .Where(p => candidateIds.Contains(p.PlayerId))
                .Select(p => new ResolvedPlayer {
                    PlayerId = p.PlayerId,
                    Name = p.Name,
                    Username = p.Username,
                    Status = p.Status,
                    Position = p.Position,
                    DraftSeason = p.DraftSeason,
                    TotalTpe = p.TotalTpe,
                })
                .ToListAsync(cancellationToken))
            .ToDictionary(p => p.PlayerId);

        // Build the ordered, de-duplicated resolved list: requested ids first (in request order),
        // then name-resolved players, preserving first-seen order. Unknown ids fall into NotFound.
        var resolved = new List<ResolvedPlayer>();
        var seenIds = new HashSet<int>();
        foreach (var id in requestedIds) {
            if (!seenIds.Add(id)) {
                continue;
            }

            if (summaries.TryGetValue(id, out var player)) {
                resolved.Add(player);
            } else {
                notFound.Add(id.ToString());
            }
        }

        foreach (var id in resolvedFromNames) {
            if (seenIds.Add(id) && summaries.TryGetValue(id, out var player)) {
                resolved.Add(player);
            }
        }

        return Ok(new ResolvePlayersResult {
            Resolved = resolved,
            NotFound = notFound,
            Ambiguous = ambiguous,
        });
    }
}

internal static class PlayerQueryableExtensions {
    /// <summary>
    /// Orders <paramref name="source"/> by <paramref name="keySelector"/> ascending or descending
    /// depending on <paramref name="descending"/>, starting a new ordering.
    /// </summary>
    public static IOrderedQueryable<T> OrderByField<T, TKey>(
        this IQueryable<T> source,
        System.Linq.Expressions.Expression<Func<T, TKey>> keySelector,
        bool descending) =>
        descending ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector);
}
