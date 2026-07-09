using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shuttle.EFCore;
using Shuttle.EFCore.Entities.Portal;

namespace Shuttle.Tests.EFCore;

public class TemporalHistoryTests : IDisposable {
    private readonly string dbPath = Path.Combine(Path.GetTempPath(), $"shuttle_temporal_{Guid.NewGuid():N}.db");

    private ShlDbContext CreateContext() {
        // Foreign keys are disabled so the test can exercise the temporal triggers on a single
        // IndexRecord without materialising its PlayerInformation parent.
        var options = new DbContextOptionsBuilder<ShlDbContext>()
            .UseSqlite($"Data Source={dbPath};Foreign Keys=False")
            .Options;
        return new ShlDbContext(options, NullLogger<ShlDbContext>.Instance);
    }

    [Fact]
    public void BuildTemporalDdl_GeneratesHistoryTableAndTriggers() {
        using var ctx = CreateContext();
        var statements = Helpers.BuildTemporalDdl(ctx.Model).ToList();

        Assert.Contains(statements, s => s.Contains("CREATE TABLE IF NOT EXISTS \"IndexRecordsHistory\""));
        Assert.Contains(statements, s => s.Contains("CREATE TRIGGER \"IndexRecords_temporal_update\"") && s.Contains("AFTER UPDATE"));
        Assert.Contains(statements, s => s.Contains("CREATE TRIGGER \"IndexRecords_temporal_delete\"") && s.Contains("AFTER DELETE"));
        Assert.Contains(statements, s => s.Contains("CREATE TABLE IF NOT EXISTS \"PlayerInformationHistory\""));
    }

    [Fact]
    public async Task UpdateAndDelete_WriteHistoryRows() {
        var ct = TestContext.Current.CancellationToken;

        await using (var setup = CreateContext()) {
            await setup.Database.MigrateAsync(ct);
            await setup.EnsureTemporalHistory(ct);
        }

        await using (var ctx = CreateContext()) {
            ctx.IndexRecords.Add(new IndexRecord {
                PlayerId = 1,
                UserId = 2,
                LeagueId = 0,
                IndexId = 5,
                StartSeason = 1
            });
            await ctx.SaveChangesAsync(ct);
        }

        await using (var ctx = CreateContext()) {
            var record = await ctx.IndexRecords.SingleAsync(ct);
            record.IndexId = 6;
            await ctx.SaveChangesAsync(ct);

            var afterUpdate = await HistoryCount(ctx, ct);
            Assert.Equal(1, afterUpdate);

            // The history snapshot must capture the OLD version (IndexId 5), not the new value.
            var historicIndexId = await ctx.Database
                .SqlQueryRaw<long>("SELECT IndexId AS Value FROM IndexRecordsHistory ORDER BY ValidTo LIMIT 1")
                .SingleAsync(ct);
            Assert.Equal(5, historicIndexId);
        }

        await using (var ctx = CreateContext()) {
            var record = await ctx.IndexRecords.SingleAsync(ct);
            ctx.IndexRecords.Remove(record);
            await ctx.SaveChangesAsync(ct);

            var afterDelete = await HistoryCount(ctx, ct);
            Assert.Equal(2, afterDelete);
        }
    }

    private static async Task<long> HistoryCount(ShlDbContext ctx, CancellationToken ct) {
        return await ctx.Database
            .SqlQueryRaw<long>("SELECT COUNT(*) AS Value FROM IndexRecordsHistory")
            .SingleAsync(ct);
    }

    public void Dispose() {
        foreach (var suffix in new[] { "", "-wal", "-shm" }) {
            var path = dbPath + suffix;
            if (File.Exists(path)) {
                try {
                    File.Delete(path);
                } catch (IOException) {
                    // Best-effort cleanup of the temp database file.
                }
            }
        }

        GC.SuppressFinalize(this);
    }
}
