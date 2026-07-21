using System.Security.Claims;
using Shuttle.Models.Scouting;

namespace Shuttle.Api.Services.Scouting;

/// <summary>
/// All business logic for user-created scouting teams: teams, membership and roles, draft boards,
/// ranked entries, and comment threads. Every operation authorizes the caller through
/// <see cref="IScoutingAccessService"/> and enforces the domain invariants (a team is never left
/// ownerless; ranks stay contiguous). Results are returned as ASP.NET-free
/// <see cref="ScoutingResult"/>/<see cref="ScoutingResult{T}"/> values for controllers to map.
/// </summary>
public interface IScoutingService {
    // Teams
    Task<IReadOnlyList<ScoutingTeamSummary>> GetMyTeamsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScoutingTeamSummary>> GetAllTeamsAsync(CancellationToken cancellationToken = default);
    Task<ScoutingResult<ScoutingTeamDetail>> GetTeamAsync(Guid teamId, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ScoutingResult<ScoutingTeamDetail>> CreateTeamAsync(CreateScoutingTeamRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ScoutingResult<ScoutingTeamDetail>> RenameTeamAsync(Guid teamId, UpdateScoutingTeamRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ScoutingResult> DeleteTeamAsync(Guid teamId, ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    // Members
    Task<ScoutingResult<ScoutingMember>> AddMemberAsync(Guid teamId, AddScoutingMemberRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ScoutingResult<ScoutingMember>> UpdateMemberRoleAsync(Guid teamId, Guid targetUserId, UpdateScoutingMemberRoleRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ScoutingResult> RemoveMemberAsync(Guid teamId, Guid targetUserId, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ScoutingResult> LeaveTeamAsync(Guid teamId, ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    // Boards
    Task<ScoutingResult<ScoutingBoardDetail>> CreateBoardAsync(Guid teamId, CreateScoutingBoardRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ScoutingResult<ScoutingBoardDetail>> GetBoardAsync(Guid boardId, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ScoutingResult<ScoutingBoardDetail>> UpdateBoardAsync(Guid boardId, UpdateScoutingBoardRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ScoutingResult> DeleteBoardAsync(Guid boardId, ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    // Entries
    Task<ScoutingResult<ScoutingBoardEntry>> AddEntryAsync(Guid boardId, AddScoutingBoardEntryRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ScoutingResult<AddScoutingBoardEntriesResult>> AddEntriesAsync(Guid boardId, AddScoutingBoardEntriesRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ScoutingResult> RemoveEntryAsync(Guid boardId, int playerId, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ScoutingResult> RemoveEntriesAsync(Guid boardId, RemoveScoutingBoardEntriesRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ScoutingResult> MoveEntryAsync(Guid boardId, MoveScoutingBoardEntryRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    // Comments
    Task<ScoutingResult<IReadOnlyList<ScoutingComment>>> GetBoardCommentsAsync(Guid boardId, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ScoutingResult<IReadOnlyList<ScoutingComment>>> GetEntryCommentsAsync(Guid boardId, int playerId, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ScoutingResult<ScoutingComment>> AddBoardCommentAsync(Guid boardId, CreateScoutingCommentRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ScoutingResult<ScoutingComment>> AddEntryCommentAsync(Guid boardId, int playerId, CreateScoutingCommentRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ScoutingResult<ScoutingComment>> EditCommentAsync(Guid commentId, UpdateScoutingCommentRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<ScoutingResult> DeleteCommentAsync(Guid commentId, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}

public static class ScoutingServiceRegistration {
    public static IServiceCollection AddScoutingService(this IServiceCollection services) {
        services.AddScoped<IScoutingService, ScoutingService>();
        return services;
    }
}
