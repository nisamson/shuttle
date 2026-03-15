namespace Shuttle.Shl.Api.Models.Index.V1;

public record Team(int Id, int Season, int League, int Conference, int? Division, string Name, string Abbreviation, string Location, TeamColors Colors);
