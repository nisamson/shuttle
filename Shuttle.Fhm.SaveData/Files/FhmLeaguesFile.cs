using Shuttle.BinarySerde.Common.QFormat;
using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.Serde.Leagues;

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

    internal static FhmLeaguesFile Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        FhmLeaguesFileHeaderData header;
        FhmLeagueRecordData wire;
        try
        {
            header = FhmLeaguesFileSerializer.DeserializeHeader(stream);
            wire = FhmLeaguesFileSerializer.DeserializeRecord(stream);
        }
        catch (InvalidDataException exception)
        {
            throw new FhmFormatException(exception.Message);
        }

        if (header.RecordCount != 1)
        {
            throw new FhmFormatException($"leagues.dat has {header.RecordCount} records; only the documented single-record boundary is supported.");
        }

        if (wire.ConfigDoublesPrimary.Count != 17)
        {
            throw new FhmFormatException("leagues.dat requires 17 primary configuration doubles.");
        }

        var league = new FhmLeagueRecord
        {
            LeagueId = wire.LeagueId,
            Flag0 = wire.Flag0,
            Flag1 = wire.Flag1,
            Flag2 = wire.Flag2,
            Name = wire.Name.Value,
            ShortName = wire.ShortName.Value,
            Abbreviation = wire.Abbreviation.Value,
            Nickname = wire.Nickname.Value,
            TypeParentId = wire.TypeParentId,
            TypeLevelId = wire.TypeLevelId,
            ConfigDouble0 = wire.ConfigDouble0,
            EarlyInt0 = wire.EarlyInt0,
            EarlyInt1 = wire.EarlyInt1,
            ConfigInt0 = wire.ConfigInt0,
            ConfigInt1 = wire.ConfigInt1,
            ConfigUInt160 = wire.ConfigUInt160,
            ConfigInt2 = wire.ConfigInt2,
            ConfigUInt161 = wire.ConfigUInt161,
            FoundingDate = new FhmDate(wire.FoundingDate.Year, wire.FoundingDate.Month, wire.FoundingDate.Day),
            OpaqueLeagueBody = ReadRemaining(stream),
        };

        wire.ConfigDoublesPrimary.CopyTo(league.ConfigDoublesPrimary);
        return new FhmLeaguesFile
        {
            VersionTag = header.VersionTag,
            League = league,
        };
    }

    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var league = League;
        if (league.ConfigDoublesPrimary.Length != 17)
        {
            throw new FhmFormatException("leagues.dat requires 17 primary configuration doubles.");
        }

        FhmLeaguesFileSerializer.SerializeHeader(stream, new() { VersionTag = VersionTag, RecordCount = 1 });
        FhmLeaguesFileSerializer.SerializeRecord(stream, new()
        {
            LeagueId = league.LeagueId,
            Flag0 = league.Flag0,
            Flag1 = league.Flag1,
            Flag2 = league.Flag2,
            Name = new() { Value = league.Name },
            ShortName = new() { Value = league.ShortName },
            Abbreviation = new() { Value = league.Abbreviation },
            Nickname = new() { Value = league.Nickname },
            TypeParentId = league.TypeParentId,
            TypeLevelId = league.TypeLevelId,
            ConfigDouble0 = league.ConfigDouble0,
            EarlyInt0 = league.EarlyInt0,
            EarlyInt1 = league.EarlyInt1,
            ConfigDoublesPrimary = league.ConfigDoublesPrimary.ToList(),
            ConfigInt0 = league.ConfigInt0,
            ConfigInt1 = league.ConfigInt1,
            ConfigUInt160 = league.ConfigUInt160,
            ConfigInt2 = league.ConfigInt2,
            ConfigUInt161 = league.ConfigUInt161,
            FoundingDate = new() { Year = league.FoundingDate.Year, Month = league.FoundingDate.Month, Day = league.FoundingDate.Day },
        });
        stream.Write(league.OpaqueLeagueBody);
    }

    private static byte[] ReadRemaining(Stream stream)
    {
        using var remaining = new MemoryStream();
        stream.CopyTo(remaining);
        return remaining.ToArray();
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
