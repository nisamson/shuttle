using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Refit;
using Shuttle.Api.Client;
using Shuttle.Models.Scouting;

namespace Shuttle.WebClient.Testing;

/// <summary>
/// In-memory <see cref="IShuttleScoutingClient"/> that mirrors the server's scouting semantics
/// (roles, rank shifting, the sole-owner guard, and comment authorship) without any HTTP, backend, or
/// Azure dependency. The caller's identity is derived from the ambient
/// <see cref="AuthenticationStateProvider"/> (the <c>oid</c> claim, matching
/// <see cref="InMemoryShuttleUserClient"/>), so the WebClient's "You" badges and author-only edit
/// rules line up with what <see cref="ICurrentUserService"/> reports. Registered as a singleton so
/// state persists across navigation within a session.
/// </summary>
public sealed class InMemoryShuttleScoutingClient : IShuttleScoutingClient {
    private const string AdminRole = "Shuttle.Admin";

    private readonly AuthenticationStateProvider? authProvider;
    private readonly bool seedDemoContent;
    private readonly Dictionary<Guid, TeamState> teams = new();
    private readonly object gate = new();
    private bool seeded;

    public InMemoryShuttleScoutingClient(
        AuthenticationStateProvider? authProvider = null,
        bool seedDemoContent = false) {
        this.authProvider = authProvider;
        this.seedDemoContent = seedDemoContent;
    }

    // Teams -----------------------------------------------------------------

    public async Task<IReadOnlyList<ScoutingTeamSummary>> GetMyTeams(CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        lock (gate) {
            return teams.Values
                .Where(t => t.Member(caller.Id) is not null)
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .Select(t => Summary(t, t.Member(caller.Id)!.Role))
                .ToList();
        }
    }

    public async Task<IReadOnlyList<ScoutingTeamSummary>> GetAllTeams(CancellationToken token = default) {
        await ResolveCallerAsync();
        lock (gate) {
            return teams.Values
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .Select(t => Summary(t, null))
                .ToList();
        }
    }

    public async Task<ScoutingTeamDetail> GetTeam(Guid teamId, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        lock (gate) {
            var team = Require(teamId);
            var role = team.Member(caller.Id)?.Role;
            if (role is null && !caller.IsAdmin) {
                throw Problem(HttpMethod.Get, HttpStatusCode.Forbidden, "You are not a member of this team.");
            }

            return Detail(team, role);
        }
    }

    public async Task<ScoutingTeamDetail> CreateTeam(CreateScoutingTeamRequest request, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length is 0 or > ScoutingLimits.TeamNameMaxLength) {
            throw Problem(HttpMethod.Post, HttpStatusCode.BadRequest, "Team name is required.");
        }

