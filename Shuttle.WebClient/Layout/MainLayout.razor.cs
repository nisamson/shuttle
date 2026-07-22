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

    [Inject]
    public required ICurrentUserService CurrentUser { private get; set; }

    private ShuttleOptions Options { get; set; } = ShuttleOptions.Default;

    private bool? appliedDarkMode;

    private bool NavCollapsed { get; set; }

    private void ToggleNav() {
        NavCollapsed = !NavCollapsed;
    }

    protected override async Task OnInitializedAsync() {
        Options = await OptionsStorage.LoadOptions(true);

        // Best-effort: warm (and lazily create server-side) the signed-in caller's account on load so
        // a returning-from-login user is initialized promptly. Not awaited — it must not block the
        // layout, and any consumer that truly needs the account fetches it on demand regardless.
        _ = CurrentUser.EnsureInitializedAsync();
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
