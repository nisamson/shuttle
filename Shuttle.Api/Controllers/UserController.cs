using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Shuttle.Api.Contracts;
using Shuttle.EFCore;
using Shuttle.EFCore.Entities;
using Shuttle.Models.Players;
using Shuttle.Models.Users;

namespace Shuttle.Api.Controllers;

/// <summary>
/// Public, unauthenticated read access to the identifying information we hold on SHL users
/// (username, Discord name, and user id).
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase {
    private readonly ShlDbContext db;
    private readonly ILogger<UserController> logger;

    public UserController(ShlDbContext db, ILogger<UserController> logger) {
        this.db = db;
        this.logger = logger;
    }

    /// <summary>
    /// Returns the <see cref="UserCard"/> for the given user, identified by either their numeric
    /// user id or their username, or 404 when no such user exists. Pass <c>?players=true</c> to also
    /// include the <see cref="PlayerCard"/>s for players the user has created.
    /// </summary>
    [HttpGet("{userIdOrName}")]
    [ProducesResponseType<UserCard>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserCard>> GetUser(
        string userIdOrName,
        [FromQuery] bool players,
        CancellationToken cancellationToken) {
        var includeDiscord = User.Identity?.IsAuthenticated == true;

        var query = db.Users.AsNoTracking().IgnoreAutoIncludes();
        query = int.TryParse(userIdOrName, out var userId)
            ? query.Where(u => u.UserId == userId)
            : query.Where(u => u.Name == userIdOrName);

        var user = await query
            .Select(u => new { u.UserId, u.Name, u.DiscordId })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null) {
            logger.LogInformation("User {UserIdOrName} not found", userIdOrName);
            return NotFound();
        }

        IReadOnlyList<PlayerCard>? playerCards = null;
        if (players) {
            var rows = await db.PlayerInformation
                .AsNoTracking()
                .IgnoreAutoIncludes()
                .Where(p => p.UserId == user.UserId)
                .OrderByDescending(p => p.CreationTime)
                .SelectCardRows(db.PlayerInformation)
                .ToListAsync(cancellationToken);

            playerCards = rows.ToPlayerCards();
        }

        return Ok(new UserCard {
            UserId = user.UserId,
            Username = user.Name,
            // Discord name is only surfaced to authenticated callers.
            DiscordName = includeDiscord ? user.DiscordId : null,
            Players = playerCards,
        });
    }

    private const int MaxPageSize = 100;

    /// <summary>
    /// Searches users by username (and, for authenticated callers who opt in via
    /// <see cref="UserSearchQuery.SearchDiscord"/>, by Discord name), returning a filtered, sorted,
    /// paginated page of <see cref="UserCard"/>s. Discord names are only populated for authenticated
    /// callers. Player cards are never included here.
    /// </summary>
    [HttpGet("~/users/search")]
    [ProducesResponseType<PagedResult<UserCard>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserCard>>> SearchUsers(
        [FromQuery] UserSearchQuery query,
        CancellationToken cancellationToken) {
        var includeDiscord = User.Identity?.IsAuthenticated == true;
        var searchDiscord = includeDiscord && query.SearchDiscord;

        var source = db.Users.AsNoTracking().IgnoreAutoIncludes();

        if (!string.IsNullOrWhiteSpace(query.Text)) {
            var text = query.Text.Trim();
            source = searchDiscord
                ? source.Where(u => u.Name.Contains(text) || (u.DiscordId != null && u.DiscordId.Contains(text)))
                : source.Where(u => u.Name.Contains(text));
        }

        var totalCount = await source.CountAsync(cancellationToken);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var users = await ApplySort(source, query)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new { u.UserId, u.Name, u.DiscordId })
            .ToListAsync(cancellationToken);

        var items = users
            .Select(u => new UserCard {
                UserId = u.UserId,
                Username = u.Name,
                DiscordName = includeDiscord ? u.DiscordId : null,
                Players = null,
            })
            .ToList();

        return Ok(new PagedResult<UserCard> {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        });
    }

    /// <summary>
    /// Returns the slim <see cref="UserSuggestion"/> directory for every user, ordered by username.
    /// Backs client-side username autocomplete: the WebClient fetches this once and filters it
    /// locally. Username-only (Discord-free) so it stays public and cacheable.
    /// </summary>
    [HttpGet("~/users/suggestions")]
    [ProducesResponseType<IReadOnlyList<UserSuggestion>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserSuggestion>>> GetUserSuggestions(
        CancellationToken cancellationToken) {
        var suggestions = await db.Users
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .OrderBy(u => u.Name)
            .Select(u => new UserSuggestion {
                UserId = u.UserId,
                Username = u.Name,
            })
            .ToListAsync(cancellationToken);

        Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue {
            Public = true,
            MaxAge = TimeSpan.FromHours(1),
        };

        return Ok(suggestions);
    }

    private static IOrderedQueryable<ShlUser> ApplySort(IQueryable<ShlUser> source, UserSearchQuery query) {
        var desc = query.SortDescending;

        // UserId is a stable tiebreaker so paging is deterministic.
        return query.SortBy switch {
            UserSortField.UserId => Order(source, u => u.UserId, desc).ThenBy(u => u.UserId),
            UserSortField.DiscordName => Order(source, u => u.DiscordId, desc).ThenBy(u => u.UserId),
            _ => Order(source, u => u.Name, desc).ThenBy(u => u.UserId),
        };

        static IOrderedQueryable<ShlUser> Order<TKey>(
            IQueryable<ShlUser> src,
            System.Linq.Expressions.Expression<Func<ShlUser, TKey>> key,
            bool descending) =>
            descending ? src.OrderByDescending(key) : src.OrderBy(key);
    }
}
