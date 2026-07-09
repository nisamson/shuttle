using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Shuttle.WebClient.Models;

namespace Shuttle.WebClient.Layout;

public partial class ShuttleNavMenu : ComponentBase {

    [Parameter]
    public bool Expanded { get; set; } = true;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    private void BeginLogOut() {
        Navigation.NavigateToLogout(Routes.Authentication.Logout);
    }

}

