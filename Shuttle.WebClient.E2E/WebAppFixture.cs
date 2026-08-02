using System.Diagnostics;
using System.Net.Http;

namespace Shuttle.WebClient.E2E;

/// <summary>
/// Boots the WebClient in its offline "Testing" run mode (fake backend + fake auth, no Azure)
/// and exposes the base URL for Playwright-driven browser tests.
/// </summary>
/// <remarks>
/// By default the fixture launches <c>dotnet run --launch-profile TestServer</c> on the
/// <c>Shuttle.WebClient</c> project (fixed at <see cref="DefaultBaseUrl"/>). To run the tests
/// against an already-running server (e.g. in CI where the app is started separately), set the
/// <c>SHUTTLE_E2E_BASEURL</c> environment variable and no process is started.
/// </remarks>
public sealed class WebAppFixture : IAsyncLifetime
{
    public const string DefaultBaseUrl = "http://localhost:5099";

    private Process? appProcess;

    public string BaseUrl { get; private set; } = DefaultBaseUrl;

    /// <summary>True when the fixture owns (started) the app process.</summary>
    public bool StartedApp { get; private set; }

    public async ValueTask InitializeAsync()
    {
        var envUrl = Environment.GetEnvironmentVariable("SHUTTLE_E2E_BASEURL");
        if (!string.IsNullOrWhiteSpace(envUrl))
        {
            BaseUrl = envUrl.TrimEnd('/');
            await WaitForReadyAsync(TimeSpan.FromMinutes(2));
            return;
        }

        var webClientProject = LocateWebClientProject();
        appProcess = StartApp(webClientProject);
        StartedApp = true;

        try
        {
            await WaitForReadyAsync(TimeSpan.FromMinutes(3));
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (appProcess is { HasExited: false })
        {
            try
            {
                // Kill the whole tree: `dotnet run` spawns the app as a child process.
                appProcess.Kill(entireProcessTree: true);
                await appProcess.WaitForExitAsync();
            }
            catch
            {
                // best effort
            }
        }

        appProcess?.Dispose();
        appProcess = null;
    }

    private static Process StartApp(string webClientProjectPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(webClientProjectPath)!,
        };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add(webClientProjectPath);
        psi.ArgumentList.Add("--launch-profile");
        psi.ArgumentList.Add("TestServer");
        // Bake the "Testing" environment into the WASM boot manifest so the app activates the fake
        // backend deterministically (the dev server does not emit a Blazor-Environment header).
        psi.ArgumentList.Add("-p:ShuttleFakeBackend=true");

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        // Drain output so the child process never blocks on a full pipe buffer.
        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, _) => { };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private async Task WaitForReadyAsync(TimeSpan timeout)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (appProcess is { HasExited: true })
            {
                throw new InvalidOperationException(
                    $"WebClient process exited early with code {appProcess.ExitCode} before becoming ready.");
            }

            try
            {
                var response = await http.GetAsync(BaseUrl + "/");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // server not up yet
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException($"WebClient at {BaseUrl} was not ready within {timeout}.");
    }

    private static string LocateWebClientProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "Shuttle.WebClient", "Shuttle.WebClient.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            "Could not locate Shuttle.WebClient.csproj by walking up from the test output directory. " +
            "Set SHUTTLE_E2E_BASEURL to point the E2E tests at an already-running server instead.");
    }
}

[CollectionDefinition(Name)]
public sealed class WebAppCollection : ICollectionFixture<WebAppFixture>
{
    public const string Name = "WebApp";
}
