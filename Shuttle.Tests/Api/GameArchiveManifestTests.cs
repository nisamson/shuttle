using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Shuttle.GameArchive;

namespace Shuttle.Tests.Api;

public sealed class GameArchiveManifestTests {
    [Fact]
    public async Task RoundTripsVersionedManifestWithFilesAndSkippedDirectories() {
        var manifest = GameArchiveManifest.Create(["shl/game.csv", "shl/manifest.json"], [
            new GameArchiveSkippedDirectory("iihf/S37/", "https://simulationhockey.com/games/iihf/S37/",
                null, GameArchiveSkippedDirectory.NonApacheReason),
        ]);

        var restored = await ReadAsync(manifest.ToJson());

        Assert.Equal(GameArchiveManifest.SchemaUri, restored.Schema);
        Assert.Equal(1, restored.SchemaVersion);
        Assert.Equal(manifest.Files, restored.Files);
        Assert.Equal(manifest.SkippedDirectories, restored.SkippedDirectories);
        Assert.Null(Assert.Single(restored.SkippedDirectories).Title);
    }

    [Fact]
    public void WritesSortedStableArraysAndExactlyTheSchemaProperties() {
        var manifest = GameArchiveManifest.Create(["b.csv", "Z.csv", "a.csv"], [
            new GameArchiveSkippedDirectory("b/", "https://example.com/games/b/", "B", GameArchiveSkippedDirectory.NonApacheReason),
            new GameArchiveSkippedDirectory("Z/", "https://example.com/games/Z/", "Z", GameArchiveSkippedDirectory.NonApacheReason),
            new GameArchiveSkippedDirectory("a/", "https://example.com/games/a/", "A", GameArchiveSkippedDirectory.NonApacheReason),
        ]);
        using var json = JsonDocument.Parse(manifest.ToJson());
        Assert.Equal(["Z.csv", "a.csv", "b.csv"], manifest.Files);
        Assert.Equal(["Z/", "a/", "b/"], manifest.SkippedDirectories.Select(entry => entry.Path));
        Assert.Equal(["$schema", "schemaVersion", "files", "skippedDirectories"],
            json.RootElement.EnumerateObject().Select(property => property.Name));
        Assert.Equal(["path", "url", "title", "reason"],
            json.RootElement.GetProperty("skippedDirectories")[0].EnumerateObject().Select(property => property.Name));
        Assert.DoesNotContain("\r", manifest.ToJson(), StringComparison.Ordinal);
        Assert.Equal(manifest.ToJson(), GameArchiveManifest.Create(
            manifest.Files.Reverse(), manifest.SkippedDirectories.Reverse()).ToJson());
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{broken")]
    public async Task RejectsMalformedOrIncompleteJson(string json) {
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadAsync(json));
    }

    [Theory]
    [InlineData("$schema")]
    [InlineData("schemaVersion")]
    [InlineData("files")]
    [InlineData("skippedDirectories")]
    public async Task RejectsMissingRequiredFields(string property) {
        var json = ManifestObject();
        json.Remove(property);
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadAsync(json.ToJsonString()));
    }

