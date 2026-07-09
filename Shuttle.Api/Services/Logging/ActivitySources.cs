using System.Diagnostics;

namespace Shuttle.Api.Services.Logging;

public class ActivitySources {
    private static string Api { get; } = "Shuttle.Api";
    private static string Version { get; } = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unspecified";

    public static readonly ActivitySource ShuttleApi = new(Api, Version);

    public static readonly ActivitySource ShuttleJobs = new("Shuttle.Api.Jobs");
}
