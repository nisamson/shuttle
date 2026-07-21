using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Shuttle.Models.Scouting;
using Entities = Shuttle.EFCore.Entities.Scouting;

namespace Shuttle.Api.Services.Scouting;

public sealed partial class ScoutingService {
    public async Task<IReadOnlyList<ScoutingTeamSummary>> GetMyTeamsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        if (!TryResolveUserId(principal, out var userId)) {
            return [];
        }

        var user = await users.GetOrCreateAsync(userId, cancellationToken);

        return await db.ScoutingTeamMembers
            .AsNoTracking()
            .Where(m => m.ShuttleUserId == user.Id)
            .Select(m => new ScoutingTeamSummary {
                Id = m.Team.Id,
                Name = m.Team.Name,
                MyRole = m.Role,
                MemberCount = m.Team.Members.Count,
                BoardCount = m.Team.Boards.Count,
                UpdatedAt = m.Team.UpdatedAt,
            })
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScoutingTeamSummary>> GetAllTeamsAsync(
        CancellationToken cancellationToken = default) {
        return await db.ScoutingTeams
            .AsNoTracking()
            .Select(t => new ScoutingTeamSummary {
                Id = t.Id,
                Name = t.Name,
                MyRole = null,
                MemberCount = t.Members.Count,
                BoardCount = t.Boards.Count,
                UpdatedAt = t.UpdatedAt,
            })
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ScoutingResult<ScoutingTeamDetail>> GetTeamAsync(
        Guid teamId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var access = await accessService.ResolveAsync(teamId, principal, cancellationToken);
        if (access is null) {
            return ScoutingResult<ScoutingTeamDetail>.NotFound("Team not found.");
        }

        if (!access.CanView) {
            return ScoutingResult<ScoutingTeamDetail>.Forbidden("You are not a member of this team.");
        }

        var detail = await LoadTeamDetailAsync(teamId, access.Role, cancellationToken);
        return detail is null
            ? ScoutingResult<ScoutingTeamDetail>.NotFound("Team not found.")
            : ScoutingResult<ScoutingTeamDetail>.Ok(detail);
    }

    public async Task<ScoutingResult<ScoutingTeamDetail>> CreateTeamAsync(
        CreateScoutingTeamRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        if (!TryResolveUserId(principal, out var userId)) {
            return ScoutingResult<ScoutingTeamDetail>.Forbidden("Could not identify the caller.");
        }

        var name = request.Name.Trim();
        if (name.Length is 0 or > ScoutingLimits.TeamNameMaxLength) {
            return ScoutingResult<ScoutingTeamDetail>.Invalid("Team name is required.");
        }

        var user = await users.GetOrCreateAsync(userId, cancellationToken);
        var now = Now;
        var team = new Entities.ScoutingTeam {
            Id = Guid.CreateVersion7(),
            Name = name,
            CreatedByUserId = user.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };
        team.Members.Add(new Entities.ScoutingTeamMember {
            Id = Guid.CreateVersion7(),
            ScoutingTeamId = team.Id,
            ShuttleUserId = user.Id,
            Role = ScoutingTeamRole.Owner,
            CreatedAt = now,
        });
        db.ScoutingTeams.Add(team);
        await db.SaveChangesAsync(cancellationToken);

        var detail = await LoadTeamDetailAsync(team.Id, ScoutingTeamRole.Owner, cancellationToken);
        return ScoutingResult<ScoutingTeamDetail>.Ok(detail!);
    }

    public async Task<ScoutingResult<ScoutingTeamDetail>> RenameTeamAsync(
        Guid teamId,
        UpdateScoutingTeamRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var access = await accessService.ResolveAsync(teamId, principal, cancellationToken);
        if (access is null) {
            return ScoutingResult<ScoutingTeamDetail>.NotFound("Team not found.");
        }

        if (!access.CanManageTeam) {
            return ScoutingResult<ScoutingTeamDetail>.Forbidden("Only team owners can rename the team.");
        }

        var name = request.Name.Trim();
        if (name.Length is 0 or > ScoutingLimits.TeamNameMaxLength) {
            return ScoutingResult<ScoutingTeamDetail>.Invalid("Team name is required.");
        }

        var team = await db.ScoutingTeams.FirstAsync(t => t.Id == teamId, cancellationToken);
        team.Name = name;
        team.UpdatedAt = Now;
        if (!await TrySaveChangesAsync(cancellationToken)) {
            return ScoutingResult<ScoutingTeamDetail>.Conflict(
                "The team was changed by someone else; please reload and try again.");
        }

        var detail = await LoadTeamDetailAsync(teamId, access.Role, cancellationToken);
        return ScoutingResult<ScoutingTeamDetail>.Ok(detail!);
    }

    public async Task<ScoutingResult> DeleteTeamAsync(
        Guid teamId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var access = await accessService.ResolveAsync(teamId, principal, cancellationToken);
        if (access is null) {
            return ScoutingResult.NotFound("Team not found.");
        }

        if (!access.IsSiteAdmin) {
            if (access.Role != ScoutingTeamRole.Owner) {
                return ScoutingResult.Forbidden("Only a team owner or a site admin can delete a team.");
            }

            if (!await accessService.IsSoleOwnerAsync(teamId, access.User.Id, cancellationToken)) {
                return ScoutingResult.Conflict(
                    "You must be the team's only owner to delete it; all other owners must leave first.");
            }
        }

        var team = await db.ScoutingTeams.FirstAsync(t => t.Id == teamId, cancellationToken);
        db.ScoutingTeams.Remove(team);
        if (!await TrySaveChangesAsync(cancellationToken)) {
            return ScoutingResult.Conflict(
                "The team was changed by someone else; please reload and try again.");
        }

        return ScoutingResult.Ok();
    }

    public async Task<ScoutingResult<ScoutingMember>> AddMemberAsync(
        Guid teamId,
        AddScoutingMemberRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var access = await accessService.ResolveAsync(teamId, principal, cancellationToken);
        if (access is null) {
            return ScoutingResult<ScoutingMember>.NotFound("Team not found.");
        }

        if (!access.CanManageTeam) {
            return ScoutingResult<ScoutingMember>.Forbidden("Only team owners can add members.");
        }

        if (!Enum.IsDefined(request.Role)) {
            return ScoutingResult<ScoutingMember>.Invalid("An unknown member role was supplied.");
        }

        var username = request.Username?.Trim() ?? string.Empty;
        if (username.Length == 0) {
            return ScoutingResult<ScoutingMember>.Invalid("A username is required.");
        }

        var target = await db.ShuttleUsers
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        if (target is null) {
            return ScoutingResult<ScoutingMember>.Invalid(
                $"No user named '{username}' was found. The user must have signed in at least once before they can be added.");
        }

        var alreadyMember = await db.ScoutingTeamMembers
            .AsNoTracking()
            .AnyAsync(m => m.ScoutingTeamId == teamId && m.ShuttleUserId == target.Id, cancellationToken);
        if (alreadyMember) {
            return ScoutingResult<ScoutingMember>.Conflict($"'{username}' is already a member of this team.");
        }

        var member = new Entities.ScoutingTeamMember {
            Id = Guid.CreateVersion7(),
            ScoutingTeamId = teamId,
            ShuttleUserId = target.Id,
            Role = request.Role,
            CreatedAt = Now,
        };
        db.ScoutingTeamMembers.Add(member);
        await TouchTeamAsync(teamId, cancellationToken);
        if (!await TrySaveChangesAsync(cancellationToken)) {
            return ScoutingResult<ScoutingMember>.Conflict(
                "The team was changed by someone else; please reload and try again.");
        }

        member.User = target;
        return ScoutingResult<ScoutingMember>.Ok(ToMemberDto(member));
    }

    public async Task<ScoutingResult<ScoutingMember>> UpdateMemberRoleAsync(
        Guid teamId,
        Guid targetUserId,
        UpdateScoutingMemberRoleRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var access = await accessService.ResolveAsync(teamId, principal, cancellationToken);
        if (access is null) {
            return ScoutingResult<ScoutingMember>.NotFound("Team not found.");
        }

        if (!access.CanManageTeam) {
            return ScoutingResult<ScoutingMember>.Forbidden("Only team owners can change member roles.");
        }

        if (!Enum.IsDefined(request.Role)) {
            return ScoutingResult<ScoutingMember>.Invalid("An unknown member role was supplied.");
        }

        var member = await db.ScoutingTeamMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.ScoutingTeamId == teamId && m.ShuttleUserId == targetUserId, cancellationToken);
        if (member is null) {
            return ScoutingResult<ScoutingMember>.NotFound("That user is not a member of this team.");
        }

        if (member.Role == request.Role) {
            return ScoutingResult<ScoutingMember>.Ok(ToMemberDto(member));
        }

        if (member.Role == ScoutingTeamRole.Owner && request.Role != ScoutingTeamRole.Owner
            && await accessService.IsSoleOwnerAsync(teamId, targetUserId, cancellationToken)) {
            return ScoutingResult<ScoutingMember>.Conflict(
                "You cannot demote the team's only owner; promote another owner first.");
        }

        member.Role = request.Role;
        await TouchTeamAsync(teamId, cancellationToken);
        if (!await TrySaveChangesAsync(cancellationToken)) {
            return ScoutingResult<ScoutingMember>.Conflict(
                "The team was changed by someone else; please reload and try again.");
        }

        return ScoutingResult<ScoutingMember>.Ok(ToMemberDto(member));
    }

