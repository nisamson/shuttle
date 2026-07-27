using Bunit;
using System.Linq;
using Shuttle.Models.Users;
using Shuttle.WebClient.Components.Users;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Render tests for <see cref="UserCardTable"/> — no browser, server, or Azure.
/// </summary>
public class UserCardTableTests : WebClientTestContext {
    private static UserCard User(int id, string name, string? discord = null) =>
        new() { UserId = id, Username = name, DiscordName = discord };

    [Fact]
    public void Renders_a_row_per_user() {
        var users = new List<UserCard> { User(1, "alpha"), User(2, "bravo"), User(3, "charlie") };

        var cut = Render<UserCardTable>(p => p.Add(c => c.Users, users));

        Assert.Equal(users.Count, cut.FindAll("tbody tr").Count);
        Assert.Contains("alpha", cut.Markup);
    }

    [Fact]
    public void Links_each_row_to_the_user_profile() {
        var cut = Render<UserCardTable>(p => p.Add(c => c.Users, new List<UserCard> { User(42, "scout") }));

        var link = cut.Find("a.emphasized-link");
        Assert.Equal("/users/42", link.GetAttribute("href"));
    }

    [Fact]
    public void Hides_discord_column_by_default() {
        var cut = Render<UserCardTable>(p => p.Add(c => c.Users, new List<UserCard> { User(1, "alpha", "alpha#1") }));

        Assert.DoesNotContain("Discord", cut.Markup);
    }

    [Fact]
    public void Shows_discord_column_when_enabled() {
        var cut = Render<UserCardTable>(p => p
            .Add(c => c.Users, new List<UserCard> { User(1, "alpha", "alpha.discord") })
            .Add(c => c.ShowDiscord, true));

        Assert.Contains("Discord", cut.Markup);
        Assert.Contains("alpha.discord", cut.Markup);
    }

    [Fact]
    public void Shows_empty_message_when_no_users() {
        var cut = Render<UserCardTable>(p => p.Add(c => c.Users, new List<UserCard>()));

        Assert.Contains("No users match your search", cut.Markup);
    }

    [Fact]
    public void Clicking_a_sortable_header_raises_sort_changed_and_flips_direction() {
        var users = new List<UserCard> { User(1, "alpha"), User(2, "bravo") };
        UserCardTable.UserTableSort? captured = null;

        var cut = Render<UserCardTable>(p => p
            .Add(c => c.Users, users)
            .Add(c => c.SortField, UserSortField.Username)
            .Add(c => c.SortDescending, false)
            .Add(c => c.SortChanged, (UserCardTable.UserTableSort s) => captured = s));

        var header = cut.FindAll("thead [role=button]").First(e => e.TextContent.Contains("Username"));
        header.Click();

        Assert.NotNull(captured);
        Assert.Equal(UserSortField.Username, captured!.Value.Field);
        // Clicking the already-active ascending column flips it to descending.
        Assert.True(captured.Value.Descending);
    }

    [Fact]
    public void Headers_are_not_clickable_when_sort_is_disabled() {
        var users = new List<UserCard> { User(1, "alpha"), User(2, "bravo") };

        var cut = Render<UserCardTable>(p => p.Add(c => c.Users, users));

        Assert.Empty(cut.FindAll("thead [role=button]"));
    }
}
