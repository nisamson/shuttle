using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shuttle.Api.Meta;
using Shuttle.EFCore;
using Shuttle.EFCore.Entities;
using Shuttle.EFCore.Entities.Portal;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.Shl.Api.Models.Portal.V1;
using Shuttle.WebClient.Shared.Blogs;

namespace Shuttle.Tests.Api;

/// <summary>
/// Behavioural tests for <see cref="MetaResolver"/> — the mapping of forwarded front-end paths to
/// the SEO / social-embed <c>PageMetadata</c> served by the <c>/meta</c> endpoint.
/// </summary>
public class MetaResolverTests {
    private const string BaseUrl = "https://shl.example";
    private const string Origin = "https://localhost:5001";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ShlDbContext CreateContext() {
        var options = new DbContextOptionsBuilder<ShlDbContext>()
            .UseInMemoryDatabase($"meta-resolve-{Guid.NewGuid()}")
            .Options;
        return new ShlDbContext(options, NullLogger<ShlDbContext>.Instance);
    }

    private static PlayerInformation Player(int id, string name) => new() {
        UserId = id,
        PlayerId = id,
        Username = $"user{id}",
        Name = name,
        CreationTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Status = PlayerStatus.Active,
        Position = PlayerPosition.Center,
        Handedness = default,
        CurrentLeague = KnownLeague.Shl,
        TotalTpe = 1234,
        AppliedTpe = 0,
        BankedTpe = 0,
        BankBalance = 0,
    };

    private static async Task<MetaResolver> SetupAsync(
        MetaOptions? options = null,
        Action<ShlDbContext>? seed = null) {
        var db = CreateContext();
        seed?.Invoke(db);
        await db.SaveChangesAsync(Ct);

        return new MetaResolver(
            db,
            new BlogService(),
            Options.Create(options ?? new MetaOptions { SiteBaseUrl = BaseUrl }));
    }

    [Fact]
    public async Task Resolves_a_known_player_to_a_profile_card() {
        var resolver = await SetupAsync(seed: db => db.PlayerInformation.Add(Player(1001, "Alice Skater")));

        var meta = await resolver.ResolveAsync("/players/1001", null, Origin, Ct);

        Assert.Contains("Alice Skater", meta.Title);
        Assert.Contains("1,234 TPE", meta.Description);
        Assert.Equal("profile", meta.OgType);
        Assert.Equal($"{BaseUrl}/players/1001", meta.CanonicalUrl);
    }

    [Fact]
    public async Task Supports_the_singular_player_route() {
        var resolver = await SetupAsync(seed: db => db.PlayerInformation.Add(Player(1001, "Alice Skater")));

        var meta = await resolver.ResolveAsync("/player/1001", null, Origin, Ct);

        Assert.Contains("Alice Skater", meta.Title);
    }

    [Fact]
    public async Task Falls_back_to_site_default_for_an_unknown_player() {
        var resolver = await SetupAsync();

        var meta = await resolver.ResolveAsync("/players/999", null, Origin, Ct);

        Assert.Equal("Shuttle — The SHL Analysis Site", meta.Title);
    }

    [Fact]
    public async Task Resolves_a_user_by_name_to_a_member_profile() {
        var resolver = await SetupAsync(seed: db => db.Users.Add(new ShlUser { UserId = 5, Name = "bob" }));

        var meta = await resolver.ResolveAsync("/users/bob", null, Origin, Ct);

        Assert.Contains("bob", meta.Title);
        Assert.Equal("profile", meta.OgType);
    }

    [Fact]
    public async Task Resolves_a_user_by_numeric_id() {
        var resolver = await SetupAsync(seed: db => db.Users.Add(new ShlUser { UserId = 5, Name = "bob" }));

        var meta = await resolver.ResolveAsync("/users/5", null, Origin, Ct);

        Assert.Contains("bob", meta.Title);
    }

    [Fact]
    public async Task Resolves_a_known_blog_slug_to_an_article() {
        var blogService = new BlogService();
        var entry = blogService.GetEntries()[0];
        var resolver = await SetupAsync();

        var meta = await resolver.ResolveAsync($"/blogs/{entry.Slug}", null, Origin, Ct);

        Assert.Equal(entry.Title, meta.Title);
        Assert.Equal("article", meta.OgType);
    }

    [Fact]
    public async Task Resolves_a_static_page() {
        var resolver = await SetupAsync();

        var meta = await resolver.ResolveAsync("/privacy", null, Origin, Ct);

        Assert.StartsWith("Privacy", meta.Title);
    }

    [Fact]
    public async Task Falls_back_to_site_default_for_an_unknown_path() {
        var resolver = await SetupAsync();

        var meta = await resolver.ResolveAsync("/totally/unknown/route", null, Origin, Ct);

        Assert.Equal("Shuttle — The SHL Analysis Site", meta.Title);
        Assert.Equal("website", meta.OgType);
    }

    [Theory]
    [InlineData("/scouting")]
    [InlineData("/scouting/teams/11111111-1111-1111-1111-111111111111")]
    [InlineData("/scouting/boards/22222222-2222-2222-2222-222222222222")]
    [InlineData("/admin/hello")]
    [InlineData("/account/settings")]
    public async Task Returns_generic_metadata_for_authorized_only_routes(string path) {
        var resolver = await SetupAsync();

        var meta = await resolver.ResolveAsync(path, null, Origin, Ct);

        Assert.Equal("Shuttle — The SHL Analysis Site", meta.Title);
        Assert.Equal(
            "Player and team statistics, scouting, and draft analysis for the Simulation Hockey League (SHL).",
            meta.Description);
        Assert.Equal("website", meta.OgType);
    }

    [Fact]
    public async Task Uses_the_request_origin_as_the_base_when_no_site_url_is_configured() {
        var resolver = await SetupAsync(new MetaOptions { SiteBaseUrl = null });

        var meta = await resolver.ResolveAsync("/privacy", null, Origin, Ct);

        Assert.Equal($"{Origin}/privacy", meta.CanonicalUrl);
    }

    [Fact]
    public async Task Preserves_the_query_string_on_the_canonical_url() {
        var resolver = await SetupAsync();

        var meta = await resolver.ResolveAsync("/players/compare", "?ids=1,2", Origin, Ct);

        Assert.Equal($"{BaseUrl}/players/compare?ids=1,2", meta.CanonicalUrl);
    }

    [Fact]
    public async Task Resolves_a_site_relative_default_image_against_the_base_url() {
        var resolver = await SetupAsync(new MetaOptions { SiteBaseUrl = BaseUrl, DefaultImageUrl = "/icon.svg" });

        var meta = await resolver.ResolveAsync("/", null, Origin, Ct);

        Assert.Equal($"{BaseUrl}/icon.svg", meta.ImageUrl);
    }
}
