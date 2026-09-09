using Mapster;
using Shuttle.BinarySerde.Common.QFormat;
using Shuttle.Fhm.Serde.Domain.Binary;
using Shuttle.Fhm.Serde.Domain.Model;
using Shuttle.Fhm.Serde.Wire.Players;

namespace Shuttle.Fhm.Serde.Domain.Files;

/// <summary>Maps the stable player object model to the direct players wire contract.</summary>
internal static class FhmPlayerWireMapper
{
    internal const int MaximumCollectionCount = FhmPlayersFileCodec.MaximumCollectionCount;

    private static readonly TypeAdapterConfig MappingConfiguration = CreateMappingConfiguration();

    internal static FhmPlayerRecordData ToWire(FhmPlayerRecord value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var result = value.Adapt<FhmPlayerRecordData>(MappingConfiguration);
        ValidateWire(result);
        return result;
    }

    internal static FhmPlayerRecord FromWire(FhmPlayerRecordData value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ValidateWire(value);
        return FromValidatedWire(value);
    }

    /// <summary>Maps a player record whose wire contract has already been validated.</summary>
    internal static FhmPlayerRecord FromValidatedWire(FhmPlayerRecordData value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Adapt<FhmPlayerRecord>(MappingConfiguration);
    }

    internal static void ValidateWire(FhmPlayersFileData value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ValidateCount(value.Players, value.PlayerCount, nameof(value.Players));
        foreach (var player in value.Players)
        {
            ValidateWire(player);
        }
    }

    internal static void ValidateWire(FhmPlayerRecordData value)
    {
        ArgumentNullException.ThrowIfNull(value);

        ValidateCount(value.UnknownU2Values01, 3, nameof(value.UnknownU2Values01));
        ValidateCount(value.UnknownS4Values01, 6, nameof(value.UnknownS4Values01));
        ValidateCount(value.UnknownU2Values02, 3, nameof(value.UnknownU2Values02));
        ValidatePositionRatings(value.PositionRatings);
        ValidateCount(value.UnknownU2Values03, 2, nameof(value.UnknownU2Values03));
        ValidateQList(value.UnknownRecordList01, nameof(value.UnknownRecordList01));
        foreach (var item in value.UnknownRecordList01.Items)
        {
            ValidateFixed24Record(item);
        }

        ValidateCount(value.UnknownU2Values04, 2, nameof(value.UnknownU2Values04));
        ValidateCount(value.UnknownU1Values01, 2, nameof(value.UnknownU1Values01));
        ValidateQList(value.Contracts, nameof(value.Contracts));
        foreach (var item in value.Contracts.Items)
        {
            ValidateContract(item);
        }

        ValidateCount(value.UnknownS4Values02, 2, nameof(value.UnknownS4Values02));
        ValidateCount(value.UnknownU1Values02, 2, nameof(value.UnknownU1Values02));
        ValidateCount(value.UnknownS4Values03, 3, nameof(value.UnknownS4Values03));
        ValidateCount(value.UnknownU2Values05, 15, nameof(value.UnknownU2Values05));
        ValidateQList(value.UnknownS4List01, nameof(value.UnknownS4List01));
        ValidateCount(value.UnknownU2Values06, 3, nameof(value.UnknownU2Values06));
        ValidateQList(value.UnknownPairList01, nameof(value.UnknownPairList01));
        foreach (var item in value.UnknownPairList01.Items)
        {
            ValidateFixed8Record(item);
        }

        ValidateQList(value.AggregateSkaterStats01, nameof(value.AggregateSkaterStats01));
        foreach (var item in value.AggregateSkaterStats01.Items)
        {
            ValidateAggregateSkaterStats(item);
        }

        ValidateQList(value.AggregateGoalieStats01, nameof(value.AggregateGoalieStats01));
        foreach (var item in value.AggregateGoalieStats01.Items)
        {
            ValidateAggregateGoalieStats(item);
        }

        ValidateQList(value.AggregateSkaterStats02, nameof(value.AggregateSkaterStats02));
        foreach (var item in value.AggregateSkaterStats02.Items)
        {
            ValidateAggregateSkaterStats(item);
        }

        ValidateQList(value.AggregateGoalieStats02, nameof(value.AggregateGoalieStats02));
        foreach (var item in value.AggregateGoalieStats02.Items)
        {
            ValidateAggregateGoalieStats(item);
        }

        ValidateQList(value.DetailedSkaterGameStats, nameof(value.DetailedSkaterGameStats));
        foreach (var item in value.DetailedSkaterGameStats.Items)
        {
            ValidateDetailedSkaterStats(item);
        }

        ValidateQList(value.DetailedGoalieGameStats, nameof(value.DetailedGoalieGameStats));
        foreach (var item in value.DetailedGoalieGameStats.Items)
        {
            ValidateDetailedGoalieStats(item);
        }

        ValidateCount(value.UnknownU1Values03, 2, nameof(value.UnknownU1Values03));
        ValidateCount(value.UnknownF8Values01, 2, nameof(value.UnknownF8Values01));
        ValidateCount(value.UnknownS4Values04, 3, nameof(value.UnknownS4Values04));
        ValidateCount(value.UnknownU2Values07, 5, nameof(value.UnknownU2Values07));
        ValidateQList(value.UnknownS4List02, nameof(value.UnknownS4List02));
        ValidateCount(value.UnknownU1Values04, 3, nameof(value.UnknownU1Values04));
        ValidateQList(value.UnknownS4List03, nameof(value.UnknownS4List03));
        ValidateCount(value.UnknownF8Values02, 3, nameof(value.UnknownF8Values02));
        ValidateCount(value.UnknownS4Values05, 2, nameof(value.UnknownS4Values05));
        ValidateCount(value.UnknownU2Values08, 3, nameof(value.UnknownU2Values08));
        ValidateCount(value.UnknownF8Values03, 2, nameof(value.UnknownF8Values03));
        ValidateCount(value.UnknownU1Values05, 4, nameof(value.UnknownU1Values05));
        ValidateCount(value.UnknownS4Values06, 2, nameof(value.UnknownS4Values06));
        ValidateCount(value.UnknownU1Values06, 5, nameof(value.UnknownU1Values06));
        ValidateCount(value.UnknownU2Values09, 2, nameof(value.UnknownU2Values09));
        ValidateCount(value.UnknownU1Values07, 5, nameof(value.UnknownU1Values07));
        ValidateCount(value.UnknownU2Values10, 6, nameof(value.UnknownU2Values10));
        ValidateCount(value.UnknownU1Values08, 4, nameof(value.UnknownU1Values08));
        ValidateCount(value.UnknownU2Values11, 2, nameof(value.UnknownU2Values11));
        ValidateCount(value.UnknownF8Values04, 5, nameof(value.UnknownF8Values04));
        ValidateQList(value.UnknownU2List01, nameof(value.UnknownU2List01));
        ValidateCount(value.UnknownF8Values05, 5, nameof(value.UnknownF8Values05));
        ValidateCount(value.UnknownU2Values12, 4, nameof(value.UnknownU2Values12));
        ValidateQList(value.DatedStringRecords, nameof(value.DatedStringRecords));
        foreach (var item in value.DatedStringRecords.Items)
        {
            ValidateDatedStringRecord(item);
        }

        ValidateCount(value.UnknownU2Values13, 2, nameof(value.UnknownU2Values13));
        ValidateCount(value.UnknownU2Values14, 3, nameof(value.UnknownU2Values14));
        ValidateCount(value.UnknownF8Values06, 5, nameof(value.UnknownF8Values06));
        ValidateQList(value.UnknownRecordList02, nameof(value.UnknownRecordList02));
        foreach (var item in value.UnknownRecordList02.Items)
        {
            ValidateFixed8Record(item);
        }

        ValidateQList(value.UnknownRecordList03, nameof(value.UnknownRecordList03));
        foreach (var item in value.UnknownRecordList03.Items)
        {
            ValidateFixed23Record(item);
        }

        ValidateCount(value.UnknownU2Values15, 2, nameof(value.UnknownU2Values15));
        ValidateCount(value.UnknownU2Values16, 2, nameof(value.UnknownU2Values16));
        ValidateCount(value.UnknownU2Values17, 2, nameof(value.UnknownU2Values17));
        ValidateCount(value.UnknownS4Values07, 4, nameof(value.UnknownS4Values07));
        ValidateOptionalRole(value.PrimaryRole);
        ValidateOptionalRole(value.SupplementaryRole);
        ValidateCount(value.UnknownU2Values18, 2, nameof(value.UnknownU2Values18));
        ValidateCount(value.UnknownU1Values09, 2, nameof(value.UnknownU1Values09));
        ValidateQList(value.UnknownU1PairList, nameof(value.UnknownU1PairList));
        ValidateCount(value.UnknownS4Values08, 2, nameof(value.UnknownS4Values08));
        ValidateQList(value.UnknownDatedU2Records, nameof(value.UnknownDatedU2Records));
        foreach (var item in value.UnknownDatedU2Records.Items)
        {
            ValidateDatedUshortRecord(item);
        }

        ValidateCount(value.UnknownF8Values07, 3, nameof(value.UnknownF8Values07));
        ValidateCount(value.UnknownU1Values10, 2, nameof(value.UnknownU1Values10));
        ValidateQList(value.UnknownS4List04, nameof(value.UnknownS4List04));
        ValidateCount(value.UnknownU1Values11, 3, nameof(value.UnknownU1Values11));
        ValidateQList(value.UnknownRecordList04, nameof(value.UnknownRecordList04));
        foreach (var item in value.UnknownRecordList04.Items)
        {
            ValidateFixed16Record(item);
        }

        ValidateQList(value.SpecialAbilities, nameof(value.SpecialAbilities));
        ValidateQList(value.UnknownS4List05, nameof(value.UnknownS4List05));
        ValidateCount(value.UnknownU1Values12, 5, nameof(value.UnknownU1Values12));
        ValidateQList(value.UnknownS4List06, nameof(value.UnknownS4List06));
        ValidateQList(value.UnknownU1S4PairList, nameof(value.UnknownU1S4PairList));
    }

