using Shuttle.Analysis;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.Tests.Analysis;

public class PositionFilterTests {

    [Fact]
    public void TryParse_NullSpec_NoFilter() {
        Assert.True(PositionFilter.TryParse(null, out var positions, out var error));
        Assert.Null(positions);
        Assert.Null(error);
    }

    [Fact]
    public void TryParse_WhitespaceSpec_NoFilter() {
        Assert.True(PositionFilter.TryParse("   ", out var positions, out var error));
        Assert.Null(positions);
        Assert.Null(error);
    }

    [Fact]
    public void TryParse_SingleGoalie_ParsesGoalie() {
        Assert.True(PositionFilter.TryParse("G", out var positions, out var error));
        Assert.Null(error);
        Assert.NotNull(positions);
        Assert.Equal(new[] { PlayerPosition.Goalie }, positions!.OrderBy(p => p));
    }

    [Fact]
    public void TryParse_ForwardCodes_ParsesEach() {
        Assert.True(PositionFilter.TryParse("C,RW,LW", out var positions, out var error));
        Assert.Null(error);
        Assert.NotNull(positions);
        Assert.Equal(
            new HashSet<PlayerPosition> {
                PlayerPosition.Center, PlayerPosition.RightWing, PlayerPosition.LeftWing,
            },
            positions);
    }

    [Fact]
    public void TryParse_DefenseCodes_CaseInsensitive() {
        Assert.True(PositionFilter.TryParse("rd,ld", out var positions, out var error));
        Assert.Null(error);
        Assert.Equal(
            new HashSet<PlayerPosition> { PlayerPosition.RightDefense, PlayerPosition.LeftDefense },
            positions);
    }

    [Fact]
    public void TryParse_ForwardAlias_ExpandsToForwards() {
        Assert.True(PositionFilter.TryParse("F", out var positions, out _));
        Assert.Equal(
            new HashSet<PlayerPosition> {
                PlayerPosition.Center, PlayerPosition.LeftWing, PlayerPosition.RightWing,
            },
            positions);
    }

    [Fact]
    public void TryParse_DefenseAlias_ExpandsToDefensemen() {
        Assert.True(PositionFilter.TryParse("d", out var positions, out _));
        Assert.Equal(
            new HashSet<PlayerPosition> { PlayerPosition.LeftDefense, PlayerPosition.RightDefense },
            positions);
    }

    [Fact]
    public void TryParse_MixedAliasesAndGoalie_Combines() {
        Assert.True(PositionFilter.TryParse("F,G", out var positions, out _));
        Assert.Equal(
            new HashSet<PlayerPosition> {
                PlayerPosition.Center, PlayerPosition.LeftWing, PlayerPosition.RightWing,
                PlayerPosition.Goalie,
            },
            positions);
    }

    [Fact]
    public void TryParse_WhitespaceAndDuplicates_Deduplicated() {
        Assert.True(PositionFilter.TryParse(" C , c , RW ", out var positions, out _));
        Assert.Equal(
            new HashSet<PlayerPosition> { PlayerPosition.Center, PlayerPosition.RightWing },
            positions);
    }

    [Fact]
    public void TryParse_AliasOverlappingExplicitCode_Deduplicated() {
        Assert.True(PositionFilter.TryParse("F,C", out var positions, out _));
        Assert.Equal(
            new HashSet<PlayerPosition> {
                PlayerPosition.Center, PlayerPosition.LeftWing, PlayerPosition.RightWing,
            },
            positions);
    }

    [Fact]
    public void TryParse_EmptyTokens_Ignored() {
        Assert.True(PositionFilter.TryParse("C,,RW,", out var positions, out _));
        Assert.Equal(
            new HashSet<PlayerPosition> { PlayerPosition.Center, PlayerPosition.RightWing },
            positions);
    }

    [Fact]
    public void TryParse_InvalidToken_ReturnsError() {
        Assert.False(PositionFilter.TryParse("C,XX", out var positions, out var error));
        Assert.Null(positions);
        Assert.NotNull(error);
        Assert.Contains("XX", error);
    }

    [Theory]
    [InlineData("g", PlayerPosition.Goalie)]
    [InlineData("C", PlayerPosition.Center)]
    [InlineData("lw", PlayerPosition.LeftWing)]
    [InlineData("RW", PlayerPosition.RightWing)]
    [InlineData("ld", PlayerPosition.LeftDefense)]
    [InlineData("RD", PlayerPosition.RightDefense)]
    [InlineData(" rd ", PlayerPosition.RightDefense)]
    [InlineData("Right Defense", PlayerPosition.RightDefense)]
    public void TryFromString_ValidCode_ReturnsPosition(string input, PlayerPosition expected) {
        Assert.True(PositionExtensions.TryFromString(input, out var position));
        Assert.Equal(expected, position);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("xx")]
    [InlineData("F")]
    [InlineData("D")]
    public void TryFromString_InvalidOrGroupCode_ReturnsFalse(string? input) {
        Assert.False(PositionExtensions.TryFromString(input, out _));
    }
}
