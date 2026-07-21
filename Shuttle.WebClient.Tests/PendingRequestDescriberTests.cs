using Shuttle.WebClient.Services;

namespace Shuttle.WebClient.Tests;

/// <summary>
/// Unit tests for <see cref="PendingRequestDescriber"/> — the mapping from an outgoing backend request
/// to the short text shown in the pending-request indicator.
/// </summary>
public class PendingRequestDescriberTests {
    private static readonly HttpMethod Query = new("QUERY");

    private static string Describe(HttpMethod method, string path) =>
        PendingRequestDescriber.Describe(method, new Uri("http://localhost" + path));

    [Fact]
    public void Player_lookup_has_bespoke_wording() {
        Assert.Equal("Looking up players…", Describe(Query, "/players/lookup"));
    }

    [Fact]
    public void Player_suggestions_has_bespoke_wording() {
        Assert.Equal("Loading player directory…", Describe(HttpMethod.Get, "/players/suggestions"));
    }

    [Fact]
    public void Player_search_has_bespoke_wording() {
        Assert.Equal("Searching players…", Describe(HttpMethod.Get, "/players/search"));
    }

    [Fact]
    public void Get_a_player_falls_back_to_loading_the_resource() {
        Assert.Equal("Loading players…", Describe(HttpMethod.Get, "/player/1042"));
    }

    [Fact]
    public void Post_to_scouting_is_a_save() {
        Assert.Equal("Saving scouting board…", Describe(HttpMethod.Post, "/scouting/boards/abc/entries"));
    }

    [Fact]
    public void Delete_is_a_removal() {
        Assert.Equal("Removing scouting board…", Describe(HttpMethod.Delete, "/scouting/boards/abc/entries/5"));
    }

    [Fact]
    public void Seasons_map_to_the_league_resource() {
        Assert.Equal("Loading league…", Describe(HttpMethod.Get, "/seasons/current"));
    }

    [Fact]
    public void Unknown_resource_falls_back_to_generic_data() {
        Assert.Equal("Loading data…", Describe(HttpMethod.Get, "/"));
    }
}
