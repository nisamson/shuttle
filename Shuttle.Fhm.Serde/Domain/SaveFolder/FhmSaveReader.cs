using Shuttle.Fhm.Serde.Domain.Binary;
using Shuttle.Fhm.Serde.Domain.Files;

namespace Shuttle.Fhm.Serde.Domain.SaveFolder;

/// <summary>Options governing asynchronous FHM save-folder reads.</summary>
public sealed class FhmSaveReadOptions
{
    /// <summary>Gets or sets the maximum number of files to read and decode concurrently.</summary>
    public int MaxDegreeOfParallelism { get; init; } = 2;
    /// <summary>Gets or sets an optional recipient of source read progress updates.</summary>
    public IProgress<FhmSaveReadProgress>? Progress { get; init; }
}

/// <summary>Reports the current source read or decode operation.</summary>
public sealed record FhmSaveReadProgress(string Phase, string RelativePath);

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
            using var content = OpenRead(filePath, asynchronous: false);
            AddFile(result, relativePath, content);
        }

        return result;
    }

    /// <summary>Asynchronously reads a save folder with bounded parallel file reads and decoding.</summary>
    public Task<FhmSave> ReadAsync(
        string sourceDirectory,
        CancellationToken cancellationToken = default) =>
        ReadAsync(sourceDirectory, new FhmSaveReadOptions(), cancellationToken);

    /// <summary>Asynchronously reads a save folder using explicit parallelism options.</summary>
    public async Task<FhmSave> ReadAsync(
        string sourceDirectory,
        FhmSaveReadOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);
        ArgumentNullException.ThrowIfNull(options);
        if (options.MaxDegreeOfParallelism < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.MaxDegreeOfParallelism,
                "The maximum degree of parallelism must be at least one.");
        }

        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"FHM save folder '{sourceDirectory}' does not exist.");
        }

        var root = Path.GetFullPath(sourceDirectory);
        if (options.MaxDegreeOfParallelism == 1)
        {
            return await ReadSequentiallyAsync(root, options.Progress, cancellationToken).ConfigureAwait(false);
        }

        var result = new FhmSave();
        var pendingPaths = new List<string>(options.MaxDegreeOfParallelism);
        foreach (var filePath in EnumerateFiles(root))
        {
            pendingPaths.Add(filePath);
            if (pendingPaths.Count == options.MaxDegreeOfParallelism)
            {
                await ReadAndAddBatchAsync(root, result, pendingPaths, options.Progress, cancellationToken)
                    .ConfigureAwait(false);
                pendingPaths.Clear();
            }
        }

        if (pendingPaths.Count > 0)
        {
            await ReadAndAddBatchAsync(root, result, pendingPaths, options.Progress, cancellationToken)
                .ConfigureAwait(false);
        }

        return result;
    }

    private static async Task<FhmSave> ReadSequentiallyAsync(
        string root,
        IProgress<FhmSaveReadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var result = new FhmSave();
        foreach (var filePath in EnumerateFiles(root))
        {
            var loadedFile = await ReadFileAsync(root, filePath, progress, cancellationToken).ConfigureAwait(false);
            progress?.Report(new FhmSaveReadProgress("Decoding", loadedFile.RelativePath));
            AddFile(result, loadedFile);
            progress?.Report(new FhmSaveReadProgress("Decoded", loadedFile.RelativePath));
        }

        return result;
    }

    private static async Task ReadAndAddBatchAsync(
        string root,
        FhmSave save,
        IReadOnlyList<string> filePaths,
        IProgress<FhmSaveReadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var loadedFiles = await Task.WhenAll(
            filePaths.Select(filePath => ReadFileAsync(root, filePath, progress, cancellationToken)))
            .ConfigureAwait(false);
        foreach (var loadedFile in loadedFiles)
        {
            progress?.Report(new FhmSaveReadProgress("Decoding", loadedFile.RelativePath));
            AddFile(save, loadedFile);
            progress?.Report(new FhmSaveReadProgress("Decoded", loadedFile.RelativePath));
        }
    }

    private static async Task<LoadedFile> ReadFileAsync(
        string root,
        string filePath,
        IProgress<FhmSaveReadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var relativePath = FhmSavePath.NormalizeRelativePath(Path.GetRelativePath(root, filePath));
        progress?.Report(new FhmSaveReadProgress("Reading", relativePath));
        await using var content = OpenRead(filePath, asynchronous: true);
        progress?.Report(new FhmSaveReadProgress("Read", relativePath));
        var documented = FhmSaveFileFactory.TryRead(relativePath, content);
        byte[]? opaqueContent = null;
        if (documented is null)
        {
            using var opaqueStream = new MemoryStream();
            await content.CopyToAsync(opaqueStream, cancellationToken).ConfigureAwait(false);
            opaqueContent = opaqueStream.ToArray();
        }

        return new LoadedFile(relativePath, documented, opaqueContent);
    }

    private static void AddFile(FhmSave save, string relativePath, Stream content)
    {
        var documented = FhmSaveFileFactory.TryRead(relativePath, content);
        if (documented is null)
        {
            using var opaqueStream = new MemoryStream();
            content.CopyTo(opaqueStream);
            save.OpaqueFiles.Add(new FhmOpaqueFile(relativePath, opaqueStream.ToArray()));
        }
        else if (!save.Files.TryAdd(documented.RelativePath, documented))
        {
            throw new FhmFormatException($"Duplicate documented FHM file '{documented.RelativePath}'.");
        }
    }

    private static void AddFile(FhmSave save, LoadedFile loadedFile)
    {
        if (loadedFile.Documented is { } documented)
        {
            if (!save.Files.TryAdd(documented.RelativePath, documented))
            {
                throw new FhmFormatException($"Duplicate documented FHM file '{documented.RelativePath}'.");
            }
        }
        else
        {
            save.OpaqueFiles.Add(new FhmOpaqueFile(
                loadedFile.RelativePath,
                loadedFile.OpaqueContent ?? throw new InvalidOperationException("Opaque file content was not loaded.")));
        }
    }

    internal static IEnumerable<string> EnumerateFiles(string root)
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
                    if (IsIgnoredTopLevelDirectory(root, entry))
                    {
                        continue;
                    }

                    pendingDirectories.Enqueue(entry);
                }
                else
                {
                    yield return entry;
                }
            }
        }
    }

    private static bool IsIgnoredTopLevelDirectory(string root, string directoryPath) =>
        string.Equals(
            Path.GetFullPath(Path.GetDirectoryName(directoryPath)!),
            root,
            StringComparison.OrdinalIgnoreCase) &&
        FhmSaveAuxiliaryPaths.IsIgnored(Path.GetFileName(directoryPath));

    private static FileStream OpenRead(string filePath, bool asynchronous) => new(
        filePath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        bufferSize: 1024 * 1024,
        asynchronous ? FileOptions.Asynchronous | FileOptions.SequentialScan : FileOptions.SequentialScan);

    private sealed record LoadedFile(string RelativePath, IFhmSaveFile? Documented, byte[]? OpaqueContent);
}
