using Shuttle.Fhm.Serde.Domain.Binary;
using Shuttle.Fhm.Serde.Wire.Tactics;

namespace Shuttle.Fhm.Serde.Domain.Files;

/// <summary>The documented header and unresolved records payload of <c>tactics.dat</c>.</summary>
public sealed class FhmTacticsFile : IFhmSaveFile
{
    /// <inheritdoc />
    public string RelativePath => "tactics.dat";

    /// <summary>Gets or sets the tactic catalogue version.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets the documented tactic record count.</summary>
    public int TacticCount { get; set; }

    /// <summary>Gets or sets exact opaque record bytes.</summary>
    public byte[] RecordsOpaque { get; set; } = [];

    internal static FhmTacticsFile Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        FhmTacticsFileData wire;
        try
        {
            wire = FhmTacticsFileSerializer.DeserializeHeader(stream);
        }
        catch (InvalidDataException exception)
        {
            throw new FhmFormatException(exception.Message);
        }

        if (wire.TacticCount < 0 || wire.TacticCount > 10_000_000)
        {
            throw new FhmFormatException($"Invalid tactics count {wire.TacticCount}.");
        }

        using var remaining = new MemoryStream();
        stream.CopyTo(remaining);
        return new FhmTacticsFile
        {
            Version = wire.Version,
            TacticCount = wire.TacticCount,
            RecordsOpaque = remaining.ToArray(),
        };
    }

    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (TacticCount < 0 || TacticCount > 10_000_000)
        {
            throw new FhmFormatException($"Invalid tactics count {TacticCount}.");
        }

        FhmTacticsFileSerializer.SerializeHeader(stream, new() { Version = Version, TacticCount = TacticCount });
        stream.Write(RecordsOpaque);
    }
}