    private static TypeAdapterConfig CreateMappingConfiguration()
    {
        var configuration = new TypeAdapterConfig();

        configuration.NewConfig<string, QString>()
            .MapWith(value => new QString { Value = value });
        configuration.NewConfig<QString, string>()
            .MapWith(value => value.Value!);
        configuration.NewConfig<FhmDate, QDate>()
            .MapWith(value => new QDate { Year = value.Year, Month = value.Month, Day = value.Day });
        configuration.NewConfig<QDate, FhmDate>()
            .MapWith(value => new FhmDate(value.Year, value.Month, value.Day));
        configuration.NewConfig<FhmOpaqueBytes, byte[]>()
            .MapWith(value => value.Value.ToArray());
        configuration.NewConfig<byte[], FhmOpaqueBytes>()
            .MapWith(value => new FhmOpaqueBytes(value.ToArray()));

        ConfigureFixedRecord<FhmFixed2Record, FhmFixed2RecordData>(configuration);
        ConfigureFixedRecord<FhmFixed8Record, FhmFixed8RecordData>(configuration);
        ConfigureFixedRecord<FhmFixed16Record, FhmFixed16RecordData>(configuration);
        ConfigureFixedRecord<FhmFixed23Record, FhmFixed23RecordData>(configuration);
        ConfigureFixedRecord<FhmFixed24Record, FhmFixed24RecordData>(configuration);
        ConfigureFixedRecord<FhmByteInt32Pair, FhmByteInt32PairData>(configuration);

        configuration.NewConfig<FhmPlayerPositionRatings, FhmPlayerPositionRatingsData>()
            .Map(destination => destination.RawValues, source => ToQList(source.RawValues, static value => value));
        configuration.NewConfig<FhmPlayerPositionRatingsData, FhmPlayerPositionRatings>()
            .Ignore(destination => destination.RawValues)
            .AfterMapping((source, destination) =>
                CopyCollection(source.RawValues.Items, destination.RawValues, nameof(source.RawValues)));

        configuration.NewConfig<FhmPlayerRoleInstance, FhmPlayerRoleInstanceData>();
        configuration.NewConfig<FhmPlayerRoleInstanceData, FhmPlayerRoleInstance>()
            .Ignore(destination => destination.UseOverride)
            .Ignore(destination => destination.TendencyValue)
            .AfterMapping((source, destination) =>
            {
                CopyCollection(source.UseOverride, destination.UseOverride, nameof(source.UseOverride));
                CopyCollection(source.TendencyValue, destination.TendencyValue, nameof(source.TendencyValue));
            });

        configuration.NewConfig<FhmPlayerAttributes, FhmPlayerAttributesData>();
        configuration.NewConfig<FhmPlayerAttributesData, FhmPlayerAttributes>();

        configuration.NewConfig<FhmDatedStringRecord, FhmDatedStringRecordData>();
        configuration.NewConfig<FhmDatedStringRecordData, FhmDatedStringRecord>()
            .Ignore(destination => destination.UnknownS4Values01)
            .Ignore(destination => destination.UnknownS4Values02)
            .AfterMapping((source, destination) =>
            {
                CopyCollection(source.UnknownS4Values01, destination.UnknownS4Values01, nameof(source.UnknownS4Values01));
                CopyCollection(source.UnknownS4Values02, destination.UnknownS4Values02, nameof(source.UnknownS4Values02));
            });

        configuration.NewConfig<FhmDatedUshortRecord, FhmDatedUshortRecordData>();
        configuration.NewConfig<FhmDatedUshortRecordData, FhmDatedUshortRecord>()
            .Ignore(destination => destination.UnknownU2Values)
            .AfterMapping((source, destination) =>
                CopyCollection(source.UnknownU2Values, destination.UnknownU2Values, nameof(source.UnknownU2Values)));

        configuration.NewConfig<FhmAggregateSkaterStats, FhmAggregateSkaterStatsData>();
        configuration.NewConfig<FhmAggregateSkaterStatsData, FhmAggregateSkaterStats>()
            .Ignore(destination => destination.HeaderU2)
            .Ignore(destination => destination.CountersU201)
            .Ignore(destination => destination.ValuesS401)
            .Ignore(destination => destination.CountersU202)
            .Ignore(destination => destination.ValuesF801)
            .Ignore(destination => destination.ValuesF802)
            .Ignore(destination => destination.ValuesU201)
            .Ignore(destination => destination.FlagsU1)
            .AfterMapping(MapAggregateSkaterStatsFromWire);
        configuration.NewConfig<FhmAggregateGoalieStats, FhmAggregateGoalieStatsData>();
        configuration.NewConfig<FhmAggregateGoalieStatsData, FhmAggregateGoalieStats>()
            .Ignore(destination => destination.HeaderU2)
            .Ignore(destination => destination.ValuesU201)
            .Ignore(destination => destination.CountersU2)
            .AfterMapping(MapAggregateGoalieStatsFromWire);
        configuration.NewConfig<FhmDetailedSkaterGameStats, FhmDetailedSkaterGameStatsData>();
        configuration.NewConfig<FhmDetailedSkaterGameStatsData, FhmDetailedSkaterGameStats>()
            .Ignore(destination => destination.HeaderU2)
            .Ignore(destination => destination.CountersU201)
            .Ignore(destination => destination.ValuesS401)
            .Ignore(destination => destination.CountersU202)
            .Ignore(destination => destination.ValuesF8)
            .Ignore(destination => destination.ValuesU201)
            .Ignore(destination => destination.TrailingU1)
            .AfterMapping(MapDetailedSkaterStatsFromWire);
        configuration.NewConfig<FhmDetailedGoalieGameStats, FhmDetailedGoalieGameStatsData>();
        configuration.NewConfig<FhmDetailedGoalieGameStatsData, FhmDetailedGoalieGameStats>()
            .Ignore(destination => destination.HeaderU2)
            .Ignore(destination => destination.ValuesU201)
            .Ignore(destination => destination.CountersU201)
            .Ignore(destination => destination.CountersU202)
            .Ignore(destination => destination.ValuesU202)
            .AfterMapping(MapDetailedGoalieStatsFromWire);

        configuration.NewConfig<FhmPlayerContract, FhmPlayerContractData>()
            .Map(destination => destination.Salaries, source => ToWireSalaries(source.Salaries))
            .Map(destination => destination.UnknownU1List01, source => ToQList(source.UnknownU1List01, static value => value))
            .Map(destination => destination.UnknownU1List02, source => ToQList(source.UnknownU1List02, static value => value));
        configuration.NewConfig<FhmPlayerContractData, FhmPlayerContract>()
            .Ignore(destination => destination.UnknownS4Values01)
            .Ignore(destination => destination.UnknownU2Values01)
            .Ignore(destination => destination.Salaries)
            .Ignore(destination => destination.UnknownU2Values02)
            .Ignore(destination => destination.UnknownS4Values02)
            .Ignore(destination => destination.UnknownU1Values01)
            .Ignore(destination => destination.UnknownS4Values03)
            .Ignore(destination => destination.UnknownU1Values02)
            .Ignore(destination => destination.UnknownU1Values03)
            .Ignore(destination => destination.UnknownU1Values04)
            .Ignore(destination => destination.UnknownU1Values05)
            .Ignore(destination => destination.UnknownU2Values03)
            .Ignore(destination => destination.UnknownU1Values06)
            .Ignore(destination => destination.UnknownU1List01)
            .Ignore(destination => destination.UnknownU1List02)
            .Ignore(destination => destination.UnknownU1Values07)
            .AfterMapping(MapContractFromWire);

        configuration.NewConfig<FhmPlayerRecord, FhmPlayerRecordData>()
            .Map(destination => destination.UnknownRecordList01, source => ToQList(source.UnknownRecordList01, MapToWire<FhmFixed24Record, FhmFixed24RecordData>))
            .Map(destination => destination.Contracts, source => ToQList(source.Contracts, MapToWire<FhmPlayerContract, FhmPlayerContractData>))
            .Map(destination => destination.UnknownS4List01, source => ToQList(source.UnknownS4List01, static value => value))
            .Map(destination => destination.UnknownPairList01, source => ToQList(source.UnknownPairList01, MapToWire<FhmFixed8Record, FhmFixed8RecordData>))
            .Map(destination => destination.AggregateSkaterStats01, source => ToQList(source.AggregateSkaterStats01, MapToWire<FhmAggregateSkaterStats, FhmAggregateSkaterStatsData>))
            .Map(destination => destination.AggregateGoalieStats01, source => ToQList(source.AggregateGoalieStats01, MapToWire<FhmAggregateGoalieStats, FhmAggregateGoalieStatsData>))
            .Map(destination => destination.AggregateSkaterStats02, source => ToQList(source.AggregateSkaterStats02, MapToWire<FhmAggregateSkaterStats, FhmAggregateSkaterStatsData>))
            .Map(destination => destination.AggregateGoalieStats02, source => ToQList(source.AggregateGoalieStats02, MapToWire<FhmAggregateGoalieStats, FhmAggregateGoalieStatsData>))
            .Map(destination => destination.DetailedSkaterGameStats, source => ToQList(source.DetailedSkaterGameStats, MapToWire<FhmDetailedSkaterGameStats, FhmDetailedSkaterGameStatsData>))
            .Map(destination => destination.DetailedGoalieGameStats, source => ToQList(source.DetailedGoalieGameStats, MapToWire<FhmDetailedGoalieGameStats, FhmDetailedGoalieGameStatsData>))
            .Map(destination => destination.UnknownS4List02, source => ToQList(source.UnknownS4List02, static value => value))
            .Map(destination => destination.UnknownS4List03, source => ToQList(source.UnknownS4List03, static value => value))
            .Map(destination => destination.UnknownU2List01, source => ToQList(source.UnknownU2List01, static value => value))
            .Map(destination => destination.DatedStringRecords, source => ToQList(source.DatedStringRecords, MapToWire<FhmDatedStringRecord, FhmDatedStringRecordData>))
            .Map(destination => destination.UnknownRecordList02, source => ToQList(source.UnknownRecordList02, MapToWire<FhmFixed8Record, FhmFixed8RecordData>))
            .Map(destination => destination.UnknownRecordList03, source => ToQList(source.UnknownRecordList03, MapToWire<FhmFixed23Record, FhmFixed23RecordData>))
            .Map(destination => destination.PrimaryRole, source => ToWireRole(source.TacticalRole))
            .Map(destination => destination.SupplementaryRole, source => ToWireRole(source.SecondaryTacticalRole))
            .Map(destination => destination.UnknownU1PairList, source => ToQList(source.UnknownU1PairList, MapToWire<FhmFixed2Record, FhmFixed2RecordData>))
            .Map(destination => destination.UnknownDatedU2Records, source => ToQList(source.UnknownDatedU2Records, MapToWire<FhmDatedUshortRecord, FhmDatedUshortRecordData>))
            .Map(destination => destination.UnknownS4List04, source => ToQList(source.UnknownS4List04, static value => value))
            .Map(destination => destination.UnknownRecordList04, source => ToQList(source.UnknownRecordList04, MapToWire<FhmFixed16Record, FhmFixed16RecordData>))
            .Map(destination => destination.SpecialAbilities, source => ToQList(source.SpecialAbilities, static value => value))
            .Map(destination => destination.UnknownS4List05, source => ToQList(source.UnknownS4List05, static value => value))
            .Map(destination => destination.UnknownS4List06, source => ToQList(source.UnknownS4List06, static value => value))
            .Map(destination => destination.UnknownU1S4PairList, source => ToQList(source.UnknownU1S4PairList, MapToWire<FhmByteInt32Pair, FhmByteInt32PairData>));

        configuration.NewConfig<FhmPlayerRecordData, FhmPlayerRecord>()
            .Ignore(destination => destination.UnknownU2Values01)
            .Ignore(destination => destination.UnknownS4Values01)
            .Ignore(destination => destination.UnknownU2Values02)
            .Ignore(destination => destination.PositionRatings)
            .Ignore(destination => destination.UnknownU2Values03)
            .Ignore(destination => destination.RatingAttributes)
            .Ignore(destination => destination.UnknownRecordList01)
            .Ignore(destination => destination.UnknownU2Values04)
            .Ignore(destination => destination.UnknownU1Values01)
            .Ignore(destination => destination.Contracts)
            .Ignore(destination => destination.UnknownS4Values02)
            .Ignore(destination => destination.UnknownU1Values02)
            .Ignore(destination => destination.UnknownS4Values03)
            .Ignore(destination => destination.UnknownU2Values05)
            .Ignore(destination => destination.UnknownS4List01)
            .Ignore(destination => destination.UnknownU2Values06)
            .Ignore(destination => destination.UnknownPairList01)
            .Ignore(destination => destination.AggregateSkaterStats01)
            .Ignore(destination => destination.AggregateGoalieStats01)
            .Ignore(destination => destination.AggregateSkaterStats02)
            .Ignore(destination => destination.AggregateGoalieStats02)
            .Ignore(destination => destination.DetailedSkaterGameStats)
            .Ignore(destination => destination.DetailedGoalieGameStats)
            .Ignore(destination => destination.UnknownU1Values03)
            .Ignore(destination => destination.UnknownF8Values01)
            .Ignore(destination => destination.UnknownS4Values04)
            .Ignore(destination => destination.UnknownU2Values07)
            .Ignore(destination => destination.UnknownS4List02)
            .Ignore(destination => destination.UnknownU1Values04)
            .Ignore(destination => destination.UnknownS4List03)
            .Ignore(destination => destination.UnknownF8Values02)
            .Ignore(destination => destination.UnknownS4Values05)
            .Ignore(destination => destination.UnknownU2Values08)
            .Ignore(destination => destination.UnknownF8Values03)
            .Ignore(destination => destination.UnknownU1Values05)
            .Ignore(destination => destination.UnknownS4Values06)
            .Ignore(destination => destination.UnknownU1Values06)
            .Ignore(destination => destination.UnknownU2Values09)
            .Ignore(destination => destination.UnknownU1Values07)
            .Ignore(destination => destination.UnknownU2Values10)
            .Ignore(destination => destination.UnknownU1Values08)
            .Ignore(destination => destination.UnknownU2Values11)
            .Ignore(destination => destination.UnknownF8Values04)
            .Ignore(destination => destination.UnknownU2List01)
            .Ignore(destination => destination.UnknownF8Values05)
            .Ignore(destination => destination.UnknownU2Values12)
            .Ignore(destination => destination.DatedStringRecords)
            .Ignore(destination => destination.UnknownU2Values13)
            .Ignore(destination => destination.UnknownU2Values14)
            .Ignore(destination => destination.UnknownF8Values06)
            .Ignore(destination => destination.UnknownRecordList02)
            .Ignore(destination => destination.UnknownRecordList03)
            .Ignore(destination => destination.UnknownU2Values15)
            .Ignore(destination => destination.UnknownU2Values16)
            .Ignore(destination => destination.UnknownU2Values17)
            .Ignore(destination => destination.UnknownS4Values07)
            .Ignore(destination => destination.UnknownU1PairList)
            .Ignore(destination => destination.UnknownU2Values18)
            .Ignore(destination => destination.UnknownU1Values09)
            .Ignore(destination => destination.UnknownDatedU2Records)
            .Ignore(destination => destination.UnknownS4Values08)
            .Ignore(destination => destination.UnknownF8Values07)
            .Ignore(destination => destination.UnknownU1Values10)
            .Ignore(destination => destination.UnknownS4List04)
            .Ignore(destination => destination.UnknownU1Values11)
            .Ignore(destination => destination.UnknownRecordList04)
            .Ignore(destination => destination.SpecialAbilities)
            .Ignore(destination => destination.UnknownS4List05)
            .Ignore(destination => destination.UnknownU1Values12)
            .Ignore(destination => destination.UnknownS4List06)
            .Ignore(destination => destination.UnknownU1S4PairList)
            .AfterMapping(MapPlayerFromWire);

        configuration.Compile();
        return configuration;
    }

