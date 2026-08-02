using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Shuttle.WebClient.Pages.Users;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Render tests for the <see cref="UserProfile"/> page, focused on its wiring to the embedded
/// <see cref="Shuttle.WebClient.Components.Users.UserRecruitmentPanel"/>. These guard against the
/// page passing a literal <c>"card.Username"</c> string (a Razor binding mistake) instead of the
/// bound expression, which would silently render the panel's empty state.
/// </summary>
public class UserProfilePageTests : WebClientTestContext {
    public UserProfilePageTests() {
        // The page reads auth state on init; sign in so it renders the loaded profile.
        this.AddAuthorization().SetAuthorized("frostbite");
    }

    /// <summary>
    /// A render fragment for the profile page at <paramref name="id"/>, wrapped in a
    /// <see cref="FluentTooltipProvider"/> (the page uses <c>FluentTooltip</c>, supplied app-wide by
    /// <c>FluentProviders</c> in MainLayout).
    /// </summary>
    private static RenderFragment Profile(string id) => builder => {
        builder.OpenComponent<FluentTooltipProvider>(0);
        builder.CloseComponent();
        builder.OpenComponent<UserProfile>(1);
        builder.AddAttribute(2, nameof(UserProfile.Id), id);
        builder.CloseComponent();
    };

    [Fact]
    public void Passes_the_bound_username_to_the_recruitment_panel() {
        // frostbite (user 5001) has a multi-level recruitment lineage in the seed graph.
        var cut = Render(Profile("5001"));

        var markup = cut.Markup;
        // The panel loaded real lineage data — not the literal-"card.Username" empty state.
        Assert.DoesNotContain("hasn't recruited anyone", markup);
        Assert.Contains("Lineage members", markup);

        // A recruit-profile link only the recruitment panel emits (dmarsh, 5004 — not one of
        // frostbite's own players, whose links the page suppresses).
        var hrefs = cut.FindAll("a.emphasized-link").Select(a => a.GetAttribute("href")).ToList();
        Assert.Contains("/users/5004", hrefs);
    }

    [Fact]
    public void Renders_the_empty_state_for_a_user_who_recruited_nobody() {
        // bridge (user 5002) recruited nobody, so the panel shows its empty state — proving the
        // username still bound correctly (a literal would look the same, so this pairs with the
        // positive test above).
        var cut = Render(Profile("5002"));

        Assert.Contains("hasn't recruited anyone", cut.Markup);
    }
}
