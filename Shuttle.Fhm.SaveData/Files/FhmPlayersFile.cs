using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.Serde.Players;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>The version-58 <c>players.dat</c> container.</summary>
public sealed class FhmPlayersFile : IFhmSaveFile
{
    /// <summary>Current documented players format version.</summary>
    public const int CurrentVersion = 58;

    /// <inheritdoc />
    public string RelativePath => "players.dat";

    /// <summary>Gets or sets the validated FHM players format version.</summary>
    public int FormatVersion { get; set; } = CurrentVersion;

    /// <summary>Gets player records in serialized order.</summary>
    public IList<FhmPlayerRecord> Players { get; } = [];

    /// <summary>
    /// Inspects the relationship between each serialized record ordinal and its persisted internal identity.
    /// </summary>
    public FhmPlayerInternalIdentityAnalysis AnalyzeInternalIdentities()
    {
        var records = Players
            .Select((player, recordOrdinal) => new FhmPlayerInternalIdentityRecord(recordOrdinal, player.InternalIdentity))
            .ToArray();
        var duplicateIdentities = records
            .GroupBy(record => record.InternalIdentity)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order()
            .ToArray();
        var negativeRecords = records
            .Where(record => record.InternalIdentity < 0)
            .ToArray();
        var outOfRangeRecords = records
            .Where(record => record.InternalIdentity < 0 || record.InternalIdentity >= Players.Count)
            .ToArray();
        var missingIdentities = Enumerable.Range(0, Players.Count)
            .Except(records.Select(record => record.InternalIdentity))
            .ToArray();
        var ordinalMismatches = records
            .Where(record => record.InternalIdentity != record.RecordOrdinal)
            .ToArray();

        return new FhmPlayerInternalIdentityAnalysis(
            records,
            duplicateIdentities,
            negativeRecords,
            outOfRangeRecords,
            missingIdentities,
            ordinalMismatches);
    }

    internal static FhmPlayersFile Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        FhmPlayersFileData wire;
        try
        {
            wire = FhmPlayersFileSerializer.Deserialize(stream);
        }
        catch (InvalidDataException exception)
        {
            throw new FhmFormatException(exception.Message);
        }

        FhmPlayerWireMapper.ValidateWire(wire);
        var result = new FhmPlayersFile
        {
            FormatVersion = wire.FormatVersion,
        };
        if (result.FormatVersion != CurrentVersion)
        {
            throw new FhmFormatException($"players.dat format version {result.FormatVersion} is unsupported; expected {CurrentVersion}.");
        }

        foreach (var player in wire.Players)
        {
            result.Players.Add(FhmPlayerWireMapper.FromWire(player));
        }

        return result;
    }

    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (FormatVersion != CurrentVersion)
        {
            throw new FhmFormatException($"players.dat can write only format version {CurrentVersion}.");
        }

        if (Players.Count > FhmPlayerWireMapper.MaximumCollectionCount)
        {
            throw new FhmFormatException($"Invalid players count {Players.Count}.");
        }

        FhmPlayersFileSerializer.Serialize(stream, new FhmPlayersFileData
        {
            FormatVersion = FormatVersion,
            PlayerCount = Players.Count,
            Players = Players.Select(FhmPlayerWireMapper.ToWire).ToList(),
        });
    }

    /// <summary>Associates a persisted internal player identity with its serialized record ordinal.</summary>
    public sealed record FhmPlayerInternalIdentityRecord(int RecordOrdinal, int InternalIdentity);

    /// <summary>Reports whether player internal identities match the serialized <c>players.dat</c> record order.</summary>
    public sealed record FhmPlayerInternalIdentityAnalysis(
        IReadOnlyList<FhmPlayerInternalIdentityRecord> Records,
        IReadOnlyList<int> DuplicateIdentities,
        IReadOnlyList<FhmPlayerInternalIdentityRecord> NegativeRecords,
        IReadOnlyList<FhmPlayerInternalIdentityRecord> OutOfRangeRecords,
        IReadOnlyList<int> MissingIdentities,
        IReadOnlyList<FhmPlayerInternalIdentityRecord> OrdinalMismatches)
    {
        /// <summary>Gets whether every identity is a unique, nonnegative record ordinal.</summary>
        public bool IsRecordOrdinalInvariant =>
            DuplicateIdentities.Count == 0 &&
            NegativeRecords.Count == 0 &&
            OutOfRangeRecords.Count == 0 &&
            MissingIdentities.Count == 0 &&
            OrdinalMismatches.Count == 0;
    }
}
