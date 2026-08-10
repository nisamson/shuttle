using Shuttle.Fhm.SaveData.Binary;
using Shuttle.Fhm.SaveData.Files;

namespace Shuttle.Fhm.SaveData.SaveFolder;

/// <summary>Reads one FHM 10 save folder without following entries outside its root.</summary>
public sealed class FhmSaveReader
{
    /// <summary>Reads the save folder at <paramref name="sourceDirectory"/>.</summary>
    public FhmSave Read(string sourceDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"FHM save folder '{sourceDirectory}' does not exist.");
        }

        var root = Path.GetFullPath(sourceDirectory);
        var result = new FhmSave();
        foreach (var filePath in EnumerateFiles(root))
        {
            var relativePath = FhmSavePath.NormalizeRelativePath(Path.GetRelativePath(root, filePath));
            var bytes = File.ReadAllBytes(filePath);
            var documented = FhmFileCodecs.TryRead(relativePath, bytes);
            if (documented is null)
            {
                result.OpaqueFiles.Add(new FhmOpaqueFile(relativePath, bytes));
            }
            else if (!result.Files.TryAdd(documented.RelativePath, documented))
            {
                throw new FhmFormatException($"Duplicate documented FHM file '{documented.RelativePath}'.");
            }
        }

        return result;
    }

    private static IEnumerable<string> EnumerateFiles(string root)
    {
        var pendingDirectories = new Queue<string>();
        pendingDirectories.Enqueue(root);
        while (pendingDirectories.Count > 0)
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(pendingDirectories.Dequeue()))
            {
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new FhmFormatException($"FHM save folders cannot contain reparse-point entry '{entry}'.");
                }

                if ((attributes & FileAttributes.Directory) != 0)
                {
                    pendingDirectories.Enqueue(entry);
                }
                else
                {
                    yield return entry;
                }
            }
        }
    }
}
