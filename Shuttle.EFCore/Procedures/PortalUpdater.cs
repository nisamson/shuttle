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

    // Rows per TPE-timeline merge batch. The timeline ingest is bound by the serialized MERGE round
    // trips to SQL, not by the HTTP fetch, so we accumulate many players' (windowed) sources and
    // merge them together instead of one round trip per player. Bounded by row count to keep each
    // MERGE statement well under the command timeout (mirrors EarnedTpeMergeBatchSize).
    private const int TpeMergeBatchSize = 2000;

    // Number of trailing seasons (current + preceding ones) whose earned-TPE totals we re-fetch on
    // every run. Only these can still be revised; older seasons are treated as immutable and are
    // fetched once (see UpdatePlayerEarnedTpe) rather than re-merged every run.
    private const int EarnedTpeRefreshSeasons = 2;

    // When re-ingesting an already-tracked player's timeline, only the most recent points can
    // realistically change (late-applied tasks or retroactive corrections, including TPE decreases
    // from penalties). We therefore rebuild the merge source from just the entries within this
    // window back from the player's stored latest TaskDate (plus anything newer), skipping the
    // long, static history. The merge keys on TaskDate, so decreases within the window still apply.
    private static readonly TimeSpan TpeOverlapWindow = TimeSpan.FromDays(30);

    // Number of never-backfilled retired players to process per run in the rolling backfill. Bounds
    // the extra per-run fan-out while the historical catch-up works through the cold (long-retired)
    // population; once every retired player is marked, the steady-state backfill cost is zero.
    private const int BackfillBatchSize = 200;

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

        // Pre-fetch each eligible player's latest stored TaskDate in one round trip so the per-player
        // merge can be trimmed to just the recent overlap window instead of re-merging the whole
        // history. Players with no rows yet are absent here and fall through to a full merge.
        var eligibleIds = eligible.Select(p => p.PlayerId).ToHashSet();
        var lastTaskDates = await dbContext.TpeEvents
            .Where(e => eligibleIds.Contains(e.PlayerId))
            .GroupBy(e => e.PlayerId)
            .Select(g => new { PlayerId = g.Key, LastTaskDate = g.Max(e => e.TaskDate) })
            .ToDictionaryAsync(x => x.PlayerId, x => x.LastTaskDate, token);

        var totalChanged = await IngestTpeTimelines(
            eligible,
            playerId => lastTaskDates.TryGetValue(playerId, out var last) ? last - TpeOverlapWindow : null,
            onProcessed: null,
            logLabel: "TPE timelines",
            token: token);

        logger.LogInformation("Changed {Count} player TPE events", totalChanged);

        await BackfillRetiredPlayerTpeEvents(playerInfo, nowUtc, token);
    }

    // Rolling one-time catch-up for retired players who aged out of the hot path's grace window
    // before their timeline was ever ingested. Each run processes a bounded batch of retired players
    // not yet recorded in TpeTimelineBackfills, ordered by PlayerId, and marks every processed player
    // (even those with empty timelines) so the batch always advances and never re-fetches them.
    private async Task BackfillRetiredPlayerTpeEvents(IList<PlayerInfo> playerInfo, DateTime nowUtc, CancellationToken token = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();

        // Retired players not already covered by the hot path (i.e. outside the grace window).
        var coldRetired = playerInfo
            .Where(p => p.Status == PlayerStatus.Retired && !ShouldIngestTpeEvents(p, nowUtc))
            .Select(p => p.PlayerId)
            .ToHashSet();

        if (coldRetired.Count == 0) {
            return;
        }

        var alreadyBackfilled = await dbContext.TpeTimelineBackfills
            .Where(b => coldRetired.Contains(b.PlayerId))
            .Select(b => b.PlayerId)
            .ToHashSetAsync(token);

        var batch = SelectTpeBackfillBatch(playerInfo, nowUtc, alreadyBackfilled, BackfillBatchSize);

        if (batch.Count == 0) {
            logger.LogInformation("No retired players pending TPE backfill");
            return;
        }

        activity?.SetTag("backfillBatch", batch.Count);
        logger.LogInformation("Backfilling TPE timelines for {Count} retired players", batch.Count);

        // Full merge for these players (first ingest); collect every processed id so all get marked.
        var processedIds = new List<int>(batch.Count);
        var totalChanged = await IngestTpeTimelines(
            batch,
            _ => null,
            onProcessed: processedIds.Add,
            logLabel: "TPE backfill",
            token: token);

        var markedAt = DateTime.UtcNow;
        var markers = processedIds
            .Select(id => new TpeTimelineBackfill { PlayerId = id, BackfilledAt = markedAt })
            .ToList();

        var marked = await dbContext.TpeTimelineBackfills.Merge()
            .Using(markers)
            .OnTargetKey()
            .InsertWhenNotMatched()
            .MergeAsync(token);

        logger.LogInformation(
            "Backfilled {Players} retired players ({Changed} TPE events, {Marked} new markers)",
            processedIds.Count,
            totalChanged,
            marked);
    }

    // Shared pipeline: fetch each player's timeline concurrently and build its (windowed, key-unique)
    // merge source off the shared portal client, then accumulate those sources and merge them in
    // row-bounded batches on a single serialized worker. The phase is bound by the MERGE round trips
    // to SQL rather than the HTTP fetch, so batching many players per MERGE (instead of one merge per
    // player) is the dominant win; a single worker also keeps the shared DbContext access serialized.
    // mergeFrom returns the per-player overlap cutoff (null = full merge); onProcessed, if supplied,
    // is invoked on the serialized worker with each processed player id (regardless of event count).
    private async Task<int> IngestTpeTimelines(
        IReadOnlyList<PlayerInfo> players,
        Func<int, DateTime?> mergeFrom,
        Action<int>? onProcessed,
        string logLabel,
        CancellationToken token) {
        if (players.Count == 0) {
            return 0;
        }

        var totalChanged = 0;
        var processed = 0;

        var fetchBlock = new TransformBlock<PlayerInfo, (int PlayerId, IReadOnlyList<TpeEvent> Source)>(
            async player => {
                var timeline = await portalClient.GetTpeTimeline(player.PlayerId, token);
                var source = BuildTpeMergeSource(player.PlayerId, timeline, mergeFrom(player.PlayerId));
                return (player.PlayerId, source);
            },
            new ExecutionDataflowBlockOptions {
                MaxDegreeOfParallelism = TpeTimelineConcurrency,
                BoundedCapacity = TpeTimelineConcurrency * 2,
                CancellationToken = token,
            });

        var buffer = new List<TpeEvent>(TpeMergeBatchSize + 128);
        var mergeBlock = new ActionBlock<(int PlayerId, IReadOnlyList<TpeEvent> Source)>(
            async item => {
                onProcessed?.Invoke(item.PlayerId);
                buffer.AddRange(item.Source);
                if (++processed % 100 == 0) {
                    logger.LogInformation("Ingested {Label} for {Processed}/{Total} players", logLabel, processed, players.Count);
                }
                if (buffer.Count >= TpeMergeBatchSize) {
                    totalChanged += await MergeTpeEventBatch(buffer, token);
                    buffer.Clear();
                }
            },
            new ExecutionDataflowBlockOptions {
                // A single merge worker keeps DbContext access serialized.
                MaxDegreeOfParallelism = 1,
                BoundedCapacity = TpeTimelineConcurrency * 2,
                CancellationToken = token,
            });

        fetchBlock.LinkTo(mergeBlock, new DataflowLinkOptions { PropagateCompletion = true });

        foreach (var player in players) {
            await fetchBlock.SendAsync(player, token);
        }

        fetchBlock.Complete();
        await mergeBlock.Completion;

        // Flush the trailing partial batch. The merge worker has completed, so the buffer is no
        // longer being mutated and is safe to read here.
        if (buffer.Count > 0) {
            totalChanged += await MergeTpeEventBatch(buffer, token);
            buffer.Clear();
        }

        return totalChanged;
    }

    // Selects the next bounded batch of retired players still needing a TPE backfill: retired,
    // outside the hot path's grace window, and not yet marked in TpeTimelineBackfills. Ordered by
    // PlayerId so the rolling backfill advances deterministically across runs.
    internal static IReadOnlyList<PlayerInfo> SelectTpeBackfillBatch(
        IEnumerable<PlayerInfo> players,
        DateTime nowUtc,
        ISet<int> alreadyBackfilled,
        int batchSize) {
        return players
            .Where(p => p.Status == PlayerStatus.Retired
                        && !ShouldIngestTpeEvents(p, nowUtc)
                        && !alreadyBackfilled.Contains(p.PlayerId))
            .OrderBy(p => p.PlayerId)
            .Take(batchSize)
            .ToList();
    }

    // Builds the key-unique merge source for a player's timeline. Collapses duplicate TaskDates
    // (keeping the last), and, when mergeFrom is supplied, trims to just entries at/after the cutoff
    // so an already-tracked player's static history isn't re-merged. Keyed on TaskDate, so a within-
    // window TotalTpe change (including a decrease) is still surfaced to the merge.
    internal static IReadOnlyList<TpeEvent> BuildTpeMergeSource(
        int playerId,
        IEnumerable<TpeTimelineEntry> timeline,
        DateTime? mergeFrom) {
        var events = timeline
            .Select(entry => TpeEvent.FromShlApi(playerId, entry))
            .GroupBy(e => e.TaskDate)
            .Select(g => g.Last());

        if (mergeFrom is { } cutoff) {
            events = events.Where(e => e.TaskDate >= cutoff);
        }

        return events.ToList();
    }

    // Merges a batch of TPE events spanning many players in a single statement. The source is keyed
    // on the target's composite (PlayerId, TaskDate) key, so combining players is safe; each source
    // is already de-duplicated per player by BuildTpeMergeSource. Insert new points and only update
    // an existing point when its TotalTpe actually changed (covers late corrections, incl. decreases).
    private async Task<int> MergeTpeEventBatch(IReadOnlyList<TpeEvent> source, CancellationToken token) {
        if (source.Count == 0) {
            return 0;
        }

        return await dbContext.TpeEvents.Merge()
            .Using(source)
            .OnTargetKey()
            .InsertWhenNotMatched()
            .UpdateWhenMatchedAnd((t, s) => t.TotalTpe != s.TotalTpe)
            .MergeAsync(token);
    }

    private async Task UpdatePlayerEarnedTpe(CancellationToken token = default) {
        using var activity = ActivitySources.ShuttleEfCore.StartActivity();
        logger.LogInformation("Updating player earned TPE");

        var entries = await FetchEarnedTpeEntries(token);
        logger.LogInformation("Got {Count} earned TPE entries from portal", entries.Count);
        activity?.SetTag("earnedTpeCount", entries.Count);

        // Collapse on the (PlayerId, Season) key so the merge source stays key-unique.
        var earnedTpe = entries
            .Select(PlayerEarnedTpe.FromShlApi)
            .GroupBy(e => new { e.PlayerId, e.Season })
            .Select(g => g.Last())
            .ToList();

        // Merge in batches: a single merge over all rows exceeds the SQL command timeout, so we
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

    // Chooses the cheapest correct earned-TPE fetch. Completed seasons are immutable, so in steady
    // state we only pull the current + previous season(s) instead of the full ~11k-row history. We
    // fall back to a full-history fetch when either (a) there is no season data yet, or (b) the
    // season just below the refresh window is missing from the DB (indicating older seasons were
    // never ingested), so history is backfilled at least once and then kept incrementally.
    private async Task<IList<EarnedTpeEntry>> FetchEarnedTpeEntries(CancellationToken token) {
        var currentSeason = await dbContext.Seasons
            .Select(s => (int?)s.Season)
            .MaxAsync(token);

        if (currentSeason is not { } season) {
            logger.LogInformation("No season data available; fetching full earned-TPE history");
            return await portalClient.GetEarnedTpe(token: token);
        }

        if (!await IsEarnedTpeHistoryComplete(season, token)) {
            logger.LogInformation(
                "Older earned-TPE seasons missing; fetching full history to backfill");
            return await portalClient.GetEarnedTpe(token: token);
        }

        var recentSeasons = RecentEarnedTpeSeasons(season);
        logger.LogInformation(
            "Refreshing earned TPE for recent seasons {Seasons}",
            string.Join(", ", recentSeasons));

        var entries = new List<EarnedTpeEntry>();
        foreach (var recent in recentSeasons) {
            entries.AddRange(await portalClient.GetEarnedTpe(season: recent, token: token));
        }

        return entries;
    }

    // True when older earned-TPE history is already stored and can be skipped. We probe the single
    // season just below the refresh window: because every season is refreshed while inside the
    // window before it ages out, that season's presence implies all older seasons were captured on
    // earlier runs. A young league with no such season needs no backfill.
    private async Task<bool> IsEarnedTpeHistoryComplete(int currentSeason, CancellationToken token) {
        if (EarnedTpeHistoryProbeSeason(currentSeason) is not { } probe) {
            return true;
        }

        return await dbContext.PlayerEarnedTpe.AnyAsync(e => e.Season == probe, token);
    }

    // The trailing seasons whose earned-TPE totals can still change (current back through
    // EarnedTpeRefreshSeasons - 1), clamped to season >= 1, newest first.
    internal static IReadOnlyList<int> RecentEarnedTpeSeasons(int currentSeason) {
        return Enumerable.Range(0, EarnedTpeRefreshSeasons)
            .Select(offset => currentSeason - offset)
            .Where(s => s >= 1)
            .ToList();
    }

    // The season immediately below the refresh window, used to probe whether older history is
    // present. Null when the league is too young to have a season older than the window (nothing to
    // backfill).
    internal static int? EarnedTpeHistoryProbeSeason(int currentSeason) {
        var probe = currentSeason - EarnedTpeRefreshSeasons;
        return probe >= 1 ? probe : null;
    }
}
