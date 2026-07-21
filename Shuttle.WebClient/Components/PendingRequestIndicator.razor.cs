using Microsoft.AspNetCore.Components;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Components;

/// <summary>
/// App-wide indicator that the client is communicating with the backend. Subscribes to the
/// <see cref="IPendingRequestTracker"/> and, while any request is in flight, shows an indeterminate
/// progress bar pinned to the top of the viewport plus a short description of the current request.
/// A brief show-delay avoids flashing the indicator for very fast requests.
/// </summary>
public partial class PendingRequestIndicator : IDisposable {
    private const int ShowDelayMs = 200;

    [Inject] private IPendingRequestTracker Tracker { get; set; } = null!;

    private bool visible;
    private string? description;

    // Bumped whenever busy-state changes; a scheduled "show" only takes effect if its generation is
    // still current when the delay elapses, so a request that finishes within the delay never shows.
    private int generation;

    protected override void OnInitialized() {
        Tracker.Changed += OnChanged;
        OnChanged();
    }

    private void OnChanged() => _ = InvokeAsync(HandleChangedAsync);

    private async Task HandleChangedAsync() {
        if (Tracker.IsBusy) {
            description = Tracker.CurrentDescription;
            if (visible) {
                StateHasChanged();
                return;
            }

            var scheduled = ++generation;
            await Task.Delay(ShowDelayMs);
            if (scheduled == generation && Tracker.IsBusy) {
                visible = true;
                description = Tracker.CurrentDescription;
                StateHasChanged();
            }
        } else {
            // Invalidate any pending show and hide if currently visible.
            generation++;
            if (visible) {
                visible = false;
                StateHasChanged();
            }
        }
    }

    public void Dispose() => Tracker.Changed -= OnChanged;
}
