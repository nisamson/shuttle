using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;
using Shuttle.Fhm.Vision.Recognition;

namespace Shuttle.Fhm.Vision.Training;

/// <summary>Runs the <see cref="DigitTrainerForm"/> on a dedicated STA thread (required by WinForms).</summary>
[SupportedOSPlatform("windows")]
public static class DigitTrainerLauncher {
    /// <summary>
    /// Opens the digit trainer over the given <paramref name="pending"/> glyph queue, appending to
    /// <paramref name="set"/> and saving to <paramref name="templatesFile"/>. The optional
    /// <paramref name="addImages"/> delegate lets the form pull in and segment more screenshots at
    /// runtime. Returns the number of templates in the set as of the last save.
    /// </summary>
    public static int Run(
        IReadOnlyList<PendingGlyph> pending,
        DigitTemplateSet set,
        FileInfo templatesFile,
        Func<IReadOnlyList<FileInfo>, IReadOnlyList<PendingGlyph>>? addImages
    ) {
        ArgumentNullException.ThrowIfNull(pending);
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(templatesFile);

        var savedCount = set.Templates.Count;
        var thread = new Thread(() => {
            ApplicationConfiguration.Initialize();
            using var form = new DigitTrainerForm(pending, set, templatesFile, addImages);
            form.ShowDialog();
            savedCount = form.SavedTemplateCount;
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return savedCount;
    }
}
