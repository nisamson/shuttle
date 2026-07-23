using Bunit;
using Shuttle.WebClient.Shared.Meta;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Render tests for the shared <see cref="MetaTags"/> component, covering the emitted SEO / Open
/// Graph / Twitter-card tags and confirming user-supplied values are HTML-encoded (injection guard).
/// </summary>
public class MetaTagsTests : BunitContext {
    private static PageMetadata Sample(string? title = null) => new() {
        Title = title ?? "Alice Skater — Center · Shuttle",
        Description = "A great center with 1,234 TPE.",
        CanonicalUrl = "https://shl.example/players/1001",
        ImageUrl = "https://shl.example/icon.svg",
        OgType = "profile",
    };

    private string RenderTags(PageMetadata metadata) =>
        Render<MetaTags>(parameters => parameters.Add(p => p.Metadata, metadata)).Markup;

    [Fact]
    public void Emits_open_graph_and_twitter_tags() {
        var markup = RenderTags(Sample());

        Assert.Contains("property=\"og:title\"", markup);
        Assert.Contains("property=\"og:description\"", markup);
        Assert.Contains("property=\"og:url\"", markup);
        Assert.Contains("property=\"og:type\"", markup);
        Assert.Contains("property=\"og:image\"", markup);
        Assert.Contains("name=\"twitter:card\"", markup);
        Assert.Contains("summary_large_image", markup);
    }

    [Fact]
    public void Emits_a_canonical_link_and_description() {
        var markup = RenderTags(Sample());

        Assert.Contains("rel=\"canonical\"", markup);
        Assert.Contains("https://shl.example/players/1001", markup);
        Assert.Contains("name=\"description\"", markup);
    }

    [Fact]
    public void Omits_image_tags_when_no_image_is_supplied() {
        var markup = RenderTags(Sample() with { ImageUrl = null });

        Assert.DoesNotContain("og:image", markup);
        Assert.DoesNotContain("twitter:image", markup);
    }

    [Fact]
    public void Html_encodes_untrusted_title_values() {
        const string rawTitle = "Bad \"quote\" & <script>alert(1)</script>";
        var cut = Render<MetaTags>(parameters => parameters.Add(p => p.Metadata, Sample(title: rawTitle)));

        // The injected markup must never become a live <script> element, and the title must be
        // carried as an attribute value (decoded back to the original string), not raw markup.
        Assert.Empty(cut.FindAll("script"));
        Assert.Equal(rawTitle, cut.Find("meta[property='og:title']").GetAttribute("content"));
    }
}
