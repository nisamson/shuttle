namespace Shuttle.Fhm.Serde.Domain.SaveFolder;

/// <summary>Identifies FHM save-folder paths that are not part of the save-data editing boundary.</summary>
public static class FhmSaveAuxiliaryPaths
{
    /// <summary>Gets whether a normalized relative path belongs to an ignored auxiliary directory.</summary>
    public static bool IsIgnored(string normalizedRelativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedRelativePath);
        var separatorIndex = normalizedRelativePath.IndexOf('/');
        var topLevelDirectory = separatorIndex < 0
            ? normalizedRelativePath
            : normalizedRelativePath[..separatorIndex];

        return topLevelDirectory.Equals("graphics", StringComparison.OrdinalIgnoreCase) ||
            topLevelDirectory.Equals("import_export", StringComparison.OrdinalIgnoreCase) ||
            topLevelDirectory.StartsWith("rs", StringComparison.OrdinalIgnoreCase);
    }
}

internal static class FhmSavePath
{
    internal static string NormalizeRelativePath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        var normalized = relativePath.Replace('\\', '/');
        if (Path.IsPathRooted(normalized) || normalized.Split('/').Any(static part => part is "." or ".." or ""))
        {
            throw new ArgumentException($"'{relativePath}' is not a safe relative save-file path.", nameof(relativePath));
        }

        return normalized;
    }

    internal static string Resolve(string root, string normalizedRelativePath)
    {
        var fullRoot = Path.GetFullPath(root);
        var destination = Path.GetFullPath(Path.Combine(fullRoot, normalizedRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        var rootWithSeparator = fullRoot.EndsWith(Path.DirectorySeparatorChar) ? fullRoot : fullRoot + Path.DirectorySeparatorChar;
        if (!destination.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"'{normalizedRelativePath}' escapes the save root.", nameof(normalizedRelativePath));
        }

        return destination;
    }
}
