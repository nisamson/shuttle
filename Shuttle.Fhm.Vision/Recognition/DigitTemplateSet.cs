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

    /// <summary>
    /// Glyphs parsed once and grouped by label, so add/dedup/lookup only touch the templates that share a
    /// candidate's label instead of scanning the whole set. Lazily built from <see cref="Templates"/> and
    /// kept in sync as templates are added; a <c>null</c> value means it must be rebuilt.
    /// </summary>
    private Dictionary<string, List<GlyphBitmap>>? byLabel;

    public int Width { get; init; } = DefaultWidth;

    public int Height { get; init; } = DefaultHeight;

    public List<DigitTemplate> Templates { get; init; } = [];

    private Dictionary<string, List<GlyphBitmap>> ByLabel {
        get {
            if (byLabel is not null) {
                return byLabel;
            }

            var grouped = new Dictionary<string, List<GlyphBitmap>>(StringComparer.Ordinal);
            foreach (var template in Templates) {
                GetOrAddGroup(grouped, template.Label).Add(GlyphBitmap.FromBitString(Width, Height, template.Bits));
            }

            byLabel = grouped;
            return grouped;
        }
    }

    /// <summary>Materializes the templates as <see cref="GlyphBitmap"/>s paired with their label.</summary>
    public IReadOnlyList<(string Label, GlyphBitmap Glyph)> Materialize() {
        var result = new List<(string, GlyphBitmap)>(Templates.Count);
        foreach (var (label, glyphs) in ByLabel) {
            foreach (var glyph in glyphs) {
                result.Add((label, glyph));
            }
        }

        return result;
    }

    public void Add(string label, GlyphBitmap glyph) {
        ArgumentException.ThrowIfNullOrEmpty(label);
        EnsureCompatible(glyph);
        Append(label, glyph);
    }

    /// <summary>
    /// Adds <paramref name="glyph"/> only if no existing <paramref name="label"/> template is within
    /// <paramref name="dedupDistance"/> Hamming pixels of it. Because the FHM font is fixed, most captures
    /// of a digit are near-identical; skipping duplicates keeps the set small and the classifier's
    /// margins meaningful. Only templates sharing <paramref name="label"/> are compared. Returns
    /// <c>true</c> when the template was added, <c>false</c> when skipped as a duplicate.
    /// </summary>
    public bool TryAdd(string label, GlyphBitmap glyph, int dedupDistance = 0) {
        ArgumentException.ThrowIfNullOrEmpty(label);
        EnsureCompatible(glyph);

        if (ByLabel.TryGetValue(label, out var group)) {
            foreach (var existing in group) {
                if (glyph.Distance(existing) <= dedupDistance) {
                    return false;
                }
            }
        }

        Append(label, glyph);
        return true;
    }

    /// <summary>
    /// Removes same-label templates that are within <paramref name="maxDistance"/> Hamming pixels of an
    /// earlier kept template (the first occurrence is always retained). Comparisons stay within each
    /// label group. Returns the number removed.
    /// </summary>
    public int Dedup(int maxDistance = 0) {
        var keptGlyphs = new Dictionary<string, List<GlyphBitmap>>(StringComparer.Ordinal);
        var kept = new List<DigitTemplate>(Templates.Count);
        var removed = 0;

        foreach (var candidate in Templates) {
            var glyph = GlyphBitmap.FromBitString(Width, Height, candidate.Bits);
            var group = GetOrAddGroup(keptGlyphs, candidate.Label);
            var duplicate = false;
            foreach (var keeper in group) {
                if (glyph.Distance(keeper) <= maxDistance) {
                    duplicate = true;
                    break;
                }
            }

            if (duplicate) {
                removed++;
            } else {
                group.Add(glyph);
                kept.Add(candidate);
            }
        }

        if (removed > 0) {
            Templates.Clear();
            Templates.AddRange(kept);
            byLabel = keptGlyphs;
        }

        return removed;
    }

    private void EnsureCompatible(GlyphBitmap glyph) {
        ArgumentNullException.ThrowIfNull(glyph);
        if (glyph.Width != Width || glyph.Height != Height) {
            throw new ArgumentException(
                $"Glyph is {glyph.Width}x{glyph.Height}; set expects {Width}x{Height}.", nameof(glyph));
        }
    }

    private void Append(string label, GlyphBitmap glyph) {
        Templates.Add(new DigitTemplate { Label = label, Bits = glyph.ToBitString() });
        if (byLabel is not null) {
            GetOrAddGroup(byLabel, label).Add(glyph);
        }
    }

    private static List<GlyphBitmap> GetOrAddGroup(Dictionary<string, List<GlyphBitmap>> groups, string label) {
        if (!groups.TryGetValue(label, out var group)) {
            group = [];
            groups[label] = group;
        }

        return group;
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
