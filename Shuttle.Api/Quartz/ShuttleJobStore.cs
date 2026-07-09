using System.Diagnostics.CodeAnalysis;
using Quartz;
using Quartz.Impl.AdoJobStore;
using Quartz.Impl.Matchers;
using Quartz.Spi;
using Quartz.Util;

namespace Shuttle.Api.Jobs.Quartz;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllConstructors 
    | DynamicallyAccessedMemberTypes.AllProperties)]
public class ShuttleJobStore : IJobStore {
    private JobStoreTX jobStore = null!;

    public Task Initialize(
        ITypeLoadHelper loadHelper,
        ISchedulerSignaler signaler,
        CancellationToken cancellationToken = new CancellationToken()
    ) {
        jobStore = new JobStoreTX();
        return jobStore.Initialize(loadHelper, signaler, cancellationToken);
    }

    public Task SchedulerStarted(CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.SchedulerStarted(cancellationToken);
    }
    public Task SchedulerPaused(CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.SchedulerPaused(cancellationToken);
    }
    public Task SchedulerResumed(CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.SchedulerResumed(cancellationToken);
    }
    public Task Shutdown(CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.Shutdown(cancellationToken);
    }

    public Task StoreJobAndTrigger(
        IJobDetail newJob,
        IOperableTrigger newTrigger,
        CancellationToken cancellationToken = new CancellationToken()
    ) {
        return jobStore.StoreJobAndTrigger(newJob, newTrigger, cancellationToken);
    }

    public async Task<bool> IsJobGroupPaused(string groupName, CancellationToken cancellationToken = new CancellationToken()) {
        return false;
    }
    public async Task<bool> IsTriggerGroupPaused(string groupName, CancellationToken cancellationToken = new CancellationToken()) {
        var paused = await jobStore.GetPausedTriggerGroups(cancellationToken);
        return paused.Contains(groupName);
    }
    public Task StoreJob(IJobDetail newJob, bool replaceExisting, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.StoreJob(newJob, replaceExisting, cancellationToken);
    }

    public Task StoreJobsAndTriggers(
        IReadOnlyDictionary<IJobDetail, IReadOnlyCollection<ITrigger>> triggersAndJobs,
        bool replace,
        CancellationToken cancellationToken = new CancellationToken()
    ) {
        return jobStore.StoreJobsAndTriggers(triggersAndJobs, replace, cancellationToken);
    }

    public Task<bool> RemoveJob(JobKey jobKey, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.RemoveJob(jobKey, cancellationToken);
    }
    public Task<bool> RemoveJobs(IReadOnlyCollection<JobKey> jobKeys, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.RemoveJobs(jobKeys, cancellationToken);
    }
    public Task<IJobDetail?> RetrieveJob(JobKey jobKey, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.RetrieveJob(jobKey, cancellationToken);
    }

    public Task StoreTrigger(
        IOperableTrigger newTrigger,
        bool replaceExisting,
        CancellationToken cancellationToken = new CancellationToken()
    ) {
        return jobStore.StoreTrigger(newTrigger, replaceExisting, cancellationToken);
    }

    public Task<bool> RemoveTrigger(TriggerKey triggerKey, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.RemoveTrigger(triggerKey, cancellationToken);
    }
    public Task<bool> RemoveTriggers(IReadOnlyCollection<TriggerKey> triggerKeys, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.RemoveTriggers(triggerKeys, cancellationToken);
    }

    public Task<bool> ReplaceTrigger(
        TriggerKey triggerKey,
        IOperableTrigger newTrigger,
        CancellationToken cancellationToken = new CancellationToken()
    ) {
        return jobStore.ReplaceTrigger(triggerKey, newTrigger, cancellationToken);
    }

    public Task<IOperableTrigger?> RetrieveTrigger(TriggerKey triggerKey, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.RetrieveTrigger(triggerKey, cancellationToken);
    }
    public Task<bool> CalendarExists(string calName, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.CalendarExists(calName, cancellationToken);
    }
    public Task<bool> CheckExists(JobKey jobKey, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.CheckExists(jobKey, cancellationToken);
    }
    public Task<bool> CheckExists(TriggerKey triggerKey, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.CheckExists(triggerKey, cancellationToken);
    }
    public Task ClearAllSchedulingData(CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.ClearAllSchedulingData(cancellationToken);
    }

    public Task StoreCalendar(
        string name,
        ICalendar calendar,
        bool replaceExisting,
        bool updateTriggers,
        CancellationToken cancellationToken = new CancellationToken()
    ) {
        return jobStore.StoreCalendar(name, calendar, replaceExisting, updateTriggers, cancellationToken);
    }

