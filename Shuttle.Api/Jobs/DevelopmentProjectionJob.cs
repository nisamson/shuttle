using System.Diagnostics;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Shuttle.Api.Services;
using Shuttle.Api.Services.DevelopmentProjections;
using Shuttle.EFCore;
using Shuttle.Models.Players;

namespace Shuttle.Api.Jobs;

/// <summary>Builds and atomically publishes development-curve projections after a database update.</summary>
public sealed class DevelopmentProjectionJob : ISelfRegisteringJob {
    public static readonly JobKey JobKey = JobKey.Create(nameof(DevelopmentProjectionJob), "data");
    public const string DataAsOfKey = "DataAsOfUtc";

    private readonly ShlDbContext db;
    private readonly DevelopmentProjectionCalculator calculator;
    private readonly DevelopmentProjectionStore store;
    private readonly IDatabaseFreshnessProvider freshness;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<DevelopmentProjectionJob> logger;

    public DevelopmentProjectionJob(
        ShlDbContext db,
        DevelopmentProjectionCalculator calculator,
        DevelopmentProjectionStore store,
        IDatabaseFreshnessProvider freshness,
        TimeProvider timeProvider,
        ILogger<DevelopmentProjectionJob> logger) {
        this.db = db;
        this.calculator = calculator;
        this.store = store;
        this.freshness = freshness;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task Execute(IJobExecutionContext context) {
        using var activity = ActivitySources.ShuttleApi.StartActivity(
            "ExecuteDevelopmentProjection",
            ActivityKind.Server);
        var cancellationToken = context.CancellationToken;
        var dataAsOf = ParseDataAsOf(context.MergedJobDataMap.GetOptionalString(DataAsOfKey))
            ?? await freshness.GetLastUpdatedAsync(cancellationToken);
        if (dataAsOf is null) {
            logger.LogWarning("Skipping development projections because no successful database update exists");
            return;
        }

        var generatedAt = timeProvider.GetUtcNow();
        logger.LogInformation(
            "Starting development projection generation for data as of {DataAsOf}",
            dataAsOf);

        var players = await db.PlayerInformation
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Select(player => new {
                player.PlayerId,
                player.Name,
                player.Username,
            })
            .ToListAsync(cancellationToken);
        var observations = await db.TpeEvents
            .AsNoTracking()
            .OrderBy(entry => entry.PlayerId)
            .ThenBy(entry => entry.TaskDate)
            .Select(entry => new {
                entry.PlayerId,
                entry.TaskDate,
                entry.TotalTpe,
            })
            .ToListAsync(cancellationToken);
        var observationsByPlayer = observations
            .GroupBy(entry => entry.PlayerId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<DevelopmentCurveObservation>)group
                    .Select(entry => new DevelopmentCurveObservation(
                        AsUtc(entry.TaskDate),
                        entry.TotalTpe))
                    .ToArray());

        var curves = players
            .Select(player => new DevelopmentPlayerCurve(
                player.PlayerId,
                player.Name,
                player.Username,
                observationsByPlayer.GetValueOrDefault(player.PlayerId) ?? []))
            .ToArray();
        var results = calculator.Calculate(curves, dataAsOf.Value, generatedAt);
        var published = await store.PublishAsync(
            results,
            dataAsOf.Value,
            generatedAt,
            cancellationToken);

        if (published) {
            logger.LogInformation(
                "Published development projection statuses for {PlayerCount} players, including {ProjectionCount} available projections",
                results.Count,
                results.Count(result => result.Status == DevelopmentProjectionStatus.Available));
        } else {
            logger.LogInformation(
                "Skipped superseded development projection run for {DataAsOf}",
                dataAsOf);
        }
    }

    public static IServiceCollectionQuartzConfigurator RegisterJob(
        IServiceCollectionQuartzConfigurator configurator) {
        configurator.AddJob<DevelopmentProjectionJob>(job => {
            job.WithIdentity(JobKey)
                .WithDescription("Builds peer-based player development projections")
                .StoreDurably()
                .DisallowConcurrentExecution();
        });
        return configurator;
    }

    private static DateTimeOffset? ParseDataAsOf(string? value) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var parsed)
            ? parsed
            : null;

    private static DateTimeOffset AsUtc(DateTime value) =>
        new(value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime());
}
