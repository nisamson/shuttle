using Shuttle.BinarySerde.Common.QFormat;
using Shuttle.BinarySerde.Dsl;
using static Shuttle.BinarySerde.Dsl.BinaryCodecs;

namespace Shuttle.Fhm.Serde.Wire.Players;

/// <summary>Serializes complete and standalone <c>players.dat</c> records.</summary>
public static class FhmPlayersFileSerializer
{
    /// <summary>Deserializes a complete <c>players.dat</c> stream.</summary>
    public static FhmPlayersFileData Deserialize(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var reader = new BigEndianBinaryReader(stream);
        try
        {
            var header = FhmPlayersFileCodec.ReadHeader(reader);
            FhmPlayersFileCodec.ValidatePlayerCount(header.PlayerCount);
            var players = new List<FhmPlayerRecordData>(header.PlayerCount);
            for (var index = 0; index < header.PlayerCount; index++)
            {
                players.Add(FhmPlayersFileCodec.ReadPlayer(reader));
            }

            reader.EnsureEndOfStream("players.dat");
            return new FhmPlayersFileData
            {
                FormatVersion = header.FormatVersion,
                PlayerCount = header.PlayerCount,
                Players = players,
            };
        }
        finally
        {
            reader.Dispose();
        }
    }

    /// <summary>Serializes a complete <c>players.dat</c> stream.</summary>
    public static void Serialize(Stream stream, FhmPlayersFileData value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(value);
        SerializeValidated(stream, value);
    }

    /// <summary>Deserializes one self-delimiting player record.</summary>
    public static FhmPlayerRecordData DeserializeRecord(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var reader = new BigEndianBinaryReader(stream, bufferSize: 1);
        try
        {
            return FhmPlayersFileCodec.ReadPlayer(reader);
        }
        finally
        {
            reader.Dispose();
        }
    }

    /// <summary>Serializes one player record without a surrounding file container.</summary>
    public static void SerializeRecord(Stream stream, FhmPlayerRecordData value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(value);
        SerializeRecordValidated(stream, value);
    }

    internal static FhmPlayersFileWireReader CreateReader(Stream stream) => new(stream);

    internal static FhmPlayerRecordData DeserializeRecordExact(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var reader = new BigEndianBinaryReader(stream, bufferSize: 1);
        try
        {
            var result = FhmPlayersFileCodec.ReadPlayer(reader);
            reader.EnsureEndOfStream("player record");
            return result;
        }
        finally
        {
            reader.Dispose();
        }
    }

    internal static void SerializeValidated(Stream stream, FhmPlayersFileData value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(value);

        var writer = new BigEndianBinaryWriter(stream);
        try
        {
            FhmPlayersFileCodec.WriteHeader(
                writer,
                new FhmPlayersFileHeaderData
                {
                    FormatVersion = value.FormatVersion,
                    PlayerCount = value.PlayerCount,
                });
            foreach (var player in value.Players)
            {
                FhmPlayersFileCodec.WritePlayer(writer, player);
            }

            writer.Flush();
        }
        finally
        {
            writer.Dispose();
        }
    }

    internal static void SerializeRecordValidated(Stream stream, FhmPlayerRecordData value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(value);

        var writer = new BigEndianBinaryWriter(stream);
        try
        {
            FhmPlayersFileCodec.WritePlayer(writer, value);
            writer.Flush();
        }
        finally
        {
            writer.Dispose();
        }
    }
}

internal sealed class FhmPlayersFileWireReader : IDisposable
{
    private readonly BigEndianBinaryReader reader;
    private int recordsRemaining;
    private bool disposed;

    internal FhmPlayersFileWireReader(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        reader = new BigEndianBinaryReader(source);
        Header = FhmPlayersFileCodec.ReadHeader(reader);
        recordsRemaining = Header.PlayerCount;
    }

    internal FhmPlayersFileHeaderData Header { get; }

    internal FhmPlayerRecordData ReadRecord()
    {
        ThrowIfDisposed();
        if (recordsRemaining <= 0)
        {
            throw new InvalidOperationException("All players.dat records have already been read.");
        }

        recordsRemaining--;
        return FhmPlayersFileCodec.ReadPlayer(reader);
    }

    internal void EnsureEndOfStream()
    {
        ThrowIfDisposed();
        if (recordsRemaining != 0)
        {
            throw new InvalidOperationException("All players.dat records must be read before checking the end of the stream.");
        }

        reader.EnsureEndOfStream("players.dat");
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            reader.Dispose();
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);
}

internal static class FhmPlayersFileCodec
{
    internal const int MaximumCollectionCount = 10_000_000;

    private static readonly ValueCodec<byte> Byte = new(
        static reader => reader.ReadByte(),
        static (writer, value) => writer.WriteByte(value));
    private static readonly ValueCodec<ushort> UInt16 = new(
        static reader => reader.ReadUInt16(),
        static (writer, value) => writer.WriteUInt16(value));
    private static readonly ValueCodec<int> Int32 = new(
        static reader => reader.ReadInt32(),
        static (writer, value) => writer.WriteInt32(value));
    private static readonly ValueCodec<long> Int64 = new(
        static reader => reader.ReadInt64(),
        static (writer, value) => writer.WriteInt64(value));
    private static readonly ValueCodec<double> Double = new(
        static reader => reader.ReadDouble(),
        static (writer, value) => writer.WriteDouble(value));
    private static readonly ValueCodec<QString> QString = new(
        static reader => ReadQString(reader),
        static (writer, value) => WriteQString(writer, value));
    private static readonly ValueCodec<QDate> QDate = new(
        static reader => new QDate
        {
            Year = reader.ReadInt32(),
            Month = reader.ReadInt32(),
            Day = reader.ReadInt32(),
        },
        static (writer, value) =>
        {
            writer.WriteInt32(value.Year);
            writer.WriteInt32(value.Month);
            writer.WriteInt32(value.Day);
        });

