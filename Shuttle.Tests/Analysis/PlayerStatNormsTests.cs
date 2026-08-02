using System.Text.Json;
using System.Text.Json.Nodes;
using Shuttle.Analysis;
using Shuttle.EFCore.Entities.Portal;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.Tests.Analysis;

public class PlayerStatNormsTests {

    private static PlayerInformation CreatePlayer(
        SkaterAttributes? skater = null,
        GoaltenderAttributes? goaltender = null,
        PlayerPosition position = PlayerPosition.RightDefense
    ) => new() {
        UserId = 42,
        PlayerId = 1,
        Username = "tester",
        CreationTime = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc),
        Status = PlayerStatus.Active,
        Name = "Test Player",
        Position = position,
        Handedness = PlayerHandedness.Left,
        TotalTpe = 500,
        AppliedTpe = 450,
        BankedTpe = 50,
        BankBalance = 1_000_000,
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

    private static JsonObject Project(PlayerInformation player) {
        var record = PlayerExportRecord.FromEntity(player);
        return JsonSerializer.SerializeToNode(record, PlayerExportJson.CreateOptions(indented: false))!.AsObject();
    }

    private static IEnumerable<double> Numbers(JsonObject obj) =>
        obj.Where(kv => kv.Value is JsonValue v && v.GetValueKind() == JsonValueKind.Number)
           .Select(kv => kv.Value!.GetValue<double>());

    [Fact]
    public void Apply_None_LeavesRawIntegerValues() {
        var obj = Project(CreatePlayer(skater: SampleSkater()));

        PlayerStatNorms.Apply(obj, StatNorm.None);

        var skater = obj["skaterAttributes"]!.AsObject();
        Assert.Equal(1, skater["checking"]!.GetValue<int>());
        Assert.Equal(24, skater["speed"]!.GetValue<int>());
    }

    [Fact]
    public void Apply_L1_ScalesComponentsToSumToOne() {
        var obj = Project(CreatePlayer(skater: SampleSkater()));

        PlayerStatNorms.Apply(obj, StatNorm.L1);

        var skater = obj["skaterAttributes"]!.AsObject();
        Assert.Equal(1.0, Numbers(skater).Sum(), 9);
        // Original names are preserved and values become the L1-normalized fractions.
        var expectedSum = new[] {
            1, 2, 3, 4, 5, 6, 7, 8, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28,
        }.Sum();
        Assert.Equal(1.0 / expectedSum, skater["checking"]!.GetValue<double>(), 9);
        Assert.Equal(24.0 / expectedSum, skater["speed"]!.GetValue<double>(), 9);
    }

    [Fact]
    public void Apply_L2_ProducesUnitVector() {
        var obj = Project(CreatePlayer(skater: SampleSkater()));

        PlayerStatNorms.Apply(obj, StatNorm.L2);

        var skater = obj["skaterAttributes"]!.AsObject();
        var sumOfSquares = Numbers(skater).Sum(v => v * v);
        Assert.Equal(1.0, sumOfSquares, 9);
    }

    [Fact]
    public void Apply_KeepsOriginalNamesAndExcludesConstantMentalAttributes() {
        var obj = Project(CreatePlayer(skater: SampleSkater()));

        PlayerStatNorms.Apply(obj, StatNorm.L2);

        var skater = obj["skaterAttributes"]!.AsObject();
        // Exact original names remain; no suffixed/renamed keys are introduced.
        Assert.True(skater.ContainsKey("checking"));
        Assert.True(skater.ContainsKey("aggression"));
        // The dropped constant mental attributes are not part of the vector.
        Assert.False(skater.ContainsKey("determination"));
        Assert.False(skater.ContainsKey("temperament"));
        // No extra normalized container objects are added.
        Assert.False(obj.ContainsKey("skaterAttributesL1Normalized"));
        Assert.False(obj.ContainsKey("skaterAttributesL2Normalized"));
    }

    [Fact]
    public void Apply_NormalizesGoaltenderVectorOnly() {
        var obj = Project(CreatePlayer(goaltender: SampleGoaltender(), position: PlayerPosition.Goalie));

        PlayerStatNorms.Apply(obj, StatNorm.L1);

        var goalie = obj["goaltenderAttributes"]!.AsObject();
        Assert.Equal(1.0, Numbers(goalie).Sum(), 9);
        // A goaltender carries no skater attribute set to normalize.
        Assert.False(obj["skaterAttributes"] is JsonObject);
    }
}
