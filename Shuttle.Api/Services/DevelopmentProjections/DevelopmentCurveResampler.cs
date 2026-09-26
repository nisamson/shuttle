using Shuttle.Models.Players;

namespace Shuttle.Api.Services.DevelopmentProjections;

/// <summary>An irregular cumulative TPE observation used by the projection calculator.</summary>
public readonly record struct DevelopmentCurveObservation(DateTimeOffset Date, int TotalTpe);

/// <summary>Pure fixed-cadence resampling for cumulative TPE timelines.</summary>
public static class DevelopmentCurveResampler {
    public static IReadOnlyList<DevelopmentCurvePoint> Resample(
        IEnumerable<DevelopmentCurveObservation> observations,
        DateTimeOffset dataAsOf,
        TimeSpan cadence) {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(cadence, TimeSpan.Zero);

        var ordered = observations
            .Where(o => o.Date <= dataAsOf)
            .OrderBy(o => o.Date)
            .GroupBy(o => o.Date)
            .Select(g => g.Last())
            .ToArray();

        if (ordered.Length == 0) {
            return [];
        }

        var result = new List<DevelopmentCurvePoint>();
        var observationIndex = 0;
        var totalTpe = ordered[0].TotalTpe;

        for (var date = ordered[0].Date; date <= dataAsOf; date += cadence) {
            while (observationIndex + 1 < ordered.Length
                && ordered[observationIndex + 1].Date <= date) {
                observationIndex++;
                totalTpe = ordered[observationIndex].TotalTpe;
            }

            result.Add(new DevelopmentCurvePoint {
                Date = date,
                TotalTpe = totalTpe,
            });
        }

        if (result[^1].Date < dataAsOf) {
            while (observationIndex + 1 < ordered.Length
                && ordered[observationIndex + 1].Date <= dataAsOf) {
                observationIndex++;
                totalTpe = ordered[observationIndex].TotalTpe;
            }

            result.Add(new DevelopmentCurvePoint {
                Date = dataAsOf,
                TotalTpe = totalTpe,
            });
        }

        return result;
    }
}
