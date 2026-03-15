using System.Diagnostics;

namespace Shuttle.Core;

public class Once {
    public bool HasRun { get; private set; }
    public bool ThrowIfAlreadyRun { get; }

    private StackTrace? firstCallStack;
    
    public Once(bool throwIfAlreadyRun = false) {
        ThrowIfAlreadyRun = throwIfAlreadyRun;
    }

    public void Ensure() {
        Run();
    }

    public void Run(Action? action = null) {
        lock (this) {
            if (TryMarkAsRun()) {
                action?.Invoke();
            }
        }
    }

    /// <summary>
    /// Attempts to mark this instance as having run.
    /// </summary>
    /// <returns>True if successfully marked as run (first call); false if already run.</returns>
    private bool TryMarkAsRun() {
        if (HasRun) {
            if (ThrowIfAlreadyRun) {
                throw new InvalidOperationException($"This code has already been run. First call stack: {firstCallStack}");
            }
            return false;
        }
        HasRun = true;
        firstCallStack = new(2, true);
        return true;
    }
}
