using Shuttle.Fhm.SaveData.Binary;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>The documented single-record prefix and opaque remainder of <c>leagues.dat</c>.</summary>
public sealed class FhmLeaguesFile : IFhmSaveFile
{
    /// <inheritdoc />
    public string RelativePath => "leagues.dat";

    /// <summary>Gets or sets the container version tag.</summary>
    public int VersionTag { get; set; }

    /// <summary>Gets or sets the known league record.</summary>
    public FhmLeagueRecord League { get; set; } = new();

    internal static FhmLeaguesFile Read(FhmBinaryReader reader)
    {
        var result = new FhmLeaguesFile { VersionTag = reader.ReadInt32() };
        var count = reader.ReadCount("league records");
        if (count != 1)
        {
            throw new FhmFormatException($"leagues.dat has {count} records; only the documented single-record boundary is supported.");
        }

        var league = new FhmLeagueRecord
        {
            LeagueId = reader.ReadInt32(),
            Flag0 = reader.ReadByte(),
            Flag1 = reader.ReadByte(),
            Flag2 = reader.ReadByte(),
            Name = reader.ReadQString(),
            ShortName = reader.ReadQString(),
            Abbreviation = reader.ReadQString(),
            Nickname = reader.ReadQString(),
            TypeParentId = reader.ReadUInt16(),
            TypeLevelId = reader.ReadUInt16(),
            ConfigDouble0 = reader.ReadDouble(),
            EarlyInt0 = reader.ReadInt32(),
            EarlyInt1 = reader.ReadInt32(),
        };
        for (var index = 0; index < league.ConfigDoublesPrimary.Length; index++)
        {
            league.ConfigDoublesPrimary[index] = reader.ReadDouble();
        }

        league.ConfigInt0 = reader.ReadInt32();
        league.ConfigInt1 = reader.ReadInt32();
        league.ConfigUInt160 = reader.ReadUInt16();
        league.ConfigInt2 = reader.ReadInt32();
        league.ConfigUInt161 = reader.ReadUInt16();
        league.FoundingDate = reader.ReadDate();
        league.OpaqueLeagueBody = reader.ReadRemaining();
        result.League = league;
        return result;
    }

    /// <inheritdoc />
    public void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteInt32(VersionTag);
        writer.WriteInt32(1);
        var league = League;
        writer.WriteInt32(league.LeagueId);
        writer.WriteByte(league.Flag0);
        writer.WriteByte(league.Flag1);
        writer.WriteByte(league.Flag2);
        writer.WriteQString(league.Name);
        writer.WriteQString(league.ShortName);
        writer.WriteQString(league.Abbreviation);
        writer.WriteQString(league.Nickname);
        writer.WriteUInt16(league.TypeParentId);
        writer.WriteUInt16(league.TypeLevelId);
        writer.WriteDouble(league.ConfigDouble0);
        writer.WriteInt32(league.EarlyInt0);
        writer.WriteInt32(league.EarlyInt1);
        if (league.ConfigDoublesPrimary.Length != 17)
        {
            throw new FhmFormatException("leagues.dat requires 17 primary configuration doubles.");
        }

        foreach (var value in league.ConfigDoublesPrimary)
        {
            writer.WriteDouble(value);
        }

        writer.WriteInt32(league.ConfigInt0);
        writer.WriteInt32(league.ConfigInt1);
        writer.WriteUInt16(league.ConfigUInt160);
        writer.WriteInt32(league.ConfigInt2);
        writer.WriteUInt16(league.ConfigUInt161);
        writer.WriteDate(league.FoundingDate);
        writer.WriteOpaqueBytes(new FhmOpaqueBytes(league.OpaqueLeagueBody));
    }
}

/// <summary>The structurally known <c>leagues.dat</c> record prefix.</summary>
public sealed class FhmLeagueRecord
{
    /// <summary>Gets or sets the league identity.</summary>
    public int LeagueId { get; set; }

    /// <summary>Gets or sets the first serialized league flag.</summary>
    public byte Flag0 { get; set; }

    /// <summary>Gets or sets the second serialized league flag.</summary>
    public byte Flag1 { get; set; }

    /// <summary>Gets or sets the third serialized league flag.</summary>
    public byte Flag2 { get; set; }

    /// <summary>Gets or sets the full league name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the short league name.</summary>
    public string? ShortName { get; set; }

    /// <summary>Gets or sets the league abbreviation.</summary>
    public string? Abbreviation { get; set; }

    /// <summary>Gets or sets the league nickname.</summary>
    public string? Nickname { get; set; }

    /// <summary>Gets or sets the type or parent identity.</summary>
    public ushort TypeParentId { get; set; }

    /// <summary>Gets or sets the type or level identity.</summary>
    public ushort TypeLevelId { get; set; }

    /// <summary>Gets or sets the first opaque configuration double.</summary>
    public double ConfigDouble0 { get; set; }

    /// <summary>Gets or sets the first early scalar.</summary>
    public int EarlyInt0 { get; set; }

    /// <summary>Gets or sets the second early scalar.</summary>
    public int EarlyInt1 { get; set; }

    /// <summary>Gets 17 documented but unclassified configuration doubles.</summary>
    public double[] ConfigDoublesPrimary { get; } = new double[17];

    /// <summary>Gets or sets an opaque configuration scalar.</summary>
    public int ConfigInt0 { get; set; }

    /// <summary>Gets or sets an opaque configuration scalar.</summary>
    public int ConfigInt1 { get; set; }

    /// <summary>Gets or sets an opaque unsigned configuration scalar.</summary>
    public ushort ConfigUInt160 { get; set; }

    /// <summary>Gets or sets an opaque configuration scalar.</summary>
    public int ConfigInt2 { get; set; }

    /// <summary>Gets or sets an opaque unsigned configuration scalar.</summary>
    public ushort ConfigUInt161 { get; set; }

    /// <summary>Gets or sets the founding or start date.</summary>
    public FhmDate FoundingDate { get; set; }

    /// <summary>Gets or sets the exact unresolved remainder of the only league record.</summary>
    public byte[] OpaqueLeagueBody { get; set; } = [];
}
