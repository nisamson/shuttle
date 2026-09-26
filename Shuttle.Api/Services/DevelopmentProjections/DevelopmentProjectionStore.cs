using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shuttle.EFCore;
using Shuttle.EFCore.Entities.Performance;
using Shuttle.Models.Players;

namespace Shuttle.Api.Services.DevelopmentProjections;

/// <summary>Atomically publishes and reads versioned development-projection snapshots.</summary>
public sealed class DevelopmentProjectionStore {
    public const int RetainedPublishedRuns = 3;
    private static readonly TimeSpan CleanupCommandTimeout = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ShlDbContext db;
    private readonly TimeProvider timeProvider;

    public DevelopmentProjectionStore(ShlDbContext db, TimeProvider timeProvider) {
        this.db = db;
        this.timeProvider = timeProvider;
    }

    public async Task<bool> PublishAsync(
        IReadOnlyList<PlayerDevelopmentProjectionResult> results,
        DateTimeOffset dataAsOf,
        DateTimeOffset generatedAt,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(results);

        if (!db.Database.IsRelational()) {
            return await PublishCoreAsync(results, dataAsOf, generatedAt, cancellationToken);
        }

        var executionStrategy = db.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(
            () => PublishCoreAsync(results, dataAsOf, generatedAt, cancellationToken));
    }

    private async Task<bool> PublishCoreAsync(
        IReadOnlyList<PlayerDevelopmentProjectionResult> results,
        DateTimeOffset dataAsOf,
        DateTimeOffset generatedAt,
        CancellationToken cancellationToken) {
        db.ChangeTracker.Clear();
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var latestDataAsOf = await db.DevelopmentProjectionRuns
            .Where(run =>
                run.AlgorithmVersion == DevelopmentProjectionAlgorithm.CurrentVersion
                && run.PublishedAt != null)
            .MaxAsync(run => (DateTimeOffset?)run.DataAsOf, cancellationToken);
        if (latestDataAsOf >= dataAsOf) {
            return false;
        }

        var run = new DevelopmentProjectionRun {
            AlgorithmVersion = DevelopmentProjectionAlgorithm.CurrentVersion,
            DataAsOf = dataAsOf,
            GeneratedAt = generatedAt,
            Projections = results
                .Select(result => new PersistedDevelopmentProjection {
                    RunId = 0,
                    PlayerId = result.PlayerId,
                    Status = result.Status,
                    ProjectedPeakTpe = result.Projection?.ProjectedPoints
                        .Max(point => (double?)point.P50),
                    PayloadJson = result.Projection is null
                        ? null
                        : JsonSerializer.Serialize(result.Projection, JsonOptions),
                })
                .ToList(),
        };

        db.DevelopmentProjectionRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        run.PublishedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        await CleanupAsync(run.Id, cancellationToken);

        if (transaction is not null) {
            await transaction.CommitAsync(cancellationToken);
        }

        return true;
    }

    public async Task<PlayerDevelopmentProjectionResult?> GetLatestAsync(
        int playerId,
        CancellationToken cancellationToken) {
        var run = await db.DevelopmentProjectionRuns
            .AsNoTracking()
            .Where(candidate =>
                candidate.AlgorithmVersion == DevelopmentProjectionAlgorithm.CurrentVersion
                && candidate.PublishedAt != null)
            .OrderByDescending(candidate => candidate.PublishedAt)
            .ThenByDescending(candidate => candidate.Id)
            .Select(candidate => new {
                candidate.Id,
                candidate.DataAsOf,
                candidate.GeneratedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (run is null) {
            return null;
        }

        var persisted = await db.DevelopmentProjections
            .AsNoTracking()
            .Where(projection =>
                projection.RunId == run.Id
                && projection.PlayerId == playerId)
            .Select(projection => new {
                projection.Status,
                projection.PayloadJson,
            })
            .FirstOrDefaultAsync(cancellationToken);

        var status = persisted?.Status ?? DevelopmentProjectionStatus.NotGenerated;
        var projection = persisted?.PayloadJson is null
            ? null
            : JsonSerializer.Deserialize<PlayerDevelopmentProjection>(persisted.PayloadJson, JsonOptions)
                ?? throw new InvalidOperationException("Stored development projection JSON was invalid.");

        if (status == DevelopmentProjectionStatus.Available && projection is null) {
            throw new InvalidOperationException("An available stored development projection had no payload.");
        }

        return new PlayerDevelopmentProjectionResult {
            PlayerId = playerId,
            AlgorithmVersion = DevelopmentProjectionAlgorithm.CurrentVersion,
            Status = status,
            DataAsOf = run.DataAsOf,
            GeneratedAt = run.GeneratedAt,
            IsCurrent = false,
            Projection = projection,
        };
    }

    private async Task CleanupAsync(long currentRunId, CancellationToken cancellationToken) {
        var obsoletePublishedIds = await db.DevelopmentProjectionRuns
            .Where(run =>
                run.AlgorithmVersion == DevelopmentProjectionAlgorithm.CurrentVersion
                && run.PublishedAt != null)
            .OrderByDescending(run => run.PublishedAt)
            .ThenByDescending(run => run.Id)
            .Skip(RetainedPublishedRuns)
            .Select(run => run.Id)
            .ToListAsync(cancellationToken);

        var obsoleteRunIds = await db.DevelopmentProjectionRuns
            .Where(run =>
                obsoletePublishedIds.Contains(run.Id)
                || run.AlgorithmVersion != DevelopmentProjectionAlgorithm.CurrentVersion
                || (run.PublishedAt == null && run.Id != currentRunId))
            .Select(run => run.Id)
            .ToListAsync(cancellationToken);

        if (obsoleteRunIds.Count == 0) {
            return;
        }

        if (db.Database.IsRelational()) {
            var previousTimeout = db.Database.GetCommandTimeout();
            db.Database.SetCommandTimeout(CleanupCommandTimeout);
            try {
                await db.DevelopmentProjections
                    .Where(projection => obsoleteRunIds.Contains(projection.RunId))
                    .ExecuteDeleteAsync(cancellationToken);
                await db.DevelopmentProjectionRuns
                    .Where(run => obsoleteRunIds.Contains(run.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            } finally {
                db.Database.SetCommandTimeout(previousTimeout);
            }
        } else {
            var obsoleteRuns = await db.DevelopmentProjectionRuns
                .Where(run => obsoleteRunIds.Contains(run.Id))
                .ToListAsync(cancellationToken);
            db.DevelopmentProjectionRuns.RemoveRange(obsoleteRuns);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