    public Task<bool> RemoveCalendar(string calName, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.RemoveCalendar(calName, cancellationToken);
    }
    public Task<ICalendar?> RetrieveCalendar(string calName, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.RetrieveCalendar(calName, cancellationToken);
    }
    public Task<int> GetNumberOfJobs(CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.GetNumberOfJobs(cancellationToken);
    }
    public Task<int> GetNumberOfTriggers(CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.GetNumberOfTriggers(cancellationToken);
    }
    public Task<int> GetNumberOfCalendars(CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.GetNumberOfCalendars(cancellationToken);
    }
    public Task<IReadOnlyCollection<JobKey>> GetJobKeys(GroupMatcher<JobKey> matcher, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.GetJobKeys(matcher, cancellationToken);
    }
    public Task<IReadOnlyCollection<TriggerKey>> GetTriggerKeys(GroupMatcher<TriggerKey> matcher, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.GetTriggerKeys(matcher, cancellationToken);
    }
    public Task<IReadOnlyCollection<string>> GetJobGroupNames(CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.GetJobGroupNames(cancellationToken);
    }
    public Task<IReadOnlyCollection<string>> GetTriggerGroupNames(CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.GetTriggerGroupNames(cancellationToken);
    }
    public Task<IReadOnlyCollection<string>> GetCalendarNames(CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.GetCalendarNames(cancellationToken);
    }
    public Task<IReadOnlyCollection<IOperableTrigger>> GetTriggersForJob(JobKey jobKey, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.GetTriggersForJob(jobKey, cancellationToken);
    }
    public Task<TriggerState> GetTriggerState(TriggerKey triggerKey, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.GetTriggerState(triggerKey, cancellationToken);
    }
    public Task ResetTriggerFromErrorState(TriggerKey triggerKey, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.ResetTriggerFromErrorState(triggerKey, cancellationToken);
    }
    public Task PauseTrigger(TriggerKey triggerKey, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.PauseTrigger(triggerKey, cancellationToken);
    }
    public Task<IReadOnlyCollection<string>> PauseTriggers(GroupMatcher<TriggerKey> matcher, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.PauseTriggers(matcher, cancellationToken);
    }
    public Task PauseJob(JobKey jobKey, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.PauseJob(jobKey, cancellationToken);
    }
    public Task<IReadOnlyCollection<string>> PauseJobs(GroupMatcher<JobKey> matcher, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.PauseJobs(matcher, cancellationToken);
    }
    public Task ResumeTrigger(TriggerKey triggerKey, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.ResumeTrigger(triggerKey, cancellationToken);
    }
    public Task<IReadOnlyCollection<string>> ResumeTriggers(GroupMatcher<TriggerKey> matcher, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.ResumeTriggers(matcher, cancellationToken);
    }
    public Task<IReadOnlyCollection<string>> GetPausedTriggerGroups(CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.GetPausedTriggerGroups(cancellationToken);
    }
    public Task ResumeJob(JobKey jobKey, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.ResumeJob(jobKey, cancellationToken);
    }
    public Task<IReadOnlyCollection<string>> ResumeJobs(GroupMatcher<JobKey> matcher, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.ResumeJobs(matcher, cancellationToken);
    }
    public Task PauseAll(CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.PauseAll(cancellationToken);
    }
    public Task ResumeAll(CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.ResumeAll(cancellationToken);
    }

    public Task<IReadOnlyCollection<IOperableTrigger>> AcquireNextTriggers(
        DateTimeOffset noLaterThan,
        int maxCount,
        TimeSpan timeWindow,
        CancellationToken cancellationToken = new CancellationToken()
    ) {
        return jobStore.AcquireNextTriggers(noLaterThan, maxCount, timeWindow, cancellationToken);
    }

    public Task ReleaseAcquiredTrigger(IOperableTrigger trigger, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.ReleaseAcquiredTrigger(trigger, cancellationToken);
    }
    public Task<IReadOnlyCollection<TriggerFiredResult>> TriggersFired(IReadOnlyCollection<IOperableTrigger> triggers, CancellationToken cancellationToken = new CancellationToken()) {
        return jobStore.TriggersFired(triggers, cancellationToken);
    }