    private static readonly RecordCodec<FhmPlayersFileHeaderData> Header = Record<FhmPlayersFileHeaderData>()
        .Field(static value => value.FormatVersion, static (value, field) => value.FormatVersion = field, Int32)
        .Field(static value => value.PlayerCount, static (value, field) => value.PlayerCount = field, Int32)
        .Build();

    private static readonly RecordCodec<FhmPlayerPositionRatingsData> PositionRatings =
        Record<FhmPlayerPositionRatingsData>()
            .Field(static value => value.RawValues, static (value, field) => value.RawValues = field, QList(UInt16, nameof(FhmPlayerPositionRatingsData.RawValues)))
            .Build();

    private static readonly RecordCodec<FhmPlayerAttributesData> PlayerAttributes =
        Record<FhmPlayerAttributesData>()
            .Field(static value => value.BigGames, static (value, field) => value.BigGames = field, Byte)
            .Field(static value => value.Consistency, static (value, field) => value.Consistency = field, Byte)
            .Field(static value => value.Greed, static (value, field) => value.Greed = field, Byte)
            .Field(static value => value.Adaptability, static (value, field) => value.Adaptability = field, Byte)
            .Field(static value => value.Loyalty, static (value, field) => value.Loyalty = field, Byte)
            .Field(static value => value.Coachability, static (value, field) => value.Coachability = field, Byte)
            .Field(static value => value.Aging, static (value, field) => value.Aging = field, Byte)
            .Field(static value => value.Sportsmanship, static (value, field) => value.Sportsmanship = field, Byte)
            .Field(static value => value.PassShootTendency, static (value, field) => value.PassShootTendency = field, Byte)
            .Field(static value => value.Controversy, static (value, field) => value.Controversy = field, Byte)
            .Field(static value => value.HandleCritics, static (value, field) => value.HandleCritics = field, Byte)
            .Field(static value => value.HandleFailure, static (value, field) => value.HandleFailure = field, Byte)
            .Field(static value => value.HandleSuccess, static (value, field) => value.HandleSuccess = field, Byte)
            .Field(static value => value.Intelligence, static (value, field) => value.Intelligence = field, Byte)
            .Field(static value => value.Mood, static (value, field) => value.Mood = field, Byte)
            .Field(static value => value.DevRate, static (value, field) => value.DevRate = field, Byte)
            .Field(static value => value.Aggression, static (value, field) => value.Aggression = field, Byte)
            .Field(static value => value.Bravery, static (value, field) => value.Bravery = field, Byte)
            .Field(static value => value.Determination, static (value, field) => value.Determination = field, Byte)
            .Field(static value => value.Teamplayer, static (value, field) => value.Teamplayer = field, Byte)
            .Field(static value => value.Leadership, static (value, field) => value.Leadership = field, Byte)
            .Field(static value => value.Temperament, static (value, field) => value.Temperament = field, Byte)
            .Field(static value => value.Professionalism, static (value, field) => value.Professionalism = field, Byte)
            .Field(static value => value.Ambition, static (value, field) => value.Ambition = field, Byte)
            .Field(static value => value.Acceleration, static (value, field) => value.Acceleration = field, Byte)
            .Field(static value => value.Agility, static (value, field) => value.Agility = field, Byte)
            .Field(static value => value.Balance, static (value, field) => value.Balance = field, Byte)
            .Field(static value => value.Speed, static (value, field) => value.Speed = field, Byte)
            .Field(static value => value.Stamina, static (value, field) => value.Stamina = field, Byte)
            .Field(static value => value.Strength, static (value, field) => value.Strength = field, Byte)
            .Field(static value => value.Fighting, static (value, field) => value.Fighting = field, Byte)
            .Field(static value => value.GoalieReflexes, static (value, field) => value.GoalieReflexes = field, Byte)
            .Field(static value => value.GoalieStamina, static (value, field) => value.GoalieStamina = field, Byte)
            .Field(static value => value.Screening, static (value, field) => value.Screening = field, Byte)
            .Field(static value => value.GettingOpen, static (value, field) => value.GettingOpen = field, Byte)
            .Field(static value => value.Passing, static (value, field) => value.Passing = field, Byte)
            .Field(static value => value.PuckHandling, static (value, field) => value.PuckHandling = field, Byte)
            .Field(static value => value.ShootingAccuracy, static (value, field) => value.ShootingAccuracy = field, Byte)
            .Field(static value => value.ShootingRange, static (value, field) => value.ShootingRange = field, Byte)
            .Field(static value => value.OffensiveRead, static (value, field) => value.OffensiveRead = field, Byte)
            .Field(static value => value.Checking, static (value, field) => value.Checking = field, Byte)
            .Field(static value => value.Faceoffs, static (value, field) => value.Faceoffs = field, Byte)
            .Field(static value => value.Hitting, static (value, field) => value.Hitting = field, Byte)
            .Field(static value => value.Positioning, static (value, field) => value.Positioning = field, Byte)
            .Field(static value => value.ShotBlocking, static (value, field) => value.ShotBlocking = field, Byte)
            .Field(static value => value.Stickchecking, static (value, field) => value.Stickchecking = field, Byte)
            .Field(static value => value.DefensiveRead, static (value, field) => value.DefensiveRead = field, Byte)
            .Field(static value => value.GoaliePositioning, static (value, field) => value.GoaliePositioning = field, Byte)
            .Field(static value => value.GoaliePassing, static (value, field) => value.GoaliePassing = field, Byte)
            .Field(static value => value.GoaliePokecheck, static (value, field) => value.GoaliePokecheck = field, Byte)
            .Field(static value => value.GoalieBlocker, static (value, field) => value.GoalieBlocker = field, Byte)
            .Field(static value => value.GoalieGlove, static (value, field) => value.GoalieGlove = field, Byte)
            .Field(static value => value.GoalieRebound, static (value, field) => value.GoalieRebound = field, Byte)
            .Field(static value => value.GoalieRecovery, static (value, field) => value.GoalieRecovery = field, Byte)
            .Field(static value => value.GoaliePuckhandling, static (value, field) => value.GoaliePuckhandling = field, Byte)
            .Field(static value => value.GoalieLowShots, static (value, field) => value.GoalieLowShots = field, Byte)
            .Field(static value => value.MentalToughness, static (value, field) => value.MentalToughness = field, Byte)
            .Field(static value => value.GoalieSkating, static (value, field) => value.GoalieSkating = field, Byte)
            .Build();

