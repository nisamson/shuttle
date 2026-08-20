using Shuttle.Fhm.Serde.Domain.Binary;
using Shuttle.Fhm.Serde.Domain.Model;
using Shuttle.Fhm.Serde.Wire.Players;

namespace Shuttle.Fhm.Serde.Domain.Files;

/// <summary>A fully framed version-58 <c>players.dat</c> player record.</summary>
public sealed class FhmPlayerRecord
{
    /// <summary>Serializes this record without the surrounding <c>players.dat</c> container.</summary>
    public byte[] ToBytes()
    {
        using var stream = new MemoryStream();
        FhmPlayersFileSerializer.SerializeRecord(stream, FhmPlayerWireMapper.ToWire(this));
        return stream.ToArray();
    }

    /// <summary>Reads one record previously produced by <see cref="ToBytes"/>.</summary>
    public static FhmPlayerRecord FromBytes(ReadOnlySpan<byte> bytes)
    {
        using var stream = new MemoryStream(bytes.ToArray(), writable: false);
        try
        {
            var result = FhmPlayerWireMapper.FromWire(FhmPlayersFileSerializer.DeserializeRecord(stream));
            if (stream.Position != stream.Length)
            {
                throw new FhmFormatException($"player record contains unread bytes at offset {stream.Position}.");
            }

            return result;
        }
        catch (InvalidDataException exception)
        {
            throw new FhmFormatException(exception.Message);
        }
    }

