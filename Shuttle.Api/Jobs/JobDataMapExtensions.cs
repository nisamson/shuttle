using Quartz;

namespace Shuttle.Api.Jobs;

internal static class JobDataMapExtensions {
    public static string? GetOptionalString(this JobDataMap dataMap, string key) =>
        dataMap.ContainsKey(key) ? dataMap.GetString(key) : null;
}