    private static void ConfigureFixedRecord<TDomain, TWire>(TypeAdapterConfig configuration)
        where TDomain : class
        where TWire : class
    {
        configuration.NewConfig<TDomain, TWire>();
        configuration.NewConfig<TWire, TDomain>();
    }

    private static TWire MapToWire<TDomain, TWire>(TDomain value)
        where TDomain : class
        where TWire : class =>
        value.Adapt<TWire>(MappingConfiguration);

    private static TDomain MapFromWire<TWire, TDomain>(TWire value)
        where TWire : class
        where TDomain : class =>
        value.Adapt<TDomain>(MappingConfiguration);

    private static QList<TWire> ToQList<TDomain, TWire>(IList<TDomain> source, Func<TDomain, TWire> map)
    {
        var items = new List<TWire>(source.Count);
        foreach (var item in source)
        {
            items.Add(map(item));
        }

        return new QList<TWire> { Length = items.Count, Items = items };
    }

    private static FhmOptionalPlayerRoleInstanceData ToWireRole(FhmPlayerRoleInstance? value) =>
        new()
        {
            Value = value is null ? null : MapToWire<FhmPlayerRoleInstance, FhmPlayerRoleInstanceData>(value),
        };

    private static FhmPlayerRoleInstance? FromWireRole(FhmOptionalPlayerRoleInstanceData value) =>
        value.Value is null ? null : MapFromWire<FhmPlayerRoleInstanceData, FhmPlayerRoleInstance>(value.Value);

