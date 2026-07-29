using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;
using Shuttle.Fhm.Vision.Recognition;

namespace Shuttle.Fhm.Vision.Training;

/// <summary>Runs the <see cref="DigitTrainerForm"/> on a dedicated STA thread (required by WinForms).</summary>
[SupportedOSPlatform("windows")]
public static class DigitTrainerLauncher {
    /// <summary>
    /// Opens the digit trainer over the given <paramref name="initialImages"/>, appending to
    /// <paramref name="set"/> and saving to <paramref name="templatesFile"/>. The
    /// <paramref name="processImage"/> delegate segments a single screenshot into pending glyphs; the
    /// form invokes it lazily (one file at a time) as its buffer drains, so large image sets stay
    /// responsive. Returns the number of templates in the set as of the last save.
    /// </summary>
    public static int Run(
        IReadOnlyList<FileInfo> initialImages,
        DigitTemplateSet set,
        FileInfo templatesFile,
        Func<FileInfo, IReadOnlyList<PendingGlyph>> processImage
    ) {
        ArgumentNullException.ThrowIfNull(initialImages);
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(templatesFile);
        ArgumentNullException.ThrowIfNull(processImage);

        var savedCount = set.Templates.Count;
        var thread = new Thread(() => {
            ApplicationConfiguration.Initialize();
            using var form = new DigitTrainerForm(initialImages, set, templatesFile, processImage);
            form.ShowDialog();
            savedCount = form.SavedTemplateCount;
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return savedCount;
    }
}