    [Fact]
    public async Task RejectsDuplicateProperties() {
        var json = GameArchiveManifest.Create([], []).ToJson()
            .Replace("\"schemaVersion\": 1", "\"schemaVersion\": 1, \"schemaVersion\": 1", StringComparison.Ordinal);
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadAsync(json));
    }

    [Theory]
    [InlineData("$schema", "\"https://example.com/other-schema\"")]
    [InlineData("schemaVersion", "2")]
    [InlineData("schemaVersion", "\"1\"")]
    [InlineData("files", "null")]
    [InlineData("files", "[null]")]
    [InlineData("files", "{}")]
    [InlineData("files", "[\"game.csv\",\"game.csv\"]")]
    [InlineData("files", "[\"game.csv\",\"GAME.csv\"]")]
    [InlineData("skippedDirectories", "null")]
    [InlineData("skippedDirectories", "[null]")]
    [InlineData("unknownProperty", "true")]
    [InlineData("Files", "[]")]
    [InlineData("SchemaVersion", "1")]
    public async Task RejectsUnsupportedSchemaAndInvalidFieldValues(string property, string value) {
        var json = ManifestObject();
        json[property] = JsonNode.Parse(value);
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadAsync(json.ToJsonString()));
    }

    [Theory]
    [InlineData("../README.md")]
    [InlineData("/absolute.csv")]
    [InlineData("shl/../game.csv")]
    [InlineData("shl/.git/config")]
    [InlineData("manifest.json")]
    [InlineData("MANIFEST.JSON/game.csv")]
    [InlineData("README.md")]
    [InlineData("NUL.csv")]
    public async Task RejectsUnsafeOrReservedFilePaths(string path) {
        var json = ManifestObject();
        json["files"] = new JsonArray(path);
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadAsync(json.ToJsonString()));
    }

    [Theory]
    [InlineData("path", "\"../\"")]
    [InlineData("path", "\"shl\"")]
    [InlineData("url", "\"https://example.com/games/custom\"")]
    [InlineData("url", "\"https://user:secret@example.com/games/custom/\"")]
    [InlineData("url", "\"https://example.com/games/custom/?token=x\"")]
    [InlineData("url", "\"file:///C:/custom/\"")]
    [InlineData("reason", "\"unknown-reason\"")]
    [InlineData("title", "123")]
    [InlineData("extra", "true")]
    [InlineData("Path", "\"custom/\"")]
    public async Task RejectsInvalidSkippedDirectoryFields(string property, string value) {
        var json = ManifestObject();
        var skipped = SkippedDirectoryObject();
        skipped[property] = JsonNode.Parse(value);
        json["skippedDirectories"] = new JsonArray(skipped);
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadAsync(json.ToJsonString()));
    }

    [Theory]
    [InlineData("path")]
    [InlineData("url")]
    [InlineData("title")]
    [InlineData("reason")]
    public async Task RequiresEverySkippedDirectoryFieldIncludingNullableTitle(string property) {
        var json = ManifestObject();
        var skipped = SkippedDirectoryObject();
        skipped.Remove(property);
        json["skippedDirectories"] = new JsonArray(skipped);
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadAsync(json.ToJsonString()));
    }

    [Fact]
    public async Task RejectsCaseCollidingSkippedDirectories() {
        var json = ManifestObject();
        var first = SkippedDirectoryObject();
        var second = SkippedDirectoryObject();
        second["path"] = "CUSTOM/";
        json["skippedDirectories"] = new JsonArray(first, second);
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadAsync(json.ToJsonString()));
    }

    [Theory]
    [InlineData("custom/")]
    [InlineData(".")]
    public async Task RejectsFilesInsideExcludedSubtrees(string skippedPath) {
        var json = ManifestObject();
        var skipped = SkippedDirectoryObject();
        skipped["path"] = skippedPath;
        json["skippedDirectories"] = new JsonArray(skipped);
        json["files"] = new JsonArray("custom/game.csv");
        await Assert.ThrowsAsync<InvalidDataException>(() => ReadAsync(json.ToJsonString()));
    }

    [Fact]
    public void SchemaDocumentMatchesSerializedManifestAndVersion() {
        using var stream = Assert.IsAssignableFrom<Stream>(
            typeof(GameArchiveManifestTests).Assembly.GetManifestResourceStream("GameArchiveManifestSchema.json"));
        using var schema = JsonDocument.Parse(stream);
        using var manifest = JsonDocument.Parse(GameArchiveManifest.Create(["shl/game.csv"], []).ToJson());
        var root = schema.RootElement;
        Assert.Equal(GameArchiveManifest.SchemaUri, root.GetProperty("$id").GetString());
        Assert.False(root.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(manifest.RootElement.EnumerateObject().Select(property => property.Name),
            root.GetProperty("required").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(GameArchiveManifest.SchemaUri, root.GetProperty("properties").GetProperty("$schema").GetProperty("const").GetString());
        Assert.Equal(GameArchiveManifest.CurrentSchemaVersion,
            root.GetProperty("properties").GetProperty("schemaVersion").GetProperty("const").GetInt32());
        Assert.Matches(Assert.IsType<string>(
            root.GetProperty("$defs").GetProperty("filePath").GetProperty("pattern").GetString()), "shl/game.csv");
        Assert.Matches(Assert.IsType<string>(
            root.GetProperty("$defs").GetProperty("directoryPath").GetProperty("anyOf")[1].GetProperty("pattern").GetString()), "custom/");
    }

    private static JsonObject ManifestObject() =>
        Assert.IsType<JsonObject>(JsonNode.Parse(GameArchiveManifest.Create([], []).ToJson()));

    private static JsonObject SkippedDirectoryObject() => new() {
        ["path"] = "custom/",
        ["url"] = "https://example.com/games/custom/",
        ["title"] = null,
        ["reason"] = GameArchiveSkippedDirectory.NonApacheReason,
    };

    private static async Task<GameArchiveManifest> ReadAsync(string json) {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return await GameArchiveManifest.ReadAsync(stream, TestContext.Current.CancellationToken);
    }
}