    private static QList<int> ToWireSalaries(IList<FhmContractYearSalary> salaries)
    {
        var values = new List<int>(salaries.Count * 2);
        foreach (var salary in salaries)
        {
            values.Add(salary.MajorLeagueSalary ?? FhmNullConstants.Null);
            values.Add(salary.MinorLeagueSalary ?? FhmNullConstants.Null);
        }

        if (values.Count != FhmPlayerContract.MaximumYears * 2)
        {
            throw new FhmFormatException(
                $"Player contract contains {values.Count / 2} salary years; expected {FhmPlayerContract.MaximumYears}.");
        }

        return new QList<int> { Length = values.Count, Items = values };
    }

    private static void MapPlayerFromWire(FhmPlayerRecordData source, FhmPlayerRecord destination)
    {
        source.PositionRatings.Adapt(destination.PositionRatings, MappingConfiguration);
        source.RatingAttributes.Adapt(destination.RatingAttributes, MappingConfiguration);
        CopyCollection(source.UnknownU2Values01, destination.UnknownU2Values01, nameof(source.UnknownU2Values01));
        CopyCollection(source.UnknownS4Values01, destination.UnknownS4Values01, nameof(source.UnknownS4Values01));
        CopyCollection(source.UnknownU2Values02, destination.UnknownU2Values02, nameof(source.UnknownU2Values02));
        CopyCollection(source.UnknownU2Values03, destination.UnknownU2Values03, nameof(source.UnknownU2Values03));
        CopyMappedCollection(source.UnknownRecordList01.Items, destination.UnknownRecordList01, MapFromWire<FhmFixed24RecordData, FhmFixed24Record>, nameof(source.UnknownRecordList01));
        CopyCollection(source.UnknownU2Values04, destination.UnknownU2Values04, nameof(source.UnknownU2Values04));
        CopyCollection(source.UnknownU1Values01, destination.UnknownU1Values01, nameof(source.UnknownU1Values01));
        CopyMappedCollection(source.Contracts.Items, destination.Contracts, MapFromWire<FhmPlayerContractData, FhmPlayerContract>, nameof(source.Contracts));
        CopyCollection(source.UnknownS4Values02, destination.UnknownS4Values02, nameof(source.UnknownS4Values02));
        CopyCollection(source.UnknownU1Values02, destination.UnknownU1Values02, nameof(source.UnknownU1Values02));
        CopyCollection(source.UnknownS4Values03, destination.UnknownS4Values03, nameof(source.UnknownS4Values03));
        CopyCollection(source.UnknownU2Values05, destination.UnknownU2Values05, nameof(source.UnknownU2Values05));
        CopyCollection(source.UnknownS4List01.Items, destination.UnknownS4List01, nameof(source.UnknownS4List01));
        CopyCollection(source.UnknownU2Values06, destination.UnknownU2Values06, nameof(source.UnknownU2Values06));
        CopyMappedCollection(source.UnknownPairList01.Items, destination.UnknownPairList01, MapFromWire<FhmFixed8RecordData, FhmFixed8Record>, nameof(source.UnknownPairList01));
        CopyMappedCollection(source.AggregateSkaterStats01.Items, destination.AggregateSkaterStats01, MapFromWire<FhmAggregateSkaterStatsData, FhmAggregateSkaterStats>, nameof(source.AggregateSkaterStats01));
        CopyMappedCollection(source.AggregateGoalieStats01.Items, destination.AggregateGoalieStats01, MapFromWire<FhmAggregateGoalieStatsData, FhmAggregateGoalieStats>, nameof(source.AggregateGoalieStats01));
        CopyMappedCollection(source.AggregateSkaterStats02.Items, destination.AggregateSkaterStats02, MapFromWire<FhmAggregateSkaterStatsData, FhmAggregateSkaterStats>, nameof(source.AggregateSkaterStats02));
        CopyMappedCollection(source.AggregateGoalieStats02.Items, destination.AggregateGoalieStats02, MapFromWire<FhmAggregateGoalieStatsData, FhmAggregateGoalieStats>, nameof(source.AggregateGoalieStats02));
        CopyMappedCollection(source.DetailedSkaterGameStats.Items, destination.DetailedSkaterGameStats, MapFromWire<FhmDetailedSkaterGameStatsData, FhmDetailedSkaterGameStats>, nameof(source.DetailedSkaterGameStats));
        CopyMappedCollection(source.DetailedGoalieGameStats.Items, destination.DetailedGoalieGameStats, MapFromWire<FhmDetailedGoalieGameStatsData, FhmDetailedGoalieGameStats>, nameof(source.DetailedGoalieGameStats));
        CopyCollection(source.UnknownU1Values03, destination.UnknownU1Values03, nameof(source.UnknownU1Values03));
        CopyCollection(source.UnknownF8Values01, destination.UnknownF8Values01, nameof(source.UnknownF8Values01));
        CopyCollection(source.UnknownS4Values04, destination.UnknownS4Values04, nameof(source.UnknownS4Values04));
        CopyCollection(source.UnknownU2Values07, destination.UnknownU2Values07, nameof(source.UnknownU2Values07));
        CopyCollection(source.UnknownS4List02.Items, destination.UnknownS4List02, nameof(source.UnknownS4List02));
        CopyCollection(source.UnknownU1Values04, destination.UnknownU1Values04, nameof(source.UnknownU1Values04));
        CopyCollection(source.UnknownS4List03.Items, destination.UnknownS4List03, nameof(source.UnknownS4List03));
        CopyCollection(source.UnknownF8Values02, destination.UnknownF8Values02, nameof(source.UnknownF8Values02));
        CopyCollection(source.UnknownS4Values05, destination.UnknownS4Values05, nameof(source.UnknownS4Values05));
        CopyCollection(source.UnknownU2Values08, destination.UnknownU2Values08, nameof(source.UnknownU2Values08));
        CopyCollection(source.UnknownF8Values03, destination.UnknownF8Values03, nameof(source.UnknownF8Values03));
        CopyCollection(source.UnknownU1Values05, destination.UnknownU1Values05, nameof(source.UnknownU1Values05));
        CopyCollection(source.UnknownS4Values06, destination.UnknownS4Values06, nameof(source.UnknownS4Values06));
        CopyCollection(source.UnknownU1Values06, destination.UnknownU1Values06, nameof(source.UnknownU1Values06));
        CopyCollection(source.UnknownU2Values09, destination.UnknownU2Values09, nameof(source.UnknownU2Values09));
        CopyCollection(source.UnknownU1Values07, destination.UnknownU1Values07, nameof(source.UnknownU1Values07));
        CopyCollection(source.UnknownU2Values10, destination.UnknownU2Values10, nameof(source.UnknownU2Values10));
        CopyCollection(source.UnknownU1Values08, destination.UnknownU1Values08, nameof(source.UnknownU1Values08));
        CopyCollection(source.UnknownU2Values11, destination.UnknownU2Values11, nameof(source.UnknownU2Values11));
        CopyCollection(source.UnknownF8Values04, destination.UnknownF8Values04, nameof(source.UnknownF8Values04));
        CopyCollection(source.UnknownU2List01.Items, destination.UnknownU2List01, nameof(source.UnknownU2List01));
        CopyCollection(source.UnknownF8Values05, destination.UnknownF8Values05, nameof(source.UnknownF8Values05));
        CopyCollection(source.UnknownU2Values12, destination.UnknownU2Values12, nameof(source.UnknownU2Values12));
        CopyMappedCollection(source.DatedStringRecords.Items, destination.DatedStringRecords, MapFromWire<FhmDatedStringRecordData, FhmDatedStringRecord>, nameof(source.DatedStringRecords));
        CopyCollection(source.UnknownU2Values13, destination.UnknownU2Values13, nameof(source.UnknownU2Values13));
        CopyCollection(source.UnknownU2Values14, destination.UnknownU2Values14, nameof(source.UnknownU2Values14));
        CopyCollection(source.UnknownF8Values06, destination.UnknownF8Values06, nameof(source.UnknownF8Values06));
        CopyMappedCollection(source.UnknownRecordList02.Items, destination.UnknownRecordList02, MapFromWire<FhmFixed8RecordData, FhmFixed8Record>, nameof(source.UnknownRecordList02));
        CopyMappedCollection(source.UnknownRecordList03.Items, destination.UnknownRecordList03, MapFromWire<FhmFixed23RecordData, FhmFixed23Record>, nameof(source.UnknownRecordList03));
        CopyCollection(source.UnknownU2Values15, destination.UnknownU2Values15, nameof(source.UnknownU2Values15));
        CopyCollection(source.UnknownU2Values16, destination.UnknownU2Values16, nameof(source.UnknownU2Values16));
        CopyCollection(source.UnknownU2Values17, destination.UnknownU2Values17, nameof(source.UnknownU2Values17));
        CopyCollection(source.UnknownS4Values07, destination.UnknownS4Values07, nameof(source.UnknownS4Values07));
        destination.TacticalRole = FromWireRole(source.PrimaryRole);
        destination.SecondaryTacticalRole = FromWireRole(source.SupplementaryRole);
        CopyCollection(source.UnknownU2Values18, destination.UnknownU2Values18, nameof(source.UnknownU2Values18));
        CopyCollection(source.UnknownU1Values09, destination.UnknownU1Values09, nameof(source.UnknownU1Values09));
        CopyMappedCollection(source.UnknownU1PairList.Items, destination.UnknownU1PairList, MapFromWire<FhmFixed2RecordData, FhmFixed2Record>, nameof(source.UnknownU1PairList));
        CopyCollection(source.UnknownS4Values08, destination.UnknownS4Values08, nameof(source.UnknownS4Values08));
        CopyMappedCollection(source.UnknownDatedU2Records.Items, destination.UnknownDatedU2Records, MapFromWire<FhmDatedUshortRecordData, FhmDatedUshortRecord>, nameof(source.UnknownDatedU2Records));
        CopyCollection(source.UnknownF8Values07, destination.UnknownF8Values07, nameof(source.UnknownF8Values07));
        CopyCollection(source.UnknownU1Values10, destination.UnknownU1Values10, nameof(source.UnknownU1Values10));
        CopyCollection(source.UnknownS4List04.Items, destination.UnknownS4List04, nameof(source.UnknownS4List04));
        CopyCollection(source.UnknownU1Values11, destination.UnknownU1Values11, nameof(source.UnknownU1Values11));
        CopyMappedCollection(source.UnknownRecordList04.Items, destination.UnknownRecordList04, MapFromWire<FhmFixed16RecordData, FhmFixed16Record>, nameof(source.UnknownRecordList04));
        CopyCollection(source.SpecialAbilities.Items, destination.SpecialAbilities, nameof(source.SpecialAbilities));
        CopyCollection(source.UnknownS4List05.Items, destination.UnknownS4List05, nameof(source.UnknownS4List05));
        CopyCollection(source.UnknownU1Values12, destination.UnknownU1Values12, nameof(source.UnknownU1Values12));
        CopyCollection(source.UnknownS4List06.Items, destination.UnknownS4List06, nameof(source.UnknownS4List06));
        CopyMappedCollection(source.UnknownU1S4PairList.Items, destination.UnknownU1S4PairList, MapFromWire<FhmByteInt32PairData, FhmByteInt32Pair>, nameof(source.UnknownU1S4PairList));
    }