    public Task TriggeredJobComplete(
        IOperableTrigger trigger,
        IJobDetail jobDetail,
        SchedulerInstruction triggerInstCode,
        CancellationToken cancellationToken = new CancellationToken()
    ) {
        return jobStore.TriggeredJobComplete(trigger, jobDetail, triggerInstCode, cancellationToken);
    }

    public bool SupportsPersistence => jobStore.SupportsPersistence;

    public long EstimatedTimeToReleaseAndAcquireTrigger => jobStore.EstimatedTimeToReleaseAndAcquireTrigger;

    public bool Clustered => jobStore.Clustered;

    public string InstanceId {
        set => jobStore.InstanceId = value;
    }

    public string InstanceName {
        set => jobStore.InstanceName = value;
    }

    public int ThreadPoolSize {
        set => jobStore.ThreadPoolSize = value;
    }
    
    public string DataSource {
        get => jobStore.DataSource;
        set => jobStore.DataSource = value;
    }

    public IDbConnectionManager ConnectionManager {
        get => jobStore.ConnectionManager;
        set => jobStore.ConnectionManager = value;
    }

    /// <summary>
    /// Get or sets the prefix that should be pre-pended to all table names.
    /// </summary>
    public string TablePrefix {
        get => jobStore.TablePrefix;
        set => jobStore.TablePrefix = value;
    }

    /// <summary>
    /// Get whether the threads spawned by this JobStore should be
    /// marked as daemon.  Possible threads include the <see cref="MisfireHandler" />
    /// and the <see cref="ClusterManager"/>.
    /// </summary>
    /// <returns></returns>
    public bool MakeThreadsDaemons {
        get => jobStore.MakeThreadsDaemons;
        set => jobStore.MakeThreadsDaemons = value;
    }

    /// <summary>
    /// Get whether to check to see if there are Triggers that have misfired
    /// before actually acquiring the lock to recover them.  This should be
    /// set to false if the majority of the time, there are misfired
    /// Triggers.
    /// </summary>
    /// <returns></returns>
    public bool DoubleCheckLockMisfireHandler {
        get => jobStore.DoubleCheckLockMisfireHandler;
        set => jobStore.DoubleCheckLockMisfireHandler = value;
    }

    /// <summary>
    /// Whether to perform a schema check on scheduler startup and try to determine if correct tables are in place.
    /// Defaults to true.
    /// </summary>
    public bool PerformSchemaValidation {
        get => jobStore.PerformSchemaValidation;
        set => jobStore.PerformSchemaValidation = value;
    }

    public virtual TimeSpan GetAcquireRetryDelay(int failureCount) {
        return jobStore.GetAcquireRetryDelay(failureCount);
    }
    /// <summary>
    /// Gets or sets the number of retries before an error is logged for recovery operations.
    /// </summary>
    public int RetryableActionErrorLogThreshold {
        get => jobStore.RetryableActionErrorLogThreshold;
        set => jobStore.RetryableActionErrorLogThreshold = value;
    }

    public IObjectSerializer? ObjectSerializer {
        get => jobStore.ObjectSerializer;
        set => jobStore.ObjectSerializer = value;
    }

    /// <summary>
    /// Get or set the frequency at which this instance "checks-in"
    /// with the other instances of the cluster. -- Affects the rate of
    /// detecting failed instances.
    /// </summary>
    [TimeSpanParseRule(TimeSpanParseRule.Milliseconds)]
    public TimeSpan ClusterCheckinInterval {
        get => jobStore.ClusterCheckinInterval;
        set => jobStore.ClusterCheckinInterval = value;
    }

    /// <summary>
    /// The time span by which a check-in must have missed its
    /// next-fire-time, in order for it to be considered "misfired" and thus
    /// other scheduler instances in a cluster can consider a "misfired" scheduler
    /// instance as failed or dead.
    /// </summary>
    [TimeSpanParseRule(TimeSpanParseRule.Milliseconds)]
    public TimeSpan ClusterCheckinMisfireThreshold {
        get => jobStore.ClusterCheckinMisfireThreshold;
        set => jobStore.ClusterCheckinMisfireThreshold = value;
    }

    /// <summary>
    /// Get or set the maximum number of misfired triggers that the misfire handling
    /// thread will try to recover at one time (within one transaction).  The
    /// default is 20.
    /// </summary>
    public int MaxMisfiresToHandleAtATime {
        get => jobStore.MaxMisfiresToHandleAtATime;
        set => jobStore.MaxMisfiresToHandleAtATime = value;
    }

