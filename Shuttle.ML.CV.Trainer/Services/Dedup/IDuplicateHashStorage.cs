namespace Shuttle.ML.CV.Trainer.Services.Dedup;

public interface IDuplicateHashStorage {
    /// <summary>
    /// Checks if the given hash is already stored.
    /// </summary>
    /// <param name="hash">The hash to check and add.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>True if the hash was not previously known.</returns>
    Task<bool> AddHash(ulong hash, CancellationToken token = default);
}
