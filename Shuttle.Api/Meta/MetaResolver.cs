using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shuttle.EFCore;
using Shuttle.Shl.Api.Models.Common;
using Shuttle.WebClient.Shared.Blogs;
using Shuttle.WebClient.Shared.Meta;

namespace Shuttle.Api.Meta;

/// <inheritdoc />
public sealed class MetaResolver : IMetaResolver {
    private const string SiteTitle = "Shuttle — The SHL Analysis Site";

    private const string SiteDescription =
        "Player and team statistics, scouting, and draft analysis for the Simulation Hockey League (SHL).";

    private static readonly IReadOnlyDictionary<string, (string Title, string Description)> StaticPages =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase) {
            ["/"] = (SiteTitle, SiteDescription),
            ["/privacy"] = ("Privacy — Shuttle", "Privacy policy for Shuttle, the SHL analysis site."),
            ["/blogs"] = ("Blog — Shuttle", "Articles and analysis from Shuttle, the SHL analysis site."),
            ["/players"] = ("Players — Shuttle", "Browse and search SHL and SMJHL players on Shuttle."),
            ["/players/compare"] = ("Compare Players — Shuttle", "Compare SHL and SMJHL players side by side on Shuttle."),
            ["/users"] = ("Members — Shuttle", "Browse SHL members and their players on Shuttle."),
        };

    /// <summary>
    /// First path segments of front-end routes gated by <c>[Authorize]</c>. The anonymous
    /// <c>/meta</c> endpoint returns generic site metadata for these so no private data leaks.
    /// </summary>
    private static readonly HashSet<string> AuthorizedOnlyRoots =
        new(StringComparer.OrdinalIgnoreCase) { "scouting", "admin", "account" };

    private readonly ShlDbContext db;
    private readonly IBlogService blogService;
    private readonly MetaOptions options;

    public MetaResolver(ShlDbContext db, IBlogService blogService, IOptions<MetaOptions> options) {
        this.db = db;
        this.blogService = blogService;
        this.options = options.Value;
    }

    public async Task<PageMetadata> ResolveAsync(
        string requestPath,
        string? queryString,
        string requestOrigin,
        CancellationToken cancellationToken) {
        var baseUrl = string.IsNullOrWhiteSpace(options.SiteBaseUrl) ? requestOrigin : options.SiteBaseUrl;
        var segments = requestPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var canonical = BuildCanonical(baseUrl, segments, queryString);
        var image = BuildAbsolute(baseUrl, options.DefaultImageUrl);

        PageMetadata Make(string title, string description, string ogType = "website") => new() {
            Title = title,
            Description = description,
            CanonicalUrl = canonical,
            ImageUrl = image,
            OgType = ogType,
        };

        var defaultMetadata = Make(SiteTitle, SiteDescription);

        // Routes that require authentication inside the SPA must never expose specifics to the
        // anonymous /meta endpoint (crawlers, Discord). Serve generic site metadata for them.
        if (segments.Length > 0 && AuthorizedOnlyRoots.Contains(segments[0])) {
            return defaultMetadata;
        }

        // Entity routes.
        if (segments.Length == 2) {
            switch (segments[0].ToLowerInvariant()) {
                case "players" or "player" when int.TryParse(segments[1], out var playerId):
                    return await ResolvePlayerAsync(playerId, Make, defaultMetadata, cancellationToken);
                case "users" or "user":
                    return await ResolveUserAsync(segments[1], Make, defaultMetadata, cancellationToken);
                case "blogs":
                    return ResolveBlog(segments[1], Make, defaultMetadata);
            }
        }

        // Known static pages.
        var key = segments.Length == 0 ? "/" : "/" + string.Join('/', segments);
        if (StaticPages.TryGetValue(key, out var page)) {
            return Make(page.Title, page.Description);
        }

        return defaultMetadata;
    }

    private async Task<PageMetadata> ResolvePlayerAsync(
        int playerId,
        Func<string, string, string, PageMetadata> make,
        PageMetadata fallback,
        CancellationToken cancellationToken) {
        var player = await db.PlayerInformation
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Where(p => p.PlayerId == playerId)
            .Select(p => new { p.Name, p.Position, p.CurrentLeague, p.TotalTpe })
            .FirstOrDefaultAsync(cancellationToken);

        if (player is null) {
            return fallback;
        }

        var leaguePart = player.CurrentLeague is { } league ? $" in the {league}" : string.Empty;
        var position = player.Position.ToLongString();
        var title = $"{player.Name} — {position} · Shuttle";
        var description =
            $"{player.Name} is a {position}{leaguePart} with {player.TotalTpe:N0} TPE. " +
            "View their card, attributes, and progression on Shuttle.";
        return make(title, description, "profile");
    }

    private async Task<PageMetadata> ResolveUserAsync(
        string userIdOrName,
        Func<string, string, string, PageMetadata> make,
        PageMetadata fallback,
        CancellationToken cancellationToken) {
        var query = db.Users.AsNoTracking().IgnoreAutoIncludes();
        query = int.TryParse(userIdOrName, out var userId)
            ? query.Where(u => u.UserId == userId)
            : query.Where(u => u.Name == userIdOrName);

        var name = await query
            .Select(u => u.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (name is null) {
            return fallback;
        }

        var title = $"{name} — Member · Shuttle";
        var description = $"Player and activity profile for {name} on Shuttle, the SHL analysis site.";
        return make(title, description, "profile");
    }

    private PageMetadata ResolveBlog(
        string slug,
        Func<string, string, string, PageMetadata> make,
        PageMetadata fallback) {
        var entry = blogService.GetEntry(slug);
        return entry is null
            ? fallback
            : make(entry.Title, blogService.GetExcerpt(entry), "article");
    }

    private static string BuildCanonical(string baseUrl, string[] segments, string? queryString) {
        var path = segments.Length == 0 ? "/" : "/" + string.Join('/', segments);
        var canonical = baseUrl.TrimEnd('/') + path;
        if (!string.IsNullOrEmpty(queryString) && queryString != "?") {
            canonical += queryString;
        }

        return canonical;
    }

    private static string BuildAbsolute(string baseUrl, string url) {
        if (Uri.TryCreate(url, UriKind.Absolute, out _)) {
            return url;
        }

        return baseUrl.TrimEnd('/') + "/" + url.TrimStart('/');
    }
}
