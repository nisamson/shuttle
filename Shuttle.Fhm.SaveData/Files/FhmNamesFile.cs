using Shuttle.Fhm.SaveData.Binary;
using Shuttle.BinarySerde.Common.QFormat;
using Shuttle.Fhm.Serde.Names;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>The name-generation reference tables in <c>names.dat</c>.</summary>
public sealed class FhmNamesFile : IFhmSaveFile
{
    private const int MaximumCollectionCount = 10_000_000;

    /// <summary>Number of nation-indexed arrays serialized by FHM 10.</summary>
    public const int NationCount = 102;

    /// <inheritdoc />
    public string RelativePath => "names.dat";

    /// <summary>Gets or sets the required leading reserved zero.</summary>
    public int ReservedZero { get; set; }

    /// <summary>Gets master name records in serialized order.</summary>
    public IList<FhmNameEntry> MasterNames { get; } = [];

    /// <summary>Gets the first-name ids for each nation.</summary>
    public IList<IList<int>> FirstNameLists { get; } = CreateNationLists();

    /// <summary>Gets the surname ids for each nation.</summary>
    public IList<IList<int>> SurnameLists { get; } = CreateNationLists();

    /// <summary>Gets the first 102-value per-nation scalar array.</summary>
    public IList<int> ScalarArrayA { get; } = new int[NationCount];

    /// <summary>Gets the second 102-value per-nation scalar array.</summary>
    public IList<int> ScalarArrayB { get; } = new int[NationCount];

    internal static FhmNamesFile Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        FhmNamesFileData wire;
        try
        {
            wire = FhmNamesFileSerializer.Deserialize(stream);
        }
        catch (InvalidDataException exception)
        {
            throw new FhmFormatException(exception.Message);
        }

        ValidateWire(wire);
        var result = new FhmNamesFile { ReservedZero = wire.ReservedZero };
        foreach (var entry in wire.MasterNames)
        {
            result.MasterNames.Add(new(
                entry.Text.Value,
                entry.NameId,
                entry.GroupId,
                unchecked((short)entry.CategoryWeight),
                entry.FlagA,
                entry.FlagB,
                entry.FlagC));
        }

        CopyNationLists(wire.FirstNameLists, result.FirstNameLists);
        CopyNationLists(wire.SurnameLists, result.SurnameLists);
        CopyScalars(wire.ScalarArrayA, result.ScalarArrayA);
        CopyScalars(wire.ScalarArrayB, result.ScalarArrayB);
        return result;
    }

    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ValidateForWrite();
        FhmNamesFileSerializer.Serialize(stream, new FhmNamesFileData
        {
            ReservedZero = ReservedZero,
            MasterNameCount = MasterNames.Count,
            MasterNames = MasterNames.Select(entry => new FhmNameData
            {
                Text = new QString { Value = entry.Text },
                NameId = entry.NameId,
                GroupId = entry.GroupId,
                CategoryWeight = unchecked((ushort)entry.CategoryWeight),
                FlagA = entry.FlagA,
                FlagB = entry.FlagB,
                FlagC = entry.FlagC,
            }).ToList(),
            FirstNameLists = ToWireNationLists(FirstNameLists),
            SurnameLists = ToWireNationLists(SurnameLists),
            ScalarArrayA = ScalarArrayA.ToList(),
            ScalarArrayB = ScalarArrayB.ToList(),
        });
    }

    private static IList<IList<int>> CreateNationLists()
    {
        var result = new List<IList<int>>(NationCount);
        for (var index = 0; index < NationCount; index++)
        {
            result.Add([]);
        }

        return result;
    }

    private static List<QList<int>> ToWireNationLists(IEnumerable<IList<int>> values) =>
        values.Select(list => new QList<int> { Length = list.Count, Items = list.ToList() }).ToList();

    private static void CopyNationLists(IReadOnlyList<QList<int>> source, IList<IList<int>> destination)
    {
        for (var nation = 0; nation < NationCount; nation++)
        {
            foreach (var value in source[nation].Items)
            {
                destination[nation].Add(value);
            }
        }
    }

    private static void CopyScalars(IReadOnlyList<int> source, IList<int> destination)
    {
        for (var index = 0; index < NationCount; index++)
        {
            destination[index] = source[index];
        }
    }

    private static void ValidateWire(FhmNamesFileData wire)
    {
        if (wire.ReservedZero != 0)
        {
            throw new FhmFormatException("names.dat reserved_zero must be zero.");
        }

        if (wire.MasterNameCount < 0 || wire.MasterNameCount > MaximumCollectionCount ||
            wire.MasterNames.Count != wire.MasterNameCount)
        {
            throw new FhmFormatException($"Invalid names.dat master names count {wire.MasterNameCount}.");
        }

        ValidateNationTables(wire.FirstNameLists, wire.SurnameLists, wire.ScalarArrayA, wire.ScalarArrayB);
        ValidateNationListCounts(wire.FirstNameLists, "first-name");
        ValidateNationListCounts(wire.SurnameLists, "surname");
    }

    private void ValidateForWrite()
    {
        if (ReservedZero != 0)
        {
            throw new FhmFormatException("names.dat ReservedZero must be zero.");
        }

        if (MasterNames.Count > MaximumCollectionCount)
        {
            throw new FhmFormatException($"Invalid names.dat master names count {MasterNames.Count}.");
        }

        ValidateNationTables(FirstNameLists, SurnameLists, ScalarArrayA, ScalarArrayB);
        foreach (var list in FirstNameLists)
        {
            ValidateNationListCount(list.Count, "first-name");
        }

        foreach (var list in SurnameLists)
        {
            ValidateNationListCount(list.Count, "surname");
        }
    }

    private static void ValidateNationTables(
        IReadOnlyCollection<QList<int>> firstNameLists,
        IReadOnlyCollection<QList<int>> surnameLists,
        IReadOnlyCollection<int> scalarArrayA,
        IReadOnlyCollection<int> scalarArrayB)
    {
        if (firstNameLists.Count != NationCount || surnameLists.Count != NationCount ||
            scalarArrayA.Count != NationCount || scalarArrayB.Count != NationCount)
        {
            throw new FhmFormatException($"names.dat requires exactly {NationCount} nation entries in every nation-indexed table.");
        }
    }

    private static void ValidateNationTables(
        ICollection<IList<int>> firstNameLists,
        ICollection<IList<int>> surnameLists,
        ICollection<int> scalarArrayA,
        ICollection<int> scalarArrayB)
    {
        if (firstNameLists.Count != NationCount || surnameLists.Count != NationCount ||
            scalarArrayA.Count != NationCount || scalarArrayB.Count != NationCount)
        {
            throw new FhmFormatException($"names.dat requires exactly {NationCount} nation entries in every nation-indexed table.");
        }
    }

    private static void ValidateNationListCounts(IEnumerable<QList<int>> lists, string description)
    {
        foreach (var list in lists)
        {
            if (list.Length < 0 || list.Length > MaximumCollectionCount || list.Items.Count != list.Length)
            {
                throw new FhmFormatException($"Invalid names.dat {description} ids count {list.Length}.");
            }
        }
    }

    private static void ValidateNationListCount(int count, string description)
    {
        if (count > MaximumCollectionCount)
        {
            throw new FhmFormatException($"Invalid names.dat {description} ids count {count}.");
        }
    }
}

/// <summary>One serialized master-name record.</summary>
public sealed record FhmNameEntry(
    string? Text,
    int NameId,
    int GroupId,
    short CategoryWeight,
    byte FlagA,
    byte FlagB,
    byte FlagC);
