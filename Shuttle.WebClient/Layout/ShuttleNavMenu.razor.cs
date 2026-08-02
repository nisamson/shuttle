using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Shuttle.WebClient.Models;

namespace Shuttle.WebClient.Layout;

public partial class ShuttleNavMenu : ComponentBase {

    [Inject]
    public required NavigationManager Navigation { private get; set; }

    private void BeginLogin() {
        Navigation.NavigateToLogin(Routes.Authentication.Login);
    }

    private void BeginLogout() {
        Navigation.NavigateToLogout(Routes.Authentication.Logout);
    }
}