    private static void MapContractFromWire(FhmPlayerContractData source, FhmPlayerContract destination)
    {
        CopyCollection(source.UnknownS4Values01, destination.UnknownS4Values01, nameof(source.UnknownS4Values01));
        CopyCollection(source.UnknownU2Values01, destination.UnknownU2Values01, nameof(source.UnknownU2Values01));
        CopySalaries(source.Salaries, destination.Salaries);
        CopyCollection(source.UnknownU2Values02, destination.UnknownU2Values02, nameof(source.UnknownU2Values02));
        CopyCollection(source.UnknownS4Values02, destination.UnknownS4Values02, nameof(source.UnknownS4Values02));
        CopyCollection(source.UnknownU1Values01, destination.UnknownU1Values01, nameof(source.UnknownU1Values01));
        CopyCollection(source.UnknownS4Values03, destination.UnknownS4Values03, nameof(source.UnknownS4Values03));
        CopyCollection(source.UnknownU1Values02, destination.UnknownU1Values02, nameof(source.UnknownU1Values02));
        CopyCollection(source.UnknownU1Values03, destination.UnknownU1Values03, nameof(source.UnknownU1Values03));
        CopyCollection(source.UnknownU1Values04, destination.UnknownU1Values04, nameof(source.UnknownU1Values04));
        CopyCollection(source.UnknownU1Values05, destination.UnknownU1Values05, nameof(source.UnknownU1Values05));
        CopyCollection(source.UnknownU2Values03, destination.UnknownU2Values03, nameof(source.UnknownU2Values03));
        CopyCollection(source.UnknownU1Values06, destination.UnknownU1Values06, nameof(source.UnknownU1Values06));
        CopyCollection(source.UnknownU1List01.Items, destination.UnknownU1List01, nameof(source.UnknownU1List01));
        CopyCollection(source.UnknownU1List02.Items, destination.UnknownU1List02, nameof(source.UnknownU1List02));
        CopyCollection(source.UnknownU1Values07, destination.UnknownU1Values07, nameof(source.UnknownU1Values07));
    }

