using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shuttle.Tests.Api;

internal sealed class GameArchiveGitHttpServer : IAsyncDisposable {
    private readonly WebApplication application;
    private readonly string directory;

    private GameArchiveGitHttpServer(string directory) {
        this.directory = directory;
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        application = builder.Build();
        application.Run(ServeAsync);
    }

    public string RemoteUrl => application.Urls.Single() + "/remote.git";

    public static async Task<GameArchiveGitHttpServer> StartAsync(string directory, CancellationToken cancellationToken) {
        var server = new GameArchiveGitHttpServer(directory);
        try {
            await server.application.StartAsync(cancellationToken);
            return server;
        } catch {
            await server.DisposeAsync();
            throw;
        }
    }

    public ValueTask DisposeAsync() => application.DisposeAsync();

    private async Task ServeAsync(HttpContext context) {
        var cancellationToken = context.RequestAborted;
        var start = new ProcessStartInfo("git") {
            WorkingDirectory = directory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        start.ArgumentList.Add("http-backend");
        start.Environment["GIT_PROJECT_ROOT"] = directory;
        start.Environment["GIT_HTTP_EXPORT_ALL"] = "1";
        start.Environment["GIT_CONFIG_GLOBAL"] = Path.Combine(directory, "empty-gitconfig");
        start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        start.Environment["REQUEST_METHOD"] = context.Request.Method;
        start.Environment["PATH_INFO"] = context.Request.Path.Value ?? "";
        start.Environment["QUERY_STRING"] = context.Request.QueryString.Value?.TrimStart('?') ?? "";
        start.Environment["CONTENT_TYPE"] = context.Request.ContentType ?? "";
        start.Environment["CONTENT_LENGTH"] = context.Request.ContentLength?.ToString(
            System.Globalization.CultureInfo.InvariantCulture) ?? "";
        start.Environment["SERVER_PROTOCOL"] = context.Request.Protocol;
        start.Environment["REMOTE_ADDR"] = "127.0.0.1";
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start Git HTTP backend.");
        var error = process.StandardError.ReadToEndAsync(cancellationToken);
        var request = WriteRequestAsync(context.Request.Body, process, cancellationToken);
        try {
            var headers = await ReadHeadersAsync(process.StandardOutput.BaseStream, cancellationToken);
            foreach (var header in headers.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)) {
                var separator = header.IndexOf(':');
                if (separator <= 0)
                    throw new InvalidDataException("Invalid Git CGI response header.");
                var name = header[..separator];
                var value = header[(separator + 1)..].Trim();
                if (name.Equals("Status", StringComparison.OrdinalIgnoreCase)) {
                    context.Response.StatusCode = int.Parse(value.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);
                } else {
                    context.Response.Headers.Append(name, value);
                }
            }
            await process.StandardOutput.BaseStream.CopyToAsync(context.Response.Body, cancellationToken);
            await request;
            await process.WaitForExitAsync(cancellationToken);
            var stderr = await error;
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Git HTTP backend failed: {stderr}");
        } finally {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
    }

    private static async Task WriteRequestAsync(Stream body, Process process, CancellationToken cancellationToken) {
        await using var input = process.StandardInput.BaseStream;
        await body.CopyToAsync(input, cancellationToken);
    }

    private static async Task<string> ReadHeadersAsync(Stream output, CancellationToken cancellationToken) {
        using var headers = new MemoryStream();
        var next = new byte[1];
        while (headers.Length < 16 * 1024) {
            if (await output.ReadAsync(next, cancellationToken) == 0)
                throw new InvalidDataException("Git HTTP backend ended before sending response headers.");
            headers.WriteByte(next[0]);
            var buffer = headers.GetBuffer();
            var length = (int)headers.Length;
            if (length >= 4 && buffer[length - 4] == '\r' && buffer[length - 3] == '\n'
                && buffer[length - 2] == '\r' && buffer[length - 1] == '\n')
                return Encoding.ASCII.GetString(buffer, 0, length - 4);
        }
        throw new InvalidDataException("Git HTTP backend response headers are too large.");
    }
}
