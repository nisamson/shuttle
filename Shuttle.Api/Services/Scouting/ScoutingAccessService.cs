using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Shuttle.EFCore;
using Shuttle.Models.Scouting;

namespace Shuttle.Api.Services.Scouting;

/// <summary>
/// Resolves a caller's authorization context against a scouting team and evaluates the team's
/// ownership invariants. This is the single source of truth for who may do what to a scouting team,
/// so controllers stay free of ad-hoc permission logic.
/// </summary>
public interface IScoutingAccessService {
    /// <summary>
    /// Resolves the caller's <see cref="ScoutingAccess"/> for the given team. Returns <c>null</c>
    /// when the team does not exist or the caller could not be identified; otherwise the caller's
    /// account, site-admin flag, and membership role (which may be <c>null</c> for a non-member).
    /// </summary>
    Task<ScoutingAccess?> ResolveAsync(Guid teamId, ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the given user is the team's <em>sole</em> <see cref="ScoutingTeamRole.Owner"/> — an
    /// Owner for whom no other Owner exists. Used to enforce that a team is never left ownerless: the
    /// sole Owner cannot leave, be removed, or be demoted (they must promote another Owner first), and
    /// an Owner may only delete a team when they are its sole Owner.
    /// </summary>
    Task<bool> IsSoleOwnerAsync(Guid teamId, Guid shuttleUserId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default <see cref="IScoutingAccessService"/> backed by <see cref="ShlDbContext"/>.
/// </summary>
public sealed class ScoutingAccessService : IScoutingAccessService {
    private readonly ShlDbContext db;
    private readonly Users.IUserService users;

    public ScoutingAccessService(ShlDbContext db, Users.IUserService users) {
        this.db = db;
        this.users = users;
    }

    public async Task<ScoutingAccess?> ResolveAsync(
        Guid teamId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        if (!Guid.TryParse(principal.GetObjectId(), out var objectId)) {
            return null;
        }

        var teamExists = await db.ScoutingTeams
            .AsNoTracking()
            .AnyAsync(t => t.Id == teamId, cancellationToken);
        if (!teamExists) {
            return null;
        }

        var user = await users.GetOrCreateAsync(objectId, cancellationToken);
        var role = await db.ScoutingTeamMembers
            .AsNoTracking()
            .Where(m => m.ScoutingTeamId == teamId && m.ShuttleUserId == user.Id)
            .Select(m => (ScoutingTeamRole?)m.Role)
            .FirstOrDefaultAsync(cancellationToken);

        var isAdmin = principal.IsInRole(Startup.AdminRole);
        return new ScoutingAccess(user, isAdmin, role);
    }

    public async Task<bool> IsSoleOwnerAsync(
        Guid teamId,
        Guid shuttleUserId,
        CancellationToken cancellationToken = default) {
        var owners = await db.ScoutingTeamMembers
            .AsNoTracking()
            .Where(m => m.ScoutingTeamId == teamId && m.Role == ScoutingTeamRole.Owner)
            .Select(m => m.ShuttleUserId)
            .Take(2)
            .ToListAsync(cancellationToken);

        return owners.Count == 1 && owners[0] == shuttleUserId;
    }
}

public static class ScoutingAccessServiceRegistration {
    public static IServiceCollection AddScoutingAccessService(this IServiceCollection services) {
        services.AddScoped<IScoutingAccessService, ScoutingAccessService>();
        return services;
    }
}
