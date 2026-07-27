using Bunit;
using Shuttle.WebClient.Components;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Render tests for <see cref="ShuttleLink"/>. The component must render a plain HTML <c>&lt;a&gt;</c>
/// anchor (not a FluentUI <c>FluentLink</c>) so Blazor's router intercepts internal navigation and
/// performs client-side routing. Rendering a <c>FluentLink</c> reintroduces FluentUI issue #5022,
/// which triggers a full page reload for internal routes.
/// </summary>
public class ShuttleLinkTests : WebClientTestContext {
    [Fact]
    public void Renders_a_plain_anchor_element() {
        var cut = Render<ShuttleLink>(p => p
            .Add(c => c.Href, "/players/1001")
            .AddChildContent("Aaron Frost"));

        var anchor = cut.Find("a");
        Assert.Equal("/players/1001", anchor.GetAttribute("href"));
        Assert.Equal("Aaron Frost", anchor.TextContent.Trim());
        // Guard against a FluentLink regression (would render a <fluent-link> custom element).
        Assert.DoesNotContain("fluent-link", cut.Markup);
    }

    [Fact]
    public void Applies_emphasized_style_class_when_emphasized() {
        var cut = Render<ShuttleLink>(p => p
            .Add(c => c.Href, "/players/1001")
            .Add(c => c.Emphasized, true)
            .AddChildContent("Aaron Frost"));

        Assert.Equal("emphasized-link", cut.Find("a").GetAttribute("class"));
    }

    [Fact]
    public void Applies_normal_style_class_by_default() {
        var cut = Render<ShuttleLink>(p => p
            .Add(c => c.Href, "/users/1")
            .AddChildContent("someone"));

        Assert.Equal("normal-link", cut.Find("a").GetAttribute("class"));
    }

    [Fact]
    public void Forwards_additional_attributes_to_the_anchor() {
        var cut = Render<ShuttleLink>(p => p
            .Add(c => c.Href, "https://example.com")
            .AddUnmatched("target", "_blank")
            .AddUnmatched("rel", "noopener noreferrer")
            .AddChildContent("external"));

        var anchor = cut.Find("a");
        Assert.Equal("_blank", anchor.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", anchor.GetAttribute("rel"));
    }
}
