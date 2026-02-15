namespace SHLAnalytics.WebApp.Helpers;

public class AsyncDisposableAction {
    private Func<Task>? action;
    
    public AsyncDisposableAction(Func<Task> action) {
        this.action = action;
    }
    
    public async ValueTask DisposeAsync() {
        var act = Interlocked.CompareExchange(ref this.action, null, this.action);
        if (act != null) {
            await act();
        }
    }
}

public static class AsyncDisposableActionExtensions {
    public static async Task<AsyncDisposableAction> WaitDisposable(
        this SemaphoreSlim semaphore,
        CancellationToken token = default
    ) {
        await semaphore.WaitAsync(token);
        return new(() => {
            semaphore.Release();
            return Task.CompletedTask;
        });
    }
}