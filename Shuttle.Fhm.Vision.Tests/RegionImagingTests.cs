using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Shuttle.Fhm.Vision.Extraction;
using Shuttle.Fhm.Vision.Layout;

namespace Shuttle.Fhm.Vision.Tests;

public sealed class RegionImagingTests {
    private static Image<Rgba32> SolidImage(int width, int height, Rgba32 color) =>
        new(width, height, color);

    [Fact]
    public async Task CropForOcrAsync_upscales_small_region_by_integer_factor() {
        using var image = SolidImage(400, 300, new Rgba32(20, 40, 60));
        // 40x30 crop -> shorter side 30 < 64 -> factor ceil(64/30) = 3 -> 120x90.
        var rect = new PixelRect(0, 0, 40, 30);

        var png = await RegionImaging.CropForOcrAsync(image, rect, TestContext.Current.CancellationToken);

        using var result = Image.Load<Rgba32>(png);
        Assert.Equal(120, result.Width);
        Assert.Equal(90, result.Height);
        // A uniform region stays the same colour after upscaling.
        Assert.Equal(new Rgba32(20, 40, 60), result[0, 0]);
    }

    [Fact]
    public async Task CropForOcrAsync_leaves_large_region_unscaled() {
        using var image = SolidImage(400, 300, new Rgba32(1, 2, 3));
        var rect = new PixelRect(0, 0, 200, 120);

        var png = await RegionImaging.CropForOcrAsync(image, rect, TestContext.Current.CancellationToken);

        using var large = Image.Load<Rgba32>(png);
        Assert.Equal(200, large.Width);
        Assert.Equal(120, large.Height);
    }

    [Fact]
    public async Task CropForOcrAsync_isolates_white_text_to_black_on_white() {
        // Left half white (text), right half blue (background).
        using var image = new Image<Rgba32>(64, 64, new Rgba32(0, 0, 255));
        for (var y = 0; y < 64; y++) {
            for (var x = 0; x < 32; x++) {
                image[x, y] = new Rgba32(255, 255, 255);
            }
        }

        var png = await RegionImaging.CropForOcrAsync(
            image, new PixelRect(0, 0, 64, 64), TestContext.Current.CancellationToken, isolateWhiteText: true);

        using var result = Image.Load<Rgba32>(png);
        Assert.True(ContainsColor(result, new Rgba32(0, 0, 0)), "white text should become black");
        Assert.True(ContainsColor(result, new Rgba32(255, 255, 255)), "background should become white");
        Assert.False(ContainsColor(result, new Rgba32(0, 0, 255)), "the coloured background should be removed");
    }

    [Fact]
    public async Task CropForOcrAsync_pads_isolated_crop_with_white_quiet_zone() {
        // A 64x64 all-white crop stays white; padding adds a white border, growing the image.
        using var image = new Image<Rgba32>(64, 64, new Rgba32(255, 255, 255));

        var png = await RegionImaging.CropForOcrAsync(
            image, new PixelRect(0, 0, 64, 64), TestContext.Current.CancellationToken, isolateWhiteText: true);

        using var result = Image.Load<Rgba32>(png);
        Assert.True(result.Width > 64, "isolated crop should be padded wider than the source");
        Assert.True(result.Height > 64, "isolated crop should be padded taller than the source");
        Assert.Equal(new Rgba32(255, 255, 255), result[0, 0]); // padded border is white
    }

    private static bool ContainsColor(Image<Rgba32> image, Rgba32 color) {
        for (var y = 0; y < image.Height; y++) {
            for (var x = 0; x < image.Width; x++) {
                if (image[x, y] == color) {
                    return true;
                }
            }
        }

        return false;
    }
}
