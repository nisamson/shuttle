using Shuttle.EFCore.SiteArchive;

namespace Shuttle.Api.Services.Crawlers;

public class PortalCrawler : IApiCrawler {
    private readonly ILogger<PortalCrawler> logger;

    public PortalCrawler(ILogger<PortalCrawler> logger) {
        this.logger = logger;
    }
    
    public static void RegisterCrawlers(IServiceCollection collection) {
        collection.AddScoped<PortalCrawler>();
    }
    public IAsyncEnumerable<ArchiveEntry> Crawl(CancellationToken token = default) {
        logger.LogInformation("Portal crawler is not implemented yet");
        return AsyncEnumerable.Empty<ArchiveEntry>();
    }
}
