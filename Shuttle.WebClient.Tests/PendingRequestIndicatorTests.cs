using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.WebClient.Components;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Component tests for <see cref="PendingRequestIndicator"/>: it shows the current request description
/// while a request is in flight (after the short show-delay) and hides again once it completes.
/// </summary>
public class PendingRequestIndicatorTests : WebClientTestContext {
    private IPendingRequestTracker Tracker => Services.GetRequiredService<IPendingRequestTracker>();

    [Fact]
    public void Hidden_when_no_request_is_in_flight() {
        var cut = Render<PendingRequestIndicator>();

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Shows_the_description_while_a_request_is_in_flight_then_hides() {
        var cut = Render<PendingRequestIndicator>();

        var token = Tracker.Begin("Looking up players…");
        cut.WaitForState(() => cut.Markup.Contains("Looking up players…"));

        token.Dispose();
        cut.WaitForState(() => !cut.Markup.Contains("Looking up players…"));
    }
}
