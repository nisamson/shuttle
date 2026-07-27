using Microsoft.AspNetCore.Components;

namespace Shuttle.WebClient.Components;

/// <summary>
/// A link that renders with the shared site link styling (see <c>app.css</c>: <c>emphasized-link</c>
/// / <c>normal-link</c>) as a plain HTML <c>&lt;a&gt;</c> anchor. Extra attributes (e.g. <c>target</c>,
/// <c>rel</c>, <c>title</c>) are forwarded to the rendered element.
/// </summary>
/// <remarks>
/// A plain anchor is used deliberately (rather than FluentUI's <c>FluentLink</c>) so that Blazor's
/// router intercepts clicks and performs client-side navigation. <c>FluentLink</c> triggers a full
/// page reload for internal routes (FluentUI issue #5022), which we must avoid.
/// </remarks>
public partial class ShuttleLink : ComponentBase {
    /// <summary>The link target URL.</summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// When <c>true</c>, uses the emphasized style (accent color, bold); otherwise the normal style.
    /// Ignored when <see cref="Class"/> is set.
    /// </summary>
    [Parameter] public bool Emphasized { get; set; }

    /// <summary>
    /// Overrides the CSS class applied to the rendered anchor. When non-<c>null</c> (including an
    /// empty string, to render an unstyled wrapper link), this replaces the default
    /// <c>emphasized-link</c> / <c>normal-link</c> styling.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>The link content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Additional attributes forwarded to the rendered element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass => Class ?? (Emphasized ? "emphasized-link" : "normal-link");
}
