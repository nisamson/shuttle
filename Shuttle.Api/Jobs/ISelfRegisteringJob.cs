using Quartz;

namespace Shuttle.Api.Jobs;

public interface ISelfRegisteringJob : IJob {
    static abstract IServiceCollectionQuartzConfigurator RegisterJob(IServiceCollectionQuartzConfigurator qc);
}
