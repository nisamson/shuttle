namespace Shuttle.ML.CV.Trainer.Services.Dedup;

public interface IImageHashConfiguration {
    HashAlgorithm HashAlgorithm { get; }
    HashStorageStrategy HashStorageStrategy { get; }
    int SimilarityThreshold { get; }
}