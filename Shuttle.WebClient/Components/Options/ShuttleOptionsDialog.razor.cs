using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Shuttle.WebClient.Models.Options;

namespace Shuttle.WebClient.Components.Options;

public partial class ShuttleOptionsDialog : FluentDialogInstance {

    [Parameter, EditorRequired]
    public required ShuttleOptions Content { get; set; }

    private ShuttleOptionsModel OptionsModel { get; set; } = ShuttleOptionsModel.FromOptions(ShuttleOptions.Default);

    [Inject]
    public required ILogger<ShuttleOptionsDialog> Logger { private get; set; }

    protected override void OnInitializeDialog(DialogOptionsHeader header, DialogOptionsFooter footer) {
        header.Title = "Shuttle Options";
        footer.SecondaryAction.Visible = true;
    }

    protected override void OnParametersSet() {
        base.OnParametersSet();
        Logger.LogTrace("Updating options from context");

        if (IShuttleOptions.Equals(OptionsModel, Content)) {
            return;
        }

        OptionsModel = ShuttleOptionsModel.FromOptions(Content);
        StateHasChanged();
    }

    protected override async Task OnActionClickedAsync(bool primary) {
        if (!primary) {
            Logger.LogDebug("Options edit cancelled");
            await DialogInstance.CancelAsync();
            return;
        }

        if (IShuttleOptions.Equals(OptionsModel, Content)) {
            Logger.LogDebug("Options edited, but options are the same as current options, ignoring");
            await DialogInstance.CancelAsync();
            return;
        }

        Logger.LogDebug("Options edited, closing dialog with new options {OptionsModel}", OptionsModel);
        var options = OptionsModel.ToOptions();
        await DialogInstance.CloseAsync(options);
    }
}
