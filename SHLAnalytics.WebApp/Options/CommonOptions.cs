namespace SHLAnalytics.WebApp.Options;

public record CommonOptions {
    public const string SectionName = "Common";
    
    public string? FileStorageLocation { get; set; }
    
    public string GetFileStorageLocation() {
        return FileStorageLocation ?? Environment.CurrentDirectory;
    }
}
