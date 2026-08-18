using System.Buffers.Binary;
using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.SaveData.Files;
using Shuttle.Fhm.SaveData.SaveFolder;

namespace Shuttle.Tests.Fhm;

public sealed class FhmSetPlayFileTests
{
    private const string RelativePath = "set_play_5on4.dat";

    [Theory]
    [InlineData("set_play_5on4.dat")]
    [InlineData("set_play_4on5.dat")]
    public void SetPlayFile_ReadsAndRewritesTheByteCompatibleWireFixture(string relativePath)
    {
        var fixture = CreateWireFixture();

        var setPlay = ReadSetPlay(relativePath, fixture);

        Assert.Equal(relativePath, setPlay.RelativePath);
        Assert.Equal(4, setPlay.Version);
        Assert.Collection(
            setPlay.Formations,
            block => Assert.Equal([1, 2, 3, 4], block.Data),
            block => Assert.Equal([], block.Data));
        Assert.Collection(
            setPlay.ExtraRecords,
            block => Assert.Equal([0xAA], block.Data),
            block => Assert.Equal([0x10, 0x20], block.Data));

        Assert.Equal(fixture, WriteSetPlay(setPlay));
        Assert.Equal(fixture, WriteSetPlayUsingPublicApi(setPlay));
    }

    [Fact]
    public void SetPlayFile_PublicApi_WritesExpectedWireFormatAndRoundTripsMutations()
    {
        var setPlay = new FhmSetPlayFile(RelativePath) { Version = 7 };
        setPlay.Formations.Add(new FhmLengthPrefixedBlock([0, 1]));
        setPlay.Formations.Add(new FhmLengthPrefixedBlock([]));
        setPlay.ExtraRecords.Add(new FhmLengthPrefixedBlock([9, 8, 7]));
        setPlay.ExtraRecords.Add(new FhmLengthPrefixedBlock([6]));

        var written = WriteSetPlay(setPlay);

        Assert.Equal(
            CreateWireFixture(
                version: 7,
                formations: [[0, 1], []],
                extraRecords: [[9, 8, 7], [6]]),
            written);

        var roundTripped = ReadSetPlay(RelativePath, written);
        roundTripped.Formations[0].Data[1] = 0x7F;
        roundTripped.ExtraRecords.Add(new FhmLengthPrefixedBlock([5, 4]));

        var mutatedBytes = WriteSetPlay(roundTripped);
        var mutated = ReadSetPlay(RelativePath, mutatedBytes);

        Assert.NotEqual(written, mutatedBytes);
        Assert.Collection(
            mutated.Formations,
            block => Assert.Equal([0, 0x7F], block.Data),
            block => Assert.Equal([], block.Data));
        Assert.Collection(
            mutated.ExtraRecords,
            block => Assert.Equal([9, 8, 7], block.Data),
            block => Assert.Equal([6], block.Data),
            block => Assert.Equal([5, 4], block.Data));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10_000_001)]
    public void SetPlayFile_RejectsInvalidFormationCounts(int formationCount)
    {
        var root = CreateTestRoot();
        try
        {
            File.WriteAllBytes(Path.Combine(root, RelativePath), CreateInvalidCountFixture(formationCount));

            var exception = Assert.Throws<FhmFormatException>(() => new FhmSaveReader().Read(root));

            Assert.Contains($"Invalid set_play_5on4.dat formations count {formationCount}", exception.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData(true, "set_play_5on4.dat formation has a negative byte length.")]
    [InlineData(false, "set_play_5on4.dat extra record has a negative byte length.")]
    public void SetPlayFile_RejectsNegativeBlockLengths(bool invalidFormation, string expectedMessage)
    {
        var root = CreateTestRoot();
        try
        {
            File.WriteAllBytes(Path.Combine(root, RelativePath), CreateNegativeLengthFixture(invalidFormation));

            var exception = Assert.Throws<FhmFormatException>(() => new FhmSaveReader().Read(root));

            Assert.Contains(expectedMessage, exception.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static FhmSetPlayFile ReadSetPlay(string relativePath, byte[] bytes)
    {
        var root = CreateTestRoot();
        try
        {
            var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, bytes);
            return Assert.IsType<FhmSetPlayFile>(new FhmSaveReader().Read(root).Files[relativePath]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static byte[] WriteSetPlay(FhmSetPlayFile setPlay)
    {
        var root = CreateTestRoot();
        try
        {
            var save = new FhmSave();
            save.Files.Add(setPlay.RelativePath, setPlay);
            new FhmSaveWriter().Write(save, root);
            return File.ReadAllBytes(Path.Combine(root, setPlay.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static byte[] WriteSetPlayUsingPublicApi(FhmSetPlayFile setPlay)
    {
        using var stream = new MemoryStream();
        setPlay.WriteTo(stream);
        return stream.ToArray();
    }

    private static byte[] CreateWireFixture(int version = 4, byte[][]? formations = null, byte[][]? extraRecords = null)
    {
        formations ??= [[1, 2, 3, 4], []];
        extraRecords ??= [[0xAA], [0x10, 0x20]];

        var bytes = new List<byte>();
        WriteInt32(bytes, version);
        WriteInt32(bytes, formations.Length);
        foreach (var block in formations)
        {
            WriteBlock(bytes, block);
        }

        foreach (var block in extraRecords)
        {
            WriteBlock(bytes, block);
        }

        return bytes.ToArray();
    }

    private static byte[] CreateInvalidCountFixture(int formationCount)
    {
        var bytes = new List<byte>();
        WriteInt32(bytes, 4);
        WriteInt32(bytes, formationCount);
        return bytes.ToArray();
    }

    private static byte[] CreateNegativeLengthFixture(bool invalidFormation)
    {
        var bytes = new List<byte>();
        WriteInt32(bytes, 4);
        WriteInt32(bytes, invalidFormation ? 1 : 0);
        WriteInt32(bytes, -1);
        return bytes.ToArray();
    }

    private static void WriteBlock(ICollection<byte> destination, byte[] block)
    {
        WriteInt32(destination, block.Length);
        foreach (var value in block)
        {
            destination.Add(value);
        }
    }

    private static void WriteInt32(ICollection<byte> destination, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        foreach (var item in bytes)
        {
            destination.Add(item);
        }
    }

    private static string CreateTestRoot()
    {
        var root = Path.Combine(Environment.CurrentDirectory, "fhm-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
