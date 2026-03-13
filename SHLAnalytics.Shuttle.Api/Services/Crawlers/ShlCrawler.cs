using SHLAnalytics.EFCore.SiteArchive;

namespace SHLAnalytics.Shuttle.Api.Services.Crawlers;

public class ShlCrawler : IApiCrawler {
    private readonly ILogger<ShlCrawler> logger;
    private readonly IndexCrawler indexCrawler;
    private readonly PortalCrawler portalCrawler; 

    public ShlCrawler(ILogger<ShlCrawler> logger, IndexCrawler indexCrawler, PortalCrawler portalCrawler) {
        this.logger = logger;
        this.indexCrawler = indexCrawler;
        this.portalCrawler = portalCrawler;
    }
    
    public static void RegisterCrawlers(IServiceCollection collection) {
        collection.AddScoped<ShlCrawler>();
        IndexCrawler.RegisterCrawlers(collection);
        PortalCrawler.RegisterCrawlers(collection);
    }
    public IAsyncEnumerable<ArchiveEntry> Crawl(CancellationToken token = default) {
        using var _ = logger.BeginScope("SHL Crawler started at {StartTime}", DateTimeOffset.UtcNow);
        return AsyncEnumerableEx.Merge(indexCrawler.Crawl(token), portalCrawler.Crawl(token));
    }
}
