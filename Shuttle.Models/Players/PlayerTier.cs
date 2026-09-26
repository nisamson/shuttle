namespace Shuttle.Models.Players;

/// <summary>
/// Position-neutral TPE bands used to filter and sort players. The numeric values are ordered from
/// the lowest to highest tier.
/// </summary>
public enum PlayerTier {
    MinorDepth,
    MinorBottom,
    MinorTop,
    Depth,
    Bottom,
    Core,
    Star,
    Elite,
    Franchise,
}

public static class PlayerTierExtensions {
    private static readonly PlayerTier[] AllTiers = Enum.GetValues<PlayerTier>();

    public static IReadOnlyList<PlayerTier> All => AllTiers;

    public static PlayerTier FromTotalTpe(int totalTpe) => FromTotalTpe((double)totalTpe);

    public static PlayerTier FromTotalTpe(double totalTpe) => totalTpe switch {
        < 200 => PlayerTier.MinorDepth,
        < 300 => PlayerTier.MinorBottom,
        < 450 => PlayerTier.MinorTop,
        < 650 => PlayerTier.Depth,
        < 1000 => PlayerTier.Bottom,
        < 1500 => PlayerTier.Core,
        < 1850 => PlayerTier.Star,
        < 2000 => PlayerTier.Elite,
        _ => PlayerTier.Franchise,
    };

    public static bool HasMajorDecline(int currentTpe, int peakTpe) =>
        peakTpe - currentTpe >= Math.Max(50, peakTpe * 0.10);

    public static PlayerTier? FromDevelopment(
        int currentTpe,
        int peakTpe,
        double? projectedPeakTpe) {
        if (HasMajorDecline(currentTpe, peakTpe)) {
            return FromTotalTpe(peakTpe);
        }

        if (projectedPeakTpe is null) {
            return null;
        }

        var potentialTpe = Math.Round(
            Math.Max(peakTpe, projectedPeakTpe.Value),
            MidpointRounding.AwayFromZero);
        return FromTotalTpe(potentialTpe);
    }

    public static string ToDisplayName(this PlayerTier tier) => tier switch {
        PlayerTier.MinorDepth => "Minor Depth",
        PlayerTier.MinorBottom => "Minor Bottom",
        PlayerTier.MinorTop => "Minor Top",
        PlayerTier.Depth => "Depth",
        PlayerTier.Bottom => "Bottom",
        PlayerTier.Core => "Core",
        PlayerTier.Star => "Star",
        PlayerTier.Elite => "Elite",
        PlayerTier.Franchise => "Franchise",
        _ => tier.ToString(),
    };
}
