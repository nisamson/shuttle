using System.Buffers.Binary;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Icc;
using SixLabors.ImageSharp.PixelFormats;
using Shuttle.Fhm.Vision.Capture;

namespace Shuttle.Fhm.Vision.Tests;

public sealed class ImageFileLoaderTests
{
    [Theory]
    [InlineData("49492A00")]
    [InlineData("4D4D002A")]
    [InlineData("49492B00")]
    [InlineData("4D4D002B")]
    public async Task LoadAsync_rejects_tiff_signature_regardless_of_extension(string signature)
    {
        var file = new FileInfo(Path.Combine(Path.GetTempPath(), $"tiff-{Guid.NewGuid():N}.png"));
        try
        {
            await File.WriteAllBytesAsync(file.FullName, Convert.FromHexString(signature),
                TestContext.Current.CancellationToken);

            var error = await Assert.ThrowsAsync<NotSupportedException>(() =>
                ImageFileLoader.LoadAsync(file, TestContext.Current.CancellationToken));

            Assert.Contains("TIFF images are not supported", error.Message);
        }
        finally
        {
            file.Delete();
        }
    }

    [Fact]
    public async Task LoadAsync_accepts_png_even_with_tiff_extension()
    {
        var file = new FileInfo(Path.Combine(Path.GetTempPath(), $"image-{Guid.NewGuid():N}.tif"));
        try
        {
            using (var source = new Image<Rgba32>(1, 1, new Rgba32(12, 34, 56)))
            {
                await source.SaveAsPngAsync(file.FullName, TestContext.Current.CancellationToken);
            }

            using var loaded = await ImageFileLoader.LoadAsync(file, TestContext.Current.CancellationToken);

            Assert.Equal(12, loaded[0, 0].R);
            Assert.Equal(34, loaded[0, 0].G);
            Assert.Equal(56, loaded[0, 0].B);
        }
        finally
        {
            file.Delete();
        }
    }

    [Fact]
    public async Task LoadAsync_skips_embedded_icc_profile()
    {
        var file = new FileInfo(Path.Combine(Path.GetTempPath(), $"image-{Guid.NewGuid():N}.png"));
        try
        {
            using (var source = new Image<Rgba32>(1, 1, new Rgba32(12, 34, 56)))
            {
                source.Metadata.IccProfile = new IccProfile(CreateTruncatedIccProfile());
                await source.SaveAsPngAsync(file.FullName, TestContext.Current.CancellationToken);
            }

            using var loaded = await ImageFileLoader.LoadAsync(file, TestContext.Current.CancellationToken);

            Assert.Null(loaded.Metadata.IccProfile);
            Assert.Equal(12, loaded[0, 0].R);
            Assert.Equal(34, loaded[0, 0].G);
            Assert.Equal(56, loaded[0, 0].B);
        }
        finally
        {
            file.Delete();
        }
    }

    private static byte[] CreateTruncatedIccProfile()
    {
        const int tagOffset = 144;
        const int elementOffset = 168;
        byte[] profile = new byte[200];
        WriteUInt32(profile, 0, (uint)profile.Length);
        WriteAscii(profile, 4, "test");
        WriteUInt32(profile, 8, 0x04000000);
        WriteUInt32(profile, 12, 0x6D6E7472);
        WriteUInt32(profile, 16, 0x52474220);
        WriteUInt32(profile, 20, 0x58595A20);
        WriteAscii(profile, 36, "acsp");
        WriteUInt32(profile, 128, 1);
        WriteUInt32(profile, 132, 0x41324230);
        WriteUInt32(profile, 136, tagOffset);
        WriteUInt32(profile, 140, 56);
        WriteUInt32(profile, tagOffset, 0x6D706574);
        WriteUInt32(profile, tagOffset + 12, 1);
        WriteUInt32(profile, tagOffset + 16, 24);
        WriteUInt32(profile, tagOffset + 20, 32);
        WriteUInt32(profile, elementOffset, 0x636C7574);
        WriteUInt16(profile, elementOffset + 4, 15);
        WriteUInt16(profile, elementOffset + 6, 15);
        profile.AsSpan(elementOffset + 8, 15).Fill(3);
        return profile;
    }

    private static void WriteUInt16(byte[] bytes, int offset, ushort value) =>
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(offset, sizeof(ushort)), value);

    private static void WriteUInt32(byte[] bytes, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset, sizeof(uint)), value);

    private static void WriteAscii(byte[] bytes, int offset, string value) =>
        Encoding.ASCII.GetBytes(value).CopyTo(bytes, offset);
}
