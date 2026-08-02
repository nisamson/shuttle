using System.Globalization;

namespace Shuttle.Analysis.Flows;

/// <summary>
/// Parses repeated <c>key=value</c> CLI tokens (from <c>--arg</c>) into a flow argument map.
/// </summary>
/// <remarks>
/// Keys are compared case-insensitively. Each token must contain a single <c>=</c> separating a
/// non-empty key from its value (the value may be empty and may itself contain <c>=</c>). Whitespace
/// around the key is trimmed. Duplicate keys are rejected so a typo cannot silently shadow a value.
/// </remarks>
public static class FlowArguments {

    /// <summary>An empty argument map.</summary>
    public static IReadOnlyDictionary<string, string> Empty { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Parses the given <c>key=value</c> tokens.
    /// </summary>
    /// <param name="tokens">The raw tokens, or <c>null</c> for none.</param>
    /// <returns>A case-insensitive map of argument names to values.</returns>
    /// <exception cref="FormatException">A token is malformed or a key is duplicated.</exception>
    public static IReadOnlyDictionary<string, string> Parse(IEnumerable<string>? tokens) {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (tokens is null) {
            return map;
        }

        foreach (var token in tokens) {
            if (string.IsNullOrWhiteSpace(token)) {
                continue;
            }

            var separator = token.IndexOf('=');
            if (separator < 0) {
                throw new FormatException($"Invalid argument '{token}'. Expected key=value.");
            }

            var key = token[..separator].Trim();
            if (key.Length == 0) {
                throw new FormatException($"Invalid argument '{token}'. The key must be non-empty.");
            }

            var value = token[(separator + 1)..];
            if (!map.TryAdd(key, value)) {
                throw new FormatException($"Duplicate argument '{key}'.");
            }
        }

        return map;
    }

    /// <summary>
    /// Reads a required integer argument, throwing a descriptive <see cref="ArgumentException"/> when
    /// it is missing or not a valid integer.
    /// </summary>
    public static int GetRequiredInt(IReadOnlyDictionary<string, string> arguments, string key) {
        ArgumentNullException.ThrowIfNull(arguments);

        if (!arguments.TryGetValue(key, out var raw)) {
            throw new ArgumentException($"Missing required argument '{key}'. Pass it via --arg {key}=<value>.");
        }

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) {
            throw new ArgumentException($"Argument '{key}' must be an integer, but was '{raw}'.");
        }

        return value;
    }

    /// <summary>
    /// Reads an optional integer argument, returning <paramref name="defaultValue"/> when absent and
    /// throwing when present but not a valid integer.
    /// </summary>
    public static int GetOptionalInt(IReadOnlyDictionary<string, string> arguments, string key, int defaultValue) {
        ArgumentNullException.ThrowIfNull(arguments);

        if (!arguments.TryGetValue(key, out var raw)) {
            return defaultValue;
        }

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) {
            throw new ArgumentException($"Argument '{key}' must be an integer, but was '{raw}'.");
        }

        return value;
    }
}
