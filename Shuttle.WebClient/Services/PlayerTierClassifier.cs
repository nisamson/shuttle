using Shuttle.Models.Players;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.WebClient.Services;

public enum PlayerTierBadgeKind {
    Current,
    Potential,
    Peak,
}

public enum PlayerTierCertainty {
    Low,
    Medium,
    High,
}

public sealed record PlayerTierBadgeModel(
    PlayerTierBadgeKind Kind,
    string LongTierName,
    string ShortTierName,
    int Tpe,
    PlayerTierCertainty? Certainty = null);

public static class PlayerTierClassifier {
    private static readonly TierDefinition[] Tiers = [
        new(0, 199, "Minor Depth F", "Minor Depth D", "Minor Depth", "Min Depth F", "Min Depth D", "Min Depth"),
        new(200, 299, "Minor Bottom 6", "Minor Bottom 4", "Minor Backup", "Min Bot 6", "Min Bot 4", "Min Backup"),
        new(300, 449, "Minor Top 6", "Minor Top 4", "Minor Starter", "Min Top 6", "Min Top 4", "Min Starter"),
        new(450, 649, "Depth Forward", "Depth Defenseman", "Depth", "Depth F", "Depth D", "Depth"),
        new(650, 999, "Bottom 6", "Bottom 4", "Fringe Starter", "Bot 6", "Bot 4", "Fringe"),
        new(1000, 1499, "Core Forward", "Core Defenseman", "Backup", "Core F", "Core D", "Backup"),
        new(1500, 1849, "Star Forward", "Star Defenseman", "Starter", "Star F", "Star D", "Starter"),
        new(1850, 1999, "Elite Forward", "Elite Defenseman", "Elite", "Elite F", "Elite D", "Elite"),
        new(2000, null, "Franchise Forward", "Franchise Defenseman", "Franchise", "Franchise F", "Franchise D", "Franchise"),
    ];

    public static PlayerTierBadgeModel Current(int totalTpe, PlayerPosition position) =>
        Create(PlayerTierBadgeKind.Current, totalTpe, position);

    public static PlayerTierBadgeModel? Development(
        PlayerPosition position,
        IReadOnlyList<TpeTimelinePoint>? timeline,
        PlayerDevelopmentProjectionResult? projectionResult) {
        if (timeline is not { Count: > 0 }) {
            return null;
        }

        var ordered = timeline.OrderBy(point => point.TaskDate).ToArray();
        var currentTpe = ordered[^1].TotalTpe;
        var peakTpe = ordered.Max(point => point.TotalTpe);
        if (HasMajorDecline(currentTpe, peakTpe)) {
            return Create(PlayerTierBadgeKind.Peak, peakTpe, position);
        }

        if (projectionResult is not {
            Status: DevelopmentProjectionStatus.Available,
            Projection.ProjectedPoints.Count: > 0,
        }) {
            return null;
        }

        var projection = projectionResult.Projection;
        var peakPoint = projection.ProjectedPoints.MaxBy(point => point.P50)!;
        var potentialTpe = (int)Math.Round(
            Math.Max(peakTpe, peakPoint.P50),
            MidpointRounding.AwayFromZero);
        var lowTier = FindTierIndex(peakPoint.P10);
        var medianTier = FindTierIndex(peakPoint.P50);
        var highTier = FindTierIndex(peakPoint.P90);
        var relativeWidth = (peakPoint.P90 - peakPoint.P10) / Math.Max(peakPoint.P50, 1);
        var certainty =
            relativeWidth <= 0.10
            && peakPoint.PeerCount >= 10
            && lowTier == medianTier
            && medianTier == highTier
                ? PlayerTierCertainty.High
                : relativeWidth <= 0.40
                  && peakPoint.PeerCount >= 7
                  && highTier - lowTier <= 1
                    ? PlayerTierCertainty.Medium
                    : PlayerTierCertainty.Low;

        return Create(PlayerTierBadgeKind.Potential, potentialTpe, position, certainty);
    }

    public static bool HasMajorDecline(int currentTpe, int peakTpe) =>
        PlayerTierExtensions.HasMajorDecline(currentTpe, peakTpe);

    private static PlayerTierBadgeModel Create(
        PlayerTierBadgeKind kind,
        int tpe,
        PlayerPosition position,
        PlayerTierCertainty? certainty = null) {
        var tier = Tiers[FindTierIndex(tpe)];
        var names = position.IsGoalie
            ? (tier.GoalieLong, tier.GoalieShort)
            : position.IsForward
                ? (tier.ForwardLong, tier.ForwardShort)
                : (tier.DefenseLong, tier.DefenseShort);
        return new(kind, names.Item1, names.Item2, tpe, certainty);
    }

    private static int FindTierIndex(double tpe) =>
        (int)PlayerTierExtensions.FromTotalTpe(tpe);

    private sealed record TierDefinition(
        int MinTpe,
        int? MaxTpe,
        string ForwardLong,
        string DefenseLong,
        string GoalieLong,
        string ForwardShort,
        string DefenseShort,
        string GoalieShort);
}
