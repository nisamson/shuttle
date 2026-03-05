using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;

namespace SHLAnalytics.ML.CV.Trainer.Services.Dedup;

public enum HashAlgorithm {
    AverageHash,
    PerceptualHash,
    DifferenceHash,
}

public static class HashAlgorithmExtensions {
    extension(IImageHashConfiguration hc) {
        IImageHash CreateHasher() => hc.HashAlgorithm switch {
            HashAlgorithm.AverageHash => new AverageHash(),
            HashAlgorithm.PerceptualHash => new PerceptualHash(),
            HashAlgorithm.DifferenceHash => new DifferenceHash(),
            _ => throw new ArgumentOutOfRangeException()
        };
        
        IDuplicateHashStorage CreateStorage() => hc.HashStorageStrategy.CreateStorage();
    }
}
