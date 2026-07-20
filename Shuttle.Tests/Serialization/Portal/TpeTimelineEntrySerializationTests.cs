using System.Text.Json;
using Shuttle.Shl.Api.Client;
using Shuttle.Shl.Api.Models.Portal.V1;

namespace Shuttle.Tests.Serialization.Portal;

/// <summary>
/// Deserialization tests for <see cref="TpeTimelineEntry"/> against the shape returned by the portal
/// <c>GET /tpeevents/timeline?pid={pid}</c> endpoint, using the client's
/// <see cref="ShlConstants.JsonSerializerOptions"/>. Guards the case-insensitive mapping of the
/// upstream <c>totalTPE</c> property onto <see cref="TpeTimelineEntry.TotalTpe"/>.
/// </summary>
public class TpeTimelineEntrySerializationTests {
    private const string Json = """
        [
            {"name":"Mikko Rashford II","taskDate":"2025-03-17T02:09:05.000Z","totalTPE":155},
            {"name":"Mikko Rashford II","taskDate":"2025-03-17T02:38:03.000Z","totalTPE":157},
            {"name":"Mikko Rashford II","taskDate":"2025-06-10T21:25:47.000Z","totalTPE":376}
        ]
        """;

    [Fact]
    public void DeserializesTimelineEntries() {
        var entries = JsonSerializer.Deserialize<List<TpeTimelineEntry>>(Json, ShlConstants.JsonSerializerOptions);

        Assert.NotNull(entries);
        Assert.Equal(3, entries!.Count);

        var first = entries[0];
        Assert.Equal("Mikko Rashford II", first.Name);
        Assert.Equal(155, first.TotalTpe);
        Assert.Equal(
            new DateTime(2025, 3, 17, 2, 9, 5, DateTimeKind.Utc),
            first.TaskDate.ToUniversalTime());

        // TPE can decrease between entries (e.g. redistribution), so the sequence is not monotonic.
        Assert.Equal(376, entries[2].TotalTpe);
    }
}
