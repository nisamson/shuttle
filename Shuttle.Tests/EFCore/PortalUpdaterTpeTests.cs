using Shuttle.EFCore.Procedures;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.Tests.EFCore;

/// <summary>
/// Unit tests for the pure decision logic behind the optimized TPE-timeline update in
/// <see cref="PortalUpdater"/>: the incremental merge-source builder (overlap-window trimming)
/// and the rolling backfill batch selector. The linq2db <c>Merge</c> itself needs a real SQL
/// provider, so these cover the source/selection logic that drives it rather than the merge call.
/// </summary>
public class PortalUpdaterTpeTests {
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static TpeTimelineEntry Entry(DateTime date, int totalTpe) =>
        new(Name: "p", TaskDate: date, TotalTpe: totalTpe);

    private static PlayerInfo Player(int pid, PlayerStatus status, DateTime? retired = null) =>
        new(
            UserId: pid,
            PlayerId: pid,
            Username: $"u{pid}",
            CreationDate: Now.AddYears(-2),
            Status: status,
            Name: $"p{pid}",
            Position: default,
            Handedness: default,
            TotalTpe: 0,
            AppliedTpe: 0,
            BankedTpe: 0,
            BankBalance: 0,
            Attributes: null!,
            TaskStatus: null,
            RetirementDate: retired);

    // ---- BuildTpeMergeSource ----

    [Fact]
    public void BuildTpeMergeSource_FullMerge_ReturnsAllEntries() {
        var timeline = new[] {
            Entry(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), 10),
            Entry(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc), 20),
            Entry(new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), 30),
        };

        var source = PortalUpdater.BuildTpeMergeSource(playerId: 5, timeline, mergeFrom: null);

        Assert.Equal(3, source.Count);
        Assert.All(source, e => Assert.Equal(5, e.PlayerId));
        Assert.Equal([10, 20, 30], source.Select(e => e.TotalTpe).Order());
    }

    [Fact]
    public void BuildTpeMergeSource_WithCutoff_TrimsEntriesBeforeCutoff() {
        var d1 = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var d2 = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var d3 = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var timeline = new[] { Entry(d1, 10), Entry(d2, 20), Entry(d3, 30) };

        var source = PortalUpdater.BuildTpeMergeSource(playerId: 5, timeline, mergeFrom: d2);

        // Entries at or after the cutoff only; the static older history is skipped.
        Assert.Equal([d2, d3], source.Select(e => e.TaskDate).Order());
    }

    [Fact]
    public void BuildTpeMergeSource_CutoffBoundary_IsInclusive() {
        var cutoff = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var timeline = new[] {
            Entry(cutoff.AddTicks(-1), 10),
            Entry(cutoff, 20),
        };

        var source = PortalUpdater.BuildTpeMergeSource(playerId: 1, timeline, mergeFrom: cutoff);

        Assert.Equal([cutoff], source.Select(e => e.TaskDate));
    }

    [Fact]
    public void BuildTpeMergeSource_KeepsWithinWindowDecrease() {
        // A penalty can lower cumulative TPE; the builder keys on TaskDate, not TPE value, so a
        // within-window entry with a *lower* total than an earlier point is still included.
        var d1 = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var d2 = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var timeline = new[] { Entry(d1, 200), Entry(d2, 150) };

        var source = PortalUpdater.BuildTpeMergeSource(playerId: 1, timeline, mergeFrom: d1);

        var decreased = Assert.Single(source, e => e.TaskDate == d2);
        Assert.Equal(150, decreased.TotalTpe);
    }

    [Fact]
    public void BuildTpeMergeSource_DuplicateTaskDate_KeepsLast() {
        var date = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var timeline = new[] { Entry(date, 10), Entry(date, 99) };

        var source = PortalUpdater.BuildTpeMergeSource(playerId: 1, timeline, mergeFrom: null);

        var only = Assert.Single(source);
        Assert.Equal(99, only.TotalTpe);
    }

    // ---- SelectTpeBackfillBatch ----

    [Fact]
    public void SelectTpeBackfillBatch_SelectsOnlyColdRetired() {
        var players = new[] {
            Player(1, PlayerStatus.Active),
            Player(2, PlayerStatus.Pending),
            Player(3, PlayerStatus.Denied),
            Player(4, PlayerStatus.Retired, retired: Now.AddDays(-10)),   // recently retired -> hot path
            Player(5, PlayerStatus.Retired, retired: Now.AddDays(-200)),  // long retired -> cold
            Player(6, PlayerStatus.Retired, retired: null),               // retired, no date -> cold
        };

        var batch = PortalUpdater.SelectTpeBackfillBatch(players, Now, new HashSet<int>(), batchSize: 100);

        Assert.Equal([5, 6], batch.Select(p => p.PlayerId));
    }

    [Fact]
    public void SelectTpeBackfillBatch_ExcludesAlreadyBackfilled() {
        var players = new[] {
            Player(5, PlayerStatus.Retired, retired: Now.AddDays(-200)),
            Player(6, PlayerStatus.Retired, retired: Now.AddDays(-200)),
        };

        var batch = PortalUpdater.SelectTpeBackfillBatch(
            players, Now, alreadyBackfilled: new HashSet<int> { 5 }, batchSize: 100);

        // Player 5 is marked done (even if their timeline was empty) so it is never re-selected.
        Assert.Equal([6], batch.Select(p => p.PlayerId));
    }

    [Fact]
    public void SelectTpeBackfillBatch_OrdersByPlayerIdAndRespectsBatchSize() {
        var players = new[] {
            Player(30, PlayerStatus.Retired, retired: Now.AddDays(-200)),
            Player(10, PlayerStatus.Retired, retired: Now.AddDays(-200)),
            Player(20, PlayerStatus.Retired, retired: Now.AddDays(-200)),
        };

        var batch = PortalUpdater.SelectTpeBackfillBatch(players, Now, new HashSet<int>(), batchSize: 2);

        // Deterministic PlayerId ordering guarantees forward progress across runs.
        Assert.Equal([10, 20], batch.Select(p => p.PlayerId));
    }
}