    public async Task<ScoutingResult> RemoveMemberAsync(
        Guid teamId,
        Guid targetUserId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var access = await accessService.ResolveAsync(teamId, principal, cancellationToken);
        if (access is null) {
            return ScoutingResult.NotFound("Team not found.");
        }

        if (!access.CanManageTeam) {
            return ScoutingResult.Forbidden("Only team owners can remove members.");
        }

        return await RemoveMembershipAsync(teamId, targetUserId, cancellationToken);
    }

    public async Task<ScoutingResult> LeaveTeamAsync(
        Guid teamId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default) {
        var access = await accessService.ResolveAsync(teamId, principal, cancellationToken);
        if (access is null) {
            return ScoutingResult.NotFound("Team not found.");
        }

        if (!access.IsMember) {
            return ScoutingResult.Forbidden("You are not a member of this team.");
        }

        return await RemoveMembershipAsync(teamId, access.User.Id, cancellationToken);
    }

    private async Task<ScoutingResult> RemoveMembershipAsync(
        Guid teamId,
        Guid targetUserId,
        CancellationToken cancellationToken) {
        var member = await db.ScoutingTeamMembers
            .FirstOrDefaultAsync(m => m.ScoutingTeamId == teamId && m.ShuttleUserId == targetUserId, cancellationToken);
        if (member is null) {
            return ScoutingResult.NotFound("That user is not a member of this team.");
        }

        if (member.Role == ScoutingTeamRole.Owner
            && await accessService.IsSoleOwnerAsync(teamId, targetUserId, cancellationToken)) {
            return ScoutingResult.Conflict(
                "The team's only owner cannot leave or be removed; assign another owner first.");
        }

        db.ScoutingTeamMembers.Remove(member);
        await TouchTeamAsync(teamId, cancellationToken);
        if (!await TrySaveChangesAsync(cancellationToken)) {
            return ScoutingResult.Conflict(
                "The team was changed by someone else; please reload and try again.");
        }

        return ScoutingResult.Ok();
    }

