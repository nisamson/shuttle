using Shuttle.Fhm.Serde.Domain.Binary;
using Shuttle.BinarySerde.Common.QFormat;
using Shuttle.Fhm.Serde.Wire.PlayerRoles;

namespace Shuttle.Fhm.Serde.Domain.Files;

/// <summary>The role catalogue in <c>player_roles.dat</c>.</summary>
public sealed class FhmPlayerRolesFile : IFhmSaveFile
{
    internal const int MaximumCollectionCount = 10_000_000;

    /// <inheritdoc />
    public string RelativePath => "player_roles.dat";

    /// <summary>Gets or sets the file format version.</summary>
    public int VersionTag { get; set; }

    /// <summary>Gets role records in serialized order.</summary>
    public IList<FhmPlayerRoleDefinition> Records { get; } = [];

    internal static FhmPlayerRolesFile Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        FhmPlayerRolesFileData wire;
        try
        {
            wire = FhmPlayerRolesFileSerializer.Deserialize(stream);
        }
        catch (InvalidDataException exception)
        {
            throw new FhmFormatException(exception.Message);
        }

        ValidateWire(wire);
        var result = new FhmPlayerRolesFile { VersionTag = wire.VersionTag };
        foreach (var record in wire.Records)
        {
            result.Records.Add(FhmPlayerRoleDefinition.FromWire(record));
        }

