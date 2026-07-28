using Shuttle.Fhm.Vision.Layout;

namespace Shuttle.Fhm.Vision.Tests;

public sealed class RatioRectTests {
    [Fact]
    public void ToPixels_scales_to_image_size() {
        var rect = new RatioRect(0.25, 0.5, 0.25, 0.25);

        var pixels = rect.ToPixels(400, 200);

        Assert.Equal(new PixelRect(100, 100, 100, 50), pixels);
    }

    [Fact]
    public void ToPixels_clamps_to_image_bounds() {
        var rect = new RatioRect(0.9, 0.9, 0.5, 0.5);

        var pixels = rect.ToPixels(100, 100);

        Assert.Equal(90, pixels.X);
        Assert.Equal(90, pixels.Y);
        Assert.Equal(10, pixels.Width);
        Assert.Equal(10, pixels.Height);
    }

    [Fact]
    public void FromPixels_round_trips_through_ToPixels() {
        var original = RatioRect.FromPixels(100, 100, 100, 50, 400, 200);

        var pixels = original.ToPixels(400, 200);

        Assert.Equal(new PixelRect(100, 100, 100, 50), pixels);
    }
}
