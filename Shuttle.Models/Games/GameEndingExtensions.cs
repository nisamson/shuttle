namespace Shuttle.Models.Games;

public static class GameEndingExtensions {
    extension(GameEnding ending) {
        public string ToFriendlyString() => ending switch {
            GameEnding.Regulation => "Regulation",
            GameEnding.Overtime => "Overtime",
            GameEnding.Shootout => "Shootout",
            _ => throw new ArgumentOutOfRangeException(nameof(ending), $"Unexpected value: {ending}")
        };
        
        public bool IsRegulation => ending == GameEnding.Regulation;
        public static GameEnding Parse(string value) => value.ToLower() switch {
            "regulation" or "" => GameEnding.Regulation,
            "overtime" or "ot" => GameEnding.Overtime,
            "shootout" or "so" => GameEnding.Shootout,
            _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unexpected value: {value}")
        };
        
        public static GameEnding? FromGameResult(SHLAnalytics.Api.Models.Index.V1.GameResult result) {
            if (!result.Played) {
                return null;
            }

            if (result.Overtime) {
                return GameEnding.Overtime;
            }
            
            if (result.Shootout) {
                return GameEnding.Shootout;
            }
            
            return GameEnding.Regulation;
        }
        
        public string ToShortString() => ending switch {
            GameEnding.Regulation => "",
            GameEnding.Overtime => "OT",
            GameEnding.Shootout => "SO",
            _ => throw new ArgumentOutOfRangeException(nameof(ending), $"Unexpected value: {ending}")
        };
    }
}

