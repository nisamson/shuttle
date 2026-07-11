using dotenv.net;

namespace Shuttle.EFCore;

/// <summary>
/// Shared helpers for loading the Shuttle database configuration from the well-known
/// <c>Shuttle.EFCore/.env</c> file that the EF Core design-time factory also uses.
/// </summary>
public static class ShuttleEnvironment {

    /// <summary>
    /// Loads database configuration from the shared <c>Shuttle.EFCore/.env</c> file, regardless of
    /// the current working directory, then any real environment variables. Existing environment
    /// variables always win, so machine/user-level configuration still overrides the .env file.
    /// </summary>
    public static void LoadDotEnv() {
        // Walk up from the executable location and the current directory looking for the
        // solution root, then load the well-known .env alongside Shuttle.EFCore.
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() }) {
            var dir = new DirectoryInfo(start);
            while (dir is not null) {
                var envPath = Path.Combine(dir.FullName, ShuttleEfCoreConstants.RootNamespace, ".env");
                if (File.Exists(envPath)) {
                    DotEnv.Load(new DotEnvOptions(envFilePaths: [envPath], overwriteExistingVars: false));
                    return;
                }

                dir = dir.Parent;
            }
        }

        // Fall back to the default behaviour (a .env in the current directory, if any).
        DotEnv.Load(new DotEnvOptions(overwriteExistingVars: false));
    }
}
