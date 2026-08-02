using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Index.V1;

namespace Shuttle.Tests.Serialization.Index;

public class GameSlugTests {

    // Real season-65 slugs (SS|L|T|DDMM|HH|AA) covering each league and game type.
    [Theory]
    [InlineData("650210100002", 65, KnownLeague.Shl, GameType.RegularSeason, 10, 10, 0, 2)]
    [InlineData("650211100407", 65, KnownLeague.Shl, GameType.RegularSeason, 11, 10, 4, 7)]
    [InlineData("651210100008", 65, KnownLeague.Smjhl, GameType.RegularSeason, 10, 10, 0, 8)]
    [InlineData("652210100212", 65, KnownLeague.Iihf, GameType.RegularSeason, 10, 10, 2, 12)]
    [InlineData("653211100002", 65, KnownLeague.Wjc, GameType.RegularSeason, 11, 10, 0, 2)]
    [InlineData("650110100001", 65, KnownLeague.Shl, GameType.PreSeason, 10, 10, 0, 1)]
    [InlineData("650312150304", 65, KnownLeague.Shl, GameType.Playoffs, 12, 15, 3, 4)]
    public void Parse_DecodesAllFields(
        string slug, int season, KnownLeague league, GameType gameType,
        int day, int month, int homeTeam, int awayTeam) {
        var parsed = GameSlug.Parse(slug);

        Assert.Equal((byte)season, parsed.Season);
        Assert.Equal(league, parsed.League);
        Assert.Equal(gameType, parsed.GameType);
        Assert.Equal((byte)day, parsed.Day);
        Assert.Equal((byte)month, parsed.Month);
        Assert.Equal((byte)homeTeam, parsed.HomeTeam);
        Assert.Equal((byte)awayTeam, parsed.AwayTeam);
    }

    [Theory]
    [InlineData("650210100002")]
    [InlineData("650211100407")]
    [InlineData("651210100008")]
    [InlineData("652210100212")]
    [InlineData("653211100002")]
    [InlineData("650110100001")]
    [InlineData("650312150304")]
    public void ToString_RoundTripsWithParse(string slug) {
        Assert.Equal(slug, GameSlug.Parse(slug).ToString());
    }

    [Theory]
    [InlineData("Pre-Season", GameType.PreSeason)]
    [InlineData("pre-season", GameType.PreSeason)]
    [InlineData("Regular Season", GameType.RegularSeason)]
    [InlineData("PLAYOFFS", GameType.Playoffs)]
    public void TryFromString_ParsesKnownValues(string value, GameType expected) {
        Assert.True(GameType.TryFromString(value, out var result));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public void TryFromString_ReturnsFalse_ForUnknownOrNull(string? value) {
        Assert.False(GameType.TryFromString(value, out _));
    }

    [Fact]
    public void ToString_ProducesCanonicalSlug() {
        var slug = new GameSlug(65, KnownLeague.Iihf, GameType.Playoffs, 5, 3, 9, 12);
        Assert.Equal("652305030912", slug.ToString());
    }

    [Fact]
    public void ToString_And_ToStringWithFormat_Agree() {
        var slug = new GameSlug(65, KnownLeague.Shl, GameType.RegularSeason, 10, 10, 0, 2);
        Assert.Equal(slug.ToString(), slug.ToString("anything", null));
    }

    [Fact]
    public void TryFormat_WritesTwelveChars() {
        var slug = new GameSlug(65, KnownLeague.Shl, GameType.RegularSeason, 10, 10, 0, 2);
        Span<char> buffer = stackalloc char[GameSlug.SlugLength];

        var ok = slug.TryFormat(buffer, out var written);

        Assert.True(ok);
        Assert.Equal(GameSlug.SlugLength, written);
        Assert.Equal("650210100002", new string(buffer));
    }

    [Fact]
    public void TryFormat_ReturnsFalse_WhenDestinationTooSmall() {
        var slug = new GameSlug(65, KnownLeague.Shl, GameType.RegularSeason, 10, 10, 0, 2);
        Span<char> buffer = stackalloc char[GameSlug.SlugLength - 1];

        var ok = slug.TryFormat(buffer, out var written);

        Assert.False(ok);
        Assert.Equal(0, written);
    }

    [Theory]
    [InlineData("6502101000021")] // too long
    [InlineData("65021010000")] // too short
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_ReturnsFalse_ForMalformedInput(string? slug) {
        Assert.False(GameSlug.TryParse(slug, null, out var result));
        Assert.Null(result);
    }
}
