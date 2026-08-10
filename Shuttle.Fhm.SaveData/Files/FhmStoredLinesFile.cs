using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.SaveData.Model;

namespace Shuttle.Fhm.SaveData.Files;

/// <summary>Saved lineup presets in <c>stored_lines.dat</c>.</summary>
public sealed class FhmStoredLinesFile : IFhmSaveFile
{
    /// <inheritdoc />
    public string RelativePath => "stored_lines.dat";

    /// <summary>Gets named presets in serialized order.</summary>
    public IList<FhmStoredLine> StoredLines { get; } = [];

    internal static FhmStoredLinesFile Read(FhmBinaryReader reader)
    {
        var result = new FhmStoredLinesFile();
        var count = reader.ReadCount("stored lines");
        for (var index = 0; index < count; index++)
        {
            result.StoredLines.Add(FhmStoredLine.Read(reader));
        }

        reader.EnsureEof("stored_lines.dat");
        return result;
    }

    /// <inheritdoc />
    public void WriteTo(FhmBinaryWriter writer)
    {
        writer.WriteCount(StoredLines.Count, "stored lines");
        foreach (var line in StoredLines)
        {
            line.WriteTo(writer);
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

    internal static FhmStoredLine Read(FhmBinaryReader reader)
    {
        var result = new FhmStoredLine { Name = reader.ReadQString() };
        ReadGroups(reader, result.PlayerGroups, static reader => reader.ReadInt32(), "stored line player group");
        ReadGroups(reader, result.UnitLocks, static reader => reader.ReadByte(), "stored line lock group");
        return result;
    }

    internal void WriteTo(FhmBinaryWriter writer)
    {
        if (PlayerGroups.Count != Enum.GetValues<FhmLineGroup>().Length || UnitLocks.Count != Enum.GetValues<FhmLineGroup>().Length)
        {
            throw new FhmFormatException("A stored line must contain exactly thirteen player and lock groups.");
        }

        writer.WriteQString(Name);
        WriteGroups(writer, PlayerGroups, static (writer, value) => writer.WriteInt32(value), "stored line player group");
        WriteGroups(writer, UnitLocks, static (writer, value) => writer.WriteByte(value), "stored line lock group");
    }

    private static IList<IList<T>> CreateLists<T>()
    {
        var result = new List<IList<T>>(Enum.GetValues<FhmLineGroup>().Length);
        for (var index = 0; index < Enum.GetValues<FhmLineGroup>().Length; index++)
        {
            result.Add([]);
        }

        return result;
    }

    private static void ReadGroups<T>(FhmBinaryReader reader, IEnumerable<IList<T>> groups, Func<FhmBinaryReader, T> read, string description)
    {
        foreach (var group in groups)
        {
            var count = reader.ReadCount(description);
            for (var index = 0; index < count; index++)
            {
                group.Add(read(reader));
            }
        }
    }

    private static void WriteGroups<T>(FhmBinaryWriter writer, IEnumerable<IList<T>> groups, Action<FhmBinaryWriter, T> write, string description)
    {
        foreach (var group in groups)
        {
            writer.WriteCount(group.Count, description);
            foreach (var value in group)
            {
                write(writer, value);
            }
        }
    }
}