    private static readonly RecordCodec<FhmPlayerRoleInstanceData> PlayerRole =
        Record<FhmPlayerRoleInstanceData>()
            .Field(static value => value.RoleId, static (value, field) => value.RoleId = field, Int32)
            .Field(static value => value.UseOverride, static (value, field) => value.UseOverride = field, FixedList(9, Byte))
            .Field(static value => value.TendencyValue, static (value, field) => value.TendencyValue = field, FixedList(9, UInt16))
            .Build();

    private static readonly ValueCodec<FhmOptionalPlayerRoleInstanceData> OptionalPlayerRole = new(
        static reader => ReadOptionalPlayerRole(reader),
        static (writer, value) => WriteOptionalPlayerRole(writer, value));

    private static readonly RecordCodec<FhmPlayerContractData> PlayerContract =
        Record<FhmPlayerContractData>()
            .Field(static value => value.UnknownS4Values01, static (value, field) => value.UnknownS4Values01 = field, FixedList(2, Int32))
            .Field(static value => value.UnknownU2Values01, static (value, field) => value.UnknownU2Values01 = field, FixedList(2, UInt16))
            .Field(static value => value.UnknownS401, static (value, field) => value.UnknownS401 = field, Int32)
            .Field(static value => value.UnknownDate01, static (value, field) => value.UnknownDate01 = field, QDate)
            .Field(static value => value.UnknownF801, static (value, field) => value.UnknownF801 = field, Double)
            .Field(static value => value.Salaries, static (value, field) => value.Salaries = field, QList(Int32, nameof(FhmPlayerContractData.Salaries)))
            .Field(static value => value.UnknownU2Values02, static (value, field) => value.UnknownU2Values02 = field, FixedList(3, UInt16))
            .Field(static value => value.UnknownS402, static (value, field) => value.UnknownS402 = field, Int32)
            .Field(static value => value.UnknownU201, static (value, field) => value.UnknownU201 = field, UInt16)
            .Field(static value => value.UnknownU101, static (value, field) => value.UnknownU101 = field, Byte)
            .Field(static value => value.UnknownS4Values02, static (value, field) => value.UnknownS4Values02 = field, FixedList(2, Int32))
            .Field(static value => value.UnknownU202, static (value, field) => value.UnknownU202 = field, UInt16)
            .Field(static value => value.UnknownU1Values01, static (value, field) => value.UnknownU1Values01 = field, FixedList(2, Byte))
            .Field(static value => value.UnknownS4Values03, static (value, field) => value.UnknownS4Values03 = field, FixedList(4, Int32))
            .Field(static value => value.UnknownU1Values02, static (value, field) => value.UnknownU1Values02 = field, FixedList(4, Byte))
            .Field(static value => value.UnknownS403, static (value, field) => value.UnknownS403 = field, Int32)
            .Field(static value => value.UnknownU1Values03, static (value, field) => value.UnknownU1Values03 = field, FixedList(2, Byte))
            .Field(static value => value.UnknownS404, static (value, field) => value.UnknownS404 = field, Int32)
            .Field(static value => value.UnknownU1Values04, static (value, field) => value.UnknownU1Values04 = field, FixedList(3, Byte))
            .Field(static value => value.UnknownS405, static (value, field) => value.UnknownS405 = field, Int32)
            .Field(static value => value.UnknownU203, static (value, field) => value.UnknownU203 = field, UInt16)
            .Field(static value => value.UnknownS406, static (value, field) => value.UnknownS406 = field, Int32)
            .Field(static value => value.UnknownU1Values05, static (value, field) => value.UnknownU1Values05 = field, FixedList(3, Byte))
            .Field(static value => value.UnknownU2Values03, static (value, field) => value.UnknownU2Values03 = field, FixedList(3, UInt16))
            .Field(static value => value.UnknownU1Values06, static (value, field) => value.UnknownU1Values06 = field, FixedList(4, Byte))
            .Field(static value => value.UnknownU1List01, static (value, field) => value.UnknownU1List01 = field, QList(Byte, nameof(FhmPlayerContractData.UnknownU1List01)))
            .Field(static value => value.UnknownU1List02, static (value, field) => value.UnknownU1List02 = field, QList(Byte, nameof(FhmPlayerContractData.UnknownU1List02)))
            .Field(static value => value.UnknownU1Values07, static (value, field) => value.UnknownU1Values07 = field, FixedList(3, Byte))
            .Build();

