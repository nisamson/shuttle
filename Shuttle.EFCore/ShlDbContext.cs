using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Shuttle.EFCore.Entities.Portal;

namespace Shuttle.EFCore;

public class ShlDbContext : DbContext {

    private readonly ILogger<ShlDbContext> logger;
    
    public ShlDbContext(DbContextOptions<ShlDbContext> options, ILogger<ShlDbContext> logger) : base(options) {
        this.logger = logger;
    }

    // public DbSet<ArchiveEntry> ArchiveEntries { get; set; }
    //
    // public async Task<int> UpsertArchiveEntries(IReadOnlyList<ArchiveEntry> entries, CancellationToken token = default) {
    //     using var _ = logger.BeginScope("Upserting {Count} archive entries", entries.Count);
    //     int handled;
    //     await using var tx = await Database.BeginTransactionAsync(token);
    //
    //     try {
    //         await using var linqConn = this.CreateLinqToDBConnection(tx);
    //
    //         handled = await ArchiveEntries.Merge()
    //             .Using(entries)
    //             .OnTargetKey()
    //             .InsertWhenNotMatched(source => new() {
    //                     Url = source.Url,
    //                     Content = source.Content,
    //                     ContentHash = source.ContentHash,
    //                 }
    //             )
    //             .UpdateWhenMatchedAnd((src, tgt) => src.ContentHash != tgt.ContentHash,
    //                 (src, tgt) => new() {
    //                     Url = tgt.Url,
    //                     Content = src.Content,
    //                     ContentHash = src.ContentHash
    //                 })
    //             .MergeAsync(token);
    //         await tx.CommitAsync(token);
    //     } catch (Exception ex) {
    //         logger.LogError(ex, "Error upserting archive entries");
    //         throw;
    //     }
    //     return handled;
    // }

    // public DbSet<PlayerInfo> PlayerInfos { get; set; }
    //
    // public async Task UpsertPlayerInfos(IReadOnlyList<PlayerInfo> infos, IDbContextTransaction? tx = null, CancellationToken token = default) {
    //
    //     var origTx = tx;
    //     if (tx is null) {
    //         tx = Database.CurrentTransaction ?? await Database.BeginTransactionAsync(token);
    //     }
    //
    //     try {
    //         
    //         await using var linqConn = this.CreateLinqToDBConnection(tx);
    //         var edited = await PlayerInfos.Merge()
    //             .Using(infos)
    //             .On((src, tgt) => src.PlayerId == tgt.PlayerId)
    //             .InsertWhenNotMatched()
    //             .UpdateWhenMatchedAnd(PlayerInfo.ShouldUpdateExpression)
    //             .MergeAsync(token);
    //         
    //         if (origTx is null) {
    //             await tx.CommitAsync(token);
    //         }
    //     } catch (Exception ex) {
    //         logger.LogError(ex, "Error upserting player infos");
    //         throw;
    //     } finally {
    //         await tx.DisposeAsync();
    //     }
    // }
}
