using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Shuttle.WebClient.Models.Options;

namespace Shuttle.WebClient.Components.Options;

public partial class ShuttleOptionsButton : ComponentBase {

    [CascadingParameter]
    public ShuttleOptionsContext? OptionsContext { get; set; }

    [Inject]
    public required ILogger<ShuttleOptionsButton> Logger { private get; set; }

    [Inject]
    public required IDialogService DialogService { private get; set; }

    private async Task ShowOptionsDialog() {
        Logger.LogDebug("Showing options dialog");
        var shuttleOptions = OptionsContext?.CurrentOptions;
        if (shuttleOptions is null) {
            Logger.LogWarning("No options context or current options, cannot show options dialog");
            return;
        }

        DialogParameters parameters = new() {
            Title = "Shuttle Options",
            PrimaryAction = string.Empty,
            SecondaryAction = string.Empty,
            Width = "400px",
            TrapFocus = true,
            Modal = true,
        };

        var dialog = await DialogService.ShowDialogAsync<ShuttleOptionsDialog>(shuttleOptions, parameters);
        var res = await dialog.Result;

        if (res is null) {
            Logger.LogDebug("Dialog result is null, not saving options");
            return;
        }

        if (!res.Cancelled && res.Data is ShuttleOptions options) {
            Logger.LogDebug("Dialog result data is of type ShuttleOptions, saving options {Options}", options);
            if (OptionsContext is not null) {
                await OptionsContext.SaveOptions(options);
            }
        } else {
            Logger.LogDebug("Dialog result data is not of type ShuttleOptions, not saving options");
        }
        Logger.LogDebug("Done");
    }
}