    private static readonly RecordCodec<FhmAggregateSkaterStatsData> AggregateSkaterStats =
        Record<FhmAggregateSkaterStatsData>()
            .Field(static value => value.HeaderU2, static (value, field) => value.HeaderU2 = field, FixedList(5, UInt16))
            .Field(static value => value.ValueS401, static (value, field) => value.ValueS401 = field, Int32)
            .Field(static value => value.CountersU201, static (value, field) => value.CountersU201 = field, FixedList(18, UInt16))
            .Field(static value => value.ValuesS401, static (value, field) => value.ValuesS401 = field, FixedList(3, Int32))
            .Field(static value => value.CountersU202, static (value, field) => value.CountersU202 = field, FixedList(7, UInt16))
            .Field(static value => value.ValuesF801, static (value, field) => value.ValuesF801 = field, FixedList(3, Double))
            .Field(static value => value.ValueU201, static (value, field) => value.ValueU201 = field, UInt16)
            .Field(static value => value.ValuesF802, static (value, field) => value.ValuesF802 = field, FixedList(3, Double))
            .Field(static value => value.ValuesU201, static (value, field) => value.ValuesU201 = field, FixedList(2, UInt16))
            .Field(static value => value.FlagsU1, static (value, field) => value.FlagsU1 = field, FixedList(3, Byte))
            .Build();

    private static readonly RecordCodec<FhmAggregateGoalieStatsData> AggregateGoalieStats =
        Record<FhmAggregateGoalieStatsData>()
            .Field(static value => value.HeaderU2, static (value, field) => value.HeaderU2 = field, FixedList(5, UInt16))
            .Field(static value => value.ValueS401, static (value, field) => value.ValueS401 = field, Int32)
            .Field(static value => value.ValuesU201, static (value, field) => value.ValuesU201 = field, FixedList(2, UInt16))
            .Field(static value => value.ValueS402, static (value, field) => value.ValueS402 = field, Int32)
            .Field(static value => value.CountersU2, static (value, field) => value.CountersU2 = field, FixedList(10, UInt16))
            .Field(static value => value.ValueF8, static (value, field) => value.ValueF8 = field, Double)
            .Field(static value => value.ValueU201, static (value, field) => value.ValueU201 = field, UInt16)
            .Field(static value => value.FlagU1, static (value, field) => value.FlagU1 = field, Byte)
            .Build();

    private static readonly RecordCodec<FhmDetailedSkaterGameStatsData> DetailedSkaterStats =
        Record<FhmDetailedSkaterGameStatsData>()
            .Field(static value => value.HeaderU2, static (value, field) => value.HeaderU2 = field, FixedList(5, UInt16))
            .Field(static value => value.ValueS401, static (value, field) => value.ValueS401 = field, Int32)
            .Field(static value => value.CountersU201, static (value, field) => value.CountersU201 = field, FixedList(18, UInt16))
            .Field(static value => value.ValuesS401, static (value, field) => value.ValuesS401 = field, FixedList(3, Int32))
            .Field(static value => value.CountersU202, static (value, field) => value.CountersU202 = field, FixedList(26, UInt16))
            .Field(static value => value.ValueS402, static (value, field) => value.ValueS402 = field, Int32)
            .Field(static value => value.ValuesF8, static (value, field) => value.ValuesF8 = field, FixedList(3, Double))
            .Field(static value => value.ValuesU201, static (value, field) => value.ValuesU201 = field, FixedList(6, UInt16))
            .Field(static value => value.TrailingU1, static (value, field) => value.TrailingU1 = field, FixedList(10, Byte))
            .Build();

    private static readonly RecordCodec<FhmDetailedGoalieGameStatsData> DetailedGoalieStats =
        Record<FhmDetailedGoalieGameStatsData>()
            .Field(static value => value.HeaderU2, static (value, field) => value.HeaderU2 = field, FixedList(5, UInt16))
            .Field(static value => value.ValueS401, static (value, field) => value.ValueS401 = field, Int32)
            .Field(static value => value.ValuesU201, static (value, field) => value.ValuesU201 = field, FixedList(2, UInt16))
            .Field(static value => value.ValueS402, static (value, field) => value.ValueS402 = field, Int32)
            .Field(static value => value.CountersU201, static (value, field) => value.CountersU201 = field, FixedList(10, UInt16))
            .Field(static value => value.CountersU202, static (value, field) => value.CountersU202 = field, FixedList(2, UInt16))
            .Field(static value => value.ValueF8, static (value, field) => value.ValueF8 = field, Double)
            .Field(static value => value.ValuesU202, static (value, field) => value.ValuesU202 = field, FixedList(6, UInt16))
            .Field(static value => value.TrailingU1, static (value, field) => value.TrailingU1 = field, Byte)
            .Build();

    private static readonly RecordCodec<FhmDatedStringRecordData> DatedStringRecord =
        Record<FhmDatedStringRecordData>()
            .Field(static value => value.JulianDay, static (value, field) => value.JulianDay = field, Int64)
            .Field(static value => value.UnknownU101, static (value, field) => value.UnknownU101 = field, Byte)
            .Field(static value => value.UnknownS4Values01, static (value, field) => value.UnknownS4Values01 = field, FixedList(3, Int32))
            .Field(static value => value.Text, static (value, field) => value.Text = field, QString)
            .Field(static value => value.UnknownS4Values02, static (value, field) => value.UnknownS4Values02 = field, FixedList(2, Int32))
            .Build();