    private static void MapAggregateSkaterStatsFromWire(FhmAggregateSkaterStatsData source, FhmAggregateSkaterStats destination)
    {
        CopyCollection(source.HeaderU2, destination.HeaderU2, nameof(source.HeaderU2));
        CopyCollection(source.CountersU201, destination.CountersU201, nameof(source.CountersU201));
        CopyCollection(source.ValuesS401, destination.ValuesS401, nameof(source.ValuesS401));
        CopyCollection(source.CountersU202, destination.CountersU202, nameof(source.CountersU202));
        CopyCollection(source.ValuesF801, destination.ValuesF801, nameof(source.ValuesF801));
        CopyCollection(source.ValuesF802, destination.ValuesF802, nameof(source.ValuesF802));
        CopyCollection(source.ValuesU201, destination.ValuesU201, nameof(source.ValuesU201));
        CopyCollection(source.FlagsU1, destination.FlagsU1, nameof(source.FlagsU1));
    }

    private static void MapAggregateGoalieStatsFromWire(FhmAggregateGoalieStatsData source, FhmAggregateGoalieStats destination)
    {
        CopyCollection(source.HeaderU2, destination.HeaderU2, nameof(source.HeaderU2));
        CopyCollection(source.ValuesU201, destination.ValuesU201, nameof(source.ValuesU201));
        CopyCollection(source.CountersU2, destination.CountersU2, nameof(source.CountersU2));
    }

