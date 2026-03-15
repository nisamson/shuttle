using System.Text.Json.Serialization;

namespace Shuttle.Shl.Api.Models.Index.V1;

public record GameResult(
    int GameId,
    int Season,
    int League,
    [property: JsonConverter(typeof(ShlDateConverter))]
    DateOnly Date,
    int HomeTeam,
    int AwayTeam,
    int HomeScore,
    int AwayScore,
    string GameType,
    [property: JsonConverter(typeof(IntBoolJsonConverter))]
    bool Played,
    [property: JsonConverter(typeof(IntBoolJsonConverter))]
    bool Overtime,
    [property: JsonConverter(typeof(IntBoolJsonConverter))]
    bool Shootout
) {
    public string ToString(string homeTeamName, string awayTeamName) =>
        $"{awayTeamName} @ {homeTeamName} on {Date}: {AwayScore}-{HomeScore} {(Overtime ? "OT" : Shootout ? "SO" : "")}";
}
