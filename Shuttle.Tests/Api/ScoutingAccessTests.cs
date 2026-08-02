using Shuttle.Api.Services.Scouting;
using Shuttle.EFCore.Entities;
using Shuttle.Models.Scouting;

namespace Shuttle.Tests.Api;

/// <summary>
/// Unit tests for the scouting permission model — <see cref="ScoutingAccess"/> and the
/// <see cref="ScoutingTeamRoleCapabilities"/> extensions. These are the single source of truth the
/// server uses to decide who may do what to a team, so they are verified independently of the
/// EF-backed services that consume them.
/// </summary>
public class ScoutingAccessTests {
    private static ShuttleUser User() => new() {
        Id = Guid.CreateVersion7(),
        ObjectId = Guid.NewGuid(),
        Username = "scout",
    };

    private static ScoutingAccess Access(ScoutingTeamRole? role, bool isAdmin = false) =>
        new(User(), isAdmin, role);

    [Fact]
    public void Owner_can_do_everything() {
        var access = Access(ScoutingTeamRole.Owner);

        Assert.True(access.IsMember);
        Assert.True(access.CanView);
        Assert.True(access.CanEditBoards);
        Assert.True(access.CanComment);
        Assert.True(access.CanManageTeam);
        Assert.True(access.CanModerateComments);
    }

    [Fact]
    public void Editor_can_edit_boards_and_comment_but_not_manage_or_moderate() {
        var access = Access(ScoutingTeamRole.Editor);

        Assert.True(access.IsMember);
        Assert.True(access.CanView);
        Assert.True(access.CanEditBoards);
        Assert.True(access.CanComment);
        Assert.False(access.CanManageTeam);
        Assert.False(access.CanModerateComments);
    }

    [Fact]
    public void Viewer_can_only_view() {
        var access = Access(ScoutingTeamRole.Viewer);

        Assert.True(access.IsMember);
        Assert.True(access.CanView);
        Assert.False(access.CanEditBoards);
        Assert.False(access.CanComment);
        Assert.False(access.CanManageTeam);
        Assert.False(access.CanModerateComments);
    }

    [Fact]
    public void Non_member_has_no_capabilities() {
        var access = Access(role: null);

        Assert.False(access.IsMember);
        Assert.False(access.CanView);
        Assert.False(access.CanEditBoards);
        Assert.False(access.CanComment);
        Assert.False(access.CanManageTeam);
        Assert.False(access.CanModerateComments);
    }

    [Fact]
    public void Site_admin_is_a_superuser_over_a_team_even_without_membership() {
        var access = Access(role: null, isAdmin: true);

        // The admin is not a member of the team but overrides every capability check.
        Assert.False(access.IsMember);
        Assert.True(access.CanView);
        Assert.True(access.CanEditBoards);
        Assert.True(access.CanComment);
        Assert.True(access.CanManageTeam);
        Assert.True(access.CanModerateComments);
    }

    [Theory]
    [InlineData(ScoutingTeamRole.Viewer, false, false, false)]
    [InlineData(ScoutingTeamRole.Editor, true, true, false)]
    [InlineData(ScoutingTeamRole.Owner, true, true, true)]
    public void Role_capability_helpers_match_the_permission_model(
        ScoutingTeamRole role, bool canEditBoards, bool canComment, bool canManage) {
        Assert.True(role.CanView());
        Assert.Equal(canEditBoards, role.CanEditBoards());
        Assert.Equal(canComment, role.CanComment());
        Assert.Equal(canManage, role.CanManageTeam());
    }
}
