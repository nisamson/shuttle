namespace Shuttle.Models.Meta;

/// <summary>
/// Metadata describing the freshness of the backing database, returned by
/// <c>GET /data/metainfo</c>. Both timestamps are derived from the scheduled database update job's
/// trigger in the Quartz job store.
/// </summary>
/// <param name="LastUpdated">
/// The UTC time at which the scheduled database update last ran, or <c>null</c> if it has not run
/// yet.
/// </param>
/// <param name="NextUpdate">
/// The UTC time at which the database update is next scheduled to run, or <c>null</c> if no future
/// run is scheduled.
/// </param>
public record DataMetaInfo(DateTimeOffset? LastUpdated, DateTimeOffset? NextUpdate);
