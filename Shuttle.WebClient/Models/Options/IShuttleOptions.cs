namespace Shuttle.WebClient.Models.Options;

public interface IShuttleOptions {
    public bool DarkMode { get; }
    
    public static bool Equals(IShuttleOptions? dis, IShuttleOptions? other) {
        if (ReferenceEquals(dis, other)) {
            return true;
        }
        if (dis is null || other is null) {
            return false;
        }
        return dis.DarkMode == other.DarkMode;
    }
}