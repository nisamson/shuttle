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

        var result = await DialogService.ShowDialogAsync<ShuttleOptionsDialog>(options => {
            options.Modal = true;
            options.Width = "400px";
            options.Parameters.Add(nameof(ShuttleOptionsDialog.Content), shuttleOptions);
        });

        if (result.Cancelled) {
            Logger.LogDebug("Dialog cancelled, not saving options");
            return;
        }

        if (result.Value is ShuttleOptions updatedOptions) {
            Logger.LogDebug("Dialog result data is of type ShuttleOptions, saving options {Options}", updatedOptions);
            if (OptionsContext is not null) {
                await OptionsContext.SaveOptions(updatedOptions);
            }
        } else {
            Logger.LogDebug("Dialog result data is not of type ShuttleOptions, not saving options");
        }
        Logger.LogDebug("Done");
    }
}
