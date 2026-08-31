using Shuttle.Fhm.Serde.Domain.Binary;

namespace Shuttle.Fhm.Serde.Domain.SaveFolder;

/// <summary>Writes one FHM 10 save folder and its path-preserving opaque entries.</summary>
public sealed class FhmSaveWriter
{
    /// <summary>Writes <paramref name="save"/> to an empty or new <paramref name="destinationDirectory"/>.</summary>
    public void Write(FhmSave save, string destinationDirectory)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);

        if (Directory.Exists(destinationDirectory) && Directory.EnumerateFileSystemEntries(destinationDirectory).Any())
        {
            throw new IOException($"FHM save destination '{destinationDirectory}' must be empty.");
        }

        Directory.CreateDirectory(destinationDirectory);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in save.Files.Values)
        {
            var relativePath = FhmSavePath.NormalizeRelativePath(file.RelativePath);
            if (FhmSaveAuxiliaryPaths.IsIgnored(relativePath))
            {
                continue;
            }

            WriteDocumentedFile(destinationDirectory, paths, file);
        }

        foreach (var file in save.OpaqueFiles)
        {
            var relativePath = FhmSavePath.NormalizeRelativePath(file.RelativePath);
            if (FhmSaveAuxiliaryPaths.IsIgnored(relativePath))
            {
                continue;
            }

            if (!paths.Add(relativePath))
            {
                throw new FhmFormatException($"Duplicate save-file path '{relativePath}'.");
            }

            var path = FhmSavePath.Resolve(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, file.Content);
        }
    }

    private static void WriteDocumentedFile(string destinationDirectory, ISet<string> paths, Files.IFhmSaveFile file)
    {
        var relativePath = FhmSavePath.NormalizeRelativePath(file.RelativePath);
        if (!paths.Add(relativePath))
        {
            throw new FhmFormatException($"Duplicate save-file path '{relativePath}'.");
        }

        var path = FhmSavePath.Resolve(destinationDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var output = File.Create(path);
        file.WriteTo(output);
    }
}
