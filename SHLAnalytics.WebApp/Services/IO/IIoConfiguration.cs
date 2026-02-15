namespace SHLAnalytics.WebApp.Services.IO;

public interface IIoConfiguration {
    Task<string> EnsureFileStorageLocation(CancellationToken cancellationToken = default);
}
