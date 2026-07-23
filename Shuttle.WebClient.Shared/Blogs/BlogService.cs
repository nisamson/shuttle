using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;

namespace Shuttle.WebClient.Shared.Blogs;

/// <summary>
/// Serves blog articles from the markdown files embedded from the <c>BlogEntries</c> folder.
/// Each file is named <c>yyyyMMdd-Title.md</c>; the date comes from the name prefix and the title
/// from the first top-level markdown heading.
/// </summary>
public sealed partial class BlogService : IBlogService {
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

    public string GetExcerpt(BlogEntry entry, int maxLength = 200) {
        ArgumentNullException.ThrowIfNull(entry);

        // Drop the leading title heading so the excerpt reflects the body, then strip markdown to
        // plain text and collapse whitespace.
        var body = RemoveFirstHeading(entry.Markdown);
        var plain = Markdown.ToPlainText(body, pipeline);
        plain = WhitespaceRegex().Replace(plain, " ").Trim();

        if (plain.Length <= maxLength) {
            return plain;
        }

        var truncated = plain[..maxLength];
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > 0) {
            truncated = truncated[..lastSpace];
        }

        return truncated.TrimEnd() + "\u2026";
    }

    private static string RemoveFirstHeading(string markdown) {
        using var reader = new StringReader(markdown);
        var builder = new StringBuilder();
        var removed = false;
        while (reader.ReadLine() is { } line) {
            if (!removed && line.TrimStart().StartsWith("# ", StringComparison.Ordinal)) {
                removed = true;
                continue;
            }

            builder.AppendLine(line);
        }

        return builder.ToString();
    }

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

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
