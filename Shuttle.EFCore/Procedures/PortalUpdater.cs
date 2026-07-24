using System.Threading.Tasks.Dataflow;
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shuttle.EFCore.Entities;
using Shuttle.EFCore.Entities.Portal;
using Shuttle.Shl.Api.Client;
using Shuttle.Shl.Api.Models.Portal.V1;
using IndexRecord = Shuttle.EFCore.Entities.Portal.IndexRecord;

namespace Shuttle.EFCore.Procedures;

public class PortalUpdater {
    private readonly IShlPortalV1Client portalClient;
    private readonly ShlDbContext dbContext;
    private readonly ILogger<IndexUpdater> logger;

    public PortalUpdater(ShlDbContext dbContext, ILogger<IndexUpdater> logger, IShlPortalV1Client portalClient) {
        this.dbContext = dbContext;
        this.logger = logger;
        this.portalClient = portalClient;
    }

    public async Task UpdatePortal(CancellationToken token = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () => {
            await using var tx = await dbContext.Database.BeginTransactionAsync(token);
            logger.LogInformation("Updating portal");
            try {
                await UpdatePlayers(token);
                dbContext.ChangeTracker.Clear();
                await dbContext.UpdatePortalCacheTables(token);
                dbContext.ChangeTracker.Clear();
            } catch (Exception ex) {
                activity?.AddException(ex);
                logger.LogError(ex, "Error updating portal");
                throw;
            }

            await tx.CommitAsync(token);
        });
    }

    private async Task UpdatePlayers(CancellationToken token = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();
        logger.LogInformation("Updating players");
        var players = await portalClient.GetPlayers(token);
        logger.LogInformation("Got {Count} players from portal", players.Count);
        activity?.SetTag("playerCount", players.Count);
        
        await UpdateUsers(players, token);
        dbContext.ChangeTracker.Clear();
        
        await UpdatePlayerInformation(players, token);
        dbContext.ChangeTracker.Clear();
        
        await UpdatePlayerIndexEntries(players, token);
        dbContext.ChangeTracker.Clear();

        await UpdatePlayerTpeEvents(players, token);
        dbContext.ChangeTracker.Clear();

        await UpdatePlayerEarnedTpe(token);
        dbContext.ChangeTracker.Clear();
    }

    private async Task UpdateUsers(IList<PlayerInfo> playerInfo, CancellationToken token = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();
        logger.LogInformation("Updating users");
        var userGroups = playerInfo.GroupBy(player => player.UserId);
        var users = userGroups
            .Select(group => group.MaxBy(player => player.CreationDate)!)
            .Select(pinfo => new ShlUser {
                    UserId = pinfo.UserId,
                    Name = pinfo.Username
                }
            );

        var updated = await dbContext.Users.Merge()
            .Using(users)
            .OnTargetKey()
            .InsertWhenNotMatched()
            .UpdateWhenMatchedAnd((t, s) => t.Name != s.Name)
            .MergeAsync(token);
        logger.LogInformation("Updated {Updated} users", updated);
    }

    private async Task UpdatePlayerInformation(IList<PlayerInfo> playerInfo, CancellationToken token = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();
        logger.LogInformation("Updating player information");
        var playerEntities = playerInfo
            .Select(PlayerInformation.FromShlApi);

        var dbEntities = await dbContext.PlayerInformation
            .ToDictionaryAsync(p => p.PlayerId, token);

        foreach (var playerEntity in playerEntities) {
            logger.LogInformation("Updating player information for {PlayerId}", playerEntity.PlayerId);
            if (dbEntities.TryGetValue(playerEntity.PlayerId, out var dbEntity)) {
                if (dbEntity.UpdateFrom(playerEntity)) {
                    dbContext.PlayerInformation.Update(dbEntity);
                }
            } else {
                await dbContext.PlayerInformation.AddAsync(playerEntity, token);
            }
        }
        await dbContext.SaveChangesAsync(token);
    }

    private async Task UpdatePlayerIndexEntries(IList<PlayerInfo> playerInfo, CancellationToken token = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();
        logger.LogInformation("Updating player index entries");
        List<IndexRecord> indexEntries = [];
        foreach (var playerEntry in playerInfo) {
            foreach (var indexEntry in playerEntry.IndexRecords ?? []) {
                indexEntries.Add(new() {
                        PlayerId = playerEntry.PlayerId,
                        UserId = playerEntry.UserId,
                        LeagueId = indexEntry.LeagueId,
                        IndexId = indexEntry.IndexId,
                        StartSeason = indexEntry.StartSeason
                    }
                );
            }
        }
        
        var changed = await dbContext.IndexRecords.Merge()
            .Using(indexEntries)
            .OnTargetKey()
            .InsertWhenNotMatched()
            .UpdateWhenMatched()
            .MergeAsync(token);
        logger.LogInformation("Changed {Count} player index entries", changed);
    }

    // Maximum number of concurrent TPE-timeline requests to the portal. The timeline endpoint is
    // per-player, so this bounds the fan-out while keeping the whole ingest from running serially.
    private const int TpeTimelineConcurrency = 8;

    // Rows per earned-TPE merge batch. The endpoint returns ~11k rows across all seasons; merging
    // them in one statement exceeds the SQL command timeout, so we merge in batches of this size.
    private const int EarnedTpeMergeBatchSize = 2000;

    // Only ingest TPE timelines for players who are still progressing: active players, plus players
    // who retired recently enough that late-arriving TPE could still land. Long-retired, pending, and
    // denied players have static (or non-existent) timelines, so skipping them avoids wasted calls.
    private static readonly TimeSpan RetiredTpeGracePeriod = TimeSpan.FromDays(90);

    private static bool ShouldIngestTpeEvents(PlayerInfo player, DateTime nowUtc) => player.Status switch {
        PlayerStatus.Active => true,
        PlayerStatus.Retired => player.RetirementDate is { } retired && retired >= nowUtc - RetiredTpeGracePeriod,
        _ => false,
    };

    private async Task UpdatePlayerTpeEvents(IList<PlayerInfo> playerInfo, CancellationToken token = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();
        var nowUtc = DateTime.UtcNow;
        var eligible = playerInfo.Where(p => ShouldIngestTpeEvents(p, nowUtc)).ToList();
        logger.LogInformation(
            "Updating player TPE events for {Eligible}/{Total} eligible players",
            eligible.Count,
            playerInfo.Count);

        var totalChanged = 0;
        var processed = 0;

        // Fetch timelines concurrently but merge them one player at a time: a single merge over every
        // player's events is large enough to time out, and the shared DbContext isn't thread-safe.
        var fetchBlock = new TransformBlock<PlayerInfo, (int PlayerId, IList<TpeTimelineEntry> Timeline)>(
            async player => (player.PlayerId, await portalClient.GetTpeTimeline(player.PlayerId, token)),
            new ExecutionDataflowBlockOptions {
                MaxDegreeOfParallelism = TpeTimelineConcurrency,
                BoundedCapacity = TpeTimelineConcurrency * 2,
                CancellationToken = token,
            });

        var mergeBlock = new ActionBlock<(int PlayerId, IList<TpeTimelineEntry> Timeline)>(
            async item => {
                totalChanged += await MergePlayerTpeEvents(item.PlayerId, item.Timeline, token);
                if (++processed % 100 == 0) {
                    logger.LogInformation("Ingested TPE timelines for {Processed}/{Total} players", processed, eligible.Count);
                }
            },
            new ExecutionDataflowBlockOptions {
                // A single merge worker keeps DbContext access serialized.
                MaxDegreeOfParallelism = 1,
                BoundedCapacity = TpeTimelineConcurrency * 2,
                CancellationToken = token,
            });

        fetchBlock.LinkTo(mergeBlock, new DataflowLinkOptions { PropagateCompletion = true });

        foreach (var player in eligible) {
            await fetchBlock.SendAsync(player, token);
        }

        fetchBlock.Complete();
        await mergeBlock.Completion;

        logger.LogInformation("Changed {Count} player TPE events", totalChanged);
    }

    private async Task<int> MergePlayerTpeEvents(int playerId, IList<TpeTimelineEntry> timeline, CancellationToken token) {
        if (timeline.Count == 0) {
            return 0;
        }

        // The portal can, in principle, return more than one entry for the same timestamp; collapse
        // on TaskDate (playerId is fixed here) so this player's merge source stays key-unique.
        var events = timeline
            .Select(entry => TpeEvent.FromShlApi(playerId, entry))
            .GroupBy(e => e.TaskDate)
            .Select(g => g.Last())
            .ToList();

        return await dbContext.TpeEvents.Merge()
            .Using(events)
            .OnTargetKey()
            .InsertWhenNotMatched()
            .UpdateWhenMatchedAnd((t, s) => t.TotalTpe != s.TotalTpe)
            .MergeAsync(token);
    }

    private async Task UpdatePlayerEarnedTpe(CancellationToken token = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();
        logger.LogInformation("Updating player earned TPE");

        // A single unfiltered call returns every player's per-season earned-TPE summary across all
        // seasons, so we ingest the full history in one pass.
        var entries = await portalClient.GetEarnedTpe(token: token);
        logger.LogInformation("Got {Count} earned TPE entries from portal", entries.Count);
        activity?.SetTag("earnedTpeCount", entries.Count);

        // Collapse on the (PlayerId, Season) key so the merge source stays key-unique.
        var earnedTpe = entries
            .Select(PlayerEarnedTpe.FromShlApi)
            .GroupBy(e => new { e.PlayerId, e.Season })
            .Select(g => g.Last())
            .ToList();

        // Merge in batches: a single merge over all ~11k rows exceeds the SQL command timeout, so we
        // chunk the source to keep each merge statement small enough to complete comfortably.
        var totalChanged = 0;
        foreach (var batch in earnedTpe.Chunk(EarnedTpeMergeBatchSize)) {
            totalChanged += await dbContext.PlayerEarnedTpe.Merge()
                .Using(batch)
                .OnTargetKey()
                .InsertWhenNotMatched()
                .UpdateWhenMatchedAnd((t, s) =>
                    t.EarnedTpe != s.EarnedTpe
                    || t.Regression != s.Regression
                    || t.ActivityCheck != s.ActivityCheck
                    || t.Training != s.Training
                    || t.TrainingCamp != s.TrainingCamp
                    || t.Coaching != s.Coaching
                    || t.Pt != s.Pt
                    || t.Fantasy != s.Fantasy
                    || t.Recruitment != s.Recruitment
                    || t.Correction != s.Correction
                    || t.Other != s.Other)
                .MergeAsync(token);
        }
        logger.LogInformation("Changed {Count} player earned TPE entries", totalChanged);
    }
}
