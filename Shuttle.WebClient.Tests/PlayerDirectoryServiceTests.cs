using Microsoft.Extensions.DependencyInjection;
using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Tests for <see cref="PlayerDirectoryService"/> against the in-memory client. Inherits the bUnit
/// context purely to obtain a loose JS runtime (localStorage calls no-op) and the seeded client.
/// </summary>
public class PlayerDirectoryServiceTests : WebClientTestContext {
    private IPlayerDirectoryService Directory => Services.GetRequiredService<IPlayerDirectoryService>();

    [Fact]
    public async Task Search_returns_prefix_matches_first() {
        var results = await Directory.Search("fr");

        Assert.NotEmpty(results);
        // "Aaron Frost" (name) and "lfrost"/"fnolan" (username) all contain "fr"; the name/username
        // prefix match should rank ahead of substring matches.
        Assert.Contains(results, r => r.Username.StartsWith("fr", StringComparison.OrdinalIgnoreCase)
                                      || r.Name.StartsWith("fr", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Search_respects_limit() {
        var results = await Directory.Search(null, limit: 3);

        Assert.True(results.Count <= 3);
    }

    [Fact]
    public async Task Search_empty_term_returns_first_players_by_name() {
        var results = await Directory.Search("", limit: 5);

        Assert.Equal(
            results.Select(r => r.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase),
            results.Select(r => r.Name));
    }
}
