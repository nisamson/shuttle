using System.Net;
using Refit;
using Shuttle.Models.Scouting;
using Shuttle.WebClient.Testing;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Pure (no-render) tests for <see cref="InMemoryShuttleScoutingClient"/> proving its team/board/
/// comment behaviour — role permissions, the sole-owner guards, rank shifting, and comment
/// authorship rules — match what the WebClient expects from the real scouting API. A single client
/// backed by a mutable <see cref="FakeAuthenticationStateProvider"/> lets a test act as several
/// identities against the same shared state.
/// </summary>
public class InMemoryShuttleScoutingClientTests {
    private const string OwnerId = "00000000-0000-0000-0000-0000000000a1";
    private const string OutsiderId = "00000000-0000-0000-0000-0000000000a2";

    private readonly FakeAuthenticationStateProvider auth;
    private readonly InMemoryShuttleScoutingClient client;

    public InMemoryShuttleScoutingClientTests() {
        auth = new FakeAuthenticationStateProvider(new FakeAuthOptions {
            IsAuthenticated = true,
            UserId = OwnerId,
            UserName = "Owner",
            Roles = [],
        });
        client = new InMemoryShuttleScoutingClient(auth);
    }

    // Deterministic ShuttleUser guid for a seeded int-keyed user (mirrors the client's own mapping).
    private static string SeedUserId(int userId) => $"00000000-0000-0000-0000-{userId:D12}";

    private void As(string userId, params string[] roles) =>
        auth.SetUser(o => {
            o.IsAuthenticated = true;
            o.UserId = userId;
            o.Roles = roles.ToList();
        });

    private void Anonymous() => auth.SetUser(o => o.IsAuthenticated = false);

    private static (string Username, string Id) FirstSeedUser() {
        var user = SeedData.Users()[0];
        return (user.Username, SeedUserId(user.UserId));
    }

    private async Task<ScoutingTeamDetail> NewTeamAsync(string name = "Scouts") =>
        await client.CreateTeam(new CreateScoutingTeamRequest { Name = name });

    // Teams -----------------------------------------------------------------

    [Fact]
    public async Task CreateTeam_makes_the_caller_the_sole_owner() {
        var team = await NewTeamAsync();

        Assert.Equal(ScoutingTeamRole.Owner, team.MyRole);
        var member = Assert.Single(team.Members);
        Assert.Equal(Guid.Parse(OwnerId), member.UserId);
        Assert.Equal(ScoutingTeamRole.Owner, member.Role);
    }

    [Fact]
    public async Task GetMyTeams_lists_only_teams_the_caller_belongs_to() {
        var mine = await NewTeamAsync("Mine");

        As(OutsiderId);
        var outsiderView = await client.GetMyTeams();

        Assert.DoesNotContain(outsiderView, t => t.Id == mine.Id);
    }

    [Fact]
    public async Task GetTeam_forbids_non_members() {
        var team = await NewTeamAsync();

        As(OutsiderId);
        var ex = await Assert.ThrowsAnyAsync<ApiException>(() => client.GetTeam(team.Id));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }

    [Fact]
    public async Task GetTeam_allows_a_site_admin_who_is_not_a_member() {
        var team = await NewTeamAsync();

        As(OutsiderId, "Shuttle.Admin");
        var detail = await client.GetTeam(team.Id);

        // A non-member admin sees the team but has no role of their own.
        Assert.Null(detail.MyRole);
        Assert.Equal(team.Id, detail.Id);
    }

    [Fact]
    public async Task RenameTeam_requires_owner() {
        var team = await NewTeamAsync();
        var (username, editorId) = FirstSeedUser();
        await client.AddMember(team.Id, new AddScoutingMemberRequest { Username = username, Role = ScoutingTeamRole.Editor });

        As(editorId);
        var ex = await Assert.ThrowsAnyAsync<ApiException>(
            () => client.RenameTeam(team.Id, new UpdateScoutingTeamRequest { Name = "Renamed" }));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }

    // Members ---------------------------------------------------------------

