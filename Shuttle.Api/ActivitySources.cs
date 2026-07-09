using System.Diagnostics;

namespace Shuttle.Api;

internal static class ActivitySources {
    public static readonly ActivitySource ShuttleApi = new ActivitySource("Shuttle.Api");
}
