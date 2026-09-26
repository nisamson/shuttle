using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Quartz;
using Shuttle.Api.Controllers;
using Shuttle.Api.Jobs;
using Shuttle.Api.Services;
using Shuttle.Api.Services.DevelopmentProjections;
using Shuttle.EFCore;
using Shuttle.EFCore.Entities.Portal;
using Shuttle.Models.Players;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.Tests.Api;

public sealed class DevelopmentProjectionTests {
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void Resampler_uses_fixed_cadence_and_last_observation_at_each_point() {
        var start = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var observations = new[] {
            new DevelopmentCurveObservation(start, 100),
            new DevelopmentCurveObservation(start.AddDays(3), 115),
            new DevelopmentCurveObservation(start.AddDays(10), 130),
        };

        var result = DevelopmentCurveResampler.Resample(
            observations,
            start.AddDays(15),
            TimeSpan.FromDays(7));

        Assert.Equal(4, result.Count);
        Assert.Equal([100, 115, 130, 130], result.Select(point => point.TotalTpe));
        Assert.Equal(
            [start, start.AddDays(7), start.AddDays(14), start.AddDays(15)],
            result.Select(point => point.Date));
    }

    [Fact]
    public void Calculator_uses_the_latest_observation_as_the_forecast_baseline() {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var dataAsOf = start.AddYears(1);
        var players = new List<DevelopmentPlayerCurve> {
            new(
                1,
                "Target",
                "target",
                Enumerable.Range(0, 10)
                    .Select(index => new DevelopmentCurveObservation(
                        start.AddDays(-63 + (index * 7)),
                        100 + (index * 10)))
                    .Append(new DevelopmentCurveObservation(start.AddDays(3), 200))
                    .ToArray()),
        };

        for (var peer = 2; peer <= 6; peer++) {
            players.Add(Curve(
                peer,
                $"Peer {peer}",
                start.AddDays(-70),
                Enumerable.Range(0, 14).Select(index => 400 + (index * 10))));
        }

        var result = new DevelopmentProjectionCalculator().Calculate(
            players,
            dataAsOf,
            dataAsOf.AddMinutes(1));
        var projection = Assert.IsType<PlayerDevelopmentProjection>(
            Assert.Single(result, item => item.PlayerId == 1).Projection);

        Assert.Equal(200, projection.BaselineTotalTpe);
        Assert.Equal(start.AddDays(3), projection.BaselineDate);
        Assert.Equal(start.AddDays(10), projection.ProjectedPoints[0].Date);
        Assert.Equal(projection.BaselineDate, projection.ObservedPoints[^1].Date);
        Assert.Equal(projection.BaselineTotalTpe, projection.ObservedPoints[^1].TotalTpe);
        Assert.True(projection.ProjectedPoints[0].P50 > 200);
    }

    [Fact]
    public void Calculator_accepts_ten_events_spanning_six_full_weeks() {
        var start = new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);
        var observations = Enumerable.Range(0, 10)
            .Select(index => new DevelopmentCurveObservation(
                start.AddDays(index * (44d / 9)),
                100 + index))
            .ToArray();

        var result = new DevelopmentProjectionCalculator().Calculate(
            [new DevelopmentPlayerCurve(2875, "Target", "target", observations)],
            start.AddDays(45),
            start.AddDays(45).AddMinutes(1));

