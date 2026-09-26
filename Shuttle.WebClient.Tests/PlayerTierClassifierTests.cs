using Shuttle.Models.Players;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Tests;

public sealed class PlayerTierClassifierTests {
    [Theory]
    [InlineData(0, PlayerTier.MinorDepth)]
    [InlineData(199, PlayerTier.MinorDepth)]
    [InlineData(200, PlayerTier.MinorBottom)]
    [InlineData(999, PlayerTier.Bottom)]
    [InlineData(1000, PlayerTier.Core)]
    [InlineData(1849, PlayerTier.Star)]
    [InlineData(1850, PlayerTier.Elite)]
    [InlineData(2000, PlayerTier.Franchise)]
    public void Shared_tier_bands_classify_boundaries(int totalTpe, PlayerTier expected) {
        Assert.Equal(expected, PlayerTierExtensions.FromTotalTpe(totalTpe));
    }

    [Fact]
    public void Development_tier_uses_projected_peak_before_a_major_decline() {
        var tier = PlayerTierExtensions.FromDevelopment(1200, 1250, 1600);

        Assert.Equal(PlayerTier.Star, tier);
    }

    [Fact]
    public void Development_tier_uses_realized_peak_after_a_major_decline() {
        var tier = PlayerTierExtensions.FromDevelopment(1300, 1600, 2100);

        Assert.Equal(PlayerTier.Star, tier);
    }

    [Fact]
    public void Development_tier_is_unavailable_without_projection_or_major_decline() {
        var tier = PlayerTierExtensions.FromDevelopment(1200, 1250, null);

        Assert.Null(tier);
    }

    [Theory]
    [InlineData(PlayerPosition.Center, 250, "Minor Bottom 6", "Min Bot 6")]
    [InlineData(PlayerPosition.LeftDefense, 250, "Minor Bottom 4", "Min Bot 4")]
    [InlineData(PlayerPosition.Goalie, 250, "Minor Backup", "Min Backup")]
    [InlineData(PlayerPosition.RightWing, 2000, "Franchise Forward", "Franchise F")]
    public void Current_uses_position_specific_long_and_short_names(
        PlayerPosition position,
        int totalTpe,
        string longName,
        string shortName) {
        var badge = PlayerTierClassifier.Current(totalTpe, position);

        Assert.Equal(PlayerTierBadgeKind.Current, badge.Kind);
        Assert.Equal(longName, badge.LongTierName);
        Assert.Equal(shortName, badge.ShortTierName);
        Assert.Null(badge.Certainty);
    }

    [Fact]
    public void Development_returns_high_certainty_when_interval_stays_in_one_tier() {
        var badge = PlayerTierClassifier.Development(
            PlayerPosition.Center,
            Timeline(100, 500),
            Projection(1510, 1600, 1650, 10));

        Assert.NotNull(badge);
        Assert.Equal(PlayerTierBadgeKind.Potential, badge.Kind);
        Assert.Equal("Star Forward", badge.LongTierName);
        Assert.Equal(PlayerTierCertainty.High, badge.Certainty);
    }

    [Fact]
    public void Development_returns_medium_certainty_across_adjacent_tiers() {
        var badge = PlayerTierClassifier.Development(
            PlayerPosition.LeftDefense,
            Timeline(100, 500),
            Projection(1450, 1600, 1800, 7));

        Assert.NotNull(badge);
        Assert.Equal("Star Defenseman", badge.LongTierName);
        Assert.Equal(PlayerTierCertainty.Medium, badge.Certainty);
    }

    [Fact]
    public void Development_returns_low_certainty_for_a_wide_multi_tier_interval() {
        var badge = PlayerTierClassifier.Development(
            PlayerPosition.Goalie,
            Timeline(100, 500),
            Projection(1000, 1600, 2000, 12));

        Assert.NotNull(badge);
        Assert.Equal("Starter", badge.LongTierName);
        Assert.Equal(PlayerTierCertainty.Low, badge.Certainty);
    }

    [Fact]
    public void Development_replaces_potential_with_realized_peak_after_major_decline() {
        var badge = PlayerTierClassifier.Development(
            PlayerPosition.Center,
            Timeline(100, 1600, 1400),
            Projection(1800, 1900, 2000, 12));

        Assert.NotNull(badge);
        Assert.Equal(PlayerTierBadgeKind.Peak, badge.Kind);
        Assert.Equal("Star Forward", badge.LongTierName);
        Assert.Equal(1600, badge.Tpe);
        Assert.Null(badge.Certainty);
    }

    [Fact]
    public void Development_returns_no_potential_when_projection_is_unavailable() {
        var badge = PlayerTierClassifier.Development(
            PlayerPosition.Center,
            Timeline(100, 500),
            new PlayerDevelopmentProjectionResult {
                PlayerId = 1,
                Status = DevelopmentProjectionStatus.InsufficientObservedData,
                IsCurrent = true,
            });

        Assert.Null(badge);
    }

    [Theory]
    [InlineData(1000, 901, false)]
    [InlineData(1000, 900, true)]
    [InlineData(400, 351, false)]
    [InlineData(400, 350, true)]
    public void Major_decline_uses_the_greater_of_ten_percent_or_fifty_tpe(
        int peakTpe,
        int currentTpe,
        bool expected) {
        Assert.Equal(expected, PlayerTierClassifier.HasMajorDecline(currentTpe, peakTpe));
    }

    private static IReadOnlyList<TpeTimelinePoint> Timeline(params int[] values) =>
        values.Select((value, index) => new TpeTimelinePoint {
            TaskDate = new DateTime(2025, 1, 1).AddDays(index * 7),
            TotalTpe = value,
        }).ToArray();

    private static PlayerDevelopmentProjectionResult Projection(
        double p10,
        double p50,
        double p90,
        int peerCount) =>
        new() {
            PlayerId = 1,
            Status = DevelopmentProjectionStatus.Available,
            IsCurrent = true,
            Projection = new PlayerDevelopmentProjection {
                PlayerId = 1,
                DataAsOf = DateTimeOffset.UtcNow,
                GeneratedAt = DateTimeOffset.UtcNow,
                IsCurrent = true,
                CadenceDays = 7,
                BaselineTotalTpe = 500,
                ObservedPoints = [],
                ProjectedPoints = [
                    new DevelopmentProjectionPoint {
                        Date = DateTimeOffset.UtcNow.AddDays(7),
                        P10 = p10,
                        P50 = p50,
                        P90 = p90,
                        PeerCount = peerCount,
                    },
                ],
                SimilarPlayers = [],
            },
        };
}
