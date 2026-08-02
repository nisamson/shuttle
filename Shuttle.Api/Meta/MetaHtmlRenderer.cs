using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shuttle.WebClient.Shared.Meta;

namespace Shuttle.Api.Meta;

/// <summary>
/// Statically renders the shared <see cref="MetaDocument"/> Razor component to an HTML string using
/// <see cref="HtmlRenderer"/> — no interactive Blazor circuit or Razor Components hosting required.
/// </summary>
public sealed class MetaHtmlRenderer {
    private readonly IServiceProvider services;
    private readonly ILoggerFactory loggerFactory;

    public MetaHtmlRenderer(IServiceProvider services, ILoggerFactory loggerFactory) {
        this.services = services;
        this.loggerFactory = loggerFactory;
    }

    /// <summary>Renders the full meta HTML document for <paramref name="metadata"/>.</summary>
    public async Task<string> RenderAsync(PageMetadata metadata) {
        await using var htmlRenderer = new HtmlRenderer(services, loggerFactory);

        return await htmlRenderer.Dispatcher.InvokeAsync(async () => {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> {
                [nameof(MetaDocument.Metadata)] = metadata,
            });

            var output = await htmlRenderer.RenderComponentAsync<MetaDocument>(parameters);
            return output.ToHtmlString();
        });
    }
}
