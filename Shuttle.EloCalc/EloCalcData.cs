using Shuttle.Shl.Api.Models.Index.V1;

namespace Shuttle.EloCalc;

public record EloCalcData(
    IDictionary<int, Team> Teams,
    IList<GameResult> GameResults
    );
