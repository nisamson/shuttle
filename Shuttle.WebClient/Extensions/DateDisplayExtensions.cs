using System.Globalization;

namespace Shuttle.WebClient.Extensions;

/// <summary>
/// Centralizes the app's user-facing date/time formatting so the same wording isn't duplicated
/// across pages and components. Every method formats with <see cref="CultureInfo.InvariantCulture"/>
/// on purpose: output must be identical for every visitor and independent of the runtime culture
/// (the app runs with <c>InvariantGlobalization</c>, and a standalone WASM app would otherwise pick
/// up the browser's culture), so these patterns render the same everywhere.
/// </summary>
public static class DateDisplayExtensions {
    private const string ShortDatePattern = "yyyy-MM-dd";
    private const string LongDatePattern = "MMMM d, yyyy";

    // Short date + 12-hour clock, e.g. "7/27/2026 2:54 AM" — matches the previous "g" (general
    // short) rendering under en-US, but pinned so it stays 12-hour under invariant globalization.
    private const string ShortDateTimePattern = "M/d/yyyy h:mm tt";

    /// <summary>Formats a calendar date as an ISO-style <c>yyyy-MM-dd</c>, e.g. <c>2026-07-27</c>.</summary>
    public static string ToShortDate(this DateTime date) =>
        date.ToString(ShortDatePattern, CultureInfo.InvariantCulture);

    /// <summary>Formats a calendar date as an ISO-style <c>yyyy-MM-dd</c>, e.g. <c>2026-07-27</c>.</summary>
    public static string ToShortDate(this DateOnly date) =>
        date.ToString(ShortDatePattern, CultureInfo.InvariantCulture);

    /// <summary>Formats a date in a long, human-friendly form, e.g. <c>July 27, 2026</c>.</summary>
    public static string ToLongDate(this DateOnly date) =>
        date.ToString(LongDatePattern, CultureInfo.InvariantCulture);

    /// <summary>Formats a date in a long, human-friendly form, e.g. <c>July 27, 2026</c>.</summary>
    public static string ToLongDate(this DateTime date) =>
        date.ToString(LongDatePattern, CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a timestamp in the viewer's local time zone as a short date and 12-hour clock,
    /// e.g. <c>7/27/2026 2:54 AM</c>.
    /// </summary>
    public static string ToShortDateTime(this DateTimeOffset timestamp) =>
        timestamp.LocalDateTime.ToString(ShortDateTimePattern, CultureInfo.InvariantCulture);
}
