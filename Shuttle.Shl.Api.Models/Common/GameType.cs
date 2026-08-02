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
            if (TryFromString(value, out var result)) {
                return result;
            }

            throw new ArgumentOutOfRangeException(nameof(value), value, null);
        }

        public static bool TryFromString(string? value, out GameType result) {
            switch (value?.ToLowerInvariant()) {
                case "pre-season":
                    result = GameType.PreSeason;
                    return true;
                case "regular season":
                    result = GameType.RegularSeason;
                    return true;
                case "playoffs":
                    result = GameType.Playoffs;
                    return true;
                default:
                    result = default;
                    return false;
            }
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