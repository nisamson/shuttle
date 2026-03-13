using SHLAnalytics.EFCore.SiteArchive;

namespace SHLAnalytics.Shuttle.Api.Services.Crawlers;

public interface IApiCrawler {

    static abstract void RegisterCrawlers(IServiceCollection collection);

    IAsyncEnumerable<ArchiveEntry> Crawl(CancellationToken token = default);
    
}
