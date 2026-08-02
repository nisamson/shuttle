using System.Reflection;

namespace Shuttle.Tests.Utilites;

public static class EmbeddedResourceHelper {
    public static string LoadEmbeddedResourceContents(Type type, string fileName) {
        var assembly = Assembly.GetAssembly(type) 
            ?? throw new InvalidOperationException($"Could not get assembly for type: {type.FullName}");
        var resourceName = $"{type.Namespace}.{fileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName) 
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

