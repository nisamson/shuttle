namespace Shuttle.WebClient.Services;

/// <summary>
/// Maps an outgoing backend request (HTTP method + path) to a short, human-readable description shown
/// in the pending-request indicator (e.g. "Looking up players…"). A handful of well-known endpoints
/// get bespoke wording; everything else falls back to a verb (from the method) plus a resource noun
/// (from the first path segment).
/// </summary>
public static class PendingRequestDescriber {
    public static string Describe(HttpRequestMessage request) => Describe(request.Method, request.RequestUri);

    public static string Describe(HttpMethod method, Uri? uri) {
        var path = uri is null
            ? string.Empty
            : (uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString);
        var segments = path.Split(['/', '?'], StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length >= 2) {
            var resource = segments[0].ToLowerInvariant();
            var sub = segments[1].ToLowerInvariant();
            if (resource is "players" or "player") {
                switch (sub) {
                    case "lookup":
                        return "Looking up players…";
                    case "suggestions":
                        return "Loading player directory…";
                    case "search":
                        return "Searching players…";
                }
            }
        }

        var noun = ResourceNoun(segments);
        return method.Method.ToUpperInvariant() switch {
            "GET" => $"Loading {noun}…",
            "QUERY" => $"Searching {noun}…",
            "POST" => $"Saving {noun}…",
            "PUT" or "PATCH" => $"Updating {noun}…",
            "DELETE" => $"Removing {noun}…",
            _ => "Communicating with server…",
        };
    }

    private static string ResourceNoun(string[] segments) {
        if (segments.Length == 0) {
            return "data";
        }

        return segments[0].ToLowerInvariant() switch {
            "players" or "player" => "players",
            "users" or "user" => "users",
            "leagues" or "league" or "seasons" => "league",
            "scouting" => "scouting board",
            _ => "data",
        };
    }
}
