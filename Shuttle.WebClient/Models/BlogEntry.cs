namespace Shuttle.WebClient.Models;

/// <summary>
/// A blog article sourced from an embedded markdown file in the <c>BlogEntries</c> folder.
/// </summary>
public sealed record BlogEntry {
    /// <summary>URL-safe identifier derived from the file name (without the <c>.md</c> extension).</summary>
    public required string Slug { get; init; }

    /// <summary>The article title, taken from the first top-level markdown heading.</summary>
    public required string Title { get; init; }

    /// <summary>The date the article was written, parsed from the <c>yyyyMMdd</c> file-name prefix.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>The raw markdown content of the article.</summary>
    public required string Markdown { get; init; }
}