        return result;
    }

    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ValidateForWrite();
        FhmPlayerRolesFileSerializer.Serialize(stream, new FhmPlayerRolesFileData
        {
            VersionTag = VersionTag,
            RecordCount = Records.Count,
            Records = Records.Select(FhmPlayerRoleDefinition.ToWire).ToList(),
        });
    }

    private static void ValidateWire(FhmPlayerRolesFileData wire)
    {
        if (wire.RecordCount < 0 || wire.RecordCount > MaximumCollectionCount || wire.Records.Count != wire.RecordCount)
        {
            throw new FhmFormatException($"Invalid player role records count {wire.RecordCount}.");
        }

        foreach (var record in wire.Records)
        {
            FhmPlayerRoleDefinition.ValidateWire(record);
        }
    }

    private void ValidateForWrite()
    {
        if (Records.Count > MaximumCollectionCount)
        {
            throw new FhmFormatException($"Invalid player role records count {Records.Count}.");
        }

        foreach (var record in Records)
        {
            ArgumentNullException.ThrowIfNull(record);
            record.ValidateForWrite();
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

    internal static FhmPlayerRoleDefinition FromWire(FhmPlayerRoleDefinitionData wire)
    {
        ArgumentNullException.ThrowIfNull(wire);
        var result = new FhmPlayerRoleDefinition
        {
            RoleId = wire.RoleId,
            Name = wire.Name.Value,
            AppliesToForwards = wire.AppliesToForwards,
            AppliesToDefencemen = wire.AppliesToDefencemen,
            AppliesToGoalies = wire.AppliesToGoalies,
            RoleFlags = wire.RoleFlags,
            PositionCategory = wire.PositionCategory,
            ShortName = wire.ShortName.Value,
            TuningValueA = wire.TuningValueA,
            TuningValueB = wire.TuningValueB,
            Description = wire.Description.Value,
            TuningValueC = wire.TuningValueC,
        };

        CopyList(wire.WeightGroupA, result.WeightGroupA);
        CopyList(wire.WeightGroupB, result.WeightGroupB);
        CopyList(wire.WeightGroupC, result.WeightGroupC);
        CopyList(wire.WeightGroupD, result.WeightGroupD);
        CopyList(wire.WeightGroupE, result.WeightGroupE);
        CopyList(wire.WeightGroupF, result.WeightGroupF);
        CopyIndexList(wire.IndexListA, result.IndexLists[0]);
        CopyIndexList(wire.IndexListB, result.IndexLists[1]);
        CopyIndexList(wire.IndexListC, result.IndexLists[2]);
        CopyIndexList(wire.IndexListD, result.IndexLists[3]);
        return result;
    }

    internal static FhmPlayerRoleDefinitionData ToWire(FhmPlayerRoleDefinition value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new()
        {
            RoleId = value.RoleId,
            Name = new QString { Value = value.Name },
            WeightGroupA = value.WeightGroupA.ToList(),
            WeightGroupB = value.WeightGroupB.ToList(),
            WeightGroupC = value.WeightGroupC.ToList(),
            WeightGroupD = value.WeightGroupD.ToList(),
            AppliesToForwards = value.AppliesToForwards,
            AppliesToDefencemen = value.AppliesToDefencemen,
            AppliesToGoalies = value.AppliesToGoalies,
            RoleFlags = value.RoleFlags,
            PositionCategory = value.PositionCategory,
            ShortName = new QString { Value = value.ShortName },
            TuningValueA = value.TuningValueA,
            TuningValueB = value.TuningValueB,
            Description = new QString { Value = value.Description },
            TuningValueC = value.TuningValueC,
            WeightGroupE = value.WeightGroupE.ToList(),
            WeightGroupF = value.WeightGroupF.ToList(),
            IndexListA = ToWireIndexList(value.IndexLists[0]),
            IndexListB = ToWireIndexList(value.IndexLists[1]),
            IndexListC = ToWireIndexList(value.IndexLists[2]),
            IndexListD = ToWireIndexList(value.IndexLists[3]),
        };
    }

    internal static void ValidateWire(FhmPlayerRoleDefinitionData wire)
    {
        ArgumentNullException.ThrowIfNull(wire);
        ValidateFixedLengths(
            wire.WeightGroupA,
            wire.WeightGroupB,
            wire.WeightGroupC,
            wire.WeightGroupD,
            wire.WeightGroupE,
            wire.WeightGroupF);
        ValidateWireIndexList(wire.IndexListA);
        ValidateWireIndexList(wire.IndexListB);
        ValidateWireIndexList(wire.IndexListC);
        ValidateWireIndexList(wire.IndexListD);
    }

    internal void ValidateForWrite()
    {
        ValidateLengths();
        foreach (var list in IndexLists)
        {
            ArgumentNullException.ThrowIfNull(list);
            ValidateIndexListCount(list.Count);
        }
    }

    private static void CopyList(IReadOnlyList<int> source, IList<int> destination)
    {
        for (var index = 0; index < destination.Count; index++)
        {
            destination[index] = source[index];
        }
    }

    private static void CopyIndexList(QList<byte> source, ICollection<byte> destination)
    {
        foreach (var value in source.Items)
        {
            destination.Add(value);
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

    private static void ValidateFixedLengths(
        IReadOnlyCollection<int> weightGroupA,
        IReadOnlyCollection<int> weightGroupB,
        IReadOnlyCollection<int> weightGroupC,
        IReadOnlyCollection<int> weightGroupD,
        IReadOnlyCollection<int> weightGroupE,
        IReadOnlyCollection<int> weightGroupF)
    {
        if (weightGroupA.Count != FhmPlayerRoleDefinitionData.WeightGroupACount ||
            weightGroupB.Count != FhmPlayerRoleDefinitionData.WeightGroupBCount ||
            weightGroupC.Count != FhmPlayerRoleDefinitionData.WeightGroupCCount ||
            weightGroupD.Count != FhmPlayerRoleDefinitionData.WeightGroupDCount ||
            weightGroupE.Count != FhmPlayerRoleDefinitionData.WeightGroupECount ||
            weightGroupF.Count != FhmPlayerRoleDefinitionData.WeightGroupFCount)
        {
            throw new FhmFormatException("player role definition contains a malformed fixed-size vector.");
        }
    }

    private static QList<byte> ToWireIndexList(IList<byte> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        ValidateIndexListCount(values.Count);
        return new() { Length = values.Count, Items = values.ToList() };
    }

    private static void ValidateWireIndexList(QList<byte> list)
    {
        if (list is null || list.Length < 0 || list.Length > FhmPlayerRolesFile.MaximumCollectionCount || list.Items.Count != list.Length)
        {
            throw new FhmFormatException($"Invalid player role index list count {list?.Length}.");
        }
    }

    private static void ValidateIndexListCount(int count)
    {
        if (count > FhmPlayerRolesFile.MaximumCollectionCount)
        {
            throw new FhmFormatException($"Invalid player role index list count {count}.");
        }
    }
}