    private static readonly RecordCodec<FhmDatedUshortRecordData> DatedUshortRecord =
        Record<FhmDatedUshortRecordData>()
            .Field(static value => value.Date, static (value, field) => value.Date = field, QDate)
            .Field(static value => value.UnknownU2Values, static (value, field) => value.UnknownU2Values = field, FixedList(2, UInt16))
            .Build();

    private static readonly RecordCodec<FhmFixed2RecordData> Fixed2Record =
        Record<FhmFixed2RecordData>()
            .Field(static value => value.Value01, static (value, field) => value.Value01 = field, Byte)
            .Field(static value => value.Value02, static (value, field) => value.Value02 = field, Byte)
            .Build();

    private static readonly RecordCodec<FhmFixed8RecordData> Fixed8Record =
        Record<FhmFixed8RecordData>()
            .Field(static value => value.Data, static (value, field) => value.Data = field, FixedBytes(8))
            .Build();

    private static readonly RecordCodec<FhmFixed16RecordData> Fixed16Record =
        Record<FhmFixed16RecordData>()
            .Field(static value => value.Data, static (value, field) => value.Data = field, FixedBytes(16))
            .Build();

    private static readonly RecordCodec<FhmFixed23RecordData> Fixed23Record =
        Record<FhmFixed23RecordData>()
            .Field(static value => value.Data, static (value, field) => value.Data = field, FixedBytes(23))
            .Build();

    private static readonly RecordCodec<FhmFixed24RecordData> Fixed24Record =
        Record<FhmFixed24RecordData>()
            .Field(static value => value.Data, static (value, field) => value.Data = field, FixedBytes(24))
            .Build();

    private static readonly RecordCodec<FhmByteInt32PairData> ByteInt32Pair =
        Record<FhmByteInt32PairData>()
            .Field(static value => value.UnknownU1, static (value, field) => value.UnknownU1 = field, Byte)
            .Field(static value => value.UnknownS4, static (value, field) => value.UnknownS4 = field, Int32)
            .Build();

