using Microsoft.AspNetCore.Components;

namespace Shuttle.WebClient.Components;

/// <summary>
/// A link that renders with the shared site link styling (see <c>app.css</c>: <c>emphasized-link</c>
/// / <c>normal-link</c>) as either a FluentUI <see cref="Microsoft.FluentUI.AspNetCore.Components.FluentLink"/>
/// or a plain HTML anchor, depending on <see cref="IsAnchor"/>. Extra attributes (e.g. <c>target</c>,
/// <c>rel</c>, <c>title</c>) are forwarded to the rendered element.
/// </summary>
public partial class ShuttleLink : ComponentBase {
    /// <summary>The link target URL.</summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// When <c>true</c>, uses the emphasized style (accent color, bold); otherwise the normal style.
    /// </summary>
    [Parameter] public bool Emphasized { get; set; }

    /// <summary>
    /// When <c>true</c>, renders a plain HTML <c>&lt;a&gt;</c> anchor; otherwise a FluentUI
    /// <c>FluentLink</c>.
    /// </summary>
    [Parameter] public bool IsAnchor { get; set; }

    /// <summary>The link content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Additional attributes forwarded to the rendered element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private string CssClass => Emphasized ? "emphasized-link" : "normal-link";
}
