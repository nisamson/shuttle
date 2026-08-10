using Shuttle.Fhm.SaveData.Binary;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>The role catalogue in <c>player_roles.dat</c>.</summary>
public sealed class FhmPlayerRolesFile : IFhmSaveFile
{
    /// <inheritdoc />
    public string RelativePath => "player_roles.dat";

    /// <summary>Gets or sets the file format version.</summary>
    public int VersionTag { get; set; }

    /// <summary>Gets role records in serialized order.</summary>
    public IList<FhmPlayerRoleDefinition> Records { get; } = [];

    internal static FhmPlayerRolesFile Read(FhmBinaryReader reader)
    {
        var result = new FhmPlayerRolesFile { VersionTag = reader.ReadInt32() };
        var count = reader.ReadCount("player role records");
        for (var index = 0; index < count; index++)
        {
            result.Records.Add(FhmPlayerRoleDefinition.Read(reader));
        }

        reader.EnsureEof("player_roles.dat");
        return result;
    }

    /// <inheritdoc />
    public void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteInt32(VersionTag);
        writer.WriteCount(Records.Count, "player role records");
        foreach (var record in Records)
        {
            record.WriteTo(writer);
        }
    }
}

/// <summary>A role definition and its serialized requirement vectors.</summary>
public sealed class FhmPlayerRoleDefinition
{
    /// <summary>Gets or sets the role catalogue id.</summary>
    public int RoleId { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets the 8-value role requirement vector.</summary>
    public IList<int> WeightGroupA { get; } = new int[8];

    /// <summary>Gets the 13-value role requirement vector.</summary>
    public IList<int> WeightGroupB { get; } = new int[13];

    /// <summary>Gets the 17-value role requirement vector.</summary>
    public IList<int> WeightGroupC { get; } = new int[17];

    /// <summary>Gets the 4-value role requirement vector.</summary>
    public IList<int> WeightGroupD { get; } = new int[4];

    /// <summary>Gets or sets the forward applicability flag.</summary>
    public byte AppliesToForwards { get; set; }

    /// <summary>Gets or sets the defenceman applicability flag.</summary>
    public byte AppliesToDefencemen { get; set; }

    /// <summary>Gets or sets the goalie applicability flag.</summary>
    public byte AppliesToGoalies { get; set; }

    /// <summary>Gets or sets unclassified role flags.</summary>
    public byte RoleFlags { get; set; }

    /// <summary>Gets or sets the raw position category.</summary>
    public ushort PositionCategory { get; set; }

    /// <summary>Gets or sets the short display name.</summary>
    public string? ShortName { get; set; }

    /// <summary>Gets or sets tuning value A.</summary>
    public ushort TuningValueA { get; set; }

    /// <summary>Gets or sets tuning value B.</summary>
    public ushort TuningValueB { get; set; }

    /// <summary>Gets or sets the role description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets tuning value C.</summary>
    public ushort TuningValueC { get; set; }

    /// <summary>Gets the 19-value role requirement vector.</summary>
    public IList<int> WeightGroupE { get; } = new int[19];

    /// <summary>Gets the 9-value role requirement vector.</summary>
    public IList<int> WeightGroupF { get; } = new int[9];

    /// <summary>Gets four serialized index lists.</summary>
    public IList<IList<byte>> IndexLists { get; } = [[], [], [], []];

    internal static FhmPlayerRoleDefinition Read(FhmBinaryReader reader)
    {
        var result = new FhmPlayerRoleDefinition
        {
            RoleId = reader.ReadInt32(),
            Name = reader.ReadQString(),
        };
        ReadInts(reader, result.WeightGroupA);
        ReadInts(reader, result.WeightGroupB);
        ReadInts(reader, result.WeightGroupC);
        ReadInts(reader, result.WeightGroupD);
        result.AppliesToForwards = reader.ReadByte();
        result.AppliesToDefencemen = reader.ReadByte();
        result.AppliesToGoalies = reader.ReadByte();
        result.RoleFlags = reader.ReadByte();
        result.PositionCategory = reader.ReadUInt16();
        result.ShortName = reader.ReadQString();
        result.TuningValueA = reader.ReadUInt16();
        result.TuningValueB = reader.ReadUInt16();
        result.Description = reader.ReadQString();
        result.TuningValueC = reader.ReadUInt16();
        ReadInts(reader, result.WeightGroupE);
        ReadInts(reader, result.WeightGroupF);
        foreach (var list in result.IndexLists)
        {
            var count = reader.ReadCount("player role index list");
            for (var index = 0; index < count; index++)
            {
                list.Add(reader.ReadByte());
            }
        }

        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        ValidateLengths();
        writer.WriteInt32(RoleId);
        writer.WriteQString(Name);
        WriteInts(writer, WeightGroupA);
        WriteInts(writer, WeightGroupB);
        WriteInts(writer, WeightGroupC);
        WriteInts(writer, WeightGroupD);
        writer.WriteByte(AppliesToForwards);
        writer.WriteByte(AppliesToDefencemen);
        writer.WriteByte(AppliesToGoalies);
        writer.WriteByte(RoleFlags);
        writer.WriteUInt16(PositionCategory);
        writer.WriteQString(ShortName);
        writer.WriteUInt16(TuningValueA);
        writer.WriteUInt16(TuningValueB);
        writer.WriteQString(Description);
        writer.WriteUInt16(TuningValueC);
        WriteInts(writer, WeightGroupE);
        WriteInts(writer, WeightGroupF);
        foreach (var list in IndexLists)
        {
            writer.WriteCount(list.Count, "player role index list");
            foreach (var value in list)
            {
                writer.WriteByte(value);
            }
        }
    }

    private static void ReadInts(FhmBinaryReader reader, IList<int> destination)
    {
        for (var index = 0; index < destination.Count; index++)
        {
            destination[index] = reader.ReadInt32();
        }
    }

    private static void WriteInts(FhmBinaryWriter writer, IEnumerable<int> values)
    {
        foreach (var value in values)
        {
            writer.WriteInt32(value);
        }
    }

    private void ValidateLengths()
    {
        if (WeightGroupA.Count != 8 || WeightGroupB.Count != 13 || WeightGroupC.Count != 17 ||
            WeightGroupD.Count != 4 || WeightGroupE.Count != 19 || WeightGroupF.Count != 9 || IndexLists.Count != 4)
        {
            throw new FhmFormatException("player role definition contains a malformed fixed-size vector.");
        }
    }
}
