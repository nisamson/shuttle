using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Shuttle.Fhm.Vision.Extraction;
using Shuttle.Fhm.Vision.Layout;

namespace Shuttle.Fhm.Vision.Recognition;

/// <summary>
/// One glyph awaiting a label in the training UI: where it came from, its normalized bitmap (what the
/// recognizer actually matches), and the raw source crop (PNG) shown as the "original" preview.
/// </summary>
/// <param name="ImageName">File name of the source screenshot the glyph was segmented from.</param>
/// <param name="RegionKey">The layout region key the glyph belongs to (e.g. <c>skating</c>).</param>
/// <param name="GlyphIndex">Zero-based position of this glyph within its region (left-to-right).</param>
/// <param name="GlyphCount">Total glyphs segmented from the region.</param>
/// <param name="Normalized">The normalized glyph bitmap classified/stored by the recognizer.</param>
/// <param name="OriginalCropPng">PNG of the raw source crop of the glyph's bounds.</param>
public sealed record PendingGlyph(
    string ImageName,
    string RegionKey,
    int GlyphIndex,
    int GlyphCount,
    GlyphBitmap Normalized,
    byte[] OriginalCropPng
);

/// <summary>
/// Builds the queue of <see cref="PendingGlyph"/>s the training UI walks through, by segmenting the
/// numeric (<see cref="FieldKind.Integer"/>/<see cref="FieldKind.Float"/>) regions of every matching
/// layout profile across one or more screenshots. Mirrors how the console <c>train-digits</c> command
/// finds glyphs, so both trainers stay consistent.
/// </summary>
public static class PendingGlyphBuilder {
    /// <summary>
    /// Segments the numeric regions of the given <paramref name="profiles"/> across
    /// <paramref name="images"/> into normalized glyphs. Non-existent image files are skipped; a profile
    /// is only used for an image whose anchors it matches (<see cref="RegionExtractor.IsPlayerScreenAsync"/>).
    /// </summary>
    public static async Task<IReadOnlyList<PendingGlyph>> BuildAsync(
        IEnumerable<FileInfo> images,
        IReadOnlyList<LayoutProfile> profiles,
        int normWidth,
        int normHeight,
        RegionExtractor extractor,
        CancellationToken cancellationToken
    ) {
        ArgumentNullException.ThrowIfNull(images);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(extractor);

        var result = new List<PendingGlyph>();
        foreach (var file in images) {
            if (file is null || !file.Exists) {
                continue;
            }

            using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(file.FullName, cancellationToken);
            foreach (var profile in profiles) {
                if (!await extractor.IsPlayerScreenAsync(image, profile, cancellationToken)) {
                    continue;
                }

                foreach (var region in profile.Regions) {
                    if (region.Kind is not (FieldKind.Integer or FieldKind.Float)) {
                        continue;
                    }

                    var pixels = region.Bounds.ToPixels(image.Width, image.Height);
                    var glyphs = DigitSegmenter.Segment(image, pixels, normWidth, normHeight);
                    for (var i = 0; i < glyphs.Count; i++) {
                        var png = await RegionImaging.CropToPngAsync(image, glyphs[i].Bounds, cancellationToken);
                        result.Add(new PendingGlyph(
                            file.Name, region.Key, i, glyphs.Count, glyphs[i].Glyph, png));
                    }
                }
            }
        }

        return result;
    }
}