    private static void MapDetailedSkaterStatsFromWire(FhmDetailedSkaterGameStatsData source, FhmDetailedSkaterGameStats destination)
    {
        CopyCollection(source.HeaderU2, destination.HeaderU2, nameof(source.HeaderU2));
        CopyCollection(source.CountersU201, destination.CountersU201, nameof(source.CountersU201));
        CopyCollection(source.ValuesS401, destination.ValuesS401, nameof(source.ValuesS401));
        CopyCollection(source.CountersU202, destination.CountersU202, nameof(source.CountersU202));
        CopyCollection(source.ValuesF8, destination.ValuesF8, nameof(source.ValuesF8));
        CopyCollection(source.ValuesU201, destination.ValuesU201, nameof(source.ValuesU201));
        CopyCollection(source.TrailingU1, destination.TrailingU1, nameof(source.TrailingU1));
    }

    private static void MapDetailedGoalieStatsFromWire(FhmDetailedGoalieGameStatsData source, FhmDetailedGoalieGameStats destination)
    {
        CopyCollection(source.HeaderU2, destination.HeaderU2, nameof(source.HeaderU2));
        CopyCollection(source.ValuesU201, destination.ValuesU201, nameof(source.ValuesU201));
        CopyCollection(source.CountersU201, destination.CountersU201, nameof(source.CountersU201));
        CopyCollection(source.CountersU202, destination.CountersU202, nameof(source.CountersU202));
        CopyCollection(source.ValuesU202, destination.ValuesU202, nameof(source.ValuesU202));
    }

    private static void CopySalaries(QList<int> source, IList<FhmContractYearSalary> destination)
    {
        if (source.Items.Count != FhmPlayerContract.MaximumYears * 2)
        {
            throw new FhmFormatException(
                $"Player contract contains {source.Items.Count} salary values; expected {FhmPlayerContract.MaximumYears * 2}.");
        }

        for (var year = 0; year < FhmPlayerContract.MaximumYears; year++)
        {
            destination[year] = new FhmContractYearSalary
            {
                MajorLeagueSalary = ToNullableSalary(source.Items[year * 2]),
                MinorLeagueSalary = ToNullableSalary(source.Items[(year * 2) + 1]),
            };
        }
    }

    private static int? ToNullableSalary(int value) =>
        value == FhmNullConstants.Null
            ? null
            : value >= 0
                ? value
                : throw new FhmFormatException($"Player contract contains invalid salary value {value}.");

    private static void CopyCollection<T>(IReadOnlyList<T> source, IList<T> destination, string fieldName)
    {
        if (destination is Array && destination.Count != source.Count)
        {
            throw new FhmFormatException($"Fixed player vector contains {source.Count} values; expected {destination.Count}.");
        }

        if (destination is Array)
        {
            for (var index = 0; index < source.Count; index++)
            {
                destination[index] = source[index];
            }

            return;
        }

        destination.Clear();
        foreach (var item in source)
        {
            destination.Add(item);
        }
    }

    private static void CopyMappedCollection<TWire, TDomain>(
        IReadOnlyList<TWire> source,
        IList<TDomain> destination,
        Func<TWire, TDomain> map,
        string fieldName)
    {
        if (destination is Array && destination.Count != source.Count)
        {
            throw new FhmFormatException($"Fixed player vector contains {source.Count} values; expected {destination.Count}.");
        }

        if (destination is Array)
        {
            for (var index = 0; index < source.Count; index++)
            {
                destination[index] = map(source[index]);
            }

            return;
        }

        destination.Clear();
        foreach (var item in source)
        {
            destination.Add(map(item));
        }
    }

