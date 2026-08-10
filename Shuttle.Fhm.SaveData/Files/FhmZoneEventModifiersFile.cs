using Shuttle.Fhm.SaveData.Binary;

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

    internal static FhmZoneEventModifiersFile Read(FhmBinaryReader reader)
    {
        var result = new FhmZoneEventModifiersFile
        {
            Version = reader.ReadInt32(),
            ZoneCount = reader.ReadCount("zone-event modifier groups"),
        };
        while (reader.Remaining > 0)
        {
            var length = reader.ReadInt32();
            if (length < 0)
            {
                throw new FhmFormatException("zone_event_mod.dat contains a negative modifier-grid length.");
            }

            result.ModifierGrids.Add(new FhmLengthPrefixedBlock(reader.ReadOpaqueBytes(length).Value));
        }

        return result;
    }

    /// <inheritdoc />
    public void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteInt32(Version);
        writer.WriteCount(ZoneCount, "zone-event modifier groups");
        foreach (var grid in ModifierGrids)
        {
            writer.WriteCount(grid.Data.Length, "zone-event modifier grid bytes");
            writer.WriteOpaqueBytes(new FhmOpaqueBytes(grid.Data));
        }
    }
}
