using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shuttle.Fhm.Vision.Layout;

/// <summary>Loads and saves <see cref="LayoutProfile"/> instances as JSON on disk.</summary>
public static class LayoutProfileStore {
    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(LayoutProfile profile) {
        ArgumentNullException.ThrowIfNull(profile);
        return JsonSerializer.Serialize(profile, Options);
    }

    public static LayoutProfile Deserialize(string json) {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<LayoutProfile>(json, Options)
               ?? throw new InvalidOperationException("The layout profile JSON deserialized to null.");
    }

    public static async Task SaveAsync(FileInfo file, LayoutProfile profile, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(file);
        file.Directory?.Create();
        await File.WriteAllTextAsync(file.FullName, Serialize(profile), cancellationToken);
    }

    public static async Task<LayoutProfile> LoadAsync(FileInfo file, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(file);
        if (!file.Exists) {
            throw new FileNotFoundException($"Layout profile not found: {file.FullName}", file.FullName);
        }

        var json = await File.ReadAllTextAsync(file.FullName, cancellationToken);
        return Deserialize(json);
    }
}
