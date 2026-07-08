namespace Shuttle.Shl.Api.Models.Common;

public enum GameType {
    PreSeason = 1,
    RegularSeason = 2,
    Playoffs = 3,
}

public static class GameTypeExtensions {
    extension(GameType gameType) {
        public string ToDisplayString() {
            return gameType switch {
                GameType.PreSeason => "Pre-Season",
                GameType.RegularSeason => "Regular Season",
                GameType.Playoffs => "Playoffs",
                _ => throw new ArgumentOutOfRangeException(nameof(gameType), gameType, null)
            };
        }
        
        public static GameType FromString(string value) {
            return value switch {
                "Pre-Season" => GameType.PreSeason,
                "Regular Season" => GameType.RegularSeason,
                "Playoffs" => GameType.Playoffs,
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
        }

        public static GameType FromId(int value) {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 3);
            return (GameType)value;
        }

        public string ToShortString() {
            return gameType switch {
                GameType.Playoffs => "po",
                GameType.RegularSeason => "rs",
                GameType.PreSeason => "ps",
                _ => throw new ArgumentOutOfRangeException(nameof(gameType), gameType, null)
            };
        }
    }
}