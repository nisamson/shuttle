using System.Text;
using Azure.Storage.Blobs;

namespace SHLAnalytics.WebApp.Services.Blobs;

public abstract class BlobReaderWriterBase : IBlobReaderWriter {

    private readonly BlobClient blobClient;
    protected readonly string BlobPrefix;
    
    public BlobReaderWriterBase(BlobClient blobClient, ILogger<BlobReaderWriterBase> logger, string blobPrefix) {
        Logger = logger;
        this.blobClient = blobClient;
        BlobPrefix = blobPrefix;
        Logger.LogInformation("BlobReaderWriter created with prefix {BlobPrefix}", blobPrefix);
    }

    protected ILogger<BlobReaderWriterBase> Logger { get; }

    private string GetBlobName(string blobName) {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(BlobPrefix)) {
            sb.Append(BlobPrefix);
            if (!BlobPrefix.EndsWith('/')) {
                sb.Append('/');
            }
            sb.Append(blobName);
            return sb.ToString();
        }
        
        return blobName;
    }

    public async Task WriteBlob(string blobName, Stream data, CancellationToken cancellationToken = default) {
        var fullBlobName = GetBlobName(blobName);
        Logger.LogInformation("Writing blob with name {BlobName}", fullBlobName);
        await using var destStream = await blobClient.OpenWriteAsync(true, cancellationToken: cancellationToken);
        await data.CopyToAsync(destStream, cancellationToken);
        Logger.LogInformation("Finished writing blob with name {BlobName}", fullBlobName);
    }
    
    public Task<Stream> ReadBlob(string blobName, CancellationToken cancellationToken = default) {
        var fullBlobName = GetBlobName(blobName);
        Logger.LogInformation("Reading blob with name {BlobName}", fullBlobName);
        return blobClient.OpenReadAsync(cancellationToken: cancellationToken);
    }
}