        var projection = Assert.Single(result);
        Assert.Equal(DevelopmentProjectionStatus.Available, projection.Status);
        Assert.Equal(8, projection.Projection!.ObservedPoints.Count);
        Assert.Equal(observations[^1].Date, projection.Projection.ObservedPoints[^1].Date);
    }

    [Fact]
    public void Dtw_is_exact_constrained_and_path_length_normalized() {
        var score = DevelopmentCurveSimilarity.GetPathLengthNormalizedDtw(
            [0, 1, 2],
            [0, 2, 4],
            bandWidth: 1);
        var identical = DevelopmentCurveSimilarity.Compare(
            [100, 120, 150],
            [100, 120, 150],
            bandWidth: 1);
        var incompatibleEndpoint = DevelopmentCurveSimilarity.Compare(
            [100, 120, 150],
            [900, 920, 950],
            bandWidth: 1);

        Assert.Equal(1, score, precision: 10);
        Assert.Equal(0, identical.Distance, precision: 10);
        Assert.Equal(1, identical.Score, precision: 10);
        Assert.True(incompatibleEndpoint.Distance > 0);
        Assert.True(incompatibleEndpoint.Score < identical.Score);
    }

    [Fact]
    public void Calculator_builds_empirical_peer_quantiles() {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var targetStart = start.AddDays(14);
        var dataAsOf = targetStart.AddDays(63);
        var players = new List<DevelopmentPlayerCurve> {
            Curve(1, "Target", targetStart, Enumerable.Range(0, 10).Select(i => 100 + (10 * i))),
        };

        for (var peer = 1; peer <= 5; peer++) {
            var values = Enumerable.Range(0, 10)
                .Select(i => 500 + (10 * i))
                .Concat([590 + peer, 590 + (2 * peer)]);
            players.Add(Curve(peer + 1, $"Peer {peer}", start, values));
        }

        var result = new DevelopmentProjectionCalculator().Calculate(
            players,
            dataAsOf,
            dataAsOf.AddMinutes(1));
        var target = Assert.Single(result, item => item.PlayerId == 1);
        Assert.Equal(DevelopmentProjectionStatus.Available, target.Status);
        var projection = Assert.IsType<PlayerDevelopmentProjection>(target.Projection);
        var first = Assert.IsType<DevelopmentProjectionPoint>(projection.ProjectedPoints[0]);

        Assert.Equal(5, first.PeerCount);
        Assert.Equal(191.4, first.P10, precision: 10);
        Assert.Equal(193, first.P50, precision: 10);
        Assert.Equal(194.6, first.P90, precision: 10);
        Assert.Equal(5, projection.SimilarPlayers.Count);
    }

    [Fact]
    public void Calculator_forecasts_through_all_retained_peer_history() {
        var peerStart = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var targetStart = peerStart.AddDays(35 * 7);
        var dataAsOf = targetStart.AddDays(9 * 7);
        var players = new List<DevelopmentPlayerCurve> {
            Curve(1, "Target", targetStart, Enumerable.Range(0, 10).Select(i => 100 + (10 * i))),
        };

        for (var peer = 1; peer <= 5; peer++) {
            players.Add(Curve(
                peer + 1,
                $"Peer {peer}",
                peerStart,
                Enumerable.Range(0, 45).Select(i => 500 + (10 * i))));
        }

        var result = new DevelopmentProjectionCalculator().Calculate(
            players,
            dataAsOf,
            dataAsOf.AddMinutes(1));
        var target = Assert.Single(result, item => item.PlayerId == 1);
        var projection = Assert.IsType<PlayerDevelopmentProjection>(target.Projection);

        Assert.Equal(35, projection.ProjectedPoints.Count);
        Assert.All(projection.ProjectedPoints, point => Assert.Equal(5, point.PeerCount));
    }

    [Fact]
    public void Calculator_records_insufficient_observed_history() {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var result = new DevelopmentProjectionCalculator().Calculate(
            [Curve(1, "New Player", start, [100, 110, 120])],
            start.AddDays(14),
            start.AddDays(14).AddMinutes(1));

        var status = Assert.Single(result);
        Assert.Equal(DevelopmentProjectionStatus.InsufficientObservedData, status.Status);
        Assert.Null(status.Projection);
    }

    [Fact]
    public void Calculator_requires_ten_earned_tpe_data_points() {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var result = new DevelopmentProjectionCalculator().Calculate(
            [Curve(1, "Nine Points", start, Enumerable.Range(0, 9).Select(i => 100 + (10 * i)))],
            start.AddDays(56),
            start.AddDays(56).AddMinutes(1));

        var status = Assert.Single(result);
        Assert.Equal(DevelopmentProjectionStatus.InsufficientObservedData, status.Status);
        Assert.Null(status.Projection);
    }

    [Fact]
    public async Task Store_publishes_latest_atomically_and_retains_three_versions() {
        await using var db = CreateContext();
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));
        var store = new DevelopmentProjectionStore(db, clock);

        for (var version = 1; version <= 4; version++) {
            var dataAsOf = clock.GetUtcNow().AddDays(version);
            Assert.True(await store.PublishAsync(
                [ProjectionResult(42, dataAsOf, version)],
                dataAsOf,
                dataAsOf.AddMinutes(1),
                Ct));
            clock.Advance(TimeSpan.FromMinutes(1));
        }

        Assert.Equal(3, await db.DevelopmentProjectionRuns.CountAsync(Ct));
        var latest = await store.GetLatestAsync(42, Ct);
        Assert.NotNull(latest);
        Assert.Equal(DevelopmentProjectionStatus.Available, latest.Status);
        Assert.Equal(4, latest.Projection!.ObservedPoints[0].TotalTpe);

        var superseded = clock.GetUtcNow().AddDays(-10);
        Assert.False(await store.PublishAsync(
            [ProjectionResult(42, superseded, 99)],
            superseded,
            clock.GetUtcNow(),
            Ct));
        Assert.Equal(3, await db.DevelopmentProjectionRuns.CountAsync(Ct));
    }

    [Fact]
    public async Task Store_persists_projected_peak_for_search() {
        await using var db = CreateContext();
        var dataAsOf = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var store = new DevelopmentProjectionStore(db, new TestTimeProvider(dataAsOf.AddMinutes(2)));

        Assert.True(await store.PublishAsync(
            [ProjectionResult(42, dataAsOf, 1200, projectedPeakTpe: 1650)],
            dataAsOf,
            dataAsOf.AddMinutes(1),
            Ct));

        var persisted = await db.DevelopmentProjections.SingleAsync(Ct);
        Assert.Equal(1650, persisted.ProjectedPeakTpe);
    }

    [Fact]
    public async Task Store_ignores_older_algorithms_and_allows_same_data_for_current_algorithm() {
        await using var db = CreateContext();
        var dataAsOf = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        db.DevelopmentProjectionRuns.Add(new() {
            AlgorithmVersion = DevelopmentProjectionAlgorithm.CurrentVersion - 1,
            DataAsOf = dataAsOf,
            GeneratedAt = dataAsOf.AddMinutes(1),
            PublishedAt = dataAsOf.AddMinutes(2),
        });
        await db.SaveChangesAsync(Ct);
        var store = new DevelopmentProjectionStore(
            db,
            new TestTimeProvider(dataAsOf.AddMinutes(3)));

        Assert.Null(await store.GetLatestAsync(42, Ct));
        Assert.True(await store.PublishAsync(
            [ProjectionResult(42, dataAsOf, 10)],
            dataAsOf,
            dataAsOf.AddMinutes(3),
            Ct));
        Assert.False(await store.PublishAsync(
            [ProjectionResult(42, dataAsOf, 20)],
            dataAsOf,
            dataAsOf.AddMinutes(4),
            Ct));

        var latest = await store.GetLatestAsync(42, Ct);
        Assert.NotNull(latest);
        Assert.Equal(DevelopmentProjectionAlgorithm.CurrentVersion, latest.AlgorithmVersion);
        Assert.Equal(10, latest.Projection!.ObservedPoints[0].TotalTpe);
        Assert.DoesNotContain(
            await db.DevelopmentProjectionRuns.ToListAsync(Ct),
            run => run.AlgorithmVersion != DevelopmentProjectionAlgorithm.CurrentVersion);
    }

    [Fact]
    public async Task Endpoint_distinguishes_missing_not_generated_and_insufficient_players() {
        await using var db = CreateContext();
        db.PlayerInformation.AddRange(Player(42), Player(43));
        await db.SaveChangesAsync(Ct);
        var dataAsOf = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var clock = new TestTimeProvider(dataAsOf.AddMinutes(1));
        var store = new DevelopmentProjectionStore(db, clock);
        var freshness = new StubFreshnessProvider(dataAsOf);

        var beforeRun = await Controller(db, freshness, store)
            .GetPlayerDevelopmentProjection(42, Ct);
        var beforeBody = Assert.IsType<PlayerDevelopmentProjectionResult>(
            Assert.IsType<OkObjectResult>(beforeRun.Result).Value);
        Assert.Equal(DevelopmentProjectionStatus.NotGenerated, beforeBody.Status);

        Assert.True(await store.PublishAsync(
            [InsufficientResult(42, dataAsOf), ProjectionResult(43, dataAsOf, 10)],
            dataAsOf,
            dataAsOf.AddMinutes(1),
            Ct));

        var insufficient = await Controller(db, freshness, store)
            .GetPlayerDevelopmentProjection(42, Ct);
        var insufficientBody = Assert.IsType<PlayerDevelopmentProjectionResult>(
            Assert.IsType<OkObjectResult>(insufficient.Result).Value);
        Assert.Equal(DevelopmentProjectionStatus.InsufficientObservedData, insufficientBody.Status);
        Assert.Null(insufficientBody.Projection);

        var missing = await Controller(db, freshness, store)
            .GetPlayerDevelopmentProjection(999, Ct);
        Assert.IsType<NotFoundResult>(missing.Result);
    }

    [Fact]
    public async Task Endpoint_etag_changes_for_stale_freshness_then_new_publication() {
        await using var db = CreateContext();
        db.PlayerInformation.Add(Player(42));
        await db.SaveChangesAsync(Ct);
        var dataAsOf = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var generatedAt = dataAsOf.AddMinutes(5);
        var clock = new TestTimeProvider(generatedAt.AddMinutes(1));
        var store = new DevelopmentProjectionStore(db, clock);
        Assert.True(await store.PublishAsync(
            [ProjectionResult(42, dataAsOf, 10, generatedAt)],
            dataAsOf,
            generatedAt,
            Ct));
        var freshness = new StubFreshnessProvider(dataAsOf);

        var currentController = Controller(db, freshness, store);
        var currentAction = await currentController.GetPlayerDevelopmentProjection(42, Ct);
        var currentBody = Assert.IsType<PlayerDevelopmentProjectionResult>(
            Assert.IsType<OkObjectResult>(currentAction.Result).Value);
        var currentEtag = currentController.Response.GetTypedHeaders().ETag;
        Assert.True(currentBody.IsCurrent);
        Assert.Equal(generatedAt, currentController.Response.GetTypedHeaders().LastModified);

        freshness.LastUpdated = dataAsOf.AddHours(6);
        var staleController = Controller(db, freshness, store);
        var staleAction = await staleController.GetPlayerDevelopmentProjection(42, Ct);
        var staleBody = Assert.IsType<PlayerDevelopmentProjectionResult>(
            Assert.IsType<OkObjectResult>(staleAction.Result).Value);
        var staleEtag = staleController.Response.GetTypedHeaders().ETag;
        Assert.False(staleBody.IsCurrent);
        Assert.NotEqual(currentEtag, staleEtag);
        Assert.Equal(freshness.LastUpdated, staleController.Response.GetTypedHeaders().LastModified);

        // A run for an intermediate refresh can publish after a newer DB refresh has already made
        // it stale. Its body must still receive a new ETag rather than reusing the prior stale one.
        var intermediateDataAsOf = dataAsOf.AddHours(3);
        var intermediateGeneratedAt = intermediateDataAsOf.AddMinutes(5);
        Assert.True(await store.PublishAsync(
            [ProjectionResult(42, intermediateDataAsOf, 15, intermediateGeneratedAt)],
            intermediateDataAsOf,
            intermediateGeneratedAt,
            Ct));

        var intermediateController = Controller(db, freshness, store);
        var intermediateAction = await intermediateController.GetPlayerDevelopmentProjection(42, Ct);
        var intermediateBody = Assert.IsType<PlayerDevelopmentProjectionResult>(
            Assert.IsType<OkObjectResult>(intermediateAction.Result).Value);
        var intermediateEtag = intermediateController.Response.GetTypedHeaders().ETag;
        Assert.False(intermediateBody.IsCurrent);
        Assert.NotEqual(staleEtag, intermediateEtag);

        var replacementGeneratedAt = freshness.LastUpdated.Value.AddMinutes(5);
        clock.Advance(TimeSpan.FromHours(6));
        Assert.True(await store.PublishAsync(
            [ProjectionResult(42, freshness.LastUpdated.Value, 20, replacementGeneratedAt)],
            freshness.LastUpdated.Value,
            replacementGeneratedAt,
            Ct));

        var replacedController = Controller(db, freshness, store);
        var replacedAction = await replacedController.GetPlayerDevelopmentProjection(42, Ct);
        var replacedBody = Assert.IsType<PlayerDevelopmentProjectionResult>(
            Assert.IsType<OkObjectResult>(replacedAction.Result).Value);
        var replacedEtag = replacedController.Response.GetTypedHeaders().ETag;
        Assert.True(replacedBody.IsCurrent);
        Assert.NotEqual(intermediateEtag, replacedEtag);
        Assert.Equal(replacementGeneratedAt, replacedController.Response.GetTypedHeaders().LastModified);
    }

    [Fact]
    public async Task Projection_job_is_registered_as_durable_and_nonconcurrent() {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddQuartz(configurator =>
            DevelopmentProjectionJob.RegisterJob(configurator));
        await using var provider = services.BuildServiceProvider();
        var scheduler = await provider.GetRequiredService<ISchedulerFactory>().GetScheduler(Ct);
        await scheduler.Start(Ct);

        try {
            var job = await scheduler.GetJobDetail(DevelopmentProjectionJob.JobKey, Ct);

            Assert.NotNull(job);
            Assert.True(job.Durable);
            Assert.True(job.ConcurrentExecutionDisallowed);
        } finally {
            await scheduler.Shutdown(waitForJobsToComplete: true, Ct);
        }
    }

    [Fact]
    public void Optional_job_data_string_returns_null_when_key_is_absent() {
        var data = new JobDataMap();

        Assert.Null(data.GetOptionalString(DevelopmentProjectionJob.DataAsOfKey));
    }

    [Fact]
    public void Optional_job_data_string_returns_stored_value() {
        var data = new JobDataMap {
            [DevelopmentProjectionJob.DataAsOfKey] = "2026-09-24T00:00:00.0000000+00:00",
        };

        Assert.Equal(
            "2026-09-24T00:00:00.0000000+00:00",
            data.GetOptionalString(DevelopmentProjectionJob.DataAsOfKey));
    }

    private static DevelopmentPlayerCurve Curve(
        int id,
        string name,
        DateTimeOffset start,
        IEnumerable<int> values) =>
        new(
            id,
            name,
            $"user{id}",
            values.Select((value, index) =>
                    new DevelopmentCurveObservation(start.AddDays(index * 7), value))
                .ToArray());

    private static PlayerDevelopmentProjectionResult ProjectionResult(
        int playerId,
        DateTimeOffset dataAsOf,
        int totalTpe,
        DateTimeOffset? generatedAt = null,
        double? projectedPeakTpe = null) {
        var generated = generatedAt ?? dataAsOf.AddMinutes(1);
        return new() {
            PlayerId = playerId,
            AlgorithmVersion = DevelopmentProjectionAlgorithm.CurrentVersion,
            Status = DevelopmentProjectionStatus.Available,
            DataAsOf = dataAsOf,
            GeneratedAt = generated,
            IsCurrent = false,
            Projection = new PlayerDevelopmentProjection {
                PlayerId = playerId,
                DataAsOf = dataAsOf,
                GeneratedAt = generated,
                IsCurrent = false,
                CadenceDays = 7,
                BaselineTotalTpe = totalTpe,
                BaselineDate = dataAsOf,
                ObservedPoints = [
                    new DevelopmentCurvePoint {
                        Date = dataAsOf,
                        TotalTpe = totalTpe,
                    },
                ],
                ProjectedPoints = projectedPeakTpe is null
                    ? []
                    : [
                        new DevelopmentProjectionPoint {
                            Date = dataAsOf.AddDays(7),
                            P10 = projectedPeakTpe.Value - 100,
                            P50 = projectedPeakTpe.Value,
                            P90 = projectedPeakTpe.Value + 100,
                            PeerCount = 10,
                        },
                    ],
                SimilarPlayers = [],
            },
        };
    }

    private static PlayerDevelopmentProjectionResult InsufficientResult(
        int playerId,
        DateTimeOffset dataAsOf) =>
        new() {
            PlayerId = playerId,
            AlgorithmVersion = DevelopmentProjectionAlgorithm.CurrentVersion,
            Status = DevelopmentProjectionStatus.InsufficientObservedData,
            DataAsOf = dataAsOf,
            GeneratedAt = dataAsOf.AddMinutes(1),
            IsCurrent = false,
        };

    private static PlayerController Controller(
        ShlDbContext db,
        IDatabaseFreshnessProvider freshness,
        DevelopmentProjectionStore store) =>
        new(
            db,
            freshness,
            store,
            NullLogger<PlayerController>.Instance) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };

    private static PlayerInformation Player(int playerId) => new() {
        UserId = playerId,
        PlayerId = playerId,
        Username = $"user{playerId}",
        CreationTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Status = PlayerStatus.Active,
        Name = $"Player {playerId}",
        Position = default,
        Handedness = default,
        TotalTpe = 0,
        AppliedTpe = 0,
        BankedTpe = 0,
        BankBalance = 0,
    };

    private static ShlDbContext CreateContext() {
        var options = new DbContextOptionsBuilder<ShlDbContext>()
            .UseInMemoryDatabase($"development-projections-{Guid.NewGuid()}")
            .Options;
        return new ShlDbContext(options, NullLogger<ShlDbContext>.Instance);
    }

    private sealed class StubFreshnessProvider(DateTimeOffset? lastUpdated)
        : IDatabaseFreshnessProvider {
        public DateTimeOffset? LastUpdated { get; set; } = lastUpdated;

        public Task<DateTimeOffset?> GetLastUpdatedAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(LastUpdated);
    }

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider {
        private DateTimeOffset now = now;

        public override DateTimeOffset GetUtcNow() => now;

        public void Advance(TimeSpan value) => now += value;
    }
}
