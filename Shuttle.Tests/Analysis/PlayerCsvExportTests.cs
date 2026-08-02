using System.Text;
using Shuttle.Analysis;
using Shuttle.EFCore.Entities.Portal;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.Tests.Analysis;

public class PlayerCsvExportTests {

    private static PlayerInformation CreatePlayer(
        string name = "Test Player",
        SkaterAttributes? skater = null,
        GoaltenderAttributes? goaltender = null,
        PlayerPosition position = PlayerPosition.RightDefense,
        int playerId = 1
    ) => new() {
        UserId = 42,
        PlayerId = playerId,
        Username = "tester",
        CreationTime = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc),
        Status = PlayerStatus.Active,
        Name = name,
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

    private static async Task<string> WriteCsvAsync(params PlayerInformation[] players) =>
        await WriteCsvAsync(StatNorm.None, players);

    private static async Task<string> WriteCsvAsync(StatNorm norm, params PlayerInformation[] players) {
        var records = players.Select(PlayerExportRecord.FromEntity).ToList();
        using var stream = new MemoryStream();
        await PlayerCsvExport.WriteAsync(stream, records, norm, CancellationToken.None);
        stream.Position = 0;
        // StreamReader detects and strips the UTF-8 BOM, decoding the content as UTF-8.
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private static string[] SplitLines(string csv) =>
        csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

    [Fact]
    public async Task WriteAsync_FlattensSkaterAttributesIntoDottedColumns() {
        var csv = await WriteCsvAsync(CreatePlayer(skater: SampleSkater()));

        var header = SplitLines(csv)[0];

        Assert.Contains("skaterAttributes.checking", header);
        Assert.Contains("skaterAttributes.speed", header);
        Assert.Contains("playerId", header);
    }

    [Fact]
    public async Task WriteAsync_OmitsConstantMentalAttributeColumns() {
        var csv = await WriteCsvAsync(
            CreatePlayer(skater: SampleSkater(), playerId: 1),
            CreatePlayer(goaltender: SampleGoaltender(), position: PlayerPosition.Goalie, playerId: 2));

        var header = SplitLines(csv)[0];

        Assert.DoesNotContain("skaterAttributes.determination", header);
        Assert.DoesNotContain("skaterAttributes.temperament", header);
        Assert.DoesNotContain("goaltenderAttributes.leadership", header);
        Assert.DoesNotContain("goaltenderAttributes.professionalism", header);
        // Varying attributes are retained.
        Assert.Contains("skaterAttributes.aggression", header);
        Assert.Contains("goaltenderAttributes.mentalToughness", header);
    }

    [Fact]
    public async Task WriteAsync_UnionsColumnsAcrossSkatersAndGoaltenders() {
        var csv = await WriteCsvAsync(
            CreatePlayer(skater: SampleSkater(), playerId: 1),
            CreatePlayer(goaltender: SampleGoaltender(), position: PlayerPosition.Goalie, playerId: 2));

        var lines = SplitLines(csv);
        var header = lines[0].Split(',');
        var skaterCheckingIndex = Array.IndexOf(header, "skaterAttributes.checking");
        var goalieBlockerIndex = Array.IndexOf(header, "goaltenderAttributes.blocker");

        Assert.True(skaterCheckingIndex >= 0);
        Assert.True(goalieBlockerIndex >= 0);

        var skaterRow = lines[1].Split(',');
        var goalieRow = lines[2].Split(',');

        // The skater has a checking value but no goaltender blocker; the goalie is the inverse.
        Assert.Equal("1", skaterRow[skaterCheckingIndex]);
        Assert.Equal(string.Empty, skaterRow[goalieBlockerIndex]);
        Assert.Equal(string.Empty, goalieRow[skaterCheckingIndex]);
        Assert.Equal("11", goalieRow[goalieBlockerIndex]);
    }

    [Fact]
    public async Task WriteAsync_HonorsEnumConverters() {
        var csv = await WriteCsvAsync(CreatePlayer(skater: SampleSkater()));

        var lines = SplitLines(csv);
        var header = lines[0].Split(',');
        var positionIndex = Array.IndexOf(header, "position");

        Assert.True(positionIndex >= 0);
        Assert.Equal("RD", lines[1].Split(',')[positionIndex]);
    }

    [Fact]
    public async Task WriteAsync_QuotesFieldsContainingSpecialCharacters() {
        var csv = await WriteCsvAsync(CreatePlayer(name: "Do,e \"The\" Man", skater: SampleSkater()));

        // A comma and embedded quotes must be RFC 4180 quoted/escaped.
        Assert.Contains("\"Do,e \"\"The\"\" Man\"", csv);
    }

    [Fact]
    public async Task WriteAsync_EmitsUtf8Bom() {
        var records = new[] { PlayerExportRecord.FromEntity(CreatePlayer(skater: SampleSkater())) };
        using var stream = new MemoryStream();

        await PlayerCsvExport.WriteAsync(stream, records, StatNorm.None, CancellationToken.None);

        var bytes = stream.ToArray();
        Assert.True(bytes.Length >= 3);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
    }

    [Theory]
    [InlineData("Åström Øyvind")]
    [InlineData("François Dupré")]
    [InlineData("日本語のプレーヤー")]
    [InlineData("Müller-Głowacki")]
    public async Task WriteAsync_PreservesNonAsciiGlyphs(string name) {
        var csv = await WriteCsvAsync(CreatePlayer(name: name, skater: SampleSkater()));

        // The non-ASCII name round-trips through the UTF-8 encoded output without corruption.
        Assert.Contains(name, csv);
    }

    [Fact]
    public async Task WriteAsync_QuotesAndPreservesNonAsciiWithSpecialCharacters() {
        var name = "Reykjavík, Ísland — Þór \"Goði\"";

        var csv = await WriteCsvAsync(CreatePlayer(name: name, skater: SampleSkater()));

        // Combined case: non-ASCII glyphs and RFC 4180 special characters together.
        Assert.Contains("\"Reykjavík, Ísland — Þór \"\"Goði\"\"\"", csv);
    }

    [Fact]
    public async Task WriteAsync_L1Norm_ReplacesValuesInPlaceKeepingOriginalColumns() {
        var csv = await WriteCsvAsync(StatNorm.L1, CreatePlayer(skater: SampleSkater()));

        var lines = SplitLines(csv);
        var header = lines[0].Split(',');

        // Original column names are preserved (no renamed/suffixed norm columns).
        Assert.Contains("skaterAttributes.checking", header);
        Assert.DoesNotContain(header, h => h.Contains("Normalized", StringComparison.Ordinal));

        var values = lines[1].Split(',');
        var skaterCells = header
            .Select((name, i) => (name, i))
            .Where(x => x.name.StartsWith("skaterAttributes.", StringComparison.Ordinal))
            .Select(x => double.Parse(values[x.i], System.Globalization.CultureInfo.InvariantCulture))
            .ToList();

        // The in-place L1-normalized skater components sum to 1.
        Assert.Equal(1.0, skaterCells.Sum(), 9);
    }
}
