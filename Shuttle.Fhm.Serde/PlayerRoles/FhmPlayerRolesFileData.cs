using BinarySerialization;
using Shuttle.BinarySerde.Common.QFormat;

namespace Shuttle.Fhm.Serde.PlayerRoles;

/// <summary>The declarative <c>player_roles.dat</c> wire contract.</summary>
public sealed class FhmPlayerRolesFileData
{
    [FieldOrder(0)]
    public int VersionTag { get; set; }

    [FieldOrder(1)]
    public int RecordCount { get; set; }

    [FieldOrder(2)]
    [FieldCount(nameof(RecordCount))]
    public List<FhmPlayerRoleDefinitionData> Records { get; set; } = [];
}

/// <summary>One player-role definition and its fixed-order requirement vectors.</summary>
public sealed class FhmPlayerRoleDefinitionData
{
    public const int WeightGroupACount = 8;
    public const int WeightGroupBCount = 13;
    public const int WeightGroupCCount = 17;
    public const int WeightGroupDCount = 4;
    public const int WeightGroupECount = 19;
    public const int WeightGroupFCount = 9;

    [FieldOrder(0)]
    public int RoleId { get; set; }

    [FieldOrder(1)]
    public QString Name { get; set; } = new();

    [FieldOrder(2)]
    [FieldCount(WeightGroupACount)]
    public List<int> WeightGroupA { get; set; } = [];

    [FieldOrder(3)]
    [FieldCount(WeightGroupBCount)]
    public List<int> WeightGroupB { get; set; } = [];

    [FieldOrder(4)]
    [FieldCount(WeightGroupCCount)]
    public List<int> WeightGroupC { get; set; } = [];

    [FieldOrder(5)]
    [FieldCount(WeightGroupDCount)]
    public List<int> WeightGroupD { get; set; } = [];

    [FieldOrder(6)]
    public byte AppliesToForwards { get; set; }

    [FieldOrder(7)]
    public byte AppliesToDefencemen { get; set; }

    [FieldOrder(8)]
    public byte AppliesToGoalies { get; set; }

    [FieldOrder(9)]
    public byte RoleFlags { get; set; }

    [FieldOrder(10)]
    public ushort PositionCategory { get; set; }

    [FieldOrder(11)]
    public QString ShortName { get; set; } = new();

    [FieldOrder(12)]
    public ushort TuningValueA { get; set; }

    [FieldOrder(13)]
    public ushort TuningValueB { get; set; }

    [FieldOrder(14)]
    public QString Description { get; set; } = new();

    [FieldOrder(15)]
    public ushort TuningValueC { get; set; }

    [FieldOrder(16)]
    [FieldCount(WeightGroupECount)]
    public List<int> WeightGroupE { get; set; } = [];

    [FieldOrder(17)]
    [FieldCount(WeightGroupFCount)]
    public List<int> WeightGroupF { get; set; } = [];

    [FieldOrder(18)]
    public QList<byte> IndexListA { get; set; } = new();

    [FieldOrder(19)]
    public QList<byte> IndexListB { get; set; } = new();

    [FieldOrder(20)]
    public QList<byte> IndexListC { get; set; } = new();

    [FieldOrder(21)]
    public QList<byte> IndexListD { get; set; } = new();
}

/// <summary>Serializes complete <c>player_roles.dat</c> streams.</summary>
public static class FhmPlayerRolesFileSerializer
{
    /// <summary>Deserializes a complete <c>player_roles.dat</c> stream.</summary>
    public static FhmPlayerRolesFileData Deserialize(Stream stream) =>
        QSerializerFactory.Deserialize<FhmPlayerRolesFileData>(stream, "player_roles.dat");

    /// <summary>Serializes a <c>player_roles.dat</c> stream.</summary>
    public static void Serialize(Stream stream, FhmPlayerRolesFileData value) =>
        QSerializerFactory.Serialize(stream, value);
}