    private async Task<ScoutingTeamDetail?> LoadTeamDetailAsync(
        Guid teamId,
        ScoutingTeamRole? myRole,
        CancellationToken cancellationToken) {
        var team = await db.ScoutingTeams
            .AsNoTracking()
            .Where(t => t.Id == teamId)
            .Select(t => new {
                t.Id,
                t.Name,
                t.CreatedAt,
                t.UpdatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (team is null) {
            return null;
        }

        var members = await db.ScoutingTeamMembers
            .AsNoTracking()
            .Where(m => m.ScoutingTeamId == teamId)
            .Select(m => new ScoutingMember {
                UserId = m.ShuttleUserId,
                Username = m.User.Username,
                Role = m.Role,
                CreatedAt = m.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        // Sort by privilege (Owner > Editor > Viewer) then username; Role persists as a string, so
        // ordering must happen in memory against the enum rather than the database column.
        members = members
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var boards = await db.ScoutingBoards
            .AsNoTracking()
            .Where(b => b.ScoutingTeamId == teamId)
            .OrderBy(b => b.Name)
            .Select(b => new ScoutingBoardSummary {
                Id = b.Id,
                Name = b.Name,
                DraftSeason = b.DraftSeason,
                EntryCount = b.Entries.Count,
                UpdatedAt = b.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return new ScoutingTeamDetail {
            Id = team.Id,
            Name = team.Name,
            MyRole = myRole,
            CreatedAt = team.CreatedAt,
            UpdatedAt = team.UpdatedAt,
            Members = members,
            Boards = boards,
        };
    }

    private async Task TouchTeamAsync(Guid teamId, CancellationToken cancellationToken) {
        var team = await db.ScoutingTeams.FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is not null) {
            team.UpdatedAt = Now;
        }
    }

    private static bool TryResolveUserId(ClaimsPrincipal principal, out Guid objectId) =>
        Guid.TryParse(Microsoft.Identity.Web.ClaimsPrincipalExtensions.GetObjectId(principal), out objectId);
}