    private static readonly RecordCodec<FhmPlayerRecordData> PlayerRecord =
        Record<FhmPlayerRecordData>()
            .Field(static value => value.FirstNameId, static (value, field) => value.FirstNameId = field, Int32)
            .Field(static value => value.SurnameId, static (value, field) => value.SurnameId = field, Int32)
            .Field(static value => value.CommonNameId, static (value, field) => value.CommonNameId = field, Int32)
            .Field(static value => value.BirthDate, static (value, field) => value.BirthDate = field, QDate)
            .Field(static value => value.UnknownU2Values01, static (value, field) => value.UnknownU2Values01 = field, FixedList(3, UInt16))
            .Field(static value => value.UnknownS4Values01, static (value, field) => value.UnknownS4Values01 = field, FixedList(6, Int32))
            .Field(static value => value.UnknownS401, static (value, field) => value.UnknownS401 = field, Int32)
            .Field(static value => value.InternalIdentity, static (value, field) => value.InternalIdentity = field, Int32)
            .Field(static value => value.UnknownString01, static (value, field) => value.UnknownString01 = field, QString)
            .Field(static value => value.UnusedString01, static (value, field) => value.UnusedString01 = field, QString)
            .Field(static value => value.UnusedString02, static (value, field) => value.UnusedString02 = field, QString)
            .Field(static value => value.UnknownU2Values02, static (value, field) => value.UnknownU2Values02 = field, FixedList(3, UInt16))
            .Field(static value => value.PositionRatings, static (value, field) => value.PositionRatings = field, Object(PositionRatings))
            .Field(static value => value.UnknownU2Values03, static (value, field) => value.UnknownU2Values03 = field, FixedList(2, UInt16))
            .Field(static value => value.UnknownU101, static (value, field) => value.UnknownU101 = field, Byte)
            .Field(static value => value.UnknownS402, static (value, field) => value.UnknownS402 = field, Int32)
            .Field(static value => value.UnknownU102, static (value, field) => value.UnknownU102 = field, Byte)
            .Field(static value => value.UnknownRecordList01, static (value, field) => value.UnknownRecordList01 = field, QList(Object(Fixed24Record), nameof(FhmPlayerRecordData.UnknownRecordList01)))
            .Field(static value => value.UnknownU2Values04, static (value, field) => value.UnknownU2Values04 = field, FixedList(2, UInt16))
            .Field(static value => value.UnknownS403, static (value, field) => value.UnknownS403 = field, Int32)
            .Field(static value => value.UnknownU201, static (value, field) => value.UnknownU201 = field, UInt16)
            .Field(static value => value.UnknownF801, static (value, field) => value.UnknownF801 = field, Double)
            .Field(static value => value.UnknownU1Values01, static (value, field) => value.UnknownU1Values01 = field, FixedList(2, Byte))
            .Field(static value => value.Contracts, static (value, field) => value.Contracts = field, QList(Object(PlayerContract), nameof(FhmPlayerRecordData.Contracts)))
            .Field(static value => value.UnknownU202, static (value, field) => value.UnknownU202 = field, UInt16)
            .Field(static value => value.UnknownS4Values02, static (value, field) => value.UnknownS4Values02 = field, FixedList(2, Int32))
            .Field(static value => value.UnknownU1Values02, static (value, field) => value.UnknownU1Values02 = field, FixedList(2, Byte))
            .Field(static value => value.UnknownU203, static (value, field) => value.UnknownU203 = field, UInt16)
            .Field(static value => value.RatingAttributes, static (value, field) => value.RatingAttributes = field, Object(PlayerAttributes))
            .Field(static value => value.UnknownS4Values03, static (value, field) => value.UnknownS4Values03 = field, FixedList(3, Int32))
            .Field(static value => value.UnknownU2Values05, static (value, field) => value.UnknownU2Values05 = field, FixedList(15, UInt16))
            .Field(static value => value.UnknownS4List01, static (value, field) => value.UnknownS4List01 = field, QList(Int32, nameof(FhmPlayerRecordData.UnknownS4List01)))
            .Field(static value => value.UnknownU2Values06, static (value, field) => value.UnknownU2Values06 = field, FixedList(3, UInt16))
            .Field(static value => value.UnknownPairList01, static (value, field) => value.UnknownPairList01 = field, QList(Object(Fixed8Record), nameof(FhmPlayerRecordData.UnknownPairList01)))
            .Field(static value => value.UnknownDate01, static (value, field) => value.UnknownDate01 = field, QDate)
            .Field(static value => value.UnknownS404, static (value, field) => value.UnknownS404 = field, Int32)
            .Field(static value => value.AggregateSkaterStats01, static (value, field) => value.AggregateSkaterStats01 = field, QList(Object(AggregateSkaterStats), nameof(FhmPlayerRecordData.AggregateSkaterStats01)))
            .Field(static value => value.AggregateGoalieStats01, static (value, field) => value.AggregateGoalieStats01 = field, QList(Object(AggregateGoalieStats), nameof(FhmPlayerRecordData.AggregateGoalieStats01)))
            .Field(static value => value.AggregateSkaterStats02, static (value, field) => value.AggregateSkaterStats02 = field, QList(Object(AggregateSkaterStats), nameof(FhmPlayerRecordData.AggregateSkaterStats02)))
            .Field(static value => value.AggregateGoalieStats02, static (value, field) => value.AggregateGoalieStats02 = field, QList(Object(AggregateGoalieStats), nameof(FhmPlayerRecordData.AggregateGoalieStats02)))
            .Field(static value => value.DetailedSkaterGameStats, static (value, field) => value.DetailedSkaterGameStats = field, QList(Object(DetailedSkaterStats), nameof(FhmPlayerRecordData.DetailedSkaterGameStats)))
            .Field(static value => value.DetailedGoalieGameStats, static (value, field) => value.DetailedGoalieGameStats = field, QList(Object(DetailedGoalieStats), nameof(FhmPlayerRecordData.DetailedGoalieGameStats)))
            .Field(static value => value.UnknownS405, static (value, field) => value.UnknownS405 = field, Int32)
            .Field(static value => value.UnknownU204, static (value, field) => value.UnknownU204 = field, UInt16)
            .Field(static value => value.UnknownU1Values03, static (value, field) => value.UnknownU1Values03 = field, FixedList(2, Byte))
            .Field(static value => value.UnknownS406, static (value, field) => value.UnknownS406 = field, Int32)
            .Field(static value => value.UnknownF8Values01, static (value, field) => value.UnknownF8Values01 = field, FixedList(2, Double))
            .Field(static value => value.UnknownS4Values04, static (value, field) => value.UnknownS4Values04 = field, FixedList(3, Int32))
            .Field(static value => value.UnknownU2Values07, static (value, field) => value.UnknownU2Values07 = field, FixedList(5, UInt16))
            .Field(static value => value.UnknownS4List02, static (value, field) => value.UnknownS4List02 = field, QList(Int32, nameof(FhmPlayerRecordData.UnknownS4List02)))
            .Field(static value => value.UnknownU1Values04, static (value, field) => value.UnknownU1Values04 = field, FixedList(3, Byte))
            .Field(static value => value.UnknownS4List03, static (value, field) => value.UnknownS4List03 = field, QList(Int32, nameof(FhmPlayerRecordData.UnknownS4List03)))
            .Field(static value => value.UnknownS407, static (value, field) => value.UnknownS407 = field, Int32)
            .Field(static value => value.UnknownF802, static (value, field) => value.UnknownF802 = field, Double)
            .Field(static value => value.UnknownF8Values02, static (value, field) => value.UnknownF8Values02 = field, FixedList(3, Double))
            .Field(static value => value.UnknownS4Values05, static (value, field) => value.UnknownS4Values05 = field, FixedList(2, Int32))
            .Field(static value => value.UnknownF803, static (value, field) => value.UnknownF803 = field, Double)
            .Field(static value => value.UnknownU103, static (value, field) => value.UnknownU103 = field, Byte)
            .Field(static value => value.UnknownS408, static (value, field) => value.UnknownS408 = field, Int32)
            .Field(static value => value.UnknownU104, static (value, field) => value.UnknownU104 = field, Byte)
            .Field(static value => value.UnknownU2Values08, static (value, field) => value.UnknownU2Values08 = field, FixedList(3, UInt16))
            .Field(static value => value.UnknownS409, static (value, field) => value.UnknownS409 = field, Int32)
            .Field(static value => value.UnknownF8Values03, static (value, field) => value.UnknownF8Values03 = field, FixedList(2, Double))
            .Field(static value => value.UnknownS410, static (value, field) => value.UnknownS410 = field, Int32)
            .Field(static value => value.UnknownU1Values05, static (value, field) => value.UnknownU1Values05 = field, FixedList(4, Byte))
            .Field(static value => value.UnknownU205, static (value, field) => value.UnknownU205 = field, UInt16)
            .Field(static value => value.UnknownS4Values06, static (value, field) => value.UnknownS4Values06 = field, FixedList(2, Int32))
            .Field(static value => value.UnknownU1Values06, static (value, field) => value.UnknownU1Values06 = field, FixedList(5, Byte))
            .Field(static value => value.UnknownU2Values09, static (value, field) => value.UnknownU2Values09 = field, FixedList(2, UInt16))
            .Field(static value => value.UnknownU1Values07, static (value, field) => value.UnknownU1Values07 = field, FixedList(5, Byte))
            .Field(static value => value.UnknownU2Values10, static (value, field) => value.UnknownU2Values10 = field, FixedList(6, UInt16))
            .Field(static value => value.UnknownU1Values08, static (value, field) => value.UnknownU1Values08 = field, FixedList(4, Byte))
            .Field(static value => value.UnknownU2Values11, static (value, field) => value.UnknownU2Values11 = field, FixedList(2, UInt16))
            .Field(static value => value.UnknownF8Values04, static (value, field) => value.UnknownF8Values04 = field, FixedList(5, Double))
            .Field(static value => value.UnknownU206, static (value, field) => value.UnknownU206 = field, UInt16)
            .Field(static value => value.UnknownU2List01, static (value, field) => value.UnknownU2List01 = field, QList(UInt16, nameof(FhmPlayerRecordData.UnknownU2List01)))
            .Field(static value => value.UnknownF8Values05, static (value, field) => value.UnknownF8Values05 = field, FixedList(5, Double))
            .Field(static value => value.UnknownU2Values12, static (value, field) => value.UnknownU2Values12 = field, FixedList(4, UInt16))
            .Field(static value => value.DatedStringRecords, static (value, field) => value.DatedStringRecords = field, QList(Object(DatedStringRecord), nameof(FhmPlayerRecordData.DatedStringRecords)))
            .Field(static value => value.UnknownU2Values13, static (value, field) => value.UnknownU2Values13 = field, FixedList(2, UInt16))
            .Field(static value => value.UnknownU105, static (value, field) => value.UnknownU105 = field, Byte)
            .Field(static value => value.UnknownU2Values14, static (value, field) => value.UnknownU2Values14 = field, FixedList(3, UInt16))
            .Field(static value => value.UnknownU106, static (value, field) => value.UnknownU106 = field, Byte)
            .Field(static value => value.UnknownS411, static (value, field) => value.UnknownS411 = field, Int32)
            .Field(static value => value.UnknownF8Values06, static (value, field) => value.UnknownF8Values06 = field, FixedList(5, Double))
            .Field(static value => value.UnknownU207, static (value, field) => value.UnknownU207 = field, UInt16)
            .Field(static value => value.UnknownF804, static (value, field) => value.UnknownF804 = field, Double)
            .Field(static value => value.UnknownRecordList02, static (value, field) => value.UnknownRecordList02 = field, QList(Object(Fixed8Record), nameof(FhmPlayerRecordData.UnknownRecordList02)))
            .Field(static value => value.UnknownS412, static (value, field) => value.UnknownS412 = field, Int32)
            .Field(static value => value.UnknownRecordList03, static (value, field) => value.UnknownRecordList03 = field, QList(Object(Fixed23Record), nameof(FhmPlayerRecordData.UnknownRecordList03)))
            .Field(static value => value.UnknownU2Values15, static (value, field) => value.UnknownU2Values15 = field, FixedList(2, UInt16))
            .Field(static value => value.UnknownU107, static (value, field) => value.UnknownU107 = field, Byte)
            .Field(static value => value.UnknownU2Values16, static (value, field) => value.UnknownU2Values16 = field, FixedList(2, UInt16))
            .Field(static value => value.UnknownString04, static (value, field) => value.UnknownString04 = field, QString)
            .Field(static value => value.UnknownU2Values17, static (value, field) => value.UnknownU2Values17 = field, FixedList(2, UInt16))
            .Field(static value => value.UnknownS4Values07, static (value, field) => value.UnknownS4Values07 = field, FixedList(4, Int32))
            .Field(static value => value.PrimaryRole, static (value, field) => value.PrimaryRole = field, OptionalPlayerRole)
            .Field(static value => value.SupplementaryRole, static (value, field) => value.SupplementaryRole = field, OptionalPlayerRole)
            .Field(static value => value.UnknownU2Values18, static (value, field) => value.UnknownU2Values18 = field, FixedList(2, UInt16))
            .Field(static value => value.UnknownU1Values09, static (value, field) => value.UnknownU1Values09 = field, FixedList(2, Byte))
            .Field(static value => value.UnknownU1PairList, static (value, field) => value.UnknownU1PairList = field, QList(Object(Fixed2Record), nameof(FhmPlayerRecordData.UnknownU1PairList)))
            .Field(static value => value.UnknownS4Values08, static (value, field) => value.UnknownS4Values08 = field, FixedList(2, Int32))
            .Field(static value => value.ExportedPlayerId, static (value, field) => value.ExportedPlayerId = field, Int32)
            .Field(static value => value.UnknownDatedU2Records, static (value, field) => value.UnknownDatedU2Records = field, QList(Object(DatedUshortRecord), nameof(FhmPlayerRecordData.UnknownDatedU2Records)))
            .Field(static value => value.UnknownF8Values07, static (value, field) => value.UnknownF8Values07 = field, FixedList(3, Double))
            .Field(static value => value.UnknownU208, static (value, field) => value.UnknownU208 = field, UInt16)
            .Field(static value => value.UnknownU1Values10, static (value, field) => value.UnknownU1Values10 = field, FixedList(2, Byte))
            .Field(static value => value.UnknownS4List04, static (value, field) => value.UnknownS4List04 = field, QList(Int32, nameof(FhmPlayerRecordData.UnknownS4List04)))
            .Field(static value => value.UnknownU1Values11, static (value, field) => value.UnknownU1Values11 = field, FixedList(3, Byte))
            .Field(static value => value.UnknownRecordList04, static (value, field) => value.UnknownRecordList04 = field, QList(Object(Fixed16Record), nameof(FhmPlayerRecordData.UnknownRecordList04)))
            .Field(static value => value.UnknownU108, static (value, field) => value.UnknownU108 = field, Byte)
            .Field(static value => value.UnknownU209, static (value, field) => value.UnknownU209 = field, UInt16)
            .Field(static value => value.SpecialAbilities, static (value, field) => value.SpecialAbilities = field, QList(Byte, nameof(FhmPlayerRecordData.SpecialAbilities)))
            .Field(static value => value.UnknownU109, static (value, field) => value.UnknownU109 = field, Byte)
            .Field(static value => value.UnknownS413, static (value, field) => value.UnknownS413 = field, Int32)
            .Field(static value => value.UnknownS4List05, static (value, field) => value.UnknownS4List05 = field, QList(Int32, nameof(FhmPlayerRecordData.UnknownS4List05)))
            .Field(static value => value.UnknownS414, static (value, field) => value.UnknownS414 = field, Int32)
            .Field(static value => value.UnknownU1Values12, static (value, field) => value.UnknownU1Values12 = field, FixedList(5, Byte))
            .Field(static value => value.UnknownS4List06, static (value, field) => value.UnknownS4List06 = field, QList(Int32, nameof(FhmPlayerRecordData.UnknownS4List06)))
            .Field(static value => value.UnknownU1S4PairList, static (value, field) => value.UnknownU1S4PairList = field, QList(Object(ByteInt32Pair), nameof(FhmPlayerRecordData.UnknownU1S4PairList)))
            .Field(static value => value.UnknownU110, static (value, field) => value.UnknownU110 = field, Byte)
            .Build();

