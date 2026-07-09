using System.Diagnostics;

namespace Shuttle.Api.Jobs;

internal static class ActivitySources {
    public static readonly ActivitySource ShuttleJobs = new ActivitySource("Shuttle.Api.Jobs");
}
