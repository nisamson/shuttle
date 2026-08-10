using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.SaveData.Model;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>The 1,303-byte team-owned tactical settings region within a team record.</summary>
public sealed class FhmTeamTacticsSettings
{
    /// <summary>Gets or sets the first team-owned tactical scalar.</summary>
    public ushort TeamValue1 { get; set; }

    /// <summary>Gets or sets the team-owned tactical flag.</summary>
    public byte TeamFlag { get; set; }

    /// <summary>Gets or sets the team-owned tactical rating.</summary>
    public byte TeamRating { get; set; }

    /// <summary>Gets or sets the three team-owned tactical scalars.</summary>
    public int[] TeamValues2To4 { get; } = new int[3];

    /// <summary>Gets or sets the nested tactics-object version.</summary>
    public int TacticsObjectVersion { get; set; }

    /// <summary>Gets 59 documented but not individually named base settings.</summary>
    public ushort[] BaseSettings { get; } = new ushort[59];

    /// <summary>Gets selector blocks: global at index zero followed by the 21 unit blocks.</summary>
    public IList<FhmZoneSelectorBlock> Selectors { get; } = CreateSelectors();

    /// <summary>Gets or sets the final offensive orientation raw value.</summary>
    public FhmEnumValue<FhmOffensiveOrientation> FinalOffensiveOrientation { get; set; }

    /// <summary>Gets or sets the final physical orientation raw value.</summary>
    public FhmEnumValue<FhmPhysicalOrientation> FinalPhysicalOrientation { get; set; }

    /// <summary>Gets the two final use-own-settings flags for selector blocks 20 and 21.</summary>
    public byte[] FinalUseOwnSettingsFlags { get; } = new byte[2];

    /// <summary>Gets the 22 tendency blocks in selector order.</summary>
    public IList<FhmTendencyBlock> Tendencies { get; } = CreateTendencies();

    /// <summary>Reads the exact team-tactics settings region from the current reader position.</summary>
    public static FhmTeamTacticsSettings ReadFrom(FhmBinaryReader reader)
    {
        var result = new FhmTeamTacticsSettings
        {
            TeamValue1 = reader.ReadUInt16(),
            TeamFlag = reader.ReadByte(),
            TeamRating = reader.ReadByte(),
        };
        ReadInts(reader, result.TeamValues2To4);
        result.TacticsObjectVersion = reader.ReadInt32();
        ReadUInt16s(reader, result.BaseSettings);
        for (var index = 0; index < result.Selectors.Count; index++)
        {
            result.Selectors[index].ReadFrom(reader);
        }

        result.FinalOffensiveOrientation = new FhmEnumValue<FhmOffensiveOrientation>(reader.ReadUInt16());
        result.FinalPhysicalOrientation = new FhmEnumValue<FhmPhysicalOrientation>(reader.ReadUInt16());
        ReadBytes(reader, result.FinalUseOwnSettingsFlags);
        foreach (var tendency in result.Tendencies)
        {
            tendency.ReadFrom(reader);
        }

        return result;
    }

    /// <summary>Writes the exact team-tactics settings region in FHM wire order.</summary>
    public void WriteTo(FhmBinaryWriter writer)
    {
        if (Selectors.Count != 22 || Tendencies.Count != 22)
        {
            throw new FhmFormatException("Team tactics requires exactly 22 selector and tendency blocks.");
        }

        for (var index = 0; index < Selectors.Count; index++)
        {
            if (Selectors[index].BlockIndex != index)
            {
                throw new FhmFormatException("Team tactic selector blocks must remain in their serialized order.");
            }
        }

        writer.WriteUInt16(TeamValue1);
        writer.WriteByte(TeamFlag);
        writer.WriteByte(TeamRating);
        WriteInts(writer, TeamValues2To4);
        writer.WriteInt32(TacticsObjectVersion);
        WriteUInt16s(writer, BaseSettings);
        foreach (var selector in Selectors)
        {
            selector.WriteTo(writer);
        }

        writer.WriteUInt16(FinalOffensiveOrientation.RawValue);
        writer.WriteUInt16(FinalPhysicalOrientation.RawValue);
        WriteBytes(writer, FinalUseOwnSettingsFlags);
        foreach (var tendency in Tendencies)
        {
            tendency.WriteTo(writer);
        }
    }

    private static IList<FhmZoneSelectorBlock> CreateSelectors()
    {
        var result = new List<FhmZoneSelectorBlock>(22);
        for (var index = 0; index < 22; index++)
        {
            result.Add(new FhmZoneSelectorBlock(index));
        }

        return result;
    }

    private static IList<FhmTendencyBlock> CreateTendencies()
    {
        var result = new List<FhmTendencyBlock>(22);
        for (var index = 0; index < 22; index++)
        {
            result.Add(new FhmTendencyBlock());
        }

        return result;
    }

