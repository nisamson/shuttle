using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shuttle.WebClient.Models;
using Shuttle.WebClient.Models.Options;

namespace Shuttle.WebClient.Services;

public class ShuttleOptionsLocalStorage : IShuttleOptionsStorage {
    private readonly ILogger<ShuttleOptionsLocalStorage> logger;
    private readonly SemaphoreSlim optionsLock = new(1, 1);
    private const string StorageKey = "shuttle-options";
    private readonly ILocalStorageService localStorage;
    private bool firstLoad = true;

    public ShuttleOptions CurrentOptions {
        get;
        private set {
            if (field == value) {
                return;
            }
            field = value;
            OptionsChanged?.Invoke(value);
        }
    } = ShuttleOptions.Default;

    public event Action<ShuttleOptions>? OptionsChanged;

    public ShuttleOptionsLocalStorage(ILocalStorageService localStorage, ILogger<ShuttleOptionsLocalStorage> logger) {
        this.localStorage = localStorage;
        this.logger = logger;
    }

    public async Task<ShuttleOptions> LoadOptions(bool forceLoad, CancellationToken token = default) {
        if (!firstLoad && !forceLoad) {
            return CurrentOptions;
        }
        var options = ShuttleOptions.Default;
        try {
            await DoWithLock(
                () => {
                    options = GetOptionsInternal();
                    return Task.CompletedTask;
                },
                token
            );
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to load options from local storage");
        }
        return options;
    }

    private ShuttleOptions GetOptionsInternal() {
        var options = localStorage.GetItem<ShuttleOptions>(StorageKey);
        if (options is null) {
            logger.LogDebug("No options found in local storage, using defaults");
            options = ShuttleOptions.Default;
        } else {
            logger.LogDebug("Loaded options from local storage: {Options}", options);
        }
        CurrentOptions = options;
        return options;
    }

    public async Task SaveOptions(ShuttleOptions options, CancellationToken token = default) {
        await DoWithLock(
            () => {
                SaveOptionsInternal(options);
                return Task.CompletedTask;
            },
            token
        );
    }

    private void SaveOptionsInternal(ShuttleOptions options) {
        logger.LogDebug("Saving options {Options}", options);
        localStorage.SetItem(StorageKey, options);
        CurrentOptions = options;
    }

    private async Task DoWithLock(Func<Task> action, CancellationToken token = default) {
        logger.LogTrace("Waiting for options lock...");
        await optionsLock.WaitAsync(token);
        try {
            logger.LogTrace("Acquired options lock");
            await action();
        } finally {
            logger.LogTrace("Releasing options lock");
            optionsLock.Release();
        }
    }
}
