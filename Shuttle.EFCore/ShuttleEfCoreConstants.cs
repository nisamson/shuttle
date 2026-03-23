namespace Shuttle.EFCore;

public class ShuttleEfCoreConstants {
    public const string DatabaseEnvironmentKey = "SHUTTLESQLSERVER_DATABASE";
    public const string DatabaseHostKey = "SHUTTLESQLSERVER_HOST";
    public const string QuartzTablePrefix = "[qrtz].QRTZ_";
    public const string QuartzTablePrefixKey = "org.quartz.jobStore.tablePrefix";
}
