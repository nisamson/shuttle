using System.Diagnostics;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;
using Microsoft.Extensions.Logging;

namespace Shuttle.ML.CV.Trainer.Services.Dedup;

public abstract partial class DuplicateImageIdentifier : IDuplicateImageIdentifier {

    private readonly ILogger<DuplicateImageIdentifier> logger;
    private readonly IImageHash imageHasher;
    private readonly IImageHashConfiguration imageHashConfiguration;
    private readonly IDuplicateHashStorage hashStorage;
    
    public DuplicateImageIdentifier(IImageHashConfiguration imageHashConfiguration, IImageHash hasher, ILogger<DuplicateImageIdentifier> logger, IDuplicateHashStorage hashStorage) {
        this.imageHashConfiguration = imageHashConfiguration;
        HashAlgorithm = imageHashConfiguration.HashAlgorithm;
        this.logger = logger;
        this.hashStorage = hashStorage;
        imageHasher = hasher;
    }
    
    public HashAlgorithm HashAlgorithm { get; }
    
    public async Task<bool> IsKnownImage(string name, Stream stream, CancellationToken token = default) {
        var hash = imageHasher.Hash(stream);
        var isNew = await hashStorage.AddHash(hash, token);
        if (!isNew) {
            LogDuplicateDetected(name, hash);
        }
        return !isNew;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Image {Name} with hash {Hash} is a duplicate.")]
    private partial void LogDuplicateDetected(string name, ulong hash);
}
