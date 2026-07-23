namespace Shuttle.WebClient.Shared.Meta;

/// <summary>
/// The SEO / social-embed metadata for a single page, consumed by <c>MetaTags</c> to emit the
/// <c>&lt;title&gt;</c>, standard SEO tags, Open Graph tags (Discord/Facebook), and Twitter-card
/// tags. Values are rendered through Razor so they are HTML-encoded automatically.
/// </summary>
public sealed record PageMetadata {
    /// <summary>The page title (used for <c>&lt;title&gt;</c>, <c>og:title</c>, <c>twitter:title</c>).</summary>
    public required string Title { get; init; }

    /// <summary>A short description (used for the meta description and <c>og/twitter:description</c>).</summary>
    public required string Description { get; init; }

    /// <summary>The absolute canonical URL of the page (<c>link[rel=canonical]</c>, <c>og:url</c>).</summary>
    public required string CanonicalUrl { get; init; }

    /// <summary>Absolute URL of the preview image (<c>og:image</c>, <c>twitter:image</c>), if any.</summary>
    public string? ImageUrl { get; init; }

    /// <summary>The site name (<c>og:site_name</c>).</summary>
    public string SiteName { get; init; } = "Shuttle";

    /// <summary>The Open Graph object type (<c>og:type</c>), e.g. <c>website</c> or <c>article</c>.</summary>
    public string OgType { get; init; } = "website";

    /// <summary>The Twitter card type (<c>twitter:card</c>).</summary>
    public string TwitterCard { get; init; } = "summary";
}
