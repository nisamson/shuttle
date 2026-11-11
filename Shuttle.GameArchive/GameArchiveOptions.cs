namespace Shuttle.GameArchive;

public sealed class GameArchiveOptions {
    public const string SectionName = "Archive";
    public const long MaxGitFileSize = 100L * 1024 * 1024;

    public bool Enabled { get; set; }
    public string SourceUrl { get; set; } = "https://simulationhockey.com/games/";
    public string RemoteUrl { get; set; } = "https://github.com/shuttle-shl/shl-games-archive.git";
    public string GitUsername { get; set; } = "shuttle-bot";
    public string SecretName { get; set; } = "shl-games-archive-git-password";
    public string? VaultUri { get; set; }
}
