namespace Shuttle.WebClient.Services;

/// <summary>
/// A <see cref="DelegatingHandler"/> that registers every outgoing backend request with the
/// <see cref="IPendingRequestTracker"/> for the duration of the call, so the UI can show an app-wide
/// "communicating with server" indicator. Attached as the outermost handler on each API client so it
/// also spans access-token acquisition.
/// </summary>
public sealed class PendingRequestHandler : DelegatingHandler {
    private readonly IPendingRequestTracker tracker;

    public PendingRequestHandler(IPendingRequestTracker tracker) {
        this.tracker = tracker;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) {
        using var _ = tracker.Begin(PendingRequestDescriber.Describe(request));
        return await base.SendAsync(request, cancellationToken);
    }
}
