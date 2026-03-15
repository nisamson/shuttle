using Shuttle.EFCore.SiteArchive;

namespace Shuttle.Api.Services.Crawlers;

public interface IApiCrawler {

    static abstract void RegisterCrawlers(IServiceCollection collection);

    IAsyncEnumerable<ArchiveEntry> Crawl(CancellationToken token = default);
    
}