    internal static FhmPlayersFileHeaderData ReadHeader(BigEndianBinaryReader reader) => Header.Read(reader);

    internal static void WriteHeader(BigEndianBinaryWriter writer, FhmPlayersFileHeaderData value) => Header.Write(writer, value);

    internal static FhmPlayerRecordData ReadPlayer(BigEndianBinaryReader reader) => PlayerRecord.Read(reader);

    internal static void WritePlayer(BigEndianBinaryWriter writer, FhmPlayerRecordData value) => PlayerRecord.Write(writer, value);

    internal static void ValidatePlayerCount(int count)
    {
        if (count < 0 || count > MaximumCollectionCount)
        {
            throw new InvalidDataException($"Invalid players count {count}.");
        }
    }

    private static FhmOptionalPlayerRoleInstanceData ReadOptionalPlayerRole(BigEndianBinaryReader reader)
    {
        var presence = reader.ReadInt32();
        return presence switch
        {
            -1 => new FhmOptionalPlayerRoleInstanceData { Value = null },
            0 => new FhmOptionalPlayerRoleInstanceData { Value = PlayerRole.Read(reader) },
            _ => throw new InvalidDataException($"Invalid player role presence {presence}."),
        };
    }

    private static void WriteOptionalPlayerRole(BigEndianBinaryWriter writer, FhmOptionalPlayerRoleInstanceData value)
    {
        if (value.Value is null)
        {
            writer.WriteInt32(-1);
            return;
        }

        writer.WriteInt32(0);
        PlayerRole.Write(writer, value.Value);
    }

