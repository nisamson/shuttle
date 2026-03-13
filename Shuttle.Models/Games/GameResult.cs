using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using SHLAnalytics.Api.Models;
using SHLAnalytics.Api.Models.Common;

namespace Shuttle.Models.Games;

public record GameResult(
    int GameId,
    int Season,
    KnownLeague League,
    DateOnly SimDate,
    int HomeTeam,
    int AwayTeam,
    int HomeScore,
    int AwayScore,
    GameType GameType,
    GameEnding? Ending,
    DateOnly? DatePlayed = null
) {
    public string ToString(string homeTeamName, string awayTeamName) =>
        $"{awayTeamName} @ {homeTeamName} on {SimDate}: {AwayScore}-{HomeScore} {(Ending?.ToShortString())}";

    [JsonIgnore]
    [MemberNotNullWhen(true, nameof(Ending))]
    public bool Played => Ending is not null;
    
    
}

