namespace Shuttle.WebClient.Services;

/// <summary>
/// Tracks the number of in-flight requests to the Shuttle backend so the UI can show a single,
/// app-wide "communicating with server" indicator. Each request is registered via <see cref="Begin"/>
/// (which returns a token disposed when the request completes); the tracker exposes whether any
/// request is active and a short human-readable description of the most recently started one.
/// </summary>
public interface IPendingRequestTracker {
    /// <summary>Whether at least one request is currently in flight.</summary>
    bool IsBusy { get; }

    /// <summary>The number of requests currently in flight.</summary>
    int Count { get; }

    /// <summary>A short description of the most recently started in-flight request, or <c>null</c>.</summary>
    string? CurrentDescription { get; }

    /// <summary>Raised whenever a request starts or completes.</summary>
    event Action? Changed;

    /// <summary>
    /// Registers a new in-flight request with the given <paramref name="description"/>. Dispose the
    /// returned token when the request completes to mark it done.
    /// </summary>
    IDisposable Begin(string description);
}

/// <inheritdoc cref="IPendingRequestTracker"/>
public sealed class PendingRequestTracker : IPendingRequestTracker {
    private readonly object gate = new();
    private readonly Dictionary<long, string> active = new();
    private long nextId;
    private long latestId = -1;

    public bool IsBusy {
        get { lock (gate) { return active.Count > 0; } }
    }

    public int Count {
        get { lock (gate) { return active.Count; } }
    }

    public string? CurrentDescription {
        get {
            lock (gate) {
                return latestId >= 0 && active.TryGetValue(latestId, out var description) ? description : null;
            }
        }
    }

    public event Action? Changed;

    public IDisposable Begin(string description) {
        long id;
        lock (gate) {
            id = nextId++;
            active[id] = description;
            latestId = id;
        }

        Changed?.Invoke();
        return new Registration(this, id);
    }

    private void End(long id) {
        lock (gate) {
            if (!active.Remove(id)) {
                return;
            }

            // Surface the newest still-active request (ids increase monotonically) as "current".
            if (latestId == id) {
                latestId = active.Count > 0 ? active.Keys.Max() : -1;
            }
        }

        Changed?.Invoke();
    }

    private sealed class Registration : IDisposable {
        private PendingRequestTracker? owner;
        private readonly long id;

        public Registration(PendingRequestTracker owner, long id) {
            this.owner = owner;
            this.id = id;
        }

        public void Dispose() {
            var toEnd = owner;
            owner = null;
            toEnd?.End(id);
        }
    }
}