        lock (gate) {
            var now = DateTimeOffset.UtcNow;
            var team = new TeamState { Id = Guid.NewGuid(), Name = name, CreatedAt = now, UpdatedAt = now };
            team.Members.Add(new MemberState {
                UserId = caller.Id,
                Username = caller.Username,
                Role = ScoutingTeamRole.Owner,
                CreatedAt = now,
            });
            teams[team.Id] = team;
            return Detail(team, ScoutingTeamRole.Owner);
        }
    }

    public async Task<ScoutingTeamDetail> RenameTeam(Guid teamId, UpdateScoutingTeamRequest request, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length is 0 or > ScoutingLimits.TeamNameMaxLength) {
            throw Problem(HttpMethod.Put, HttpStatusCode.BadRequest, "Team name is required.");
        }

        lock (gate) {
            var team = Require(teamId);
            RequireManage(team, caller, "Only team owners can rename the team.");
            team.Name = name;
            team.UpdatedAt = DateTimeOffset.UtcNow;
            return Detail(team, team.Member(caller.Id)?.Role);
        }
    }

    public async Task DeleteTeam(Guid teamId, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        lock (gate) {
            var team = Require(teamId);
            if (!caller.IsAdmin) {
                var role = team.Member(caller.Id)?.Role;
                if (role != ScoutingTeamRole.Owner) {
                    throw Problem(HttpMethod.Delete, HttpStatusCode.Forbidden,
                        "Only a team owner or a site admin can delete a team.");
                }

                if (team.OwnerCount > 1) {
                    throw Problem(HttpMethod.Delete, HttpStatusCode.Conflict,
                        "You must be the team's only owner to delete it; all other owners must leave first.");
                }
            }

            teams.Remove(teamId);
        }
    }

    // Members ---------------------------------------------------------------

    public async Task<ScoutingMember> AddMember(Guid teamId, AddScoutingMemberRequest request, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        if (!Enum.IsDefined(request.Role)) {
            throw Problem(HttpMethod.Post, HttpStatusCode.BadRequest, "An unknown member role was supplied.");
        }

        var username = (request.Username ?? string.Empty).Trim();
        if (username.Length == 0) {
            throw Problem(HttpMethod.Post, HttpStatusCode.BadRequest, "A username is required.");
        }

        var seedUser = SeedData.Users()
            .FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
        if (seedUser is null) {
            throw Problem(HttpMethod.Post, HttpStatusCode.BadRequest,
                $"No user named '{username}' was found. The user must have signed in at least once before they can be added.");
        }

        lock (gate) {
            var team = Require(teamId);
            RequireManage(team, caller, "Only team owners can add members.");

            var targetId = SeedUserId(seedUser.UserId);
            if (team.Member(targetId) is not null) {
                throw Problem(HttpMethod.Post, HttpStatusCode.Conflict,
                    $"'{username}' is already a member of this team.");
            }

            var member = new MemberState {
                UserId = targetId,
                Username = seedUser.Username,
                Role = request.Role,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            team.Members.Add(member);
            team.UpdatedAt = DateTimeOffset.UtcNow;
            return ToMemberDto(member);
        }
    }

    public async Task<ScoutingMember> UpdateMemberRole(Guid teamId, Guid userId, UpdateScoutingMemberRoleRequest request, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        if (!Enum.IsDefined(request.Role)) {
            throw Problem(HttpMethod.Put, HttpStatusCode.BadRequest, "An unknown member role was supplied.");
        }

        lock (gate) {
            var team = Require(teamId);
            RequireManage(team, caller, "Only team owners can change member roles.");

            var member = team.Member(userId)
                ?? throw Problem(HttpMethod.Put, HttpStatusCode.NotFound, "That user is not a member of this team.");

            if (member.Role == request.Role) {
                return ToMemberDto(member);
            }

            if (member.Role == ScoutingTeamRole.Owner && request.Role != ScoutingTeamRole.Owner
                && team.OwnerCount == 1) {
                throw Problem(HttpMethod.Put, HttpStatusCode.Conflict,
                    "You cannot demote the team's only owner; promote another owner first.");
            }

            member.Role = request.Role;
            team.UpdatedAt = DateTimeOffset.UtcNow;
            return ToMemberDto(member);
        }
    }

    public async Task RemoveMember(Guid teamId, Guid userId, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        lock (gate) {
            var team = Require(teamId);
            RequireManage(team, caller, "Only team owners can remove members.");
            RemoveMembership(team, userId, HttpMethod.Delete);
        }
    }

    public async Task LeaveTeam(Guid teamId, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        lock (gate) {
            var team = Require(teamId);
            if (team.Member(caller.Id) is null) {
                throw Problem(HttpMethod.Post, HttpStatusCode.Forbidden, "You are not a member of this team.");
            }

            RemoveMembership(team, caller.Id, HttpMethod.Post);
        }
    }

    private void RemoveMembership(TeamState team, Guid userId, HttpMethod method) {
        var member = team.Member(userId)
            ?? throw Problem(method, HttpStatusCode.NotFound, "That user is not a member of this team.");

        if (member.Role == ScoutingTeamRole.Owner && team.OwnerCount == 1) {
            throw Problem(method, HttpStatusCode.Conflict,
                "The team's only owner cannot leave or be removed; assign another owner first.");
        }

        team.Members.Remove(member);
        team.UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Boards ----------------------------------------------------------------

    public async Task<ScoutingBoardDetail> CreateBoard(Guid teamId, CreateScoutingBoardRequest request, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length is 0 or > ScoutingLimits.BoardNameMaxLength) {
            throw Problem(HttpMethod.Post, HttpStatusCode.BadRequest, "Board name is required.");
        }

        lock (gate) {
            var team = Require(teamId);
            RequireEditBoards(team, caller);
            var now = DateTimeOffset.UtcNow;
            var board = new BoardState {
                Id = Guid.NewGuid(),
                TeamId = teamId,
                Name = name,
                DraftSeason = request.DraftSeason,
                CreatedAt = now,
                UpdatedAt = now,
            };
            team.Boards.Add(board);
            team.UpdatedAt = now;
            return BoardDetail(board);
        }
    }

    public async Task<ScoutingBoardDetail> GetBoard(Guid boardId, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        lock (gate) {
            var (team, board) = RequireBoard(boardId);
            var role = team.Member(caller.Id)?.Role;
            if (role is null && !caller.IsAdmin) {
                throw Problem(HttpMethod.Get, HttpStatusCode.Forbidden, "You are not a member of this team.");
            }

            return BoardDetail(board);
        }
    }

    public async Task<ScoutingBoardDetail> UpdateBoard(Guid boardId, UpdateScoutingBoardRequest request, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length is 0 or > ScoutingLimits.BoardNameMaxLength) {
            throw Problem(HttpMethod.Put, HttpStatusCode.BadRequest, "Board name is required.");
        }

        lock (gate) {
            var (team, board) = RequireBoard(boardId);
            RequireEditBoards(team, caller);
            board.Name = name;
            board.DraftSeason = request.DraftSeason;
            board.UpdatedAt = DateTimeOffset.UtcNow;
            return BoardDetail(board);
        }
    }

    public async Task DeleteBoard(Guid boardId, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        lock (gate) {
            var (team, board) = RequireBoard(boardId);
            RequireEditBoards(team, caller);
            team.Boards.Remove(board);
            team.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    // Entries ---------------------------------------------------------------

    public async Task<ScoutingBoardEntry> AddEntry(Guid boardId, AddScoutingBoardEntryRequest request, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        lock (gate) {
            var (team, board) = RequireBoard(boardId);
            RequireEditBoards(team, caller);

            if (board.Entries.Any(e => e.PlayerId == request.PlayerId)) {
                throw Problem(HttpMethod.Post, HttpStatusCode.Conflict, "That player is already on this board.");
            }

            var maxRank = board.Entries.Count == 0 ? 0 : board.Entries.Max(e => e.Rank);
            var entry = new EntryState {
                Id = Guid.NewGuid(),
                PlayerId = request.PlayerId,
                Rank = maxRank + 1,
            };
            board.Entries.Add(entry);
            board.UpdatedAt = DateTimeOffset.UtcNow;
            return new ScoutingBoardEntry {
                Id = entry.Id,
                PlayerId = entry.PlayerId,
                Rank = entry.Rank,
                CommentCount = 0,
            };
        }
    }

    public async Task RemoveEntry(Guid boardId, int playerId, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        lock (gate) {
            var (team, board) = RequireBoard(boardId);
            RequireEditBoards(team, caller);

            var removed = board.Entries.FirstOrDefault(e => e.PlayerId == playerId)
                ?? throw Problem(HttpMethod.Delete, HttpStatusCode.NotFound, "That player is not on this board.");

            board.Entries.Remove(removed);
            foreach (var entry in board.Entries.Where(e => e.Rank > removed.Rank)) {
                entry.Rank--;
            }

            board.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public async Task RemoveEntries(Guid boardId, RemoveScoutingBoardEntriesRequest request, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        lock (gate) {
            var (team, board) = RequireBoard(boardId);
            RequireEditBoards(team, caller);

            var playerIds = request.PlayerIds.Distinct().ToHashSet();
            if (playerIds.Count == 0) {
                throw Problem(HttpMethod.Post, HttpStatusCode.BadRequest, "No players were selected for removal.");
            }

            var removed = board.Entries.Where(e => playerIds.Contains(e.PlayerId)).ToList();
            if (removed.Count == 0) {
                throw Problem(HttpMethod.Post, HttpStatusCode.NotFound, "None of the selected players are on this board.");
            }

            var removedIds = removed.Select(e => e.Id).ToHashSet();
            board.Comments.RemoveAll(c => c.EntryId is { } entryId && removedIds.Contains(entryId));
            board.Entries.RemoveAll(e => removedIds.Contains(e.Id));

            var rank = 1;
            foreach (var entry in board.Entries.OrderBy(e => e.Rank).ToList()) {
                entry.Rank = rank++;
            }

            board.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public async Task MoveEntry(Guid boardId, MoveScoutingBoardEntryRequest request, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        lock (gate) {
            var (team, board) = RequireBoard(boardId);
            RequireEditBoards(team, caller);

            var moved = board.Entries.FirstOrDefault(e => e.PlayerId == request.PlayerId)
                ?? throw Problem(HttpMethod.Post, HttpStatusCode.NotFound, "That player is not on this board.");

            if (moved.Rank != request.FromRank) {
                throw Problem(HttpMethod.Post, HttpStatusCode.Conflict,
                    "The player's position has changed since you loaded the board; refresh and try again.");
            }

            if (request.ToRank < 1 || request.ToRank > board.Entries.Count) {
                throw Problem(HttpMethod.Post, HttpStatusCode.BadRequest,
                    $"Target rank must be between 1 and {board.Entries.Count}.");
            }

            var from = moved.Rank;
            var to = request.ToRank;
            if (from == to) {
                return;
            }

            if (to < from) {
                foreach (var entry in board.Entries.Where(e => e.Rank >= to && e.Rank < from)) {
                    entry.Rank++;
                }
            } else {
                foreach (var entry in board.Entries.Where(e => e.Rank > from && e.Rank <= to)) {
                    entry.Rank--;
                }
            }

            moved.Rank = to;
            board.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    // Comments --------------------------------------------------------------

    public async Task<IReadOnlyList<ScoutingComment>> GetBoardComments(Guid boardId, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        lock (gate) {
            var (team, board) = RequireBoard(boardId);
            RequireView(team, caller);
            return Thread(board, entryId: null);
        }
    }

    public async Task<ScoutingComment> AddBoardComment(Guid boardId, CreateScoutingCommentRequest request, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        var body = ValidateBody(request.Body, HttpMethod.Post);
        lock (gate) {
            var (team, board) = RequireBoard(boardId);
            RequireComment(team, caller);
            return AddComment(board, entryId: null, body, caller);
        }
    }

    public async Task<IReadOnlyList<ScoutingComment>> GetEntryComments(Guid boardId, int playerId, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        lock (gate) {
            var (team, board) = RequireBoard(boardId);
            RequireView(team, caller);
            var entry = RequireEntry(board, playerId, HttpMethod.Get);
            return Thread(board, entry.Id);
        }
    }

    public async Task<ScoutingComment> AddEntryComment(Guid boardId, int playerId, CreateScoutingCommentRequest request, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        var body = ValidateBody(request.Body, HttpMethod.Post);
        lock (gate) {
            var (team, board) = RequireBoard(boardId);
            RequireComment(team, caller);
            var entry = RequireEntry(board, playerId, HttpMethod.Post);
            return AddComment(board, entry.Id, body, caller);
        }
    }

    public async Task<ScoutingComment> EditComment(Guid commentId, UpdateScoutingCommentRequest request, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        var body = ValidateBody(request.Body, HttpMethod.Put);
        lock (gate) {
            var (team, board, comment) = RequireComment(commentId);
            if (comment.AuthorUserId != caller.Id) {
                throw Problem(HttpMethod.Put, HttpStatusCode.Forbidden, "You can only edit your own comments.");
            }

            if (!CanComment(team, caller)) {
                throw Problem(HttpMethod.Put, HttpStatusCode.Forbidden,
                    "You no longer have permission to post on this team's boards.");
            }

            comment.Body = body;
            comment.EditedAt = DateTimeOffset.UtcNow;
            return ToCommentDto(board, comment);
        }
    }

    public async Task DeleteComment(Guid commentId, CancellationToken token = default) {
        var caller = await ResolveCallerAsync();
        lock (gate) {
            var (team, board, comment) = RequireComment(commentId);
            if (comment.AuthorUserId != caller.Id && !CanModerate(team, caller)) {
                throw Problem(HttpMethod.Delete, HttpStatusCode.Forbidden,
                    "You can only delete your own comments unless you are an owner or admin.");
            }

            board.Comments.Remove(comment);
        }
    }

    // Permission helpers ----------------------------------------------------

    private void RequireManage(TeamState team, Caller caller, string message) {
        if (!(caller.IsAdmin || team.Member(caller.Id)?.Role == ScoutingTeamRole.Owner)) {
            throw Problem(HttpMethod.Post, HttpStatusCode.Forbidden, message);
        }
    }

    private void RequireEditBoards(TeamState team, Caller caller) {
        if (!(caller.IsAdmin || team.Member(caller.Id)?.Role >= ScoutingTeamRole.Editor)) {
            throw Problem(HttpMethod.Post, HttpStatusCode.Forbidden,
                "You do not have permission to edit this team's boards.");
        }
    }

    private void RequireView(TeamState team, Caller caller) {
        if (!(caller.IsAdmin || team.Member(caller.Id) is not null)) {
            throw Problem(HttpMethod.Get, HttpStatusCode.Forbidden, "You are not a member of this team.");
        }
    }

    private void RequireComment(TeamState team, Caller caller) {
        if (!CanComment(team, caller)) {
            throw Problem(HttpMethod.Post, HttpStatusCode.Forbidden,
                "You do not have permission to comment on this team's boards.");
        }
    }

    private static bool CanComment(TeamState team, Caller caller) =>
        caller.IsAdmin || team.Member(caller.Id)?.Role >= ScoutingTeamRole.Editor;

    private static bool CanModerate(TeamState team, Caller caller) =>
        caller.IsAdmin || team.Member(caller.Id)?.Role == ScoutingTeamRole.Owner;

    // Lookups ---------------------------------------------------------------

    private TeamState Require(Guid teamId) =>
        teams.TryGetValue(teamId, out var team)
            ? team
            : throw Problem(HttpMethod.Get, HttpStatusCode.NotFound, "Team not found.");

    private (TeamState Team, BoardState Board) RequireBoard(Guid boardId) {
        foreach (var team in teams.Values) {
            var board = team.Boards.FirstOrDefault(b => b.Id == boardId);
            if (board is not null) {
                return (team, board);
            }
        }

        throw Problem(HttpMethod.Get, HttpStatusCode.NotFound, "Board not found.");
    }

    private (TeamState Team, BoardState Board, CommentState Comment) RequireComment(Guid commentId) {
        foreach (var team in teams.Values) {
            foreach (var board in team.Boards) {
                var comment = board.Comments.FirstOrDefault(c => c.Id == commentId);
                if (comment is not null) {
                    return (team, board, comment);
                }
            }
        }

        throw Problem(HttpMethod.Put, HttpStatusCode.NotFound, "Comment not found.");
    }

    private static EntryState RequireEntry(BoardState board, int playerId, HttpMethod method) =>
        board.Entries.FirstOrDefault(e => e.PlayerId == playerId)
        ?? throw Problem(method, HttpStatusCode.NotFound, "That player is not on this board.");

    private static string ValidateBody(string? body, HttpMethod method) {
        var trimmed = (body ?? string.Empty).Trim();
        if (trimmed.Length is 0 or > ScoutingLimits.CommentBodyMaxLength) {
            throw Problem(method, HttpStatusCode.BadRequest, "Comment body is required.");
        }

        return trimmed;
    }

    private ScoutingComment AddComment(BoardState board, Guid? entryId, string body, Caller caller) {
        var comment = new CommentState {
            Id = Guid.NewGuid(),
            EntryId = entryId,
            AuthorUserId = caller.Id,
            AuthorUsername = caller.Username,
            Body = body,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        board.Comments.Add(comment);
        return ToCommentDto(board, comment);
    }

    // Projection ------------------------------------------------------------

    private static ScoutingTeamSummary Summary(TeamState team, ScoutingTeamRole? myRole) => new() {
        Id = team.Id,
        Name = team.Name,
        MyRole = myRole,
        MemberCount = team.Members.Count,
        BoardCount = team.Boards.Count,
        UpdatedAt = team.UpdatedAt,
    };

    private static ScoutingTeamDetail Detail(TeamState team, ScoutingTeamRole? myRole) => new() {
        Id = team.Id,
        Name = team.Name,
        MyRole = myRole,
        CreatedAt = team.CreatedAt,
        UpdatedAt = team.UpdatedAt,
        Members = team.Members
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.Username, StringComparer.OrdinalIgnoreCase)
            .Select(ToMemberDto)
            .ToList(),
        Boards = team.Boards
            .OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
            .Select(b => new ScoutingBoardSummary {
                Id = b.Id,
                Name = b.Name,
                DraftSeason = b.DraftSeason,
                EntryCount = b.Entries.Count,
                UpdatedAt = b.UpdatedAt,
            })
            .ToList(),
    };

    private static ScoutingBoardDetail BoardDetail(BoardState board) => new() {
        Id = board.Id,
        ScoutingTeamId = board.TeamId,
        Name = board.Name,
        DraftSeason = board.DraftSeason,
        CreatedAt = board.CreatedAt,
        UpdatedAt = board.UpdatedAt,
        Entries = board.Entries
            .OrderBy(e => e.Rank)
            .Select(e => new ScoutingBoardEntry {
                Id = e.Id,
                PlayerId = e.PlayerId,
                Rank = e.Rank,
                CommentCount = board.Comments.Count(c => c.EntryId == e.Id),
            })
            .ToList(),
    };

    private static IReadOnlyList<ScoutingComment> Thread(BoardState board, Guid? entryId) =>
        board.Comments
            .Where(c => c.EntryId == entryId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => ToCommentDto(board, c))
            .ToList();

    private static ScoutingMember ToMemberDto(MemberState member) => new() {
        UserId = member.UserId,
        Username = member.Username,
        Role = member.Role,
        CreatedAt = member.CreatedAt,
    };

    private static ScoutingComment ToCommentDto(BoardState board, CommentState comment) => new() {
        Id = comment.Id,
        BoardId = board.Id,
        EntryId = comment.EntryId,
        AuthorUserId = comment.AuthorUserId,
        AuthorUsername = comment.AuthorUsername,
        Body = comment.Body,
        CreatedAt = comment.CreatedAt,
        EditedAt = comment.EditedAt,
    };

    // Identity --------------------------------------------------------------

    private async Task<Caller> ResolveCallerAsync() {
        var id = Guid.Empty;
        var username = "Test Scout";
        var isAdmin = false;
        var authenticated = false;

        if (authProvider is not null) {
            var state = await authProvider.GetAuthenticationStateAsync();
            var user = state.User;
            authenticated = user.Identity?.IsAuthenticated == true;
            if (Guid.TryParse(user.FindFirst("oid")?.Value, out var parsed)) {
                id = parsed;
            }

            username = user.FindFirst("name")?.Value ?? user.Identity?.Name ?? username;
            isAdmin = user.IsInRole(AdminRole);
        }

        if (!authenticated) {
            throw Problem(HttpMethod.Get, HttpStatusCode.Unauthorized, "Authentication is required.");
        }

        EnsureSeeded(new Caller(id, username, isAdmin));
        return new Caller(id, username, isAdmin);
    }

    // Lazily plant a demo team (owned by the caller) the first time an authenticated caller is seen,
    // so the WebClient's fake-backend run mode shows meaningful content offline.
    private void EnsureSeeded(Caller caller) {
        if (!seedDemoContent) {
            return;
        }

        lock (gate) {
            if (seeded) {
                return;
            }

            seeded = true;
            var now = DateTimeOffset.UtcNow;
            var team = new TeamState { Id = Guid.NewGuid(), Name = "Demo Scouts", CreatedAt = now, UpdatedAt = now };
            team.Members.Add(new MemberState {
                UserId = caller.Id, Username = caller.Username, Role = ScoutingTeamRole.Owner, CreatedAt = now,
            });

            var seedUsers = SeedData.Users();
            var editor = seedUsers.ElementAtOrDefault(0);
            if (editor is not null) {
                team.Members.Add(new MemberState {
                    UserId = SeedUserId(editor.UserId), Username = editor.Username,
                    Role = ScoutingTeamRole.Editor, CreatedAt = now,
                });
            }

            var board = new BoardState {
                Id = Guid.NewGuid(), TeamId = team.Id, Name = "Top Draft Prospects",
                DraftSeason = 73, CreatedAt = now, UpdatedAt = now,
            };
            var players = SeedData.Players();
            var rank = 1;
            foreach (var player in players.Take(3)) {
                board.Entries.Add(new EntryState { Id = Guid.NewGuid(), PlayerId = player.PlayerId, Rank = rank++ });
            }

            board.Comments.Add(new CommentState {
                Id = Guid.NewGuid(), EntryId = null, AuthorUserId = caller.Id, AuthorUsername = caller.Username,
                Body = "Kicking off our board for the upcoming draft.", CreatedAt = now,
            });

            team.Boards.Add(board);
            teams[team.Id] = team;
        }
    }

    // Deterministic ShuttleUser Guid for a seeded (int-keyed) user.
    private static Guid SeedUserId(int userId) => new($"00000000-0000-0000-0000-{userId:D12}");

    private static ApiException Problem(HttpMethod method, HttpStatusCode status, string detail) {
        using var request = new HttpRequestMessage(method, "/scouting");
        using var response = new HttpResponseMessage(status) {
            Content = new StringContent(
                JsonSerializer.Serialize(new ProblemBody(status.ToString(), detail)),
                Encoding.UTF8,
                "application/problem+json"),
        };
        return ApiException.Create(request, method, response, new RefitSettings()).GetAwaiter().GetResult();
    }

    private sealed record ProblemBody(string Title, string Detail);

    private readonly record struct Caller(Guid Id, string Username, bool IsAdmin);

    private sealed class TeamState {
        public required Guid Id { get; init; }
        public required string Name { get; set; }
        public required DateTimeOffset CreatedAt { get; init; }
        public required DateTimeOffset UpdatedAt { get; set; }
        public List<MemberState> Members { get; } = [];
        public List<BoardState> Boards { get; } = [];

        public MemberState? Member(Guid userId) => Members.FirstOrDefault(m => m.UserId == userId);
        public int OwnerCount => Members.Count(m => m.Role == ScoutingTeamRole.Owner);
    }

    private sealed class MemberState {
        public required Guid UserId { get; init; }
        public required string Username { get; init; }
        public required ScoutingTeamRole Role { get; set; }
        public required DateTimeOffset CreatedAt { get; init; }
    }

    private sealed class BoardState {
        public required Guid Id { get; init; }
        public required Guid TeamId { get; init; }
        public required string Name { get; set; }
        public int? DraftSeason { get; set; }
        public required DateTimeOffset CreatedAt { get; init; }
        public required DateTimeOffset UpdatedAt { get; set; }
        public List<EntryState> Entries { get; } = [];
        public List<CommentState> Comments { get; } = [];
    }

    private sealed class EntryState {
        public required Guid Id { get; init; }
        public required int PlayerId { get; init; }
        public required int Rank { get; set; }
    }

    private sealed class CommentState {
        public required Guid Id { get; init; }
        public required Guid? EntryId { get; init; }
        public required Guid AuthorUserId { get; init; }
        public required string AuthorUsername { get; init; }
        public required string Body { get; set; }
        public required DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? EditedAt { get; set; }
    }
}
