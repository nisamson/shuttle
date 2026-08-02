using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Shuttle.Fhm.Vision.Capture;

/// <summary>
/// Captures a window using GDI <c>PrintWindow</c> with <c>PW_RENDERFULLCONTENT</c>, which works for
/// most windows — including many GPU/DirectX-rendered ones — even when partially occluded.
/// </summary>
/// <remarks>
/// A Windows.Graphics.Capture-based engine is a planned enhancement for windows this cannot read;
/// it would implement the same <see cref="IFrameCapture"/> seam.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class GdiWindowCapture : IFrameCapture {
    public Image<Rgba32> Capture(IntPtr handle) {
        if (!NativeMethods.GetWindowRect(handle, out var rect)) {
            throw new InvalidOperationException($"GetWindowRect failed for window 0x{handle:X}.");
        }

        var width = rect.Width;
        var height = rect.Height;
        if (width <= 0 || height <= 0) {
            throw new InvalidOperationException($"Window 0x{handle:X} has a non-positive size ({width}x{height}).");
        }

        using var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap)) {
            var hdc = graphics.GetHdc();
            try {
                if (!NativeMethods.PrintWindow(handle, hdc, NativeMethods.PwRenderFullContent)) {
                    throw new InvalidOperationException($"PrintWindow failed for window 0x{handle:X}.");
                }
            } finally {
                graphics.ReleaseHdc(hdc);
            }
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return SixLabors.ImageSharp.Image.Load<Rgba32>(stream.ToArray());
    }
}
