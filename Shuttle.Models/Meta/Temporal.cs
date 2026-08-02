namespace Shuttle.Models.Meta;

public record Temporal<T> {
    public required T Value { get; init; }
    public required DateTime Timestamp { get; init; }
}