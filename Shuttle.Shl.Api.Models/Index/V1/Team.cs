namespace Shuttle.Shl.Api.Models.Index.V1;

public record Team(
    int Id,
    int Season,
    int League,
    int Conference,
    int? Division,
    string Name,
    string Abbreviation,
    string Location,
    TeamColors Colors,
    NameDetails NameDetails,
    TeamSeasonStats Stats
);

public record NameDetails(string First, string Second);

public record TeamSeasonStats(
    int Wins,
    int Losses,
    int OvertimeLosses,
    int ShootoutWins,
    int ShootoutLosses,
    int Points,
    int GoalsFor,
    int GoalsAgainst,
    float WinPercent
);
