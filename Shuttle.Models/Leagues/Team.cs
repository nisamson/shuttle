using SHLAnalytics.Api.Models.Common;
using SHLAnalytics.Api.Models.Index.V1;

namespace Shuttle.Models.Leagues;

public record Team {
    public required int Id { get; init; } 
    public required int Season { get; init; } 
    public required KnownLeague League { get; init; } 
    public required int Conference { get; init; } 
    public int? Division { get; init; } 
    public required string Name { get; init; } 
    public required string Abbreviation { get; init; } 
    public required string Location { get; init; } 
    public required TeamColors Colors { get; init; } 
}
