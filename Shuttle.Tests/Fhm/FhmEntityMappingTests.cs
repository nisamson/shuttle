using Microsoft.EntityFrameworkCore;
using Shuttle.Fhm.Entities.Names;
using Shuttle.Fhm.Entities.Players;
using Shuttle.Fhm.Serde.Domain.Binary;
using Shuttle.Fhm.Serde.Domain.Files;
using Shuttle.Fhm.Serde.Domain.Model;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Common.Mixins;
using EntityPlayerAttributes = Shuttle.Fhm.Entities.Players.FhmPlayerAttributes;
using SavePlayerAttributes = Shuttle.Fhm.Serde.Domain.Files.FhmPlayerAttributes;

namespace Shuttle.Tests.Fhm;

public sealed class FhmEntityMappingTests
{
    [Fact]
    public void FromSave_MapsIdentityNamesDatePositionsAndAttributes()
    {
        var source = CreatePlayerRecord();
        source.FirstNameId = 10;
        source.SurnameId = 11;
        source.CommonNameId = 12;
        source.BirthDate = new FhmDate(1999, 2, 3);
        source.PositionRatings.Goalie = 2;
        source.PositionRatings.LeftDefenceman = 18;
        source.PositionRatings.RightDefenceman = 18;
        source.PositionRatings.LeftWing = 4;
        source.PositionRatings.Centre = 5;
        source.PositionRatings.RightWing = 5;

        var player = FhmPlayer.FromSave(source, new Dictionary<int, FhmName> {
            [10] = new(10, "Ada", 1, 2, true, false, true),
            [11] = new(11, "Lovelace", 2, 3, false, true, false),
            [12] = new(12, "A. Lovelace", 3, 4, true, true, false),
        });

        Assert.Equal(42, player.InternalId);
        Assert.Equal("Ada", player.FirstName);
        Assert.Equal("Lovelace", player.LastName);
        Assert.Equal("A. Lovelace", player.NickName);
        Assert.Equal(new DateOnly(1999, 2, 3), player.BirthDate);
        Assert.Equal(PlayerPosition.LeftDefense, player.Position);
        Assert.Equal(18, player.PositionAffinity.LeftDefense);
        Assert.Equal(42, player.Attributes.InternalId);
        Assert.Equal(42, player.OpaqueData.PlayerId);
    }

    [Fact]
    public void FromSave_HandlesEmptyNamesAndRejectsInvalidReferencesAndDates()
    {
        var source = CreatePlayerRecord();
        var names = new Dictionary<int, FhmName>();

        var player = FhmPlayer.FromSave(source, names);
        Assert.Null(player.First);
        Assert.Null(player.Last);
        Assert.Null(player.Nick);

        source.FirstNameId = 99;
        Assert.Throws<ArgumentException>(() => FhmPlayer.FromSave(source, names));

        source.FirstNameId = FhmNullConstants.Null;
        source.BirthDate = new FhmDate(0, 1, 1);
        Assert.Throws<ArgumentException>(() => FhmPlayer.FromSave(source, names));

        source.BirthDate = new FhmDate(2000, 1, 1);
        source.InternalIdentity = FhmNullConstants.Null;
        Assert.Throws<ArgumentException>(() => FhmPlayer.FromSave(source, names));
    }

