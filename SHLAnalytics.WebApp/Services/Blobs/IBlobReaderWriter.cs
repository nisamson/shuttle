namespace SHLAnalytics.WebApp.Services.Blobs;

public interface IBlobReaderWriter {
    Task WriteBlob(string blobName, Stream data, CancellationToken cancellationToken = default);
    Task<Stream> ReadBlob(string blobName, CancellationToken cancellationToken = default);
}
