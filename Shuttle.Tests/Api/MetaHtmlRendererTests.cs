using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shuttle.Api.Meta;
using Shuttle.WebClient.Shared.Meta;

namespace Shuttle.Tests.Api;

/// <summary>
/// Verifies that <see cref="MetaHtmlRenderer"/> statically renders the shared meta component to a
/// complete HTML document — the output served by the <c>/meta</c> endpoint.
/// </summary>
public class MetaHtmlRendererTests {
    [Fact]
    public async Task Renders_a_full_html_document_with_the_meta_tags() {
        var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var renderer = new MetaHtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());

        var metadata = new PageMetadata {
            Title = "Alice Skater — Center · Shuttle",
            Description = "A great center.",
            CanonicalUrl = "https://shl.example/players/1001",
            ImageUrl = "https://shl.example/icon.svg",
            OgType = "profile",
        };

        var html = await renderer.RenderAsync(metadata);

        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<title>Alice Skater", html);
        Assert.Contains("property=\"og:title\"", html);
        Assert.Contains("property=\"og:type\" content=\"profile\"", html);
        Assert.Contains("https://shl.example/players/1001", html);
    }
}
