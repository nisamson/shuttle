using Microsoft.AspNetCore.Components;

namespace Shuttle.WebClient.Components;

public partial class ShuttleLogo
{
    /// <summary>
    /// The rendered width and height of the logo in pixels.
    /// </summary>
    [Parameter]
    public int Size { get; set; } = 24;
}
