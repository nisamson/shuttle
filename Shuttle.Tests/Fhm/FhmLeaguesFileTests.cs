using Shuttle.BinarySerde.Common.QFormat;
using Shuttle.Fhm.Serde.Domain.Files;
using Shuttle.Fhm.Serde.Wire.Leagues;

namespace Shuttle.Tests.Fhm;

public sealed class FhmLeaguesFileTests
{
    [Fact]
    public void LeaguesFile_PreservesOpaqueBodyAfterBufferedDirectCodecRead()
    {
        using var encoded = new MemoryStream();
        FhmLeaguesFileSerializer.SerializeHeader(encoded, new() { VersionTag = 4, RecordCount = 1 });
        FhmLeaguesFileSerializer.SerializeRecord(encoded, new()
        {
            LeagueId = 15,
            Name = new QString { Value = "Synthetic Hockey League" },
            ConfigDoublesPrimary = Enumerable.Range(0, 17).Select(index => (double)index).ToList(),
        });
        encoded.Write([0xDE, 0xAD, 0xBE, 0xEF]);

        using var source = new MemoryStream(encoded.ToArray(), writable: false);
        var decoded = FhmLeaguesFile.Read(source);
        using var rewritten = new MemoryStream();
        decoded.WriteTo(rewritten);

        Assert.Equal([0xDE, 0xAD, 0xBE, 0xEF], decoded.League.OpaqueLeagueBody);
        Assert.Equal(encoded.ToArray(), rewritten.ToArray());
    }
}