    [Fact]
    public void PositionAffinity_ValidatesRatingsAndUsesStableTieBreakOrder()
    {
        var ratings = new FhmPlayerPositionRatings();
        ratings.LeftDefenceman = 20;
        ratings.RightDefenceman = 20;
        Assert.Equal(PlayerPosition.LeftDefense, FhmPositionAffinity.FromSave(ratings).PrimaryPosition);

        ratings.RawValues[0] = 21;
        Assert.Throws<ArgumentOutOfRangeException>(() => FhmPositionAffinity.FromSave(ratings));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FhmPositionAffinity { Center = 21 });
    }

    [Fact]
    public void Attributes_FromSavePreservesSkaterAndGoalieFields()
    {
        var source = new SavePlayerAttributes();
        source.BigGames = 1;
        source.DevRate = 16;
        source.Teamplayer = 20;
        source.Stamina = 29;
        source.GoalieReflexes = 32;
        source.GoalieStamina = 33;
        source.PuckHandling = 37;
        source.GoaliePositioning = 48;
        source.GoaliePassing = 49;
        source.GoaliePokecheck = 50;
        source.GoalieBlocker = 51;
        source.GoalieGlove = 52;
        source.GoalieRebound = 53;
        source.GoalieRecovery = 54;
        source.GoaliePuckhandling = 55;
        source.GoalieLowShots = 56;
        source.MentalToughness = 57;
        source.GoalieSkating = 58;

        var attributes = EntityPlayerAttributes.FromSave(source);
        var goalie = (IGoaltenderRatings)attributes;

        Assert.Equal(1, attributes.BigGames);
        Assert.Equal(16, attributes.DevelopmentRate);
        Assert.Equal(20, attributes.TeamPlayer);
        Assert.Equal(29, attributes.Stamina);
        Assert.Equal(32, attributes.GoalieReflexes);
        Assert.Equal(33, attributes.GoalieStamina);
        Assert.Equal(37, attributes.Puckhandling);
        Assert.Equal(48, attributes.GoaliePositioning);
        Assert.Equal(49, attributes.GoaliePassing);
        Assert.Equal(50, attributes.GoaliePokeCheck);
        Assert.Equal(51, attributes.GoalieBlocker);
        Assert.Equal(52, attributes.GoalieGlove);
        Assert.Equal(53, attributes.GoalieRebound);
        Assert.Equal(54, attributes.GoalieRecovery);
        Assert.Equal(55, attributes.GoaliePuckhandling);
        Assert.Equal(56, attributes.GoalieLowShots);
        Assert.Equal(57, attributes.MentalToughness);
        Assert.Equal(58, attributes.GoalieSkating);
        Assert.Equal(32, goalie.Reflexes);
        Assert.Equal(33, goalie.Stamina);
        Assert.Equal(48, goalie.Positioning);
        Assert.Equal(49, goalie.Passing);
        Assert.Equal(58, goalie.Skating);
    }

    [Fact]
    public void OpaqueData_RoundTripsADeepSnapshot()
    {
        var source = CreatePlayerRecord();
        source.UnknownS401 = 1234;
        source.UnknownString01 = "opaque";
        source.UnknownU2Values01[0] = 77;
        source.SpecialAbilities.Add(9);
        var opaque = FhmPlayerOpaqueData.FromSave(source);

        source.UnknownS401 = 0;
        source.UnknownString01 = null;
        source.UnknownU2Values01[0] = 0;
        source.SpecialAbilities.Clear();

        var restored = opaque.ToSaveRecord();
        Assert.Equal(1234, restored.UnknownS401);
        Assert.Equal("opaque", restored.UnknownString01);
        Assert.Equal((ushort)77, restored.UnknownU2Values01[0]);
        Assert.Equal([9], restored.SpecialAbilities);
        Assert.Equal(opaque.SerializedRecord, restored.ToBytes());
    }

    [Fact]
    public void EntityConfigurations_ConstructAnRdbmsAgnosticModel()
    {
        using var context = new FhmModelContext();

        Assert.NotNull(context.Model.FindEntityType(typeof(FhmPlayer)));
        Assert.NotNull(context.Model.FindEntityType(typeof(FhmName)));
        Assert.NotNull(context.Model.FindEntityType(typeof(EntityPlayerAttributes)));
        Assert.NotNull(context.Model.FindEntityType(typeof(FhmPlayerOpaqueData)));
        Assert.NotNull(context.Model.FindEntityType(typeof(FhmPositionAffinity)));
        Assert.Null(context.Model.FindEntityType(typeof(FhmPlayer))!.FindProperty(nameof(FhmPlayer.Position)));
    }

    private static FhmPlayerRecord CreatePlayerRecord() =>
        new() {
            InternalIdentity = 42,
            BirthDate = new FhmDate(2000, 1, 1),
        };

    private sealed class FhmModelContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new FhmNameEntityConfiguration());
            modelBuilder.ApplyConfiguration(new FhmPlayerEntityConfiguration());
            modelBuilder.ApplyConfiguration(new FhmPlayerAttributesEntityConfiguration());
            modelBuilder.ApplyConfiguration(new FhmPlayerOpaqueDataEntityConfiguration());
        }
    }
}
