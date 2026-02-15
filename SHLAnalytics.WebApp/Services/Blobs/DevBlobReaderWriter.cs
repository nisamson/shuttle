using Azure.Storage.Blobs;

namespace SHLAnalytics.WebApp.Services.Blobs;

public class DevBlobReaderWriter : BlobReaderWriterBase {

    public DevBlobReaderWriter(BlobClient blobClient, ILogger<BlobReaderWriterBase> logger) : base(blobClient, logger, "dev") { }
}