    public int FirstNameId { get; set; } = FhmNullConstants.Null;
    public int SurnameId { get; set; } = FhmNullConstants.Null;
    public int CommonNameId { get; set; } = FhmNullConstants.Null;
    public FhmDate BirthDate { get; set; }
    public IList<ushort> UnknownU2Values01 { get; } = new ushort[3];
    public IList<int> UnknownS4Values01 { get; } = new int[6];
    public int TeamId { get => UnknownS4Values01[4]; set => UnknownS4Values01[4] = value; }
    public int UnknownS401 { get; set; }
    public int InternalIdentity { get; set; } = FhmNullConstants.Null;
    public string? UnknownString01 { get; set; }
    public string? UnusedString01 { get; set; }
    public string? UnusedString02 { get; set; }
    public IList<ushort> UnknownU2Values02 { get; } = new ushort[3];
    public FhmPlayerPositionRatings PositionRatings { get; } = new();
    public IList<ushort> UnknownU2Values03 { get; } = new ushort[2];
    public byte UnknownU101 { get; set; }
    public int UnknownS402 { get; set; }
    public byte UnknownU102 { get; set; }
    public IList<FhmFixed24Record> UnknownRecordList01 { get; } = [];
    public IList<ushort> UnknownU2Values04 { get; } = new ushort[2];
    public int UnknownS403 { get; set; }
    public ushort UnknownU201 { get; set; }
    public double UnknownF801 { get; set; }
    public IList<byte> UnknownU1Values01 { get; } = new byte[2];
    public IList<FhmPlayerContract> Contracts { get; } = [];
    public ushort UnknownU202 { get; set; }
    public IList<int> UnknownS4Values02 { get; } = new int[2];
    public IList<byte> UnknownU1Values02 { get; } = new byte[2];
    public ushort UnknownU203 { get; set; }
    public FhmPlayerAttributes RatingAttributes { get; } = new();
    public IList<int> UnknownS4Values03 { get; } = new int[3];
    public IList<ushort> UnknownU2Values05 { get; } = new ushort[15];
    public IList<int> UnknownS4List01 { get; } = [];
    public IList<ushort> UnknownU2Values06 { get; } = new ushort[3];
    public IList<FhmFixed8Record> UnknownPairList01 { get; } = [];
    public FhmDate UnknownDate01 { get; set; }
    public int UnknownS404 { get; set; }
    public IList<FhmAggregateSkaterStats> AggregateSkaterStats01 { get; } = [];
    public IList<FhmAggregateGoalieStats> AggregateGoalieStats01 { get; } = [];
    public IList<FhmAggregateSkaterStats> AggregateSkaterStats02 { get; } = [];
    public IList<FhmAggregateGoalieStats> AggregateGoalieStats02 { get; } = [];
    public IList<FhmDetailedSkaterGameStats> DetailedSkaterGameStats { get; } = [];
    public IList<FhmDetailedGoalieGameStats> DetailedGoalieGameStats { get; } = [];
    public int UnknownS405 { get; set; }
    public ushort UnknownU204 { get; set; }
    public IList<byte> UnknownU1Values03 { get; } = new byte[2];
    public int UnknownS406 { get; set; }
    public IList<double> UnknownF8Values01 { get; } = new double[2];
    public IList<int> UnknownS4Values04 { get; } = new int[3];
    public IList<ushort> UnknownU2Values07 { get; } = new ushort[5];
    public IList<int> UnknownS4List02 { get; } = [];
    public IList<byte> UnknownU1Values04 { get; } = new byte[3];
    public IList<int> UnknownS4List03 { get; } = [];
    public int UnknownS407 { get; set; }
    public double UnknownF802 { get; set; }
    public IList<double> UnknownF8Values02 { get; } = new double[3];
    public IList<int> UnknownS4Values05 { get; } = new int[2];
    public double UnknownF803 { get; set; }
    public byte UnknownU103 { get; set; }
    public int UnknownS408 { get; set; }
    public byte UnknownU104 { get; set; }
    public IList<ushort> UnknownU2Values08 { get; } = new ushort[3];
    public int UnknownS409 { get; set; }
    public IList<double> UnknownF8Values03 { get; } = new double[2];
    public int UnknownS410 { get; set; }
    public IList<byte> UnknownU1Values05 { get; } = new byte[4];
    public ushort UnknownU205 { get; set; }
    public IList<int> UnknownS4Values06 { get; } = new int[2];
    public IList<byte> UnknownU1Values06 { get; } = new byte[5];
    public IList<ushort> UnknownU2Values09 { get; } = new ushort[2];
    public IList<byte> UnknownU1Values07 { get; } = new byte[5];
    public IList<ushort> UnknownU2Values10 { get; } = new ushort[6];
    public IList<byte> UnknownU1Values08 { get; } = new byte[4];
    public IList<ushort> UnknownU2Values11 { get; } = new ushort[2];
    public IList<double> UnknownF8Values04 { get; } = new double[5];
    public ushort UnknownU206 { get; set; }
    public IList<ushort> UnknownU2List01 { get; } = [];
    public IList<double> UnknownF8Values05 { get; } = new double[5];
    public IList<ushort> UnknownU2Values12 { get; } = new ushort[4];
    public IList<FhmDatedStringRecord> DatedStringRecords { get; } = [];
    public IList<ushort> UnknownU2Values13 { get; } = new ushort[2];
    public byte UnknownU105 { get; set; }
    public IList<ushort> UnknownU2Values14 { get; } = new ushort[3];
    public byte UnknownU106 { get; set; }
    public int UnknownS411 { get; set; }
    public IList<double> UnknownF8Values06 { get; } = new double[5];
    public ushort UnknownU207 { get; set; }
    public double UnknownF804 { get; set; }
    public IList<FhmFixed8Record> UnknownRecordList02 { get; } = [];
    public int UnknownS412 { get; set; }
    public IList<FhmFixed23Record> UnknownRecordList03 { get; } = [];
    public IList<ushort> UnknownU2Values15 { get; } = new ushort[2];
    public byte UnknownU107 { get; set; }
    public IList<ushort> UnknownU2Values16 { get; } = new ushort[2];
    public string? UnknownString04 { get; set; }
    public IList<ushort> UnknownU2Values17 { get; } = new ushort[2];
    public IList<int> UnknownS4Values07 { get; } = new int[4];
    public int FranchiseId { get => UnknownS4Values07[0]; set => UnknownS4Values07[0] = value; }
    public FhmPlayerRoleInstance? PrimaryRole { get; set; }
    public FhmPlayerRoleInstance? SupplementaryRole { get; set; }
    public IList<ushort> UnknownU2Values18 { get; } = new ushort[2];
    public IList<byte> UnknownU1Values09 { get; } = new byte[2];
    public IList<FhmFixed2Record> UnknownU1PairList { get; } = [];
    public IList<int> UnknownS4Values08 { get; } = new int[2];
    public int ExportedPlayerId { get; set; }
    public IList<FhmDatedUshortRecord> UnknownDatedU2Records { get; } = [];
    public IList<double> UnknownF8Values07 { get; } = new double[3];
    public ushort UnknownU208 { get; set; }
    public IList<byte> UnknownU1Values10 { get; } = new byte[2];
    public IList<int> UnknownS4List04 { get; } = [];
    public IList<byte> UnknownU1Values11 { get; } = new byte[3];
    public IList<FhmFixed16Record> UnknownRecordList04 { get; } = [];
    public byte UnknownU108 { get; set; }
    public ushort UnknownU209 { get; set; }
    public IList<byte> SpecialAbilities { get; } = [];
    public byte UnknownU109 { get; set; }
    public int UnknownS413 { get; set; }
    public IList<int> UnknownS4List05 { get; } = [];
    public int UnknownS414 { get; set; }
    public IList<byte> UnknownU1Values12 { get; } = new byte[5];
    public IList<int> UnknownS4List06 { get; } = [];
    public IList<FhmByteInt32Pair> UnknownU1S4PairList { get; } = [];
    public byte UnknownU110 { get; set; }
}
