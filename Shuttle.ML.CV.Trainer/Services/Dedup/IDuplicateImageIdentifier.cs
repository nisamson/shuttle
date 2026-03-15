namespace Shuttle.ML.CV.Trainer.Services.Dedup;

public interface IDuplicateImageIdentifier {
    HashAlgorithm HashAlgorithm { get; }
    Task<bool> IsKnownImage(string name, Stream stream, CancellationToken token = default);
}
