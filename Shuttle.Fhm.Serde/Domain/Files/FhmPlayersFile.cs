using Shuttle.Fhm.Serde.Domain.Binary;
using Shuttle.Fhm.Serde.Wire.Players;

namespace Shuttle.Fhm.Serde.Domain.Files;

/// <summary>The fixed header that precedes records in a <c>players.dat</c> file.</summary>
public readonly record struct FhmPlayersFileHeader(int FormatVersion, int PlayerCount);

/// <summary>
/// Streams validated <c>players.dat</c> records in serialized order.
/// </summary>
/// <remarks>
/// The caller owns the source stream and must keep it open for the entire enumeration. Disposing this reader does
/// not dispose the source stream. A reader may be enumerated exactly once.
/// </remarks>
public sealed class FhmPlayersFileReader : IEnumerable<FhmPlayerRecord>, IDisposable
{
    private readonly FhmPlayersFileWireReader reader;
    private bool enumerationStarted;
    private bool disposed;

    /// <summary>Initializes a reader over a caller-owned <c>players.dat</c> stream.</summary>
    public FhmPlayersFileReader(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);

        FhmPlayersFileWireReader? createdReader = null;
        try
        {
            createdReader = FhmPlayersFileSerializer.CreateReader(source);
            Header = new FhmPlayersFileHeader(
                createdReader.Header.FormatVersion,
                createdReader.Header.PlayerCount);
            if (FormatVersion != FhmPlayersFile.CurrentVersion)
            {
                throw new FhmFormatException(
                    $"players.dat format version {FormatVersion} is unsupported; expected {FhmPlayersFile.CurrentVersion}.");
            }

            if (PlayerCount < 0 || PlayerCount > FhmPlayerWireMapper.MaximumCollectionCount)
            {
                throw new FhmFormatException($"Invalid players count {PlayerCount}.");
            }

            reader = createdReader;
        }
        catch (InvalidDataException exception)
        {
            createdReader?.Dispose();
            throw new FhmFormatException(exception.Message);
        }
        catch
        {
            createdReader?.Dispose();
            throw;
        }
    }

    /// <summary>Gets the decoded file header.</summary>
    public FhmPlayersFileHeader Header { get; }

    /// <summary>Gets the validated players format version.</summary>
    public int FormatVersion => Header.FormatVersion;

    /// <summary>Gets the validated number of records available from this reader.</summary>
    public int PlayerCount => Header.PlayerCount;

    /// <inheritdoc />
    public IEnumerator<FhmPlayerRecord> GetEnumerator()
    {
        ThrowIfDisposed();
        if (enumerationStarted)
        {
            throw new InvalidOperationException("A FhmPlayersFileReader can be enumerated only once.");
        }

        enumerationStarted = true;
        return EnumerateRecords().GetEnumerator();
    }

    /// <inheritdoc />
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            reader.Dispose();
        }
    }

    private IEnumerable<FhmPlayerRecord> EnumerateRecords()
    {
        for (var index = 0; index < PlayerCount; index++)
        {
            yield return ReadRecord();
        }

        EnsureEndOfStream();
    }

    private FhmPlayerRecord ReadRecord()
    {
        try
        {
            var wire = reader.ReadRecord();
            FhmPlayerWireMapper.ValidateWire(wire);
            return FhmPlayerWireMapper.FromValidatedWire(wire);
        }
        catch (InvalidDataException exception)
        {
            throw new FhmFormatException(exception.Message);
        }
    }

    private void EnsureEndOfStream()
    {
        try
        {
            reader.EnsureEndOfStream();
        }
        catch (InvalidDataException exception)
        {
            throw new FhmFormatException(exception.Message);
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);
}

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

    public static FhmPlayersFile Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new FhmPlayersFileReader(stream);
        var result = new FhmPlayersFile
        {
            FormatVersion = reader.FormatVersion,
        };
        foreach (var player in reader)
        {
            result.Players.Add(player);
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

        var wirePlayers = new List<FhmPlayerRecordData>(Players.Count);
        foreach (var player in Players)
        {
            wirePlayers.Add(FhmPlayerWireMapper.ToWire(player));
        }

        FhmPlayersFileSerializer.SerializeValidated(stream, new FhmPlayersFileData
        {
            FormatVersion = FormatVersion,
            PlayerCount = Players.Count,
            Players = wirePlayers,
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
