using System.Text.Json.Serialization;

namespace Shuttle.GameArchive;

public sealed record GameArchiveInventory(
    IReadOnlyDictionary<string, Uri> Files,
    IReadOnlyList<GameArchiveSkippedDirectory> SkippedDirectories);

public sealed record GameArchiveSkippedDirectory(
    [property: JsonPropertyOrder(0)] string Path,
    [property: JsonPropertyOrder(1)] string Url,
    [property: JsonPropertyOrder(2)] string? Title,
    [property: JsonPropertyOrder(3)] string Reason) {
    public const string NonApacheReason = "non-apache-directory-listing";
}
