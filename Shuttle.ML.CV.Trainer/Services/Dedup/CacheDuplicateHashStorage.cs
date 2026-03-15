using Microsoft.Extensions.Caching.Memory;

namespace Shuttle.ML.CV.Trainer.Services.Dedup;

public class CacheDuplicateHashStorage : IDuplicateHashStorage {

    public const int HashSize = sizeof(ulong);
    public const int FourMebibytes = 4 * 1024 * 1024;
    public const int MaxEntries = FourMebibytes / HashSize;
    
    private readonly IMemoryCache cache;
    private readonly MemoryCacheEntryOptions cacheEntryOptions;
    
    public CacheDuplicateHashStorage() {
        cache = new MemoryCache(new MemoryCacheOptions {
            SizeLimit = MaxEntries,
            ExpirationScanFrequency = TimeSpan.MaxValue
        });
        cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSize(1);
    }
    
    public Task<bool> AddHash(ulong hash, CancellationToken token = default) {
        // Try to add the hash to the cache. If it already exists, return false.
        var value = Guid.NewGuid();
        var guid = cache.GetOrCreate(hash, _ => value, cacheEntryOptions);
        var isNew = guid == value;
        return Task.FromResult(isNew);
    }
}
