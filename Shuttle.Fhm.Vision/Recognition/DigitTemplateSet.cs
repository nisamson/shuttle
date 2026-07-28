using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shuttle.Fhm.Vision.Recognition;

/// <summary>One labelled template glyph in a <see cref="DigitTemplateSet"/>.</summary>
public sealed record DigitTemplate {
    /// <summary>The character this template represents (e.g. <c>"5"</c>, <c>"."</c>, <c>"-"</c>).</summary>
    public required string Label { get; init; }

    /// <summary>Row-major <c>'1'</c>/<c>'0'</c> pixels, sized <c>Width*Height</c> of the owning set.</summary>
    public required string Bits { get; init; }
}

/// <summary>
/// A trained set of normalized glyph templates for the fixed FHM rating font. Recognition normalizes
/// each segmented glyph to <see cref="Width"/>x<see cref="Height"/> and classifies it by nearest
/// template (multiple samples per label are allowed and improve robustness).
/// </summary>
public sealed class DigitTemplateSet {
    /// <summary>Default normalized glyph width used when starting a fresh template set.</summary>
    public const int DefaultWidth = 12;

    /// <summary>Default normalized glyph height used when starting a fresh template set.</summary>
    public const int DefaultHeight = 20;

    public int Width { get; init; } = DefaultWidth;

    public int Height { get; init; } = DefaultHeight;

    public List<DigitTemplate> Templates { get; init; } = [];

    /// <summary>Materializes the templates as <see cref="GlyphBitmap"/>s paired with their label.</summary>
    public IReadOnlyList<(string Label, GlyphBitmap Glyph)> Materialize() =>
        Templates.Select(t => (t.Label, GlyphBitmap.FromBitString(Width, Height, t.Bits))).ToList();

    public void Add(string label, GlyphBitmap glyph) {
        ArgumentException.ThrowIfNullOrEmpty(label);
        ArgumentNullException.ThrowIfNull(glyph);
        if (glyph.Width != Width || glyph.Height != Height) {
            throw new ArgumentException(
                $"Glyph is {glyph.Width}x{glyph.Height}; set expects {Width}x{Height}.", nameof(glyph));
        }

        Templates.Add(new DigitTemplate { Label = label, Bits = glyph.ToBitString() });
    }

    /// <summary>
    /// Adds <paramref name="glyph"/> only if no existing <paramref name="label"/> template is within
    /// <paramref name="dedupDistance"/> Hamming pixels of it. Because the FHM font is fixed, most captures
    /// of a digit are near-identical; skipping duplicates keeps the set small and the classifier's
    /// margins meaningful. Returns <c>true</c> when the template was added, <c>false</c> when skipped as a
    /// duplicate.
    /// </summary>
    public bool TryAdd(string label, GlyphBitmap glyph, int dedupDistance = 0) {
        ArgumentException.ThrowIfNullOrEmpty(label);
        ArgumentNullException.ThrowIfNull(glyph);
        if (glyph.Width != Width || glyph.Height != Height) {
            throw new ArgumentException(
                $"Glyph is {glyph.Width}x{glyph.Height}; set expects {Width}x{Height}.", nameof(glyph));
        }

        foreach (var existing in Templates) {
            if (existing.Label != label) {
                continue;
            }

            var other = GlyphBitmap.FromBitString(Width, Height, existing.Bits);
            if (glyph.Distance(other) <= dedupDistance) {
                return false;
            }
        }

        Templates.Add(new DigitTemplate { Label = label, Bits = glyph.ToBitString() });
        return true;
    }

    /// <summary>
    /// Removes same-label templates that are within <paramref name="maxDistance"/> Hamming pixels of an
    /// earlier kept template (the first occurrence is always retained). Returns the number removed.
    /// </summary>
    public int Dedup(int maxDistance = 0) {
        var kept = new List<DigitTemplate>(Templates.Count);
        var removed = 0;
        foreach (var candidate in Templates) {
            var glyph = GlyphBitmap.FromBitString(Width, Height, candidate.Bits);
            var duplicate = false;
            foreach (var keeper in kept) {
                if (keeper.Label != candidate.Label) {
                    continue;
                }

                if (glyph.Distance(GlyphBitmap.FromBitString(Width, Height, keeper.Bits)) <= maxDistance) {
                    duplicate = true;
                    break;
                }
            }

            if (duplicate) {
                removed++;
            } else {
                kept.Add(candidate);
            }
        }

        if (removed > 0) {
            Templates.Clear();
            Templates.AddRange(kept);
        }

        return removed;
    }
}

/// <summary>Loads and saves <see cref="DigitTemplateSet"/> instances as JSON on disk.</summary>
public static class DigitTemplateStore {
    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static async Task SaveAsync(FileInfo file, DigitTemplateSet set, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(set);
        file.Directory?.Create();
        await File.WriteAllTextAsync(file.FullName, JsonSerializer.Serialize(set, Options), cancellationToken);
    }

    public static async Task<DigitTemplateSet> LoadAsync(FileInfo file, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(file);
        if (!file.Exists) {
            throw new FileNotFoundException($"Digit template set not found: {file.FullName}", file.FullName);
        }

        var json = await File.ReadAllTextAsync(file.FullName, cancellationToken);
        return JsonSerializer.Deserialize<DigitTemplateSet>(json, Options)
               ?? throw new InvalidOperationException("The digit template JSON deserialized to null.");
    }
}
