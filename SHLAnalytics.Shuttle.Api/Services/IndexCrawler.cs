using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using SHLAnalytics.Api.Client;
using SHLAnalytics.Api.Client.Raw.Index;
using SHLAnalytics.EFCore.SiteArchive;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SHLAnalytics.Shuttle.Api.Services;

public class IndexCrawler : IApiCrawler {
    private readonly ILogger<IndexCrawler> logger;
    private readonly IShlIndexV1Client indexClient;
    private readonly IRawIndexV1Client rawClient;

    public IndexCrawler(ILogger<IndexCrawler> logger, IShlIndexV1Client indexClient, IRawIndexV1Client rawClient) {
        this.logger = logger;
        this.indexClient = indexClient;
        this.rawClient = rawClient;
    }
    
    public static void RegisterCrawlers(IServiceCollection collection) {
        collection.AddScoped<IndexCrawler>();
    }
    
    public async IAsyncEnumerable<ArchiveEntry> Crawl([EnumeratorCancellation] CancellationToken token = default) {
        using var _ = logger.BeginScope("Index crawler started at {StartTime}", DateTimeOffset.UtcNow);

        logger.LogInformation("Fetching leagues from index API");
        var leagues = await indexClient.GetLeagues(token);
        var leaguesJson = JsonSerializer.Serialize(leagues);
        yield break;
    }
}
