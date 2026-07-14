using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Shuttle.WebClient.Models.Options;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Layout;

public partial class MainLayout {

    [Inject]
    public required IShuttleOptionsStorage OptionsStorage { private get; set; }

    [Inject]
    public required IThemeService ThemeService { private get; set; }

    private ShuttleOptions Options { get; set; } = ShuttleOptions.Default;

    private bool? appliedDarkMode;

    private bool NavCollapsed { get; set; }

    private void ToggleNav() {
        NavCollapsed = !NavCollapsed;
    }

    protected override async Task OnInitializedAsync() {
        Options = await OptionsStorage.LoadOptions(true);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await ApplyThemeAsync();
    }

    private void OnOptionsChanged(ShuttleOptions options) {
        if (Options != options) {
            Options = options;
            StateHasChanged();
        }
    }

    private async Task ApplyThemeAsync() {
        if (appliedDarkMode == Options.DarkMode) {
            return;
        }

        appliedDarkMode = Options.DarkMode;
        await ThemeService.SetThemeAsync(Options.DarkMode ? ThemeMode.Dark : ThemeMode.Light);
    }
}
