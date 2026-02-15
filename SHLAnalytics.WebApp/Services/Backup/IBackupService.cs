namespace SHLAnalytics.WebApp.Services.Backup;

public interface IBackupService {
    public Task Backup(CancellationToken token = default);
}
