using System.Text.Json;
using Shuttle.Analysis;
using Shuttle.EFCore.Entities.Portal;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.Tests.Analysis;

public class PlayerExportRecordTests {

    private static PlayerInformation CreatePlayer(
        Height? height = null,
        SkaterAttributes? skater = null,
        GoaltenderAttributes? goaltender = null,
        PlayerPosition position = PlayerPosition.RightDefense,
        PlayerStatus status = PlayerStatus.Active,
        PlayerHandedness handedness = PlayerHandedness.Left
    ) => new() {
        UserId = 42,
        PlayerId = 1001,
        Username = "tester",
        CreationTime = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc),
        Status = status,
        Name = "Test Player",
        Position = position,
        Handedness = handedness,
        TotalTpe = 500,
        AppliedTpe = 450,
        BankedTpe = 50,
        BankBalance = 1_000_000,
        Height = height,
        SkaterAttributes = skater,
        GoaltenderAttributes = goaltender,
    };

    private static SkaterAttributes SampleSkater() => new(
        Checking: 1, Stickchecking: 2, Hitting: 3, Positioning: 4, ShotBlocking: 5,
        DefensiveRead: 6, Aggression: 7, Bravery: 8, Determination: 9, TeamPlayer: 10,
        Leadership: 11, Temperament: 12, Professionalism: 13, Screening: 14, GettingOpen: 15,
        Passing: 16, Puckhandling: 17, ShootingAccuracy: 18, ShootingRange: 19, OffensiveRead: 20,
        Acceleration: 21, Agility: 22, Balance: 23, Speed: 24, Stamina: 25,
        Strength: 26, Fighting: 27, Faceoffs: 28);

    private static GoaltenderAttributes SampleGoaltender() => new(
        Aggression: 1, MentalToughness: 2, Determination: 3, TeamPlayer: 4, Leadership: 5,
        Stamina: 6, Professionalism: 7, Positioning: 8, Passing: 9, PokeCheck: 10,
        Blocker: 11, Glove: 12, Rebound: 13, Recovery: 14, Puckhandling: 15,
        LowShots: 16, Skating: 17, Reflexes: 18);

    [Fact]
    public void FromEntity_ConvertsHeightToTotalInches() {
        var record = PlayerExportRecord.FromEntity(CreatePlayer(height: new Height(6, 2)));

        Assert.Equal(74, record.HeightInches);
    }

    [Fact]
    public void FromEntity_NullHeight_ProducesNullInches() {
        var record = PlayerExportRecord.FromEntity(CreatePlayer(height: null));

        Assert.Null(record.HeightInches);
    }

    [Fact]
    public void FromEntity_CopiesScalarColumns() {
        var player = CreatePlayer();

        var record = PlayerExportRecord.FromEntity(player);

        Assert.Equal(player.UserId, record.UserId);
        Assert.Equal(player.PlayerId, record.PlayerId);
        Assert.Equal(player.Username, record.Username);
        Assert.Equal(player.CreationTime, record.CreationTime);
        Assert.Equal(player.BankBalance, record.BankBalance);
        Assert.Equal(player.TotalTpe, record.TotalTpe);
    }

    [Fact]
    public void FromEntity_IncludesSkaterAttributesAndOmitsGoaltender() {
        var skater = SampleSkater();

        var record = PlayerExportRecord.FromEntity(CreatePlayer(skater: skater));

        Assert.Same(skater, record.SkaterAttributes);
        Assert.Null(record.GoaltenderAttributes);
    }

    [Fact]
    public void Serialize_ExcludesNavigationProperties() {
        var record = PlayerExportRecord.FromEntity(CreatePlayer(skater: SampleSkater()));

        var json = JsonSerializer.Serialize(record, PlayerExportJson.CreateOptions());

        Assert.DoesNotContain("\"user\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("indexRecords", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Serialize_HonorsEnumConvertersAndFlatHeight() {
        var record = PlayerExportRecord.FromEntity(CreatePlayer(
            height: new Height(6, 0),
            position: PlayerPosition.RightDefense,
            status: PlayerStatus.Active,
            handedness: PlayerHandedness.Left));

        var json = JsonSerializer.Serialize(record, PlayerExportJson.CreateOptions());
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // PositionConverter writes the short code, PlayerStatus writes the lowercase value.
        Assert.Equal("RD", root.GetProperty("position").GetString());
        Assert.Equal("active", root.GetProperty("status").GetString());
        Assert.Equal("Left", root.GetProperty("handedness").GetString());
        Assert.Equal(72, root.GetProperty("heightInches").GetInt32());
        // Height is flattened to inches; the nested height object is not emitted.
        Assert.False(root.TryGetProperty("height", out _));
    }

    [Fact]
    public void Serialize_OmitsConstantSkaterMentalAttributes() {
        var record = PlayerExportRecord.FromEntity(CreatePlayer(skater: SampleSkater()));

        var json = JsonSerializer.Serialize(record, PlayerExportJson.CreateOptions());
        using var doc = JsonDocument.Parse(json);
        var skater = doc.RootElement.GetProperty("skaterAttributes");

        // Constant-15 mental attributes are dropped.
        Assert.False(skater.TryGetProperty("determination", out _));
        Assert.False(skater.TryGetProperty("teamPlayer", out _));
        Assert.False(skater.TryGetProperty("leadership", out _));
        Assert.False(skater.TryGetProperty("temperament", out _));
        Assert.False(skater.TryGetProperty("professionalism", out _));

        // Mental attributes that vary, and non-mental attributes, are retained.
        Assert.Equal(7, skater.GetProperty("aggression").GetInt32());
        Assert.Equal(8, skater.GetProperty("bravery").GetInt32());
        Assert.Equal(1, skater.GetProperty("checking").GetInt32());
        Assert.Equal(24, skater.GetProperty("speed").GetInt32());
    }

    [Fact]
    public void Serialize_OmitsConstantGoaltenderMentalAttributes() {
        var record = PlayerExportRecord.FromEntity(CreatePlayer(goaltender: SampleGoaltender()));

        var json = JsonSerializer.Serialize(record, PlayerExportJson.CreateOptions());
        using var doc = JsonDocument.Parse(json);
        var goalie = doc.RootElement.GetProperty("goaltenderAttributes");

        // Constant-15 mental attributes are dropped.
        Assert.False(goalie.TryGetProperty("determination", out _));
        Assert.False(goalie.TryGetProperty("teamPlayer", out _));
        Assert.False(goalie.TryGetProperty("leadership", out _));
        Assert.False(goalie.TryGetProperty("professionalism", out _));

        // Attributes that vary (or are non-15 constants) are retained.
        Assert.Equal(1, goalie.GetProperty("aggression").GetInt32());
        Assert.Equal(2, goalie.GetProperty("mentalToughness").GetInt32());
        Assert.Equal(6, goalie.GetProperty("goaltenderStamina").GetInt32());
        Assert.Equal(11, goalie.GetProperty("blocker").GetInt32());
    }
}
