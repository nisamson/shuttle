namespace SHLAnalytics.ML.CV.Trainer.Services.Dedup;

public enum HashStorageStrategy {
    InMemoryCache
}

public static class HashStorageStrategyExtensions {
    public static IDuplicateHashStorage CreateStorage(this HashStorageStrategy strategy) => strategy switch {
        HashStorageStrategy.InMemoryCache => new CacheDuplicateHashStorage(),
        _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null)
    };
}
