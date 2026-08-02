namespace Shuttle.Api.Meta;

/// <summary>
/// Options controlling how the <c>/meta</c> endpoint builds canonical URLs and preview images.
/// Bound from the <c>Shuttle</c> configuration section.
/// </summary>
public sealed class MetaOptions {
    public const string SectionName = "Shuttle";

    /// <summary>
    /// The public base URL of the front-end SPA (e.g. <c>https://shl.nes.sh</c>). Canonical and
    /// Open Graph URLs are built by combining this with the originally requested path. Falls back to
    /// the request's own origin when not configured (useful in local development).
    /// </summary>
    public string? SiteBaseUrl { get; set; }

    /// <summary>
    /// Absolute (or site-relative) URL of the default social-embed image used for every page. When
    /// site-relative it is resolved against <see cref="SiteBaseUrl"/>. Defaults to <c>/icon.svg</c>.
    /// </summary>
    public string DefaultImageUrl { get; set; } = "/icon.svg";
}
