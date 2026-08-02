using System.Text;
using Shuttle.Analysis;
using Shuttle.Analysis.Flows;
using Shuttle.EFCore.Entities.Portal;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.Tests.Analysis;

public class CsvDataIngestorTests {

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

    private static async Task<IngestedData> IngestExportedAsync(params PlayerInformation[] players) {
        var records = players.Select(PlayerExportRecord.FromEntity).ToList();
        var path = Path.Combine(Path.GetTempPath(), $"shuttle-ingest-{Guid.NewGuid():N}.csv");
        try {
            await using (var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
                await PlayerCsvExport.WriteAsync(stream, records, StatNorm.None, CancellationToken.None);
            }

            return await CsvDataIngestor.IngestAsync(new FileInfo(path), CancellationToken.None);
        } finally {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task IngestAsync_ReadsHeaderAndRowCount() {
        var data = await IngestExportedAsync(
            CreatePlayer(skater: SampleSkater(), playerId: 1),
            CreatePlayer(goaltender: SampleGoaltender(), position: PlayerPosition.Goalie, playerId: 2));

        Assert.Equal(2, data.RowCount);
        Assert.Contains("playerId", data.Columns);
        Assert.True(data.HasColumn("skaterAttributes.checking"));
        Assert.True(data.HasColumn("goaltenderAttributes.blocker"));
    }

    [Fact]
    public async Task IngestAsync_MapsCellsByColumnNameAndHonorsEnumConverters() {
        var data = await IngestExportedAsync(CreatePlayer(skater: SampleSkater()));

        var row = data.Rows[0];
        Assert.Equal("RD", row["position"]);
        Assert.Equal("1", row["skaterAttributes.checking"]);
    }

    [Fact]
    public async Task IngestAsync_TreatsEmptyUnionCellsAsNull() {
        var data = await IngestExportedAsync(
            CreatePlayer(skater: SampleSkater(), playerId: 1),
            CreatePlayer(goaltender: SampleGoaltender(), position: PlayerPosition.Goalie, playerId: 2));

        var skaterRow = data.Rows[0];
        // A skater carries no goaltender attributes: those union columns are null, not empty strings.
        Assert.Null(skaterRow["goaltenderAttributes.blocker"]);
        Assert.Equal("1", skaterRow["skaterAttributes.checking"]);
    }

    [Fact]
    public async Task IngestAsync_PreservesNonAsciiGlyphsThroughBom() {
        var data = await IngestExportedAsync(CreatePlayer(name: "François Dupré", skater: SampleSkater()));

        Assert.Equal("François Dupré", data.Rows[0]["name"]);
    }

    [Fact]
    public async Task IngestAsync_ThrowsWhenFileMissing() {
        var missing = new FileInfo(Path.Combine(Path.GetTempPath(), $"shuttle-missing-{Guid.NewGuid():N}.csv"));

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => CsvDataIngestor.IngestAsync(missing, CancellationToken.None));
    }

    [Fact]
    public async Task IngestAsync_ThrowsWhenFileEmpty() {
        var path = Path.Combine(Path.GetTempPath(), $"shuttle-empty-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(path, string.Empty, Encoding.UTF8, TestContext.Current.CancellationToken);
        try {
            await Assert.ThrowsAsync<InvalidDataException>(
                () => CsvDataIngestor.IngestAsync(new FileInfo(path), CancellationToken.None));
        } finally {
            File.Delete(path);
        }
    }
}
