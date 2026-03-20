using Microsoft.AspNetCore.Components;
using Shuttle.WebClient.Models.Options;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Components.Options;

public partial class ShuttleOptionsContext : ComponentBase, IDisposable {
    
    private bool isDisposed;
    
    [Inject]
    public required IShuttleOptionsStorage OptionsStorage { private get; set; }
    
    [Inject]
    public required ILogger<ShuttleOptionsContext> Logger { private get; set; }

    public ShuttleOptions CurrentOptions { get ; private set; } = ShuttleOptions.Default;
    
    protected override async Task OnInitializedAsync() {
        OptionsStorage.OptionsChanged += OnOptionsChanged;
        var options = await OptionsStorage.LoadOptions(true);
        CurrentOptions = options;
        Logger.LogDebug("Loaded options on initialization: {Options}", options);
    }

    private void OnOptionsChanged(ShuttleOptions options) {
        if (options.Equals(CurrentOptions)) {
            Logger.LogDebug("Received options changed event, but options are the same instance, ignoring");
            return;
        }
        Logger.LogDebug("Options changed, updating context");
        CurrentOptions = options;
        StateHasChanged();
    }
    
    public async Task SaveOptions(ShuttleOptions options) {
        Logger.LogDebug("Saving options: {Options}", options);
        await OptionsStorage.SaveOptions(options);
    }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
    
    public void Dispose() {
        if (isDisposed) {
            return;
        }
        isDisposed = true;
        OptionsStorage.OptionsChanged -= OnOptionsChanged;
    }
}

