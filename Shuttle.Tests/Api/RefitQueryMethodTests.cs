using System.Net;
using System.Text;
using System.Text.Json;
using Refit;
using Shuttle.Api.Client;
using Shuttle.Models.Players;

namespace Shuttle.Tests.Api;

/// <summary>
/// Verifies the Refit client actually emits the (non-standard) HTTP <c>QUERY</c> verb for
/// <see cref="IShuttlePlayerClient.ResolvePlayers"/>, with the request serialized into the body —
/// the fake-backend tests exercise the resolution semantics but never the real HTTP method, so this
/// guards the custom <see cref="HttpQueryAttribute"/> wiring.
/// </summary>
public class RefitQueryMethodTests {
    private sealed class CapturingHandler : HttpMessageHandler {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) {
            Request = request;
            if (request.Content is not null) {
                Body = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            var payload = JsonSerializer.Serialize(
                new ResolvePlayersResult { Resolved = [], NotFound = [], Ambiguous = [] });
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
        }
    }

    [Fact]
    public async Task ResolvePlayers_sends_QUERY_verb_with_json_body() {
        var handler = new CapturingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var settings = new RefitSettings {
            ContentSerializer = new SystemTextJsonContentSerializer(ShuttleApiClientExtensions.JsonSerializerOptions),
        };
        var client = RestService.For<IShuttlePlayerClient>(http, settings);

        await client.ResolvePlayers(
            new ResolvePlayersRequest { PlayerIds = [1001], Names = ["Aaron Frost"] },
            TestContext.Current.CancellationToken);

        Assert.NotNull(handler.Request);
        Assert.Equal("QUERY", handler.Request!.Method.Method);
        Assert.Equal("/players/resolve", handler.Request.RequestUri!.AbsolutePath);
        Assert.NotNull(handler.Body);
        Assert.Contains("playerIds", handler.Body!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Aaron Frost", handler.Body!, StringComparison.OrdinalIgnoreCase);
    }
}
