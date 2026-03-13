using System.Text.Json.Serialization;

namespace Shuttle.Models.Games;

[JsonConverter(typeof(JsonStringEnumConverter<GameEnding>))]
public enum GameEnding {
    Regulation,
    Overtime,
    Shootout
}
