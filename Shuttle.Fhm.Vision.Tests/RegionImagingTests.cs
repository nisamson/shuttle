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

        using var result = Image.Load<Rgba32>(png);
        Assert.Equal(200, result.Width);
        Assert.Equal(120, result.Height);
    }
}
