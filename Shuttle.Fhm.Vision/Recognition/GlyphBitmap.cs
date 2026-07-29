using System.Numerics;

namespace Shuttle.Fhm.Vision.Recognition;

/// <summary>
/// A fixed-size, normalized monochrome glyph: a row-major bit per pixel, set where there is ink (text).
/// Used both as a template and as a candidate to classify by nearest neighbour. Pixels are stored packed
/// into 32-bit words so the hot-path <see cref="Distance"/> is a XOR + population count over words rather
/// than a per-pixel loop.
/// </summary>
public sealed class GlyphBitmap : IEquatable<GlyphBitmap> {
    private const int BitsPerWord = 32;

    private readonly uint[] words;
    private readonly int hash;

    public GlyphBitmap(int width, int height, bool[] pixels) {
        ValidateSize(width, height);
        ArgumentNullException.ThrowIfNull(pixels);
        if (pixels.Length != width * height) {
            throw new ArgumentException($"Expected {width * height} pixels, got {pixels.Length}.", nameof(pixels));
        }

        Width = width;
        Height = height;
        Length = pixels.Length;
        words = new uint[WordCount(Length)];
        for (var i = 0; i < pixels.Length; i++) {
            if (pixels[i]) {
                words[i / BitsPerWord] |= 1u << (i % BitsPerWord);
            }
        }

        InkCount = PopCount(words);
        hash = ComputeHash(width, height, words);
    }

    private GlyphBitmap(int width, int height, int length, uint[] words) {
        Width = width;
        Height = height;
        Length = length;
        this.words = words;
        InkCount = PopCount(words);
        hash = ComputeHash(width, height, words);
    }

    public int Width { get; }

    public int Height { get; }

    /// <summary>Number of pixels (<see cref="Width"/> * <see cref="Height"/>).</summary>
    public int Length { get; }

    /// <summary>Reads a single row-major pixel; <c>true</c> where there is ink.</summary>
    public bool this[int index] {
        get {
            if ((uint)index >= (uint)Length) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return (words[index / BitsPerWord] & (1u << (index % BitsPerWord))) != 0;
        }
    }

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
            distance += BitOperations.PopCount(a[i] ^ b[i]);
        }

        return distance;
    }

    /// <summary>Serializes the pixels as a run of <c>'1'</c>/<c>'0'</c> characters (row-major).</summary>
    public string ToBitString() {
        var chars = new char[Length];
        for (var i = 0; i < Length; i++) {
            chars[i] = this[i] ? '1' : '0';
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

        var words = new uint[WordCount(bits.Length)];
        for (var i = 0; i < bits.Length; i++) {
            if (bits[i] == '1') {
                words[i / BitsPerWord] |= 1u << (i % BitsPerWord);
            }
        }

        return new GlyphBitmap(width, height, bits.Length, words);
    }

    /// <summary>
    /// Value equality by exact pixel content and dimensions. Lets glyphs be used as hash-set/dictionary
    /// keys so exact-duplicate detection is O(1) per glyph instead of a pairwise scan.
    /// </summary>
    public bool Equals(GlyphBitmap? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        if (Width != other.Width || Height != other.Height || hash != other.hash) {
            return false;
        }

        return words.AsSpan().SequenceEqual(other.words);
    }

    public override bool Equals(object? obj) => Equals(obj as GlyphBitmap);

    public override int GetHashCode() => hash;

    private static int WordCount(int length) => (length + BitsPerWord - 1) / BitsPerWord;

    private static void ValidateSize(int width, int height) {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(width, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(height, 0);
    }

    private static int PopCount(uint[] words) {
        var count = 0;
        foreach (var word in words) {
            count += BitOperations.PopCount(word);
        }

        return count;
    }

    private static int ComputeHash(int width, int height, uint[] words) {
        var hash = new HashCode();
        hash.Add(width);
        hash.Add(height);
        hash.AddBytes(System.Runtime.InteropServices.MemoryMarshal.AsBytes(words.AsSpan()));
        return hash.ToHashCode();
    }
}
