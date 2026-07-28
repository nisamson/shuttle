using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;
using Shuttle.Fhm.Vision.Layout;

namespace Shuttle.Fhm.Vision.Calibration;

/// <summary>Runs the <see cref="CalibrationForm"/> on a dedicated STA thread (required by WinForms).</summary>
[SupportedOSPlatform("windows")]
public static class CalibrationLauncher {
    /// <summary>
    /// Opens the calibration editor for <paramref name="screenshot"/>, optionally seeded with an
    /// existing profile. The form saves directly to <paramref name="profileFile"/>; the returned
    /// profile is the last saved value (or <c>null</c> if the user saved nothing).
    /// </summary>
    public static LayoutProfile? Run(Bitmap screenshot, LayoutProfile? existing, FileInfo profileFile) {
        ArgumentNullException.ThrowIfNull(screenshot);
        ArgumentNullException.ThrowIfNull(profileFile);

        LayoutProfile? result = null;
        var thread = new Thread(() => {
            ApplicationConfiguration.Initialize();
            using var form = new CalibrationForm(screenshot, existing, profileFile);
            form.ShowDialog();
            result = form.Result;
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return result;
    }
}
