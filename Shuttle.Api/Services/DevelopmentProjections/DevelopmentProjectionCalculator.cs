using Shuttle.Models.Players;

namespace Shuttle.Api.Services.DevelopmentProjections;

public sealed record DevelopmentPlayerCurve(
    int PlayerId,
    string Name,
    string Username,
    IReadOnlyList<DevelopmentCurveObservation> Observations);

/// <summary>Identity of the currently supported persisted projection algorithm.</summary>
public static class DevelopmentProjectionAlgorithm {
    public const int CurrentVersion = DevelopmentProjectionAlgorithmVersions.Current;
}

/// <summary>Builds deterministic peer-based player development projections.</summary>
public sealed class DevelopmentProjectionCalculator {
    public const int CadenceDays = 7;
    public const int MinimumEarnedDataPoints = 10;
    public const int MinimumObservedPoints = 8;
    public const int MaximumForecastPeers = 25;
    public const int MinimumForecastPeers = 5;
    public const int SimilarPlayersReturned = 5;
    public const double SakoeChibaFraction = 0.10;

    private static readonly TimeSpan Cadence = TimeSpan.FromDays(CadenceDays);

    public IReadOnlyList<PlayerDevelopmentProjectionResult> Calculate(
        IReadOnlyList<DevelopmentPlayerCurve> players,
        DateTimeOffset dataAsOf,
        DateTimeOffset generatedAt) {
        ArgumentNullException.ThrowIfNull(players);

        var curves = players
            .Select(player => {
                var observations = player.Observations
                    .Where(observation => observation.Date <= dataAsOf)
                    .OrderBy(observation => observation.Date)
                    .ToArray();
                var latest = observations.LastOrDefault();
                return new ResampledPlayer(
                    player.PlayerId,
                    player.Name,
                    player.Username,
                    latest.TotalTpe,
                    latest.Date,
                    observations.Length,
                    DevelopmentCurveResampler.Resample(observations, latest.Date, Cadence));
            })
            .ToArray();
        var eligibleCurves = curves
            .Where(IsEligible)
            .ToArray();

        return curves
            .Select(target => !IsEligible(target)
                ? new PlayerDevelopmentProjectionResult {
                    PlayerId = target.PlayerId,
                    AlgorithmVersion = DevelopmentProjectionAlgorithm.CurrentVersion,
                    Status = DevelopmentProjectionStatus.InsufficientObservedData,
                    DataAsOf = dataAsOf,
                    GeneratedAt = generatedAt,
                    IsCurrent = false,
                }
                : Calculate(target, eligibleCurves, dataAsOf, generatedAt))
            .ToArray();
    }

    private static bool IsEligible(ResampledPlayer player) =>
        player.EarnedDataPointCount >= MinimumEarnedDataPoints
        && player.Points.Count >= MinimumObservedPoints;

    private static PlayerDevelopmentProjectionResult Calculate(
        ResampledPlayer target,
        IReadOnlyList<ResampledPlayer> allPlayers,
        DateTimeOffset dataAsOf,
        DateTimeOffset generatedAt) {
        var targetValues = target.Points.Select(point => (double)point.TotalTpe).ToArray();
        var bandWidth = Math.Max(1, (int)Math.Ceiling(targetValues.Length * SakoeChibaFraction));
        var peers = allPlayers
            .Where(candidate =>
                candidate.PlayerId != target.PlayerId
                && candidate.Points.Count > target.Points.Count)
            .Select(candidate => {
                var candidateValues = candidate.Points
                    .Take(target.Points.Count)
                    .Select(point => (double)point.TotalTpe)
                    .ToArray();
                var similarity = DevelopmentCurveSimilarity.Compare(
                    targetValues,
                    candidateValues,
                    bandWidth);
                return new ScoredPeer(candidate, similarity.Distance, similarity.Score);
            })
            .OrderBy(peer => peer.Distance)
            .ThenBy(peer => peer.Player.PlayerId)
            .Take(MaximumForecastPeers)
            .ToArray();

        var projectedPoints = CalculateForecast(target, peers);
        var similarPlayers = peers
            .Take(SimilarPlayersReturned)
            .Select(peer => new DevelopmentSimilarity {
                PlayerId = peer.Player.PlayerId,
                Name = peer.Player.Name,
                Username = peer.Player.Username,
                Score = peer.Score,
                Distance = peer.Distance,
            })
            .ToArray();

        return new PlayerDevelopmentProjectionResult {
            PlayerId = target.PlayerId,
            AlgorithmVersion = DevelopmentProjectionAlgorithm.CurrentVersion,
            Status = DevelopmentProjectionStatus.Available,
            DataAsOf = dataAsOf,
            GeneratedAt = generatedAt,
            IsCurrent = false,
            Projection = new PlayerDevelopmentProjection {
                PlayerId = target.PlayerId,
                DataAsOf = dataAsOf,
                GeneratedAt = generatedAt,
                IsCurrent = false,
                CadenceDays = CadenceDays,
                BaselineTotalTpe = target.LatestTotalTpe,
                BaselineDate = target.LatestObservationDate,
                ObservedPoints = target.Points,
                ProjectedPoints = projectedPoints,
                SimilarPlayers = similarPlayers,
            },
        };
    }

    private static IReadOnlyList<DevelopmentProjectionPoint> CalculateForecast(
        ResampledPlayer target,
        IReadOnlyList<ScoredPeer> peers) {
        var forecast = new List<DevelopmentProjectionPoint>();
        var targetIndex = target.Points.Count - 1;
        var targetTotalTpe = target.LatestTotalTpe;

        var maximumHorizon = peers.Count == 0
            ? 0
            : peers.Max(peer => peer.Player.Points.Count - target.Points.Count);

        for (var horizon = 1; horizon <= maximumHorizon; horizon++) {
            var values = peers
                .Where(peer => peer.Player.Points.Count > targetIndex + horizon)
                .Select(peer => {
                    var peerCurrent = peer.Player.Points[targetIndex].TotalTpe;
                    var peerFuture = peer.Player.Points[targetIndex + horizon].TotalTpe;
                    return (double)targetTotalTpe + peerFuture - peerCurrent;
                })
                .Order()
                .ToArray();

            if (values.Length < MinimumForecastPeers) {
                break;
            }

            forecast.Add(new DevelopmentProjectionPoint {
                Date = target.LatestObservationDate + (Cadence * horizon),
                P10 = Quantile(values, 0.10),
                P50 = Quantile(values, 0.50),
                P90 = Quantile(values, 0.90),
                PeerCount = values.Length,
            });
        }

        return forecast;
    }

    private static double Quantile(IReadOnlyList<double> sortedValues, double probability) {
        var position = (sortedValues.Count - 1) * probability;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        if (lower == upper) {
            return sortedValues[lower];
        }

        var fraction = position - lower;
        return sortedValues[lower] + ((sortedValues[upper] - sortedValues[lower]) * fraction);
    }

    private sealed record ResampledPlayer(
        int PlayerId,
        string Name,
        string Username,
        int LatestTotalTpe,
        DateTimeOffset LatestObservationDate,
        int EarnedDataPointCount,
        IReadOnlyList<DevelopmentCurvePoint> Points);

    private sealed record ScoredPeer(ResampledPlayer Player, double Distance, double Score);
}
