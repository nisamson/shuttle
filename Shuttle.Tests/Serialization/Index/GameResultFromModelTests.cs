using Shuttle.Shl.Api.Models.Common;
using ApiGameResult = Shuttle.Shl.Api.Models.Index.V1.GameResult;
using EntityGameResult = Shuttle.EFCore.Entities.Index.GameResult;

namespace Shuttle.Tests.Serialization.Index;

public class GameResultFromModelTests {

    private static ApiGameResult Model(string? gameType, string slug) => new(
        GameId: 1,
        Season: 65,
        League: 0,
        Date: new DateOnly(2031, 10, 10),
        HomeTeam: 0,
        AwayTeam: 2,
        HomeScore: 5,
        AwayScore: 0,
        GameType: gameType!,
        Played: true,
        Overtime: false,
        Shootout: false,
        Slug: slug);

    [Fact]
    public void UsesGameTypeString_WhenPresent() {
        var entity = EntityGameResult.FromModel(Model("Playoffs", "650210100002"));
        Assert.Equal(GameType.Playoffs, entity.GameType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Something Unexpected")]
    public void FallsBackToSlug_WhenGameTypeStringMissingOrInvalid(string? gameType) {
        // Slug encodes a Regular Season game (type digit 2).
        var entity = EntityGameResult.FromModel(Model(gameType, "650210100002"));
        Assert.Equal(GameType.RegularSeason, entity.GameType);
    }

    [Fact]
    public void Throws_WhenGameTypeStringInvalidAndSlugUnparseable() {
        Assert.Throws<FormatException>(() => EntityGameResult.FromModel(Model(null, "not-a-slug")));
    }
}
