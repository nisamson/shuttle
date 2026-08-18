using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.Serde.Teams;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>The <c>teams.dat</c> container.</summary>
public sealed class FhmTeamsFile : IFhmSaveFile
{
    /// <inheritdoc />
    public string RelativePath => "teams.dat";

    /// <summary>Gets or sets the teams file format version.</summary>
    public int VersionTag { get; set; }

    /// <summary>Gets team records in serialized order.</summary>
    public IList<FhmTeamRecord> Teams { get; } = [];

    internal static FhmTeamsFile Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        try
        {
            return FhmTeamWireMapper.FromWire(FhmTeamsFileSerializer.Deserialize(stream));
        }
        catch (InvalidDataException exception)
        {
            throw new FhmFormatException(exception.Message);
        }
    }

    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        FhmTeamsFileSerializer.Serialize(stream, FhmTeamWireMapper.ToWire(this));
    }
}
