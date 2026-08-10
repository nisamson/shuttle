using Shuttle.Fhm.SaveData.Binary;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>The name-generation reference tables in <c>names.dat</c>.</summary>
public sealed class FhmNamesFile : IFhmSaveFile
{
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

    internal static FhmNamesFile Read(FhmBinaryReader reader)
    {
        var result = new FhmNamesFile { ReservedZero = reader.ReadInt32() };
        if (result.ReservedZero != 0)
        {
            throw new FhmFormatException("names.dat reserved_zero must be zero.");
        }

        var count = reader.ReadCount("names.dat master names");
        for (var index = 0; index < count; index++)
        {
            result.MasterNames.Add(new FhmNameEntry(
                reader.ReadQString(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                unchecked((short)reader.ReadUInt16()),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadByte()));
        }

        ReadNationLists(reader, result.FirstNameLists, "first-name");
        ReadNationLists(reader, result.SurnameLists, "surname");
        ReadScalars(reader, result.ScalarArrayA);
        ReadScalars(reader, result.ScalarArrayB);
        reader.EnsureEof("names.dat");
        return result;
    }

    /// <inheritdoc />
    public void WriteTo(FhmBinaryWriter writer)
    {
        if (ReservedZero != 0)
        {
            throw new FhmFormatException("names.dat ReservedZero must be zero.");
        }

        ValidateNationTables();
        writer.WriteInt32(ReservedZero);
        writer.WriteCount(MasterNames.Count, "names.dat master names");
        foreach (var entry in MasterNames)
        {
            writer.WriteQString(entry.Text);
            writer.WriteInt32(entry.NameId);
            writer.WriteInt32(entry.GroupId);
            writer.WriteUInt16(unchecked((ushort)entry.CategoryWeight));
            writer.WriteByte(entry.FlagA);
            writer.WriteByte(entry.FlagB);
            writer.WriteByte(entry.FlagC);
        }

        WriteNationLists(writer, FirstNameLists, "first-name");
        WriteNationLists(writer, SurnameLists, "surname");
        WriteScalars(writer, ScalarArrayA);
        WriteScalars(writer, ScalarArrayB);
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

    private static void ReadNationLists(FhmBinaryReader reader, IList<IList<int>> destination, string description)
    {
        for (var nation = 0; nation < NationCount; nation++)
        {
            var count = reader.ReadCount($"names.dat {description} ids");
            for (var index = 0; index < count; index++)
            {
                destination[nation].Add(reader.ReadInt32());
            }
        }
    }

    private static void ReadScalars(FhmBinaryReader reader, IList<int> destination)
    {
        for (var index = 0; index < NationCount; index++)
        {
            destination[index] = reader.ReadInt32();
        }
    }

    private static void WriteNationLists(FhmBinaryWriter writer, IEnumerable<IList<int>> values, string description)
    {
        foreach (var list in values)
        {
            writer.WriteCount(list.Count, $"names.dat {description} ids");
            foreach (var value in list)
            {
                writer.WriteInt32(value);
            }
        }
    }

    private static void WriteScalars(FhmBinaryWriter writer, IEnumerable<int> values)
    {
        foreach (var value in values)
        {
            writer.WriteInt32(value);
        }
    }

    private void ValidateNationTables()
    {
        if (FirstNameLists.Count != NationCount || SurnameLists.Count != NationCount ||
            ScalarArrayA.Count != NationCount || ScalarArrayB.Count != NationCount)
        {
            throw new FhmFormatException($"names.dat requires exactly {NationCount} nation entries in every nation-indexed table.");
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
