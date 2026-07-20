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

        var events = new List<TpeEvent>();
        var processed = 0;
        for (var i = 0; i < eligible.Count; i += TpeTimelineConcurrency) {
            var chunk = eligible.Skip(i).Take(TpeTimelineConcurrency).ToList();
            var timelines = await Task.WhenAll(chunk.Select(async player => (
                player.PlayerId,
                Timeline: await portalClient.GetTpeTimeline(player.PlayerId, token)
            )));

            foreach (var (playerId, timeline) in timelines) {
                foreach (var entry in timeline) {
                    events.Add(TpeEvent.FromShlApi(playerId, entry));
                }
            }

            processed += chunk.Count;
            logger.LogInformation("Fetched TPE timelines for {Processed}/{Total} players", processed, eligible.Count);
        }

        activity?.SetTag("tpeEventCount", events.Count);

        // The portal can, in principle, return more than one entry for the same (player, timestamp);
        // collapse to the composite key so the merge source stays unique.
        var deduped = events
            .GroupBy(e => (e.PlayerId, e.TaskDate))
            .Select(g => g.Last())
            .ToList();

        var changed = await dbContext.TpeEvents.Merge()
            .Using(deduped)
            .OnTargetKey()
            .InsertWhenNotMatched()
            .UpdateWhenMatchedAnd((t, s) => t.TotalTpe != s.TotalTpe)
            .MergeAsync(token);
        logger.LogInformation("Changed {Count} player TPE events", changed);
    }
}
