using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using Shuttle.Fhm.Vision.Recognition;

namespace Shuttle.Fhm.Vision.Training;

/// <summary>WinForms bitmap helpers for the digit-training UI (converts recognizer glyphs to <see cref="Bitmap"/>s).</summary>
[SupportedOSPlatform("windows")]
public static class GlyphImaging {
    /// <summary>
    /// Renders a normalized <see cref="GlyphBitmap"/> as a black-on-white <see cref="Bitmap"/>, scaled up
    /// by <paramref name="scale"/> so the low-resolution template is legible in a preview box.
    /// </summary>
    public static Bitmap Render(GlyphBitmap glyph, int scale = 12) {
        ArgumentNullException.ThrowIfNull(glyph);
        ArgumentOutOfRangeException.ThrowIfLessThan(scale, 1);

        var bitmap = new Bitmap(glyph.Width * scale, glyph.Height * scale);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        for (var y = 0; y < glyph.Height; y++) {
            for (var x = 0; x < glyph.Width; x++) {
                if (glyph.Pixels[(y * glyph.Width) + x]) {
                    graphics.FillRectangle(Brushes.Black, x * scale, y * scale, scale, scale);
                }
            }
        }

        return bitmap;
    }

    /// <summary>Decodes PNG bytes into a standalone <see cref="Bitmap"/> (detached from the source stream).</summary>
    public static Bitmap FromPng(byte[] png) {
        ArgumentNullException.ThrowIfNull(png);
        using var stream = new MemoryStream(png);
        using var loaded = new Bitmap(stream);
        return new Bitmap(loaded);
    }
}
