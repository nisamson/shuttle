using SHLAnalytics.Api.Models.Index.V1;

namespace SHLAnalytics.EloCalc;

public record EloCalcData(
    IDictionary<int, Team> Teams,
    IList<GameResult> GameResults
    );