    /// <summary>
    /// Gets or sets the database retry interval.
    /// </summary>
    /// <value>The db retry interval.</value>
    [TimeSpanParseRule(TimeSpanParseRule.Milliseconds)]
    public TimeSpan DbRetryInterval {
        get => jobStore.DbRetryInterval;
        set => jobStore.DbRetryInterval = value;
    }

    /// <summary>
    /// Get or set whether this instance should use database-based thread
    /// synchronization.
    /// </summary>
    public bool UseDBLocks {
        get => jobStore.UseDBLocks;
        set => jobStore.UseDBLocks = value;
    }

    /// <summary>
    /// Whether or not to obtain locks when inserting new jobs/triggers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defaults to <see langword="true" />, which is safest - some db's (such as
    /// MS SQLServer) seem to require this to avoid deadlocks under high load,
    /// while others seem to do fine without.  Settings this to false means
    /// isolation guarantees between job scheduling and trigger acquisition are
    /// entirely enforced by the database.  Depending on the database and it's
    /// configuration this may cause unusual scheduling behaviors.
    /// </para>
    /// <para>
    /// Setting this property to <see langword="false" /> will provide a
    /// significant performance increase during the addition of new jobs
    /// and triggers.
    /// </para>
    /// </remarks>
    public virtual bool LockOnInsert {
        get => jobStore.LockOnInsert;
        set => jobStore.LockOnInsert = value;
    }

    /// <summary>
    /// The time span by which a trigger must have missed its
    /// next-fire-time, in order for it to be considered "misfired" and thus
    /// have its misfire instruction applied.
    /// </summary>
    [TimeSpanParseRule(TimeSpanParseRule.Milliseconds)]
    public virtual TimeSpan MisfireThreshold {
        get => jobStore.MisfireThreshold;
        set => jobStore.MisfireThreshold = value;
    }

    /// <summary>
    /// How often should the misfire handler check for misfires. Defaults to
    /// <see cref="MisfireThreshold"/>.
    /// </summary>
    [TimeSpanParseRule(TimeSpanParseRule.Milliseconds)]
    public virtual TimeSpan MisfireHandlerFrequency
    {
        get => jobStore.MisfireHandlerFrequency;
        set => jobStore.MisfireHandlerFrequency = value;
    }

    /// <summary>
    /// Don't call set autocommit(false) on connections obtained from the
    /// DataSource. This can be helpful in a few situations, such as if you
    /// have a driver that complains if it is called when it is already off.
    /// </summary>
    public virtual bool DontSetAutoCommitFalse {
        get => jobStore.DontSetAutoCommitFalse;
        set => jobStore.DontSetAutoCommitFalse = value;
    }

    /// <summary>
    /// Set the transaction isolation level of DB connections to sequential.
    /// </summary>
    public virtual bool TxIsolationLevelSerializable {
        get => jobStore.TxIsolationLevelSerializable;
        set => jobStore.TxIsolationLevelSerializable = value;
    }

    /// <summary>
    /// Whether or not the query and update to acquire a Trigger for firing
    /// should be performed after obtaining an explicit DB lock (to avoid
    /// possible race conditions on the trigger's db row).  This is
    /// is considered unnecessary for most databases (due to the nature of
    ///  the SQL update that is performed), and therefore a superfluous performance hit.
    /// </summary>
    /// <remarks>
    /// However, if batch acquisition is used, it is important for this behavior
    /// to be used for all dbs.
    /// </remarks>
    public bool AcquireTriggersWithinLock {
        get => jobStore.AcquireTriggersWithinLock;
        set => jobStore.AcquireTriggersWithinLock = value;
    }

    /// <summary>
    /// Get or set the ADO.NET driver delegate class name.
    /// </summary>
    public virtual string DriverDelegateType {
        get => jobStore.DriverDelegateType;
        set => jobStore.DriverDelegateType = value;
    }

    /// <summary>
    /// The driver delegate's initialization string.
    /// </summary>
    public string? DriverDelegateInitString {
        get => jobStore.DriverDelegateInitString;
        set => jobStore.DriverDelegateInitString = value;
    }

    public string UseProperties {
        get;
        set {
            if (value == null) {
                value = "false";
            }

            jobStore.UseProperties = value;
            field = value;
        }
    } = null!;

    /// <summary>
    /// set the SQL statement to use to select and lock a row in the "locks"
    /// table.
    /// </summary>
    /// <seealso cref="StdRowLockSemaphore" />
    public virtual string? SelectWithLockSQL {
        get => jobStore.SelectWithLockSQL;
        set => jobStore.SelectWithLockSQL = value;
    }
}