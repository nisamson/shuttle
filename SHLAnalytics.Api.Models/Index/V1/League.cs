namespace SHLAnalytics.Api.Models.Index.V1;

public record League(int Id, string Name, string Abbreviation);

public record LeagueSeason(int Id, string Name, string Abbreviation, int Season) : League(Id, Name, Abbreviation);
