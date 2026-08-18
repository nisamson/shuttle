using System.Buffers.Binary;
using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.SaveData.Files;
using Shuttle.Fhm.SaveData.SaveFolder;

namespace Shuttle.Tests.Fhm;

public sealed class FhmNamesFileTests
{
    [Fact]
    public void NamesFile_ReadsAndRewritesTheByteCompatibleWireFixture()
    {
        Assert.Equal(102, FhmNamesFile.NationCount);
        var fixture = CreateWireFixture();

        var names = ReadNames(fixture);

        Assert.Equal(0, names.ReservedZero);
        Assert.Equal(
            [
                new FhmNameEntry(null, 0x01020304, -2, -3, 7, 8, 9),
                new FhmNameEntry("Å", -1, 2, short.MinValue + 1, 10, 11, 12),
            ],
            names.MasterNames);
        Assert.Equal([101, -2], names.FirstNameLists[0]);
        Assert.Empty(names.FirstNameLists[1]);
        Assert.Equal([77], names.FirstNameLists[^1]);
        Assert.Equal([9], names.SurnameLists[1]);
        Assert.Equal([-9], names.SurnameLists[^1]);
        Assert.Equal(-51, names.ScalarArrayA[0]);
        Assert.Equal(50, names.ScalarArrayA[^1]);
        Assert.Equal(0, names.ScalarArrayB[0]);
        Assert.Equal(202, names.ScalarArrayB[^1]);

        Assert.Equal(fixture, WriteNames(names));
    }

    [Fact]
    public void NamesFile_RejectsInvalidReservedZeroAndNationTableLengths()
    {
        var invalidReservedZero = CreateWireFixture();
        invalidReservedZero[3] = 1;
        var readException = Assert.Throws<FhmFormatException>(() => ReadNames(invalidReservedZero));
        Assert.Contains("reserved_zero", readException.Message);

        var reservedZeroException = Assert.Throws<FhmFormatException>(() => WriteNames(new FhmNamesFile { ReservedZero = 1 }));
        Assert.Contains("ReservedZero", reservedZeroException.Message);

        foreach (var makeInvalid in new Action<FhmNamesFile>[]
                 {
                     names => names.FirstNameLists.RemoveAt(0),
                     names => names.SurnameLists.RemoveAt(0),
                 })
        {
            var names = ReadNames(CreateWireFixture());
            makeInvalid(names);

            var nationTableException = Assert.Throws<FhmFormatException>(() => WriteNames(names));
            Assert.Contains(FhmNamesFile.NationCount.ToString(), nationTableException.Message);
        }
    }

    [Fact]
    public void NamesFile_RejectsTrailingBytes()
    {
        var exception = Assert.Throws<FhmFormatException>(() => ReadNames([.. CreateWireFixture(), 0xFF]));

        Assert.Contains("unread bytes", exception.Message);
    }

    private static FhmNamesFile ReadNames(byte[] bytes)
    {
        var root = CreateTestRoot();
        try
        {
            File.WriteAllBytes(Path.Combine(root, "names.dat"), bytes);
            return Assert.IsType<FhmNamesFile>(new FhmSaveReader().Read(root).Files["names.dat"]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static byte[] WriteNames(FhmNamesFile names)
    {
        var root = CreateTestRoot();
        try
        {
            var save = new FhmSave();
            save.Files.Add(names.RelativePath, names);
            new FhmSaveWriter().Write(save, root);
            return File.ReadAllBytes(Path.Combine(root, "names.dat"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static byte[] CreateWireFixture()
    {
        var bytes = new List<byte>();
        WriteInt32(bytes, 0);
        WriteInt32(bytes, 2);
        WriteQString(bytes, null);
        WriteInt32(bytes, 0x01020304);
        WriteInt32(bytes, -2);
        WriteUInt16(bytes, unchecked((ushort)-3));
        bytes.AddRange([7, 8, 9]);
        WriteQString(bytes, "Å");
        WriteInt32(bytes, -1);
        WriteInt32(bytes, 2);
        WriteUInt16(bytes, unchecked((ushort)(short.MinValue + 1)));
        bytes.AddRange([10, 11, 12]);

        for (var nation = 0; nation < FhmNamesFile.NationCount; nation++)
        {
            int[] ids = nation switch
            {
                0 => new[] { 101, -2 },
                FhmNamesFile.NationCount - 1 => [77],
                _ => [],
            };
            WriteInt32(bytes, ids.Length);
            foreach (var id in ids)
            {
                WriteInt32(bytes, id);
            }
        }

        for (var nation = 0; nation < FhmNamesFile.NationCount; nation++)
        {
            int[] ids = nation switch
            {
                1 => [9],
                FhmNamesFile.NationCount - 1 => [-9],
                _ => [],
            };
            WriteInt32(bytes, ids.Length);
            foreach (var id in ids)
            {
                WriteInt32(bytes, id);
            }
        }

        for (var nation = 0; nation < FhmNamesFile.NationCount; nation++)
        {
            WriteInt32(bytes, nation - 51);
        }

        for (var nation = 0; nation < FhmNamesFile.NationCount; nation++)
        {
            WriteInt32(bytes, nation * 2);
        }

        return [.. bytes];
    }

    private static void WriteInt32(List<byte> bytes, int value)
    {
        var buffer = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        bytes.AddRange(buffer);
    }

    private static void WriteUInt16(List<byte> bytes, ushort value)
    {
        var buffer = new byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
        bytes.AddRange(buffer);
    }

    private static void WriteQString(List<byte> bytes, string? value)
    {
        if (value is null)
        {
            WriteInt32(bytes, -1);
            return;
        }

        WriteInt32(bytes, value.Length * sizeof(char));
        foreach (var character in value)
        {
            WriteUInt16(bytes, character);
        }
    }

    private static string CreateTestRoot()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "fhm-names-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
