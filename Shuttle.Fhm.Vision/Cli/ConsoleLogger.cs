using Microsoft.Extensions.Logging;

namespace Shuttle.Fhm.Vision.Cli;

/// <summary>A minimal <see cref="ILogger{T}"/> that writes single-line, timestamped output to the console.</summary>
public sealed class ConsoleLogger<T> : ILogger<T> {
    private static readonly Lock Gate = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    ) {
        ArgumentNullException.ThrowIfNull(formatter);
        if (!IsEnabled(logLevel)) {
            return;
        }

        var message = formatter(state, exception);
        lock (Gate) {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} [{logLevel}] {message}");
            if (exception is not null) {
                Console.WriteLine(exception);
            }
        }
    }
}
