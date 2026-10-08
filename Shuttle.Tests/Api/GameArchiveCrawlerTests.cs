using System.Net;
using System.Text;
using Shuttle.GameArchive;

namespace Shuttle.Tests.Api;

public sealed class GameArchiveCrawlerTests {
    private static readonly Uri Root = new("https://simulationhockey.com/games/");

    [Fact]
    public async Task Inventory_recurses_and_ignores_navigation_and_external_links() {
        using var http = Client(new Dictionary<string, string> {
            ["/games/"] = Listing("games", "<a href=\"/\">Parent Directory</a><a href=\"?C=N;O=D\">Sort</a>"
                + "<a href=\"shl/\">shl/</a><a href=\"https://elsewhere.example/a.csv\">other</a>"),
            ["/games/shl/"] = Listing("games/shl", "<a href=\"/games/\">Parent Directory</a>"
                + "<a href=\"S85/\">S85/</a>"),
            ["/games/shl/S85/"] = Listing("games/shl/S85", "<a href=\"stats.csv\">stats.csv</a>"),
        });
        var inventory = await new GameArchiveCrawler(http).InventoryAsync(Root, TestContext.Current.CancellationToken);
        Assert.Equal("https://simulationhockey.com/games/shl/S85/stats.csv", inventory.Files["shl/S85/stats.csv"].AbsoluteUri);
        Assert.Single(inventory.Files);
        Assert.Empty(inventory.SkippedDirectories);
    }

    [Theory]
    [InlineData("<a href=\"../README.md\">README</a>")]
    [InlineData("<a href=\".git/config\">config</a>")]
    [InlineData("<a href=\"manifest.json\">manifest</a>")]
    [InlineData("<a href=\"manifest.json/file.csv\">manifest directory</a>")]
    [InlineData("<a href=\"MANIFEST.JSON\">manifest case collision</a>")]
    [InlineData("<a href=\"CON.csv\">device</a>")]
    [InlineData("<a href=\"A.csv\">A</a><a href=\"a.csv\">a</a>")]
    public async Task Inventory_rejects_unsafe_or_colliding_entries(string links) {
        using var http = Client(new Dictionary<string, string> {
            ["/games/"] = Listing("games", links),
        });
        var crawler = new GameArchiveCrawler(http);
        if (links.Contains("../", StringComparison.Ordinal)) {
            Assert.Empty((await crawler.InventoryAsync(Root, TestContext.Current.CancellationToken)).Files);
        } else {
            await Assert.ThrowsAsync<InvalidDataException>(
                () => crawler.InventoryAsync(Root, TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    public async Task Inventory_records_and_skips_non_listing_root() {
        using var http = Client(new Dictionary<string, string> {
            ["/games/"] = "<html><body><table><a href=\"x.csv\">x</a></table></body></html>",
        });
        var inventory = await new GameArchiveCrawler(http).InventoryAsync(Root, TestContext.Current.CancellationToken);
        Assert.Empty(inventory.Files);
        var skipped = Assert.Single(inventory.SkippedDirectories);
        Assert.Equal(".", skipped.Path);
        Assert.Equal(Root.AbsoluteUri, skipped.Url);
        Assert.Null(skipped.Title);
        Assert.Equal("non-apache-directory-listing", skipped.Reason);
    }

    [Fact]
    public async Task Inventory_skips_custom_subtrees_but_keeps_listing_files_and_sorts_report() {
        var handler = new FixtureHandler(new Dictionary<string, string> {
            ["/games/"] = Listing("games",
                "<a href=\"z-custom/\">Custom Z</a><a href=\"a-custom/\">Custom A</a><a href=\"shl/\">SHL</a>"),
            ["/games/z-custom/"] = "<html><head><title>IIHF Indexes</title></head><body><a href=\"roundrobin/\">Round Robin</a></body></html>",
            ["/games/a-custom/"] = "<html><head><title>Other Index</title></head><body><a href=\"hidden.csv\">Hidden</a></body></html>",
            ["/games/shl/"] = Listing("games/shl", "<a href=\"game.csv\">Game</a>"),
        });
        using var http = new HttpClient(handler);

        var inventory = await new GameArchiveCrawler(http).InventoryAsync(Root, TestContext.Current.CancellationToken);

        Assert.Equal("shl/game.csv", Assert.Single(inventory.Files).Key);
        Assert.Equal(["a-custom/", "z-custom/"], inventory.SkippedDirectories.Select(entry => entry.Path));
        Assert.Equal("https://simulationhockey.com/games/z-custom/", inventory.SkippedDirectories[1].Url);
        Assert.Equal("IIHF Indexes", inventory.SkippedDirectories[1].Title);
        Assert.Equal(4, handler.RequestCount);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Inventory_does_not_skip_http_failures(HttpStatusCode status) {
        using var http = new HttpClient(new FixtureHandler(new Dictionary<string, string>()) { Status = status });
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            new GameArchiveCrawler(http).InventoryAsync(Root, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Inventory_does_not_skip_non_html_responses() {
        using var http = new HttpClient(new FixtureHandler(new Dictionary<string, string>()) { MediaType = "application/octet-stream" });
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new GameArchiveCrawler(http).InventoryAsync(Root, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Download_rejects_declared_oversized_file_before_writing() {
        var handler = new FixtureHandler(new Dictionary<string, string>());
        handler.Oversized = true;
        using var http = new HttpClient(handler);
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        await Assert.ThrowsAsync<InvalidDataException>(
            () => new GameArchiveCrawler(http).DownloadAsync(
                new Uri(Root, "large.zip"), Root, path, TestContext.Current.CancellationToken));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task Download_refuses_redirects_to_other_hosts() {
        var handler = new FixtureHandler(new Dictionary<string, string>()) {
            Redirect = new Uri("https://elsewhere.example/private"),
        };
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<InvalidDataException>(
            () => new GameArchiveCrawler(http).DownloadAsync(
                new Uri(Root, "stats.csv"), Root, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
                TestContext.Current.CancellationToken));
        Assert.Equal(1, handler.RequestCount);
    }

    private static string Listing(string path, string links) =>
        $"<html><head><title>Index of /{path}</title></head><body><table><tr><td>{links}</td></tr></table></body></html>";

    private static HttpClient Client(Dictionary<string, string> pages) => new(new FixtureHandler(pages));

    private sealed class FixtureHandler(Dictionary<string, string> pages) : HttpMessageHandler {
        public bool Oversized { get; set; }
        public Uri? Redirect { get; set; }
        public int RequestCount { get; private set; }
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public string MediaType { get; set; } = "text/html";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            RequestCount++;
            if (Redirect is not null) {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Redirect) {
                    Headers = { Location = Redirect },
                });
            }
            var path = request.RequestUri!.AbsolutePath;
            var response = new HttpResponseMessage(Status) {
                Content = new StringContent(pages.GetValueOrDefault(path, ""), Encoding.UTF8, MediaType),
            };
            if (Oversized)
                response.Content.Headers.ContentLength = GameArchiveOptions.MaxGitFileSize + 1;
            return Task.FromResult(response);
        }
    }
}
