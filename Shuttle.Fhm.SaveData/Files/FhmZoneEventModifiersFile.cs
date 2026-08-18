using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.Serde.ZoneEvents;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>The header and length-prefixed modifier grids in <c>zone_event_mod.dat</c>.</summary>
public sealed class FhmZoneEventModifiersFile : IFhmSaveFile
{
    /// <inheritdoc />
    public string RelativePath => "zone_event_mod.dat";

    /// <summary>Gets or sets the file version.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets the documented zone group count.</summary>
    public int ZoneCount { get; set; }

    /// <summary>Gets length-prefixed modifier grids to EOF.</summary>
    public IList<FhmLengthPrefixedBlock> ModifierGrids { get; } = [];

    internal static FhmZoneEventModifiersFile Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        FhmZoneEventModifiersFileData header;
        try
        {
            header = FhmZoneEventModifiersFileSerializer.DeserializeHeader(stream);
        }
        catch (InvalidDataException exception)
        {
            throw new FhmFormatException(exception.Message);
        }

        var result = new FhmZoneEventModifiersFile
        {
            Version = header.Version,
            ZoneCount = header.ZoneCount,
        };
        if (result.ZoneCount < 0 || result.ZoneCount > 10_000_000)
        {
            throw new FhmFormatException($"Invalid zone-event modifier groups count {result.ZoneCount}.");
        }

        while (!stream.CanSeek || stream.Position < stream.Length)
        {
            FhmZoneEventModifierGridData grid;
            try
            {
                grid = FhmZoneEventModifiersFileSerializer.DeserializeGrid(stream);
            }
            catch (InvalidDataException exception)
            {
                throw new FhmFormatException(exception.Message);
            }

            if (grid.ByteLength < 0)
            {
                throw new FhmFormatException("zone_event_mod.dat contains a negative modifier-grid length.");
            }

            result.ModifierGrids.Add(new FhmLengthPrefixedBlock(grid.Data));
        }

        return result;
    }

    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (ZoneCount < 0 || ZoneCount > 10_000_000)
        {
            throw new FhmFormatException($"Invalid zone-event modifier groups count {ZoneCount}.");
        }

        FhmZoneEventModifiersFileSerializer.SerializeHeader(stream, new() { Version = Version, ZoneCount = ZoneCount });
        foreach (var grid in ModifierGrids)
        {
            ArgumentNullException.ThrowIfNull(grid);
            FhmZoneEventModifiersFileSerializer.SerializeGrid(stream, new()
            {
                ByteLength = grid.Data.Length,
                Data = grid.Data,
            });
        }
    }
}
