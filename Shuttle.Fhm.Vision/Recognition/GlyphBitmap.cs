using System.Collections;
using System.Numerics;

namespace Shuttle.Fhm.Vision.Recognition;

/// <summary>
/// A fixed-size, normalized monochrome glyph: a row-major bit per pixel, set where there is ink (text).
/// Used both as a template and as a candidate to classify by nearest neighbour. Pixels are stored packed
/// (a <see cref="BitArray"/> plus a cached 32-bit-word view) so the hot-path <see cref="Distance"/> is a
/// XOR + population count over words rather than a per-pixel loop.
/// </summary>
public sealed class GlyphBitmap {
    private readonly int[] words;

    public GlyphBitmap(int width, int height, bool[] pixels)
        : this(width, height, BuildBits(width, height, pixels)) {
    }

    private GlyphBitmap(int width, int height, BitArray bits) {
        Width = width;
        Height = height;
        Bits = bits;
        words = ToWords(bits);
        InkCount = PopCount(words);
    }

    public int Width { get; }

    public int Height { get; }

    /// <summary>The packed pixel bits (row-major), <c>true</c> where there is ink.</summary>
    public BitArray Bits { get; }

    /// <summary>Number of pixels (<see cref="Width"/> * <see cref="Height"/>).</summary>
    public int Length => Bits.Length;

    /// <summary>Reads a single row-major pixel; <c>true</c> where there is ink.</summary>
    public bool this[int index] => Bits[index];

    /// <summary>Number of ink pixels (used to reject noise/empty glyphs).</summary>
    public int InkCount { get; }

    /// <summary>
    /// Hamming distance (count of differing pixels) to a same-size glyph, computed as the population
    /// count of the XOR of the two packed bit vectors. Unused high bits in the final word are zero in
    /// both operands, so they never contribute.
    /// </summary>
    public int Distance(GlyphBitmap other) {
        ArgumentNullException.ThrowIfNull(other);
        if (other.Width != Width || other.Height != Height) {
            throw new ArgumentException("Glyph bitmaps must have the same dimensions to compare.", nameof(other));
        }

        var distance = 0;
        var a = words;
        var b = other.words;
        for (var i = 0; i < a.Length; i++) {
            distance += BitOperations.PopCount((uint)(a[i] ^ b[i]));
        }

        return distance;
    }

    /// <summary>Serializes the pixels as a run of <c>'1'</c>/<c>'0'</c> characters (row-major).</summary>
    public string ToBitString() {
        var chars = new char[Bits.Length];
        for (var i = 0; i < chars.Length; i++) {
            chars[i] = Bits[i] ? '1' : '0';
        }

        return new string(chars);
    }

    /// <summary>Rebuilds a glyph from <see cref="ToBitString"/> output.</summary>
    public static GlyphBitmap FromBitString(int width, int height, string bits) {
        ArgumentNullException.ThrowIfNull(bits);
        ValidateSize(width, height);
        if (bits.Length != width * height) {
            throw new ArgumentException($"Expected {width * height} bits, got {bits.Length}.", nameof(bits));
        }

        var array = new BitArray(bits.Length);
        for (var i = 0; i < bits.Length; i++) {
            if (bits[i] == '1') {
                array[i] = true;
            }
        }

        return new GlyphBitmap(width, height, array);
    }

    private static BitArray BuildBits(int width, int height, bool[] pixels) {
        ValidateSize(width, height);
        ArgumentNullException.ThrowIfNull(pixels);
        if (pixels.Length != width * height) {
            throw new ArgumentException($"Expected {width * height} pixels, got {pixels.Length}.", nameof(pixels));
        }

        return new BitArray(pixels);
    }

    private static void ValidateSize(int width, int height) {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(width, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(height, 0);
    }

    private static int[] ToWords(BitArray bits) {
        var packed = new int[(bits.Length + 31) / 32];
        bits.CopyTo(packed, 0);
        return packed;
    }

    private static int PopCount(int[] words) {
        var count = 0;
        foreach (var word in words) {
            count += BitOperations.PopCount((uint)word);
        }

        return count;
    }
}
