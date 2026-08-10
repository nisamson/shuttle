namespace Shuttle.Fhm.SaveData.SaveFolder;

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