    private static void ReadInts(FhmBinaryReader reader, int[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = reader.ReadInt32();
        }
    }

    private static void WriteInts(FhmBinaryWriter writer, IEnumerable<int> values)
    {
        foreach (var value in values)
        {
            writer.WriteInt32(value);
        }
    }

    private static void ReadUInt16s(FhmBinaryReader reader, ushort[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = reader.ReadUInt16();
        }
    }

    private static void WriteUInt16s(FhmBinaryWriter writer, IEnumerable<ushort> values)
    {
        foreach (var value in values)
        {
            writer.WriteUInt16(value);
        }
    }

    private static void ReadBytes(FhmBinaryReader reader, byte[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            values[index] = reader.ReadByte();
        }
    }

    private static void WriteBytes(FhmBinaryWriter writer, IEnumerable<byte> values)
    {
        foreach (var value in values)
        {
            writer.WriteByte(value);
        }
    }
}

/// <summary>A per-unit zone-system selection block.</summary>
public sealed class FhmZoneSelectorBlock
{
    /// <summary>Initializes a selector block at its documented ordinal.</summary>
    public FhmZoneSelectorBlock(int blockIndex)
    {
        if (blockIndex is < 0 or > 21)
        {
            throw new ArgumentOutOfRangeException(nameof(blockIndex));
        }

        BlockIndex = blockIndex;
        DelayedUseOwnSettingsFlags = new byte[GetDelayedFlagCount(blockIndex)];
    }

    /// <summary>Gets the global-or-unit selector ordinal.</summary>
    public int BlockIndex { get; }

    /// <summary>Gets twelve selected team-tactics catalogue system ids.</summary>
    public ushort[] SystemIds { get; } = new ushort[12];

    /// <summary>Gets or sets the unit orientation, except in the final selector block.</summary>
    public FhmEnumValue<FhmOffensiveOrientation>? OffensiveOrientation { get; set; }

    /// <summary>Gets or sets the unit orientation, except in the final selector block.</summary>
    public FhmEnumValue<FhmPhysicalOrientation>? PhysicalOrientation { get; set; }

    /// <summary>Gets delayed use-own-settings flags emitted after specific selector groups.</summary>
    public byte[] DelayedUseOwnSettingsFlags { get; }

    internal void ReadFrom(FhmBinaryReader reader)
    {
        for (var index = 0; index < SystemIds.Length; index++)
        {
            SystemIds[index] = reader.ReadUInt16();
        }

        if (BlockIndex != 21)
        {
            OffensiveOrientation = new FhmEnumValue<FhmOffensiveOrientation>(reader.ReadUInt16());
            PhysicalOrientation = new FhmEnumValue<FhmPhysicalOrientation>(reader.ReadUInt16());
        }

        for (var index = 0; index < DelayedUseOwnSettingsFlags.Length; index++)
        {
            DelayedUseOwnSettingsFlags[index] = reader.ReadByte();
        }
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        foreach (var systemId in SystemIds)
        {
            writer.WriteUInt16(systemId);
        }

        if (BlockIndex != 21)
        {
            if (OffensiveOrientation is not { } offensive || PhysicalOrientation is not { } physical)
            {
                throw new FhmFormatException($"Selector block {BlockIndex} requires both orientations.");
            }

            writer.WriteUInt16(offensive.RawValue);
            writer.WriteUInt16(physical.RawValue);
        }

        foreach (var flag in DelayedUseOwnSettingsFlags)
        {
            writer.WriteByte(flag);
        }
    }

    private static int GetDelayedFlagCount(int blockIndex) => blockIndex switch
    {
        4 => 4,
        13 => 3,
        6 or 8 or 10 or 15 or 17 or 19 => 2,
        _ => 0,
    };
}

/// <summary>Eight tactical tendency values and their per-unit override flags.</summary>
public sealed class FhmTendencyBlock
{
    /// <summary>Gets tendency values ordered Aggressiveness through Tempo.</summary>
    public ushort[] Values { get; } = new ushort[8];

    /// <summary>Gets matching override flags ordered Aggressiveness through Tempo.</summary>
    public byte[] Overrides { get; } = new byte[8];

    internal void ReadFrom(FhmBinaryReader reader)
    {
        for (var index = 0; index < Values.Length; index++)
        {
            Values[index] = reader.ReadUInt16();
        }

        for (var index = 0; index < Overrides.Length; index++)
        {
            Overrides[index] = reader.ReadByte();
        }
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        foreach (var value in Values)
        {
            writer.WriteUInt16(value);
        }

        foreach (var flag in Overrides)
        {
            writer.WriteByte(flag);
        }
    }
}
