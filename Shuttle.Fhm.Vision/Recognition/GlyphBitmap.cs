namespace Shuttle.Fhm.Vision.Recognition;

/// <summary>
/// A fixed-size, normalized monochrome glyph: <see cref="Pixels"/> is row-major, <c>true</c> where
/// there is ink (text). Used both as a template and as a candidate to classify by nearest neighbour.
/// </summary>
public sealed class GlyphBitmap {
    public GlyphBitmap(int width, int height, bool[] pixels) {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(width, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(height, 0);
        ArgumentNullException.ThrowIfNull(pixels);
        if (pixels.Length != width * height) {
            throw new ArgumentException($"Expected {width * height} pixels, got {pixels.Length}.", nameof(pixels));
        }

        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public int Width { get; }

    public int Height { get; }

    public bool[] Pixels { get; }

    /// <summary>Number of ink pixels (used to reject noise/empty glyphs).</summary>
    public int InkCount {
        get {
            var count = 0;
            foreach (var pixel in Pixels) {
                if (pixel) {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>Hamming distance (count of differing pixels) to a same-size glyph.</summary>
    public int Distance(GlyphBitmap other) {
        ArgumentNullException.ThrowIfNull(other);
        if (other.Width != Width || other.Height != Height) {
            throw new ArgumentException("Glyph bitmaps must have the same dimensions to compare.", nameof(other));
        }

        var distance = 0;
        for (var i = 0; i < Pixels.Length; i++) {
            if (Pixels[i] != other.Pixels[i]) {
                distance++;
            }
        }

        return distance;
    }

    /// <summary>Serializes the pixels as a run of <c>'1'</c>/<c>'0'</c> characters (row-major).</summary>
    public string ToBitString() {
        var chars = new char[Pixels.Length];
        for (var i = 0; i < Pixels.Length; i++) {
            chars[i] = Pixels[i] ? '1' : '0';
        }

        return new string(chars);
    }

    /// <summary>Rebuilds a glyph from <see cref="ToBitString"/> output.</summary>
    public static GlyphBitmap FromBitString(int width, int height, string bits) {
        ArgumentNullException.ThrowIfNull(bits);
        if (bits.Length != width * height) {
            throw new ArgumentException($"Expected {width * height} bits, got {bits.Length}.", nameof(bits));
        }

        var pixels = new bool[bits.Length];
        for (var i = 0; i < bits.Length; i++) {
            pixels[i] = bits[i] == '1';
        }

        return new GlyphBitmap(width, height, pixels);
    }
}
