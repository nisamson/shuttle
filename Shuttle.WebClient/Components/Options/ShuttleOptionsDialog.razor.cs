using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Shuttle.WebClient.Models.Options;

namespace Shuttle.WebClient.Components.Options;

public partial class ShuttleOptionsDialog : ComponentBase, IDialogContentComponent<ShuttleOptions> {
    private bool IsTouched => !IShuttleOptions.Equals(OptionsModel, Content);

    [CascadingParameter]
    public FluentDialog Dialog { get; set; } = null!;

    [Parameter, EditorRequired]
    public required ShuttleOptions Content { get; set; }

    private ShuttleOptionsModel OptionsModel { get; set; } = ShuttleOptionsModel.FromOptions(ShuttleOptions.Default);

    [Inject]
    public required ILogger<ShuttleOptionsDialog> Logger { private get; set; }

    protected override void OnParametersSet() {
        Logger.LogTrace("Updating options from context");

        if (IShuttleOptions.Equals(OptionsModel, Content)) {
            return;
        }

        OptionsModel = ShuttleOptionsModel.FromOptions(Content);
        StateHasChanged();
    }

    private async Task OnOptionsEdited() {
        if (IShuttleOptions.Equals(OptionsModel, Content)) {
            Logger.LogDebug("Options edited, but options are the same as current options, ignoring");
            await Dialog.CancelAsync();
            return;
        }
        Logger.LogDebug("Options edited, closing dialog with new options {OptionsModel}", OptionsModel);
        var options = OptionsModel.ToOptions();
        await Dialog.CloseAsync(options);
    }

    private async Task OnCancelEdit() {
        await Dialog.CancelAsync();
    }

}
