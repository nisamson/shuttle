using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;

namespace Shuttle.WebClient.Layout;

/// <summary>
/// Site footer with links to the backend Swagger UI and the project's AGPL-3.0 license. The Swagger
/// link targets the backend API origin (from <c>Api:BaseUrl</c>), which is a different origin than the
/// WebClient, so it is resolved at runtime rather than from a compile-time constant.
/// </summary>
public partial class Footer : ComponentBase {
    [Inject] private IConfiguration Configuration { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    /// <summary>Extra CSS classes appended to the footer element.</summary>
    [Parameter] public string? Class { get; set; }

    private string SwaggerUrl {
        get {
            var apiBase = Configuration["Api:BaseUrl"] ?? Navigation.BaseUri;
            return $"{apiBase.TrimEnd('/')}/swagger";
        }
    }
}
