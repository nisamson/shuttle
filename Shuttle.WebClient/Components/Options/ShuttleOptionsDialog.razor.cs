using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Options;
using Shuttle.WebClient.Components.Dev;
using Shuttle.WebClient.Models.Options;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Components.Options;

public partial class ShuttleOptionsDialog : ComponentBase {
    private bool IsTouched => !IShuttleOptions.Equals(OptionsModel, Options);
    
    [CascadingParameter]
    public IMudDialogInstance Dialog { get; set; } = null!;

    [Parameter, EditorRequired]
    public required ShuttleOptions Options { get; set; }
    
    private ShuttleOptionsModel OptionsModel { get; set; } = ShuttleOptionsModel.FromOptions(ShuttleOptions.Default);

    [Inject]
    public required ILogger<ShuttleOptionsDialog> Logger { private get; set; }

    protected override void OnParametersSet() {
        Logger.LogTrace("Updating options from context");
        
        if (IShuttleOptions.Equals(OptionsModel, Options)) {
            return;
        }

        OptionsModel = ShuttleOptionsModel.FromOptions(Options);
        StateHasChanged();
    }

    private void OnOptionsEdited() {
        if (IShuttleOptions.Equals(OptionsModel, Options)) {
            Logger.LogDebug("Options edited, but options are the same as current options, ignoring");
            Dialog.Close(DialogResult.Ok(null as ShuttleOptions));
            return;
        }
        Logger.LogDebug("Options edited, closing dialog with new options {OptionsModel}", OptionsModel);
        var options = OptionsModel.ToOptions();
        Dialog.Close(DialogResult.Ok(options));
    }

    private void OnCancelEdit() {
        Dialog.Close(DialogResult.Cancel());
    }
    
}

