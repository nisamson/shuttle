using System.Threading.Tasks.Dataflow;

namespace SHLAnalytics.Shuttle.Api.Utilites;

public static class DataflowHelpers {
    public static async Task FeedAll<T>(this IAsyncEnumerable<T> source, ITargetBlock<T> target, CancellationToken token = default) {
        await foreach (var item in source.WithCancellation(token)) {
            await target.SendAsync(item, token);
        }
        target.Complete();
    }
}
