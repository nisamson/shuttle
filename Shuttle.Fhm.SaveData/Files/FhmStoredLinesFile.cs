using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.SaveData.Model;
using Shuttle.BinarySerde.Common.QFormat;
using Shuttle.Fhm.Serde.StoredLines;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>Saved lineup presets in <c>stored_lines.dat</c>.</summary>
public sealed class FhmStoredLinesFile : IFhmSaveFile
{
    private const int MaximumCollectionCount = 10_000_000;

    /// <inheritdoc />
    public string RelativePath => "stored_lines.dat";

    /// <summary>Gets named presets in serialized order.</summary>
    public IList<FhmStoredLine> StoredLines { get; } = [];

    internal static FhmStoredLinesFile Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        FhmStoredLinesFileData wire;
        try
        {
            wire = FhmStoredLinesFileSerializer.Deserialize(stream);
        }
        catch (InvalidDataException exception)
        {
            throw new FhmFormatException(exception.Message);
        }

        ValidateWire(wire);
        var result = new FhmStoredLinesFile();
        foreach (var line in wire.StoredLines)
        {
            var storedLine = new FhmStoredLine { Name = line.Name.Value };
            CopyGroups(line.PlayerGroups, storedLine.PlayerGroups);
            CopyGroups(line.UnitLocks, storedLine.UnitLocks);
            result.StoredLines.Add(storedLine);
        }

        return result;
    }

    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ValidateForWrite();
        FhmStoredLinesFileSerializer.Serialize(stream, new FhmStoredLinesFileData
        {
            StoredLineCount = StoredLines.Count,
            StoredLines = StoredLines.Select(ToWire).ToList(),
        });
    }

    private static FhmStoredLineData ToWire(FhmStoredLine line) => new()
    {
        Name = new QString { Value = line.Name },
        PlayerGroups = ToWireGroups(line.PlayerGroups),
        UnitLocks = ToWireGroups(line.UnitLocks),
    };

    private static List<QList<T>> ToWireGroups<T>(IEnumerable<IList<T>> groups) =>
        groups.Select(group => new QList<T> { Length = group.Count, Items = group.ToList() }).ToList();

    private static void CopyGroups<T>(IReadOnlyList<QList<T>> source, IList<IList<T>> destination)
    {
        for (var index = 0; index < FhmStoredLineData.GroupCount; index++)
        {
            foreach (var value in source[index].Items)
            {
                destination[index].Add(value);
            }
        }
    }

    private static void ValidateWire(FhmStoredLinesFileData wire)
    {
        if (wire.StoredLineCount < 0 || wire.StoredLineCount > MaximumCollectionCount ||
            wire.StoredLines.Count != wire.StoredLineCount)
        {
            throw new FhmFormatException($"Invalid stored lines count {wire.StoredLineCount}.");
        }

        foreach (var line in wire.StoredLines)
        {
            ValidateWireGroups(line.PlayerGroups, "player");
            ValidateWireGroups(line.UnitLocks, "lock");
        }
    }

    private void ValidateForWrite()
    {
        if (StoredLines.Count > MaximumCollectionCount)
        {
            throw new FhmFormatException($"Invalid stored lines count {StoredLines.Count}.");
        }

        foreach (var line in StoredLines)
        {
            ArgumentNullException.ThrowIfNull(line);
            ValidateGroups(line.PlayerGroups, "player");
            ValidateGroups(line.UnitLocks, "lock");
        }
    }

    private static void ValidateWireGroups<T>(IReadOnlyCollection<QList<T>> groups, string description)
    {
        if (groups.Count != FhmStoredLineData.GroupCount)
        {
            throw new FhmFormatException($"A stored line must contain exactly thirteen {description} groups.");
        }

        foreach (var group in groups)
        {
            if (group.Length < 0 || group.Length > MaximumCollectionCount || group.Items.Count != group.Length)
            {
                throw new FhmFormatException($"Invalid stored line {description} group count {group.Length}.");
            }
        }
    }

    private static void ValidateGroups<T>(ICollection<IList<T>> groups, string description)
    {
        if (groups.Count != FhmStoredLineData.GroupCount)
        {
            throw new FhmFormatException($"A stored line must contain exactly thirteen {description} groups.");
        }

        foreach (var group in groups)
        {
            if (group is null || group.Count > MaximumCollectionCount)
            {
                throw new FhmFormatException($"Invalid stored line {description} group count {group?.Count}.");
            }
        }
    }
}

/// <summary>One named lineup preset with parallel player and lock-state groups.</summary>
public sealed class FhmStoredLine
{
    /// <summary>Gets or sets the preset name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets the 13 ordered line groups containing player internal identities. <c>-1</c> is empty.</summary>
    public IList<IList<int>> PlayerGroups { get; } = CreateLists<int>();

    /// <summary>Gets the 13 ordered lock-state groups corresponding to <see cref="PlayerGroups"/>.</summary>
    public IList<IList<byte>> UnitLocks { get; } = CreateLists<byte>();

    private static IList<IList<T>> CreateLists<T>()
    {
        var result = new List<IList<T>>(Enum.GetValues<FhmLineGroup>().Length);
        for (var index = 0; index < Enum.GetValues<FhmLineGroup>().Length; index++)
        {
            result.Add([]);
        }

        return result;
    }
}
