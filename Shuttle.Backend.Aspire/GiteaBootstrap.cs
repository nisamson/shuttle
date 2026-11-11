using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

internal static class GiteaBootstrap {
    private const string Username = "shuttle-bot";
    private const string Password = "testtest";
    private const string Repository = "shl-games-archive";

    public static async Task RunAsync() {
        var url = Environment.GetEnvironmentVariable("GITEA_URL")
            ?? throw new InvalidOperationException("GITEA_URL must be provided by the AppHost.");
        var container = Environment.GetEnvironmentVariable("GITEA_CONTAINER_NAME")
            ?? throw new InvalidOperationException("GITEA_CONTAINER_NAME must be provided by the AppHost.");

        using var client = new HttpClient {
            BaseAddress = new Uri(url.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(10),
        };

        for (var attempt = 0; attempt < 60; attempt++) {
            try {
                using var health = await client.GetAsync("api/healthz");
                if (health.IsSuccessStatusCode) {
                    break;
                }
            } catch (HttpRequestException) when (attempt < 59) {
                // The container may still be initializing its database.
            } catch (TaskCanceledException) when (attempt < 59) {
                // Retry a slow startup.
            }

            if (attempt == 59) {
                throw new InvalidOperationException("Gitea did not become healthy.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        using (var user = await client.GetAsync($"api/v1/users/{Username}")) {
            if (user.StatusCode == HttpStatusCode.NotFound) {
                await CreateUserAsync(container);
            } else {
                user.EnsureSuccessStatusCode();
            }
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}")));

        using (var repo = await client.GetAsync($"api/v1/repos/{Username}/{Repository}")) {
            if (repo.StatusCode == HttpStatusCode.NotFound) {
                using var created = await client.PostAsJsonAsync("api/v1/user/repos", new {
                    name = Repository,
                    auto_init = true,
                    default_branch = "main",
                    readme = "Default",
                });
                created.EnsureSuccessStatusCode();
            } else {
                repo.EnsureSuccessStatusCode();
            }
        }

        using var branch = await client.GetAsync($"api/v1/repos/{Username}/{Repository}/branches/main");
        branch.EnsureSuccessStatusCode();
        using var readme = await client.GetAsync($"api/v1/repos/{Username}/{Repository}/contents/README.md?ref=main");
        readme.EnsureSuccessStatusCode();
        Console.WriteLine("Local games archive Gitea user and main/README.md are ready.");
    }

    private static async Task CreateUserAsync(string container) {
        var start = new ProcessStartInfo("docker") {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in new[] {
            "exec", "--user", "git", container, "gitea", "admin", "user", "create",
            "--username", Username, "--password", Password,
            "--email", "shuttle-bot@localhost.invalid", "--must-change-password=false",
        }) {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Could not start Docker to provision the local Gitea user.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await Task.WhenAll(standardOutput, standardError);
        if (process.ExitCode != 0) {
            throw new InvalidOperationException($"Gitea admin user creation failed (Docker exit {process.ExitCode}).");
        }
    }
}