    private static ValueCodec<QList<T>> QList<T>(ValueCodec<T> element, string fieldName) =>
        new(
            reader =>
            {
                var length = reader.ReadInt32();
                if (length < 0 || length > MaximumCollectionCount)
                {
                    throw new InvalidDataException($"Invalid {fieldName} count {length}.");
                }

                var items = new List<T>(length);
                for (var index = 0; index < length; index++)
                {
                    items.Add(element.Read(reader));
                }

                return new QList<T> { Length = length, Items = items };
            },
            (writer, value) =>
            {
                writer.WriteInt32(value.Length);
                foreach (var item in value.Items)
                {
                    element.Write(writer, item);
                }
            });

    private static QString ReadQString(BigEndianBinaryReader reader)
    {
        var byteLength = reader.ReadInt32();
        if (byteLength == -1)
        {
            return new QString { Value = null };
        }

        if (byteLength < 0 || (byteLength & 1) != 0)
        {
            throw new InvalidDataException($"Qt QString has invalid byte length {byteLength}.");
        }

        var characters = new char[byteLength / sizeof(char)];
        for (var index = 0; index < characters.Length; index++)
        {
            characters[index] = (char)reader.ReadUInt16();
        }

        return new QString { Value = new string(characters) };
    }

    private static void WriteQString(BigEndianBinaryWriter writer, QString value)
    {
        if (value.Value is null)
        {
            writer.WriteInt32(-1);
            return;
        }

        writer.WriteInt32(checked(value.Value.Length * sizeof(char)));
        foreach (var character in value.Value)
        {
            writer.WriteUInt16(character);
        }
    }
}
