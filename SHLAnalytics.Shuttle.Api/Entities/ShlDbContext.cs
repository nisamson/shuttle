using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SHLAnalytics.EFCore;
using SHLAnalytics.EFCore.SiteArchive;

namespace SHLAnalytics.Shuttle.Api.Entities;

public class ShlDbContext : DbContext, IShlDatabaseContext {

    private readonly ILogger<ShlDbContext> logger;
    
    public ShlDbContext(DbContextOptions<ShlDbContext> options, ILogger<ShlDbContext> logger) : base(options) {
        this.logger = logger;
    }

    public DbSet<ArchiveEntry> ArchiveEntries { get; set; }

    public async Task<int> UpsertArchiveEntries(IReadOnlyList<ArchiveEntry> entries, CancellationToken token = default) {
        using var _ = logger.BeginScope("Upserting {Count} archive entries", entries.Count);
        int handled;
        await using var tx = await Database.BeginTransactionAsync(token);

        try {
            await using var linqConn = this.CreateLinqToDBConnection(tx);

            handled = await ArchiveEntries.Merge()
                .Using(entries)
                .OnTargetKey()
                .InsertWhenNotMatched(source => new() {
                        Url = source.Url,
                        Content = source.Content,
                        ContentHash = source.ContentHash,
                    }
                )
                .UpdateWhenMatchedAnd((src, tgt) => src.ContentHash != tgt.ContentHash,
                    (src, tgt) => new() {
                        Url = tgt.Url,
                        Content = src.Content,
                        ContentHash = src.ContentHash
                    })
                .MergeAsync(token);
            await tx.CommitAsync(token);
        } catch (Exception ex) {
            logger.LogError(ex, "Error upserting archive entries");
            throw;
        }
        return handled;
    }
}
