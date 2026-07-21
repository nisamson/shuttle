using Refit;
using Shuttle.Models.Scouting;

namespace Shuttle.Api.Client;

/// <summary>
/// Typed Refit client for the Shuttle backend API scouting endpoints (user-created scouting teams,
/// their draft boards, ranked entries, and comment threads). Every endpoint requires an
/// authenticated caller, so the client must be registered with the access-token handler (see
/// <see cref="ShuttleApiClientExtensions.AddShuttleScoutingClient"/>).
/// </summary>
public interface IShuttleScoutingClient {
    // Teams
    [Get("/scouting/teams")]
    Task<IReadOnlyList<ScoutingTeamSummary>> GetMyTeams(CancellationToken token = default);

    [Get("/scouting/teams/all")]
    Task<IReadOnlyList<ScoutingTeamSummary>> GetAllTeams(CancellationToken token = default);

    [Get("/scouting/teams/{teamId}")]
    Task<ScoutingTeamDetail> GetTeam(Guid teamId, CancellationToken token = default);

    [Post("/scouting/teams")]
    Task<ScoutingTeamDetail> CreateTeam([Body] CreateScoutingTeamRequest request, CancellationToken token = default);

    [Put("/scouting/teams/{teamId}")]
    Task<ScoutingTeamDetail> RenameTeam(Guid teamId, [Body] UpdateScoutingTeamRequest request, CancellationToken token = default);

    [Delete("/scouting/teams/{teamId}")]
    Task DeleteTeam(Guid teamId, CancellationToken token = default);

    // Members
    [Post("/scouting/teams/{teamId}/members")]
    Task<ScoutingMember> AddMember(Guid teamId, [Body] AddScoutingMemberRequest request, CancellationToken token = default);

    [Put("/scouting/teams/{teamId}/members/{userId}")]
    Task<ScoutingMember> UpdateMemberRole(Guid teamId, Guid userId, [Body] UpdateScoutingMemberRoleRequest request, CancellationToken token = default);

    [Delete("/scouting/teams/{teamId}/members/{userId}")]
    Task RemoveMember(Guid teamId, Guid userId, CancellationToken token = default);

    [Post("/scouting/teams/{teamId}/leave")]
    Task LeaveTeam(Guid teamId, CancellationToken token = default);

    // Boards
    [Post("/scouting/teams/{teamId}/boards")]
    Task<ScoutingBoardDetail> CreateBoard(Guid teamId, [Body] CreateScoutingBoardRequest request, CancellationToken token = default);

    [Get("/scouting/boards/{boardId}")]
    Task<ScoutingBoardDetail> GetBoard(Guid boardId, CancellationToken token = default);

    [Put("/scouting/boards/{boardId}")]
    Task<ScoutingBoardDetail> UpdateBoard(Guid boardId, [Body] UpdateScoutingBoardRequest request, CancellationToken token = default);

    [Delete("/scouting/boards/{boardId}")]
    Task DeleteBoard(Guid boardId, CancellationToken token = default);

    // Entries
    [Post("/scouting/boards/{boardId}/entries")]
    Task<ScoutingBoardEntry> AddEntry(Guid boardId, [Body] AddScoutingBoardEntryRequest request, CancellationToken token = default);

    [Post("/scouting/boards/{boardId}/entries/bulk")]
    Task<AddScoutingBoardEntriesResult> AddEntries(Guid boardId, [Body] AddScoutingBoardEntriesRequest request, CancellationToken token = default);

    [Delete("/scouting/boards/{boardId}/entries/{playerId}")]
    Task RemoveEntry(Guid boardId, int playerId, CancellationToken token = default);

    [Post("/scouting/boards/{boardId}/entries/remove")]
    Task RemoveEntries(Guid boardId, [Body] RemoveScoutingBoardEntriesRequest request, CancellationToken token = default);

    [Post("/scouting/boards/{boardId}/entries/move")]
    Task MoveEntry(Guid boardId, [Body] MoveScoutingBoardEntryRequest request, CancellationToken token = default);

    [Put("/scouting/boards/{boardId}/entries/{playerId}")]
    Task<ScoutingBoardEntry> UpdateEntry(Guid boardId, int playerId, [Body] UpdateScoutingBoardEntryRequest request, CancellationToken token = default);

    // Comments
    [Get("/scouting/boards/{boardId}/comments")]
    Task<IReadOnlyList<ScoutingComment>> GetBoardComments(Guid boardId, CancellationToken token = default);

    [Post("/scouting/boards/{boardId}/comments")]
    Task<ScoutingComment> AddBoardComment(Guid boardId, [Body] CreateScoutingCommentRequest request, CancellationToken token = default);

    [Get("/scouting/boards/{boardId}/entries/{playerId}/comments")]
    Task<IReadOnlyList<ScoutingComment>> GetEntryComments(Guid boardId, int playerId, CancellationToken token = default);

    [Post("/scouting/boards/{boardId}/entries/{playerId}/comments")]
    Task<ScoutingComment> AddEntryComment(Guid boardId, int playerId, [Body] CreateScoutingCommentRequest request, CancellationToken token = default);

    [Put("/scouting/comments/{commentId}")]
    Task<ScoutingComment> EditComment(Guid commentId, [Body] UpdateScoutingCommentRequest request, CancellationToken token = default);

    [Delete("/scouting/comments/{commentId}")]
    Task DeleteComment(Guid commentId, CancellationToken token = default);
}