    private static void ValidatePositionRatings(FhmPlayerPositionRatingsData value) =>
        ValidateQList(value.RawValues, nameof(value.RawValues));

    private static void ValidateOptionalRole(FhmOptionalPlayerRoleInstanceData value)
    {
        if (value.Value is not null)
        {
            ValidateRole(value.Value);
        }
    }

    private static void ValidateRole(FhmPlayerRoleInstanceData value)
    {
        ValidateCount(value.UseOverride, 9, nameof(value.UseOverride));
        ValidateCount(value.TendencyValue, 9, nameof(value.TendencyValue));
    }

    private static void ValidateContract(FhmPlayerContractData value)
    {
        ValidateCount(value.UnknownS4Values01, 2, nameof(value.UnknownS4Values01));
        ValidateCount(value.UnknownU2Values01, 2, nameof(value.UnknownU2Values01));
        ValidateQList(value.Salaries, nameof(value.Salaries));
        ValidateCount(value.UnknownU2Values02, 3, nameof(value.UnknownU2Values02));
        ValidateCount(value.UnknownS4Values02, 2, nameof(value.UnknownS4Values02));
        ValidateCount(value.UnknownU1Values01, 2, nameof(value.UnknownU1Values01));
        ValidateCount(value.UnknownS4Values03, 4, nameof(value.UnknownS4Values03));
        ValidateCount(value.UnknownU1Values02, 4, nameof(value.UnknownU1Values02));
        ValidateCount(value.UnknownU1Values03, 2, nameof(value.UnknownU1Values03));
        ValidateCount(value.UnknownU1Values04, 3, nameof(value.UnknownU1Values04));
        ValidateCount(value.UnknownU1Values05, 3, nameof(value.UnknownU1Values05));
        ValidateCount(value.UnknownU2Values03, 3, nameof(value.UnknownU2Values03));
        ValidateCount(value.UnknownU1Values06, 4, nameof(value.UnknownU1Values06));
        ValidateQList(value.UnknownU1List01, nameof(value.UnknownU1List01));
        ValidateQList(value.UnknownU1List02, nameof(value.UnknownU1List02));
        ValidateCount(value.UnknownU1Values07, 3, nameof(value.UnknownU1Values07));
    }

    private static void ValidateAggregateSkaterStats(FhmAggregateSkaterStatsData value)
    {
        ValidateCount(value.HeaderU2, 5, nameof(value.HeaderU2));
        ValidateCount(value.CountersU201, 18, nameof(value.CountersU201));
        ValidateCount(value.ValuesS401, 3, nameof(value.ValuesS401));
        ValidateCount(value.CountersU202, 7, nameof(value.CountersU202));
        ValidateCount(value.ValuesF801, 3, nameof(value.ValuesF801));
        ValidateCount(value.ValuesF802, 3, nameof(value.ValuesF802));
        ValidateCount(value.ValuesU201, 2, nameof(value.ValuesU201));
        ValidateCount(value.FlagsU1, 3, nameof(value.FlagsU1));
    }

    private static void ValidateAggregateGoalieStats(FhmAggregateGoalieStatsData value)
    {
        ValidateCount(value.HeaderU2, 5, nameof(value.HeaderU2));
        ValidateCount(value.ValuesU201, 2, nameof(value.ValuesU201));
        ValidateCount(value.CountersU2, 10, nameof(value.CountersU2));
    }

    private static void ValidateDetailedSkaterStats(FhmDetailedSkaterGameStatsData value)
    {
        ValidateCount(value.HeaderU2, 5, nameof(value.HeaderU2));
        ValidateCount(value.CountersU201, 18, nameof(value.CountersU201));
        ValidateCount(value.ValuesS401, 3, nameof(value.ValuesS401));
        ValidateCount(value.CountersU202, 26, nameof(value.CountersU202));
        ValidateCount(value.ValuesF8, 3, nameof(value.ValuesF8));
        ValidateCount(value.ValuesU201, 6, nameof(value.ValuesU201));
        ValidateCount(value.TrailingU1, 10, nameof(value.TrailingU1));
    }

    private static void ValidateDetailedGoalieStats(FhmDetailedGoalieGameStatsData value)
    {
        ValidateCount(value.HeaderU2, 5, nameof(value.HeaderU2));
        ValidateCount(value.ValuesU201, 2, nameof(value.ValuesU201));
        ValidateCount(value.CountersU201, 10, nameof(value.CountersU201));
        ValidateCount(value.CountersU202, 2, nameof(value.CountersU202));
        ValidateCount(value.ValuesU202, 6, nameof(value.ValuesU202));
    }

    private static void ValidateDatedStringRecord(FhmDatedStringRecordData value)
    {
        ValidateCount(value.UnknownS4Values01, 3, nameof(value.UnknownS4Values01));
        ValidateCount(value.UnknownS4Values02, 2, nameof(value.UnknownS4Values02));
    }

    private static void ValidateDatedUshortRecord(FhmDatedUshortRecordData value) =>
        ValidateCount(value.UnknownU2Values, 2, nameof(value.UnknownU2Values));

    private static void ValidateFixed8Record(FhmFixed8RecordData value) =>
        ValidateLength(value.Data, 8, nameof(value.Data));

    private static void ValidateFixed16Record(FhmFixed16RecordData value) =>
        ValidateLength(value.Data, 16, nameof(value.Data));

    private static void ValidateFixed23Record(FhmFixed23RecordData value) =>
        ValidateLength(value.Data, 23, nameof(value.Data));

    private static void ValidateFixed24Record(FhmFixed24RecordData value) =>
        ValidateLength(value.Data, 24, nameof(value.Data));

    private static void ValidateQList<T>(QList<T> value, string fieldName)
    {
        if (value.Length < 0 || value.Length > MaximumCollectionCount || value.Items.Count != value.Length)
        {
            throw new FhmFormatException($"Invalid {fieldName} count {value.Length}.");
        }
    }

    private static void ValidateCount<T>(ICollection<T> value, int expectedCount, string fieldName)
    {
        if (expectedCount < 0 || expectedCount > MaximumCollectionCount || value.Count != expectedCount)
        {
            throw new FhmFormatException($"Invalid {fieldName} count {expectedCount}.");
        }
    }

    private static void ValidateLength(byte[] value, int expectedLength, string fieldName)
    {
        if (value.Length != expectedLength)
        {
            throw new FhmFormatException($"{fieldName} must contain exactly {expectedLength} bytes.");
        }
    }
}
