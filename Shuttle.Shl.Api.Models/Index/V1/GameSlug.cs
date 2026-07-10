using System.Diagnostics.CodeAnalysis;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.Shl.Api.Models.Index.V1;

/// <summary>
/// Strongly-typed representation of the Index API's 12-digit game <c>slug</c>.
/// <para>
/// Layout (fixed width, zero-padded, left to right): <c>SS L T DDMM HH AA</c>
/// </para>
/// <list type="bullet">
///   <item><description><c>SS</c> (2 digits) — <see cref="Season"/>.</description></item>
///   <item><description><c>L</c> (1 digit) — <see cref="League"/> id (0 = SHL, 1 = SMJHL, 2 = IIHF, 3 = WJC).</description></item>
///   <item><description><c>T</c> (1 digit) — <see cref="GameType"/> (1 = Pre-Season, 2 = Regular Season, 3 = Playoffs).</description></item>
///   <item><description><c>DDMM</c> (4 digits) — the in-game date at time of play, <b>day first, then month</b>.</description></item>
///   <item><description><c>HH</c> (2 digits) — <see cref="HomeTeam"/> number.</description></item>
///   <item><description><c>AA</c> (2 digits) — <see cref="AwayTeam"/> number.</description></item>
/// </list>
/// <para>
/// <see cref="ToString()"/> renders the canonical slug string, round-tripping with <see cref="Parse(string?, IFormatProvider?)"/>.
/// </para>
/// </summary>
public record GameSlug(
    byte Season,
    KnownLeague League,
    GameType GameType,
    byte Day,
    byte Month,
    byte HomeTeam,
    byte AwayTeam
    ) : ISpanParsable<GameSlug>, ISpanFormattable {
    
    public const int SlugLength = 12;
    
    public static GameSlug Parse(string? s, IFormatProvider? provider = null) {
        return Parse(s.AsSpan(), provider);
    }
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out GameSlug result) {
        if (string.IsNullOrWhiteSpace(s)) {
            result = null;
            return false;
        }
        return TryParse(s.AsSpan(), provider, out result);
    }
    public static GameSlug Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(s.Length, SlugLength);
        
        var season = byte.Parse(s[..2]);
        var league = KnownLeague.FromId(byte.Parse(s[2..3]));
        var gameType = GameType.FromId(byte.Parse(s[3..4]));
        var day = byte.Parse(s[4..6]);
        var month = byte.Parse(s[6..8]);
        var homeTeam = byte.Parse(s[8..10]);
        var awayTeam = byte.Parse(s[10..12]);

        return new(season, league, gameType, day, month, homeTeam, awayTeam);
    }
    
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out GameSlug result) {
        try {
            result = Parse(s);
            return true;
        } catch (Exception ex) when (ex is ArgumentException or FormatException) {
            result = null;
            return false;
        }
    }

    /// <summary>Renders the canonical 12-digit slug string (see <see cref="GameSlug"/>).</summary>
    public override string ToString() {
        Span<char> buffer = stackalloc char[SlugLength];
        if (!TryFormat(buffer, out var written)) {
            throw new FormatException(
                $"Cannot format game slug (Season={Season}, League={(int)League}, GameType={(int)GameType}, " +
                $"Day={Day}, Month={Month}, HomeTeam={HomeTeam}, AwayTeam={AwayTeam}): one or more fields exceed the slug's fixed width.");
        }
        return new string(buffer[..written]);
    }

    /// <summary>The <paramref name="format"/> and <paramref name="formatProvider"/> are ignored; a slug has a single canonical form.</summary>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    public bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) {
        charsWritten = 0;
        if (destination.Length < SlugLength) {
            return false;
        }

        var leagueId = (int)League;
        var gameTypeId = (int)GameType;

        // Every fixed-width field must fit in its allotted digits, otherwise the slug is not
        // representable and callers should treat this instance as invalid.
        if (Season > 99 || Day > 99 || Month > 99 || HomeTeam > 99 || AwayTeam > 99
            || leagueId is < 0 or > 9 || gameTypeId is < 0 or > 9) {
            return false;
        }

        WritePair(destination, 0, Season);
        destination[2] = (char)('0' + leagueId);
        destination[3] = (char)('0' + gameTypeId);
        WritePair(destination, 4, Day);
        WritePair(destination, 6, Month);
        WritePair(destination, 8, HomeTeam);
        WritePair(destination, 10, AwayTeam);

        charsWritten = SlugLength;
        return true;

        static void WritePair(Span<char> destination, int offset, byte value) {
            destination[offset] = (char)('0' + value / 10);
            destination[offset + 1] = (char)('0' + value % 10);
        }
    }
}
