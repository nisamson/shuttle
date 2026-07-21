using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Unit tests for <see cref="PendingRequestTracker"/> — the in-flight request counter that drives the
/// app-wide "communicating with server" indicator.
/// </summary>
public class PendingRequestTrackerTests {
    [Fact]
    public void Starts_idle() {
        var tracker = new PendingRequestTracker();

        Assert.False(tracker.IsBusy);
        Assert.Equal(0, tracker.Count);
        Assert.Null(tracker.CurrentDescription);
    }

    [Fact]
    public void Begin_marks_busy_and_exposes_the_description() {
        var tracker = new PendingRequestTracker();

        using var _ = tracker.Begin("Loading players…");

        Assert.True(tracker.IsBusy);
        Assert.Equal(1, tracker.Count);
        Assert.Equal("Loading players…", tracker.CurrentDescription);
    }

    [Fact]
    public void Disposing_the_token_marks_idle_again() {
        var tracker = new PendingRequestTracker();

        var token = tracker.Begin("Loading players…");
        token.Dispose();

        Assert.False(tracker.IsBusy);
        Assert.Null(tracker.CurrentDescription);
    }

    [Fact]
    public void Current_description_reflects_the_most_recent_active_request() {
        var tracker = new PendingRequestTracker();

        var first = tracker.Begin("Loading players…");
        var second = tracker.Begin("Saving scouting board…");

        Assert.Equal(2, tracker.Count);
        Assert.Equal("Saving scouting board…", tracker.CurrentDescription);

        // Completing the newest falls back to the previous still-active request.
        second.Dispose();
        Assert.Equal("Loading players…", tracker.CurrentDescription);

        first.Dispose();
        Assert.False(tracker.IsBusy);
    }

    [Fact]
    public void Changed_is_raised_on_begin_and_end() {
        var tracker = new PendingRequestTracker();
        var count = 0;
        tracker.Changed += () => count++;

        var token = tracker.Begin("Loading players…");
        token.Dispose();

        Assert.Equal(2, count);
    }

    [Fact]
    public void Disposing_a_token_twice_is_a_no_op() {
        var tracker = new PendingRequestTracker();
        var count = 0;
        tracker.Changed += () => count++;

        var token = tracker.Begin("Loading players…");
        token.Dispose();
        token.Dispose();

        Assert.Equal(2, count);
        Assert.False(tracker.IsBusy);
    }
}
