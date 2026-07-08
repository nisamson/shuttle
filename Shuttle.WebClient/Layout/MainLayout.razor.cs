using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Shuttle.WebClient.Components.Options;
using Shuttle.WebClient.Models.Options;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Layout;

public partial class MainLayout {
    
    [Inject]
    public required IShuttleOptionsStorage OptionsStorage { private get; set; }
    
    private ShuttleOptions Options { get; set; } = ShuttleOptions.Default;
    
    protected override async Task OnInitializedAsync() {
        Options = await OptionsStorage.LoadOptions(true);
    }

    private void OnOptionsChanged(ShuttleOptions options) {
        if (Options != options) {
            Options = options;
            StateHasChanged();
        }
    }
}
