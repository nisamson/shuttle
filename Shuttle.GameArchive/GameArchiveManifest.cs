using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shuttle.GameArchive;

public sealed record GameArchiveManifest(
    [property: JsonPropertyName("$schema"), JsonPropertyOrder(0)] string Schema,
    [property: JsonPropertyOrder(1)] int SchemaVersion,
    [property: JsonPropertyOrder(2)] string[] Files,
    [property: JsonPropertyOrder(3)] GameArchiveSkippedDirectory[] SkippedDirectories) {

    public const string FileName = "manifest.json";
    public const int CurrentSchemaVersion = 1;
    public const string SchemaUri = "https://raw.githubusercontent.com/nisamson/shuttle/main/docs/game-archive-manifest.schema.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
        WriteIndented = true,
        IndentSize = 2,
        NewLine = "\n",
        NumberHandling = JsonNumberHandling.Strict,
        PropertyNameCaseInsensitive = false,
        AllowDuplicateProperties = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
    };

    public static GameArchiveManifest Create(
        IEnumerable<string> files, IEnumerable<GameArchiveSkippedDirectory> skippedDirectories) =>
        new(SchemaUri, CurrentSchemaVersion, files.Order(StringComparer.Ordinal).ToArray(),
            skippedDirectories.OrderBy(entry => entry.Path, StringComparer.Ordinal).ToArray());

    public string ToJson() {
        Validate();
        return JsonSerializer.Serialize(this, JsonOptions) + "\n";
    }

    public static async Task<GameArchiveManifest> ReadAsync(Stream stream, CancellationToken cancellationToken) {
        GameArchiveManifest manifest;
        try {
            manifest = await JsonSerializer.DeserializeAsync<GameArchiveManifest>(stream, JsonOptions, cancellationToken)
                ?? throw new InvalidDataException("The archive manifest must be a JSON object.");
        } catch (JsonException ex) {
            throw new InvalidDataException("The archive manifest does not match the required JSON structure.", ex);
        }
        manifest.Validate();
        return manifest;
    }

    private void Validate() {
        if (SchemaVersion != CurrentSchemaVersion || Schema != SchemaUri)
            throw new InvalidDataException("The archive manifest has an unsupported schema or version.");
        if (Files is null || SkippedDirectories is null)
            throw new InvalidDataException("The archive manifest must contain files and skippedDirectories arrays.");
        if (Files.Any(path => path is null || !GameArchiveCrawler.IsValidFilePath(path)))
            throw new InvalidDataException("The archive manifest contains an unsafe file path.");
        if (Files.Distinct(StringComparer.OrdinalIgnoreCase).Count() != Files.Length)
            throw new InvalidDataException("The archive manifest contains duplicate or case-colliding file paths.");
        foreach (var skipped in SkippedDirectories) {
            if (skipped is null || skipped.Path is null || !GameArchiveCrawler.IsValidDirectoryPath(skipped.Path)
                || !Uri.TryCreate(skipped.Url, UriKind.Absolute, out var url)
                || url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeHttp
                || !url.AbsolutePath.EndsWith('/') || url.UserInfo.Length > 0
                || url.Query.Length > 0 || url.Fragment.Length > 0
                || skipped.Reason != GameArchiveSkippedDirectory.NonApacheReason)
                throw new InvalidDataException("The archive manifest contains an invalid skipped directory.");
            if (Files.Any(path => skipped.Path == "." || path.StartsWith(skipped.Path, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("The archive manifest lists files beneath a skipped directory.");
        }
        if (SkippedDirectories.Select(entry => entry.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != SkippedDirectories.Length)
            throw new InvalidDataException("The archive manifest contains duplicate or case-colliding skipped directories.");
    }
}
