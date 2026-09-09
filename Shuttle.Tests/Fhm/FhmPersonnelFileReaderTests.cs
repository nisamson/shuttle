using System.Buffers.Binary;
using Shuttle.Fhm.Serde.Domain.Binary;
using Shuttle.Fhm.Serde.Domain.Files;

namespace Shuttle.Tests.Fhm;

public sealed class FhmPersonnelFileReaderTests
{
    private const int HeaderLength = 8;
    private const int ReferenceRecordLength = 0x1B6;
    private const int IntegerListCountOffset = 0xAB;
    private const int IntegerListItemLength = sizeof(int);

    [Fact]
    public void Read_StreamsOneRecordAtATime_WithoutReadingTheRemainingRecords()
    {
        var content = CreateVariableLengthPersonnelContent();
        using var source = new MemoryStream(content, writable: false);
        using var reader = new FhmPersonnelFileReader(source);

        Assert.Equal(new FhmPersonnelFileHeader(35, 2), reader.Header);
        using var records = reader.GetEnumerator();

        Assert.True(records.MoveNext());
        Assert.Equal(8, records.Current.PersonnelId);
        Assert.Equal(ReferenceRecordLength, records.Current.GetSourceBytes().Length);
        Assert.Equal(HeaderLength + ReferenceRecordLength, source.Position);
        Assert.True(source.Position < source.Length);

        Assert.True(records.MoveNext());
        Assert.Equal(400, records.Current.PersonnelId);
        Assert.Equal(ReferenceRecordLength - (8 * IntegerListItemLength), records.Current.GetSourceBytes().Length);
        Assert.False(records.MoveNext());
        Assert.Throws<InvalidOperationException>(() => reader.GetEnumerator());
    }

    [Fact]
    public void Read_RejectsTruncatedRecord_WithTheSameRecordAndFieldContext()
    {
        var content = FhmSaveTests.CreatePersonnelFileBytes(
            (1, 1, 0, FhmPersonnelJob.GeneralManager),
            (2, 1, 0, FhmPersonnelJob.Scout));
        using var source = new MemoryStream(content[..^1], writable: false);
        using var reader = new FhmPersonnelFileReader(source);

        var exception = Assert.Throws<FhmFormatException>(() => reader.ToArray());

        Assert.Contains("record 2", exception.Message, StringComparison.Ordinal);
        Assert.Contains("offset 0x", exception.Message, StringComparison.Ordinal);
        Assert.Contains("native +0xE8", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_RejectsTrailingBytes_AfterTheDeclaredRecordCount()
    {
        var content = FhmSaveTests.CreatePersonnelFileBytes(
            (1, 1, 0, FhmPersonnelJob.GeneralManager),
            (2, 1, 0, FhmPersonnelJob.Scout))
            .Append((byte)0xA5)
            .Append((byte)0x5A)
            .ToArray();
        using var source = new MemoryStream(content, writable: false);
        using var reader = new FhmPersonnelFileReader(source);

        var exception = Assert.Throws<FhmFormatException>(() => reader.ToArray());

        Assert.Contains("contains 2 unread bytes after 2 records", exception.Message, StringComparison.Ordinal);
    }

    private static byte[] CreateVariableLengthPersonnelContent()
    {
        var content = FhmSaveTests.CreatePersonnelFileBytes(
            (8, 1, 0, FhmPersonnelJob.GeneralManager),
            (400, 1, 0, FhmPersonnelJob.Scout));
        var secondRecordStart = HeaderLength + ReferenceRecordLength;
        BinaryPrimitives.WriteInt32BigEndian(
            content.AsSpan(secondRecordStart + IntegerListCountOffset, sizeof(int)),
            0);
        var result = new List<byte>(content);
        result.RemoveRange(
            secondRecordStart + IntegerListCountOffset + sizeof(int),
            8 * IntegerListItemLength);
        return result.ToArray();
    }
}
