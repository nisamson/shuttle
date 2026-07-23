using Shuttle.WebClient.Shared.Meta;

namespace Shuttle.Api.Meta;

/// <summary>
/// Maps an originally requested front-end path (as forwarded to <c>/meta/&lt;path&gt;</c>) to the
/// SEO / social-embed <see cref="PageMetadata"/> to render for it.
/// </summary>
public interface IMetaResolver {
    /// <summary>
    /// Resolves the metadata for <paramref name="requestPath"/>. Always returns metadata: unknown or
    /// missing entities fall back to sensible site-wide defaults so crawlers never see a bare page.
    /// </summary>
    /// <param name="requestPath">The original front-end path, e.g. <c>/players/123</c>.</param>
    /// <param name="queryString">The original query string (including leading <c>?</c>), if any.</param>
    /// <param name="requestOrigin">The scheme+host of the incoming request, used as a base-URL fallback.</param>
    Task<PageMetadata> ResolveAsync(
        string requestPath,
        string? queryString,
        string requestOrigin,
        CancellationToken cancellationToken);
}
