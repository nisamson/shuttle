using Shuttle.Fhm.Serde.Sqlite.Entities;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Common.Mixins;
using SqlitePlayerAttributes = Shuttle.Fhm.Serde.Sqlite.Entities.PlayerAttributes;

namespace Shuttle.Tests.Fhm;

public sealed class PlayerAttributesTests
{
    [Fact]
    public void PositionAffinity_RejectsRatingsOutsideValidRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerPositionAffinity { Goalie = -1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerPositionAffinity { Center = 21 });
    }

    [Theory]
    [InlineData(PlayerPosition.Goalie)]
    [InlineData(PlayerPosition.LeftDefense)]
    [InlineData(PlayerPosition.RightDefense)]
    [InlineData(PlayerPosition.LeftWing)]
    [InlineData(PlayerPosition.Center)]
    [InlineData(PlayerPosition.RightWing)]
    public void PositionAffinity_SetPrimaryPositionSetsSelectedRatingAndClearsOthers(PlayerPosition position)
    {
        var affinity = new PlayerPositionAffinity
        {
            Goalie = 1,
            LeftDefense = 2,
            RightDefense = 3,
            LeftWing = 4,
            Center = 5,
            RightWing = 6,
        };

        affinity.SetPrimaryPosition(position);

        Assert.Equal(position, affinity.PrimaryPosition);
        Assert.Equal(20, affinity.GetRating(position));
        Assert.All(
            new[]
            {
                PlayerPosition.Goalie,
                PlayerPosition.LeftDefense,
                PlayerPosition.RightDefense,
                PlayerPosition.LeftWing,
                PlayerPosition.Center,
                PlayerPosition.RightWing,
            }.Where(value => value != position),
            value => Assert.Equal(0, affinity.GetRating(value)));
    }

    [Fact]
    public void PositionAffinity_SetPrimaryPositionRejectsAmbiguousPositionWithoutChangingRatings()
    {
        var affinity = new PlayerPositionAffinity { Center = 10 };

        Assert.Throws<ArgumentOutOfRangeException>(() => affinity.SetPrimaryPosition(PlayerPosition.Left | PlayerPosition.Right));
        Assert.Equal(10, affinity.Center);
    }

    [Fact]
    public void Attributes_ImplementSharedRatingContracts()
    {
        var attributes = new SqlitePlayerAttributes
        {
            Reflexes = 17,
            Skating = 13,
            GoalieStamina = 11,
            Passing = 19,
            GoaliePassing = 14,
        };

        Assert.IsAssignableFrom<IHiddenAttributes>(attributes);
        Assert.IsAssignableFrom<ISkaterRatings>(attributes);
        var goalie = Assert.IsAssignableFrom<IGoaltenderRatings>(attributes);
        Assert.Equal(17, goalie.Reflexes);
        Assert.Equal(13, goalie.Skating);
        Assert.Equal(11, goalie.Stamina);
        Assert.Equal(14, goalie.Passing);
        Assert.Equal(19, attributes.Passing);
    }
}
