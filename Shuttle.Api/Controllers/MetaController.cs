using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Shuttle.Api.Meta;

namespace Shuttle.Api.Controllers;

/// <summary>
/// Server-side rendered SEO / social-embed meta tags. Requests to the front-end SPA that come from
/// crawlers or Discord's link unfurler are forwarded here with the original path preserved after the
/// <c>/meta</c> prefix (e.g. <c>/meta/players/123</c>). We resolve that path to page metadata and
/// return a minimal HTML document whose <c>&lt;head&gt;</c> carries the tags.
/// </summary>
[AllowAnonymous]
[ApiController]
public sealed class MetaController : ControllerBase {
    private readonly IMetaResolver resolver;
    private readonly MetaHtmlRenderer renderer;

    public MetaController(IMetaResolver resolver, MetaHtmlRenderer renderer) {
        this.resolver = resolver;
        this.renderer = renderer;
    }

    [HttpGet("/meta/{**path}")]
    [Produces("text/html")]
    public async Task<IActionResult> GetMeta(string? path, CancellationToken cancellationToken) {
        var requestPath = "/" + (path ?? string.Empty);
        var requestOrigin = $"{Request.Scheme}://{Request.Host}";

        var metadata = await resolver.ResolveAsync(
            requestPath,
            Request.QueryString.Value,
            requestOrigin,
            cancellationToken);

        var html = await renderer.RenderAsync(metadata);

        Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue {
            Public = true,
            MaxAge = TimeSpan.FromMinutes(15),
        };

        return Content(html, "text/html");
    }
}
