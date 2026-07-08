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
}
