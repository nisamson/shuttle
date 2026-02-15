using System.Diagnostics;
using SHLAnalytics.WebApp.Helpers;
using SHLAnalytics.WebApp.Options;

namespace SHLAnalytics.WebApp.Services.IO;

public class IoConfiguration: IIoConfiguration {

    private readonly CommonOptions commonOptions;
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private ILogger<IoConfiguration> logger;
    private string? storageLocation;
    
    public IoConfiguration(IConfiguration config, ILogger<IoConfiguration> logger) {
        this.logger = logger;
        commonOptions = config.GetSection(CommonOptions.SectionName).Get<CommonOptions>() ?? new CommonOptions();
    }
    
    public async Task<string> EnsureFileStorageLocation(CancellationToken cancellationToken = default) {
        if (storageLocation is not null) {
            return storageLocation;
        }
        logger.LogInformation("Ensuring file storage location exists");
        await using var _ = await semaphore.WaitDisposable(cancellationToken);
        var location = commonOptions.GetFileStorageLocation();
        if (!Directory.Exists(location)) {
            Directory.CreateDirectory(location);
        }
        storageLocation = location;
        return storageLocation;
    }
}
