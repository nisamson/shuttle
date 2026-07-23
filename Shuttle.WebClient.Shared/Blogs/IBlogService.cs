using Markdig;
using Shuttle.WebClient.Shared.Blogs;

namespace Shuttle.WebClient.Shared.Blogs;

public interface IBlogService {
    /// <summary>All blog entries, newest first.</summary>
    IReadOnlyList<BlogEntry> GetEntries();

    /// <summary>Gets a single entry by its slug, or <see langword="null"/> if not found.</summary>
    BlogEntry? GetEntry(string slug);

    /// <summary>Renders markdown content to HTML.</summary>
    string RenderHtml(string markdown);

    /// <summary>
    /// Produces a plain-text excerpt (markdown stripped, whitespace collapsed) suitable for a meta
    /// description, truncated to <paramref name="maxLength"/> characters with an ellipsis.
    /// </summary>
    string GetExcerpt(BlogEntry entry, int maxLength = 200);
}
