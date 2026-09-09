using System.Buffers.Binary;
using Shuttle.Fhm.Serde.Domain.Binary;
using Shuttle.Fhm.Serde.Domain.Files;

namespace Shuttle.Tests.Fhm;

public sealed class FhmPersonnelFileTests
{
    private const int HeaderLength = 8;
    private const int ReferenceRecordLength = 0x1B6;
    private const int IntegerListCountOffset = 0xAB;
    private const int IntegerListItemLength = sizeof(int);

    [Fact]
    public void Read_UsesHeaderAsRecordCount_WhenPersonnelIdsAreSparse()
    {
        var bytes = FhmSaveTests.CreatePersonnelFileBytes(
            (8, 1, null, FhmPersonnelJob.GeneralManager),
            (400, 2, null, FhmPersonnelJob.Scout));

        var file = Read(bytes);

        Assert.Equal(2, file.RecordCount);
        Assert.Equal(2, file.NextPersonnelId);
        Assert.Equal([8, 400], file.Records.Select(record => record.PersonnelId));
        Assert.Equal(
            bytes.AsSpan(HeaderLength, ReferenceRecordLength).ToArray(),
            file.Records[0].GetSourceBytes());
        Assert.Equal(
            bytes.AsSpan(HeaderLength + ReferenceRecordLength, ReferenceRecordLength).ToArray(),
            file.Records[1].GetSourceBytes());
    }

    [Fact]
    public void Read_UsesSchemaBoundaries_ForVariableLengthVersion35Records()
    {
        var bytes = FhmSaveTests.CreatePersonnelFileBytes(
            (1, 1, null, FhmPersonnelJob.GeneralManager),
            (2, 2, null, FhmPersonnelJob.Scout));
        var secondRecordStart = HeaderLength + ReferenceRecordLength;
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(secondRecordStart + IntegerListCountOffset, sizeof(int)), 0);
        var content = new List<byte>(bytes);
        content.RemoveRange(
            secondRecordStart + IntegerListCountOffset + sizeof(int),
            8 * IntegerListItemLength);
        bytes = content.ToArray();

        var file = Read(bytes);
        using var output = new MemoryStream();
        file.WriteTo(output);

        Assert.Equal(2, file.Records.Count);
        Assert.Equal(ReferenceRecordLength, file.Records[0].GetSourceBytes().Length);
        Assert.Equal(ReferenceRecordLength - (8 * IntegerListItemLength), file.Records[1].GetSourceBytes().Length);
        Assert.Equal(
            bytes.AsSpan(HeaderLength + ReferenceRecordLength).ToArray(),
            file.Records[1].GetSourceBytes());
        Assert.Equal(bytes, output.ToArray());
    }

    [Fact]
    public void Read_RejectsTruncatedVariableLengthRecord_WithRecordAndFieldContext()
    {
        var bytes = FhmSaveTests.CreatePersonnelFileBytes(
            (1, 1, null, FhmPersonnelJob.GeneralManager),
            (2, 2, null, FhmPersonnelJob.Scout));

        var exception = Assert.Throws<FhmFormatException>(() => Read(bytes[..^1]));

        Assert.Contains("record 2", exception.Message, StringComparison.Ordinal);
        Assert.Contains("offset 0x", exception.Message, StringComparison.Ordinal);
        Assert.Contains("native +0xE8", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_RejectsUnsupportedVersion()
    {
        var bytes = FhmSaveTests.CreatePersonnelFileBytes(
            (1, 1, null, FhmPersonnelJob.GeneralManager));
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(0, sizeof(int)), 36);

        var exception = Assert.Throws<FhmFormatException>(() => Read(bytes));

        Assert.Contains("Unsupported personal.dat version 36", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_RejectsBytesBeyondDeclaredRecordCount()
    {
        var bytes = FhmSaveTests.CreatePersonnelFileBytes(
            (1, 1, null, FhmPersonnelJob.GeneralManager),
            (2, 2, null, FhmPersonnelJob.Scout));
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(sizeof(int), sizeof(int)), 1);

        var exception = Assert.Throws<FhmFormatException>(() => Read(bytes));

        Assert.Contains("unread bytes after 1 records", exception.Message, StringComparison.Ordinal);
    }

    private static FhmPersonnelFile Read(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        return FhmPersonnelFile.Read(stream);
    }
}
