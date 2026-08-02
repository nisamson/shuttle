namespace Shuttle.EloCalc.Sinks;

public interface IResultSink {
    public ValueTask StoreResults(int season, IList<TeamPlayerSeasonRankings> data);
}