    [Fact]
    public async Task AddMember_rejects_an_unknown_username() {
        var team = await NewTeamAsync();

        var ex = await Assert.ThrowsAnyAsync<ApiException>(
            () => client.AddMember(team.Id, new AddScoutingMemberRequest { Username = "nobody-here" }));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task AddMember_rejects_a_duplicate() {
        var team = await NewTeamAsync();
        var (username, _) = FirstSeedUser();
        await client.AddMember(team.Id, new AddScoutingMemberRequest { Username = username });

        var ex = await Assert.ThrowsAnyAsync<ApiException>(
            () => client.AddMember(team.Id, new AddScoutingMemberRequest { Username = username }));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
    }

    [Fact]
    public async Task SoleOwner_cannot_leave() {
        var team = await NewTeamAsync();

        var ex = await Assert.ThrowsAnyAsync<ApiException>(() => client.LeaveTeam(team.Id));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
    }

    [Fact]
    public async Task SoleOwner_cannot_be_demoted() {
        var team = await NewTeamAsync();

        var ex = await Assert.ThrowsAnyAsync<ApiException>(() => client.UpdateMemberRole(
            team.Id, Guid.Parse(OwnerId), new UpdateScoutingMemberRoleRequest { Role = ScoutingTeamRole.Editor }));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
    }

    [Fact]
    public async Task Owner_can_leave_after_promoting_a_second_owner() {
        var team = await NewTeamAsync();
        var (username, coOwnerId) = FirstSeedUser();
        await client.AddMember(team.Id, new AddScoutingMemberRequest { Username = username });
        await client.UpdateMemberRole(team.Id, Guid.Parse(coOwnerId),
            new UpdateScoutingMemberRoleRequest { Role = ScoutingTeamRole.Owner });

        await client.LeaveTeam(team.Id);

        // The original owner is gone; the promoted co-owner still sees the team.
        As(coOwnerId);
        var remaining = await client.GetTeam(team.Id);
        Assert.DoesNotContain(remaining.Members, m => m.UserId == Guid.Parse(OwnerId));
        Assert.Equal(ScoutingTeamRole.Owner, remaining.MyRole);
    }

    [Fact]
    public async Task DeleteTeam_forbids_non_owners() {
        var team = await NewTeamAsync();
        var (username, editorId) = FirstSeedUser();
        await client.AddMember(team.Id, new AddScoutingMemberRequest { Username = username, Role = ScoutingTeamRole.Editor });

        As(editorId);
        var ex = await Assert.ThrowsAnyAsync<ApiException>(() => client.DeleteTeam(team.Id));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }

    [Fact]
    public async Task DeleteTeam_conflicts_when_more_than_one_owner() {
        var team = await NewTeamAsync();
        var (username, coOwnerId) = FirstSeedUser();
        await client.AddMember(team.Id, new AddScoutingMemberRequest { Username = username });
        await client.UpdateMemberRole(team.Id, Guid.Parse(coOwnerId),
            new UpdateScoutingMemberRoleRequest { Role = ScoutingTeamRole.Owner });

        var ex = await Assert.ThrowsAnyAsync<ApiException>(() => client.DeleteTeam(team.Id));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
    }

    [Fact]
    public async Task DeleteTeam_succeeds_for_the_sole_owner() {
        var team = await NewTeamAsync();

        await client.DeleteTeam(team.Id);

        var ex = await Assert.ThrowsAnyAsync<ApiException>(() => client.GetTeam(team.Id));
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task DeleteTeam_always_succeeds_for_a_site_admin() {
        var team = await NewTeamAsync();
        var (username, coOwnerId) = FirstSeedUser();
        await client.AddMember(team.Id, new AddScoutingMemberRequest { Username = username });
        await client.UpdateMemberRole(team.Id, Guid.Parse(coOwnerId),
            new UpdateScoutingMemberRoleRequest { Role = ScoutingTeamRole.Owner });

        // An admin can delete a multi-owner team the owners themselves could not.
        As(OutsiderId, "Shuttle.Admin");
        await client.DeleteTeam(team.Id);

        var ex = await Assert.ThrowsAnyAsync<ApiException>(() => client.GetTeam(team.Id));
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    // Boards & entries ------------------------------------------------------

    [Fact]
    public async Task CreateBoard_forbids_a_viewer_but_allows_an_editor() {
        var team = await NewTeamAsync();
        var (username, editorId) = FirstSeedUser();
        await client.AddMember(team.Id, new AddScoutingMemberRequest { Username = username, Role = ScoutingTeamRole.Viewer });

        As(editorId);
        var forbidden = await Assert.ThrowsAnyAsync<ApiException>(
            () => client.CreateBoard(team.Id, new CreateScoutingBoardRequest { Name = "Board" }));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        As(OwnerId);
        await client.UpdateMemberRole(team.Id, Guid.Parse(editorId),
            new UpdateScoutingMemberRoleRequest { Role = ScoutingTeamRole.Editor });

        As(editorId);
        var board = await client.CreateBoard(team.Id, new CreateScoutingBoardRequest { Name = "Board" });
        Assert.Equal("Board", board.Name);
    }

    [Fact]
    public async Task AddEntry_appends_at_the_next_rank_and_rejects_duplicates() {
        var board = await NewBoardAsync();

        var first = await client.AddEntry(board.Id, new AddScoutingBoardEntryRequest { PlayerId = 1001 });
        var second = await client.AddEntry(board.Id, new AddScoutingBoardEntryRequest { PlayerId = 1002 });

        Assert.Equal(1, first.Rank);
        Assert.Equal(2, second.Rank);

        var dup = await Assert.ThrowsAnyAsync<ApiException>(
            () => client.AddEntry(board.Id, new AddScoutingBoardEntryRequest { PlayerId = 1001 }));
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
    }

    [Fact]
    public async Task MoveEntry_reorders_and_guards_against_a_stale_from_rank() {
        var board = await NewBoardAsync();
        await client.AddEntry(board.Id, new AddScoutingBoardEntryRequest { PlayerId = 1001 });
        await client.AddEntry(board.Id, new AddScoutingBoardEntryRequest { PlayerId = 1002 });
        await client.AddEntry(board.Id, new AddScoutingBoardEntryRequest { PlayerId = 1003 });

        // Stale FromRank is rejected.
        var stale = await Assert.ThrowsAnyAsync<ApiException>(() => client.MoveEntry(board.Id,
            new MoveScoutingBoardEntryRequest { PlayerId = 1003, FromRank = 1, ToRank = 1 }));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        // Move player 1003 from rank 3 to rank 1; the others shift down.
        await client.MoveEntry(board.Id,
            new MoveScoutingBoardEntryRequest { PlayerId = 1003, FromRank = 3, ToRank = 1 });

        var reloaded = await client.GetBoard(board.Id);
        Assert.Equal([1003, 1001, 1002], reloaded.Entries.OrderBy(e => e.Rank).Select(e => e.PlayerId));
        Assert.Equal([1, 2, 3], reloaded.Entries.OrderBy(e => e.Rank).Select(e => e.Rank));
    }

    [Fact]
    public async Task RemoveEntry_renumbers_the_remaining_ranks() {
        var board = await NewBoardAsync();
        await client.AddEntry(board.Id, new AddScoutingBoardEntryRequest { PlayerId = 1001 });
        await client.AddEntry(board.Id, new AddScoutingBoardEntryRequest { PlayerId = 1002 });
        await client.AddEntry(board.Id, new AddScoutingBoardEntryRequest { PlayerId = 1003 });

        await client.RemoveEntry(board.Id, 1002);

        var reloaded = await client.GetBoard(board.Id);
        Assert.Equal([1001, 1003], reloaded.Entries.OrderBy(e => e.Rank).Select(e => e.PlayerId));
        Assert.Equal([1, 2], reloaded.Entries.OrderBy(e => e.Rank).Select(e => e.Rank));
    }

    // Comments --------------------------------------------------------------

    [Fact]
    public async Task Comment_can_be_edited_only_by_its_author() {
        var board = await NewBoardAsync();
        var (username, editorId) = FirstSeedUser();
        await client.AddMember(board.ScoutingTeamId, new AddScoutingMemberRequest { Username = username, Role = ScoutingTeamRole.Editor });

        var comment = await client.AddBoardComment(board.Id, new CreateScoutingCommentRequest { Body = "Mine" });

        // A different member cannot edit someone else's comment.
        As(editorId);
        var ex = await Assert.ThrowsAnyAsync<ApiException>(() => client.EditComment(
            comment.Id, new UpdateScoutingCommentRequest { Body = "Hijacked" }));
        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);

        // The author can.
        As(OwnerId);
        var edited = await client.EditComment(comment.Id, new UpdateScoutingCommentRequest { Body = "Updated" });
        Assert.Equal("Updated", edited.Body);
        Assert.NotNull(edited.EditedAt);
    }

    [Fact]
    public async Task Demoted_author_can_no_longer_edit_their_own_comment() {
        var board = await NewBoardAsync();
        var (username, editorId) = FirstSeedUser();
        await client.AddMember(board.ScoutingTeamId, new AddScoutingMemberRequest { Username = username, Role = ScoutingTeamRole.Editor });

        As(editorId);
        var comment = await client.AddBoardComment(board.Id, new CreateScoutingCommentRequest { Body = "From the editor" });

        // The owner demotes the author to Viewer, stripping their posting rights.
        As(OwnerId);
        await client.UpdateMemberRole(board.ScoutingTeamId, Guid.Parse(editorId),
            new UpdateScoutingMemberRoleRequest { Role = ScoutingTeamRole.Viewer });

        As(editorId);
        var ex = await Assert.ThrowsAnyAsync<ApiException>(() => client.EditComment(
            comment.Id, new UpdateScoutingCommentRequest { Body = "Sneaky edit" }));
        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }

    [Fact]
    public async Task Owner_can_delete_another_members_comment() {
        var board = await NewBoardAsync();
        var (username, editorId) = FirstSeedUser();
        await client.AddMember(board.ScoutingTeamId, new AddScoutingMemberRequest { Username = username, Role = ScoutingTeamRole.Editor });

        As(editorId);
        var comment = await client.AddBoardComment(board.Id, new CreateScoutingCommentRequest { Body = "Editor note" });

        As(OwnerId);
        await client.DeleteComment(comment.Id);

        var thread = await client.GetBoardComments(board.Id);
        Assert.DoesNotContain(thread, c => c.Id == comment.Id);
    }

    [Fact]
    public async Task Viewer_cannot_delete_another_members_comment() {
        var board = await NewBoardAsync();
        var (username, viewerId) = FirstSeedUser();
        await client.AddMember(board.ScoutingTeamId, new AddScoutingMemberRequest { Username = username, Role = ScoutingTeamRole.Viewer });

        var comment = await client.AddBoardComment(board.Id, new CreateScoutingCommentRequest { Body = "Owner note" });

        As(viewerId);
        var ex = await Assert.ThrowsAnyAsync<ApiException>(() => client.DeleteComment(comment.Id));
        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }

    [Fact]
    public async Task EntryComment_count_is_reflected_on_the_board_entry() {
        var board = await NewBoardAsync();
        await client.AddEntry(board.Id, new AddScoutingBoardEntryRequest { PlayerId = 1001 });

        await client.AddEntryComment(board.Id, 1001, new CreateScoutingCommentRequest { Body = "Great skater" });
        await client.AddEntryComment(board.Id, 1001, new CreateScoutingCommentRequest { Body = "Needs work on defense" });

        var reloaded = await client.GetBoard(board.Id);
        var entry = Assert.Single(reloaded.Entries);
        Assert.Equal(2, entry.CommentCount);
    }

    // Auth ------------------------------------------------------------------

    [Fact]
    public async Task Anonymous_callers_are_rejected() {
        Anonymous();

        var ex = await Assert.ThrowsAnyAsync<ApiException>(() => client.GetMyTeams());
        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
    }

    private async Task<ScoutingBoardDetail> NewBoardAsync() {
        var team = await NewTeamAsync();
        return await client.CreateBoard(team.Id, new CreateScoutingBoardRequest { Name = "Prospects", DraftSeason = 73 });
    }
}
