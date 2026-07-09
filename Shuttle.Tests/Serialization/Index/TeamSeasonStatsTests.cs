using System.Text.Json;
using Shuttle.Shl.Api.Models.Index.V1;

namespace Shuttle.Tests.Serialization.Index;

public class TeamSeasonStatsTests {

    private static readonly JsonSerializerOptions Options = new() {
        PropertyNameCaseInsensitive = true,
    };

    [Theory]
    [InlineData("0.625", 0.625f)]
    [InlineData("0", 0f)]
    [InlineData("1", 1f)]
    public void DeserializesWinPercentFromString(string winPercent, float expected) {
        var json = $$"""
            {
                "wins": 10, "losses": 4, "overtimeLosses": 2, "shootoutWins": 1,
                "shootoutLosses": 1, "points": 23, "goalsFor": 40, "goalsAgainst": 25,
                "winPercent": "{{winPercent}}"
            }
            """;

        var stats = JsonSerializer.Deserialize<TeamSeasonStats>(json, Options);

        Assert.NotNull(stats);
        Assert.Equal(expected, stats.WinPercent);
    }

    [Theory]
    [InlineData("0.625", 0.625f)]
    [InlineData("0", 0f)]
    [InlineData("1", 1f)]
    public void DeserializesWinPercentFromNumber(string winPercent, float expected) {
        var json = $$"""
            {
                "wins": 10, "losses": 4, "overtimeLosses": 2, "shootoutWins": 1,
                "shootoutLosses": 1, "points": 23, "goalsFor": 40, "goalsAgainst": 25,
                "winPercent": {{winPercent}}
            }
            """;

        var stats = JsonSerializer.Deserialize<TeamSeasonStats>(json, Options);

        Assert.NotNull(stats);
        Assert.Equal(expected, stats.WinPercent);
    }
}
