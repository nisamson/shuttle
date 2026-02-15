using Azure.Storage.Blobs;

namespace SHLAnalytics.WebApp.Services.Blobs;

public class BlobReaderWriter : BlobReaderWriterBase {
    public BlobReaderWriter(BlobClient blobClient, ILogger<BlobReaderWriterBase> logger) : base(blobClient, logger, string.Empty) { }
}
