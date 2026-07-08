using System.Diagnostics;
using OpenTelemetry.Trace;

namespace Shuttle.EFCore;

internal static class ActivitySources {
    public static readonly ActivitySource ShuttleEfCore = new(ShuttleEfCoreConstants.RootNamespace);
}
