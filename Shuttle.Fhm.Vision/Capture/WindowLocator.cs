using System.Diagnostics;
using System.Runtime.Versioning;

namespace Shuttle.Fhm.Vision.Capture;

/// <summary>A candidate top-level window.</summary>
public sealed record WindowInfo(IntPtr Handle, uint ProcessId, string Title, string ProcessName);

/// <summary>Finds candidate FHM windows by enumeration, process id, or process name.</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowLocator {
    /// <summary>Enumerates visible top-level windows that have a title.</summary>
    public IReadOnlyList<WindowInfo> EnumerateWindows() {
        var results = new List<WindowInfo>();

        NativeMethods.EnumWindows((handle, lParam) => {
            if (!NativeMethods.IsWindowVisible(handle)) {
                return true;
            }

            var title = GetWindowTitle(handle);
            if (string.IsNullOrWhiteSpace(title)) {
                return true;
            }

            _ = NativeMethods.GetWindowThreadProcessId(handle, out var pid);
            results.Add(new WindowInfo(handle, pid, title, GetProcessName(pid)));
            return true;
        }, IntPtr.Zero);

        return results;
    }

    /// <summary>The best (largest-titled) visible window owned by the given process id, if any.</summary>
    public WindowInfo? FindByProcessId(int processId) =>
        EnumerateWindows()
            .Where(w => w.ProcessId == (uint)processId)
            .OrderByDescending(w => w.Title.Length)
            .FirstOrDefault();

    /// <summary>
    /// Visible windows whose process name contains <paramref name="processNameFragment"/>
    /// (case-insensitive), most-likely candidate first.
    /// </summary>
    public IReadOnlyList<WindowInfo> FindByProcessName(string processNameFragment) {
        ArgumentException.ThrowIfNullOrWhiteSpace(processNameFragment);
        return [.. EnumerateWindows()
            .Where(w => w.ProcessName.Contains(processNameFragment, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(w => w.Title.Length)];
    }

    private static string GetWindowTitle(IntPtr handle) {
        var length = NativeMethods.GetWindowTextLengthW(handle);
        if (length <= 0) {
            return string.Empty;
        }

        var buffer = new char[length + 1];
        var copied = NativeMethods.GetWindowTextW(handle, buffer, buffer.Length);
        return new string(buffer, 0, copied);
    }

    private static string GetProcessName(uint pid) {
        try {
            using var process = Process.GetProcessById((int)pid);
            return process.ProcessName;
        } catch (ArgumentException) {
            return string.Empty;
        } catch (InvalidOperationException) {
            return string.Empty;
        }
    }
}
