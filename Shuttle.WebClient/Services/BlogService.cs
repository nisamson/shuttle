using System.Globalization;
using Markdig;
using Shuttle.WebClient.Models;

namespace Shuttle.WebClient.Services;

public interface IBlogService {
    /// <summary>All blog entries, newest first.</summary>
    IReadOnlyList<BlogEntry> GetEntries();

    /// <summary>Gets a single entry by its slug, or <see langword="null"/> if not found.</summary>
    BlogEntry? GetEntry(string slug);

    /// <summary>Renders markdown content to HTML.</summary>
    string RenderHtml(string markdown);
}

/// <summary>
/// Serves blog articles from the markdown files embedded from the <c>BlogEntries</c> folder.
/// Each file is named <c>yyyyMMdd-Title.md</c>; the date comes from the name prefix and the title
/// from the first top-level markdown heading.
/// </summary>
public sealed class BlogService : IBlogService {
    private const string ResourceMarker = ".BlogEntries.";

    private readonly IReadOnlyList<BlogEntry> entries;
    private readonly MarkdownPipeline pipeline;

    public BlogService() {
        pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
        entries = LoadEntries();
    }

    public IReadOnlyList<BlogEntry> GetEntries() => entries;

    public BlogEntry? GetEntry(string slug) =>
        entries.FirstOrDefault(e => string.Equals(e.Slug, slug, StringComparison.OrdinalIgnoreCase));

    public string RenderHtml(string markdown) => Markdown.ToHtml(markdown, pipeline);

    private static IReadOnlyList<BlogEntry> LoadEntries() {
        var assembly = typeof(BlogService).Assembly;
        var result = new List<BlogEntry>();

        foreach (var resourceName in assembly.GetManifestResourceNames()) {
            var markerIndex = resourceName.IndexOf(ResourceMarker, StringComparison.Ordinal);
            if (markerIndex < 0 || !resourceName.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            var fileName = resourceName[(markerIndex + ResourceMarker.Length)..];
            if (!TryParseDate(fileName, out var date)) {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null) {
                continue;
            }

            using var reader = new StreamReader(stream);
            var markdown = reader.ReadToEnd();

            var slug = fileName[..^".md".Length];
            var title = ExtractTitle(markdown) ?? slug;

            result.Add(new BlogEntry {
                Slug = slug,
                Title = title,
                Date = date,
                Markdown = markdown,
            });
        }

        return result
            .OrderByDescending(e => e.Date)
            .ThenBy(e => e.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool TryParseDate(string fileName, out DateOnly date) {
        date = default;
        return fileName.Length >= 8
               && DateOnly.TryParseExact(
                   fileName[..8],
                   "yyyyMMdd",
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.None,
                   out date);
    }

    private static string? ExtractTitle(string markdown) {
        using var reader = new StringReader(markdown);
        while (reader.ReadLine() is { } line) {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("# ", StringComparison.Ordinal)) {
                return trimmed[2..].Trim();
            }
        }

        return null;
    }
}
