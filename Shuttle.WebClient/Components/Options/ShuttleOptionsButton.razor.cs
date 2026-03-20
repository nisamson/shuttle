using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Shuttle.WebClient.Models.Options;

namespace Shuttle.WebClient.Components.Options;

public partial class ShuttleOptionsButton : ComponentBase {
    
    [CascadingParameter]
    public ShuttleOptionsContext? OptionsContext { get; set; }
    
    [Inject]
    public required ILogger<ShuttleOptionsButton> Logger { private get; set; }
    
    [Inject]
    public required IDialogService DialogService { private get; set; }
    
    private async Task ShowOptionsDialog(MouseEventArgs args) {
        Logger.LogDebug("Showing options dialog");
        var shuttleOptions = OptionsContext?.CurrentOptions;
        if (shuttleOptions is null) {
            Logger.LogWarning("No options context or current options, cannot show options dialog");
            return;
        }
        var dialog = await DialogService.ShowAsync<ShuttleOptionsDialog>(
            null,
            new() {
                [nameof(ShuttleOptionsDialog.Options)] = OptionsContext?.CurrentOptions
            },
            new() {
                MaxWidth = MaxWidth.Medium,
                CloseOnEscapeKey = true,
                Position = DialogPosition.Center,
            }
            );
        var res = await dialog.Result;

        if (res is null) {
            Logger.LogDebug("Dialog result is null, not saving options");
            return;
        }
        
        if (res.Data is ShuttleOptions options) {
            Logger.LogDebug("Dialog result data is of type ShuttleOptions, saving options {Options}", options);
            OptionsContext?.SaveOptions(options);
        } else {
            Logger.LogDebug("Dialog result data is not of type ShuttleOptions, not saving options");
        }
        Logger.LogDebug("Done");
    }
}

