using System.Diagnostics.CodeAnalysis;
using Shuttle.Shl.Api.Models.Common;

namespace Shuttle.Shl.Api.Models.Index.V1;

public record GameSlug(
    byte Season,
    KnownLeague League,
    GameType GameType,
    byte Day,
    byte Month,
    byte HomeTeam,
    byte AwayTeam
    ) : ISpanParsable<GameSlug> {
    
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
}
