using Microsoft.AspNetCore.Components;

namespace Shuttle.WebClient.Layout;

public partial class ShuttleNavMenu : ComponentBase {

    [Parameter]
    public bool Expanded { get; set; } = true;

}

