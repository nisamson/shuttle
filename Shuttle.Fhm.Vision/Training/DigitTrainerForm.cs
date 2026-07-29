using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;
using Shuttle.Fhm.Vision.Recognition;

namespace Shuttle.Fhm.Vision.Training;

/// <summary>
/// Interactive WinForms trainer for the FHM rating font. Walks a queue of segmented glyphs, showing
/// each glyph's original crop and its normalized (matched) bitmap alongside the current model's
/// best-guess confidence metrics, and lets the user label them, pull in more screenshots, and save the
/// resulting <see cref="DigitTemplateSet"/>. The recognizer is rebuilt after every added template so
/// guesses improve within a session (the dataset is small).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DigitTrainerForm : Form {
    private readonly DigitTemplateSet set;
    private readonly FileInfo templatesFile;
    private readonly List<PendingGlyph> pending;
    private readonly Func<IReadOnlyList<FileInfo>, IReadOnlyList<PendingGlyph>>? addImages;

    private TemplateDigitRecognizer? recognizer;
    private int index;
    private int added;
    private int skippedDuplicates;

    private readonly PictureBox originalBox = new() {
        Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Gainsboro,
    };
    private readonly PictureBox normalizedBox = new() {
        Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Gainsboro,
    };
    private readonly Label contextLabel = new() { Dock = DockStyle.Fill, AutoSize = true };
    private readonly Label confidenceLabel = new() {
        Dock = DockStyle.Fill, AutoSize = true, Font = new Font(FontFamily.GenericMonospace, 10, FontStyle.Bold),
    };
    private readonly TextBox labelInput = new() {
        Dock = DockStyle.Fill, Font = new Font(FontFamily.GenericMonospace, 14), MaxLength = 1,
    };
    private readonly Label status = new() { Dock = DockStyle.Fill, AutoSize = true, ForeColor = Color.ForestGreen };
    private readonly Button acceptButton = new() { Text = "Accept guess (Enter)", Dock = DockStyle.Fill, Height = 32 };
    private readonly Button saveLabelButton = new() { Text = "Save typed label", Dock = DockStyle.Fill, Height = 32 };
    private readonly Button skipButton = new() { Text = "Skip", Dock = DockStyle.Fill };

    /// <summary>Total templates in the set as of the last save (used by the launcher to report totals).</summary>
    public int SavedTemplateCount { get; private set; }

    public DigitTrainerForm(
        IReadOnlyList<PendingGlyph> pending,
        DigitTemplateSet set,
        FileInfo templatesFile,
        Func<IReadOnlyList<FileInfo>, IReadOnlyList<PendingGlyph>>? addImages
    ) {
        ArgumentNullException.ThrowIfNull(pending);
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(templatesFile);
        this.set = set;
        this.templatesFile = templatesFile;
        this.pending = [.. pending];
        this.addImages = addImages;
        SavedTemplateCount = set.Templates.Count;
        recognizer = set.Templates.Count > 0 ? new TemplateDigitRecognizer(set) : null;

        Text = "FHM Vision — Digit Trainer";
        Width = 1100;
        Height = 720;
        MinimumSize = new Size(820, 560);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        BuildLayout();
        ShowCurrent();
    }

    private void BuildLayout() {
        var previews = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(8),
        };
        previews.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        previews.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        previews.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        previews.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        previews.Controls.Add(new Label { Text = "Original crop", Dock = DockStyle.Fill, AutoSize = true }, 0, 0);
        previews.Controls.Add(new Label { Text = "Normalized (matched)", Dock = DockStyle.Fill, AutoSize = true }, 1, 0);
        previews.Controls.Add(originalBox, 0, 1);
        previews.Controls.Add(normalizedBox, 1, 1);

        const int sideWidth = 320;
        var side = new TableLayoutPanel {
            Dock = DockStyle.Right, Width = sideWidth, ColumnCount = 1, AutoScroll = true,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows, Padding = new Padding(8),
        };
        side.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        AddRow(side, contextLabel);
        AddRow(side, Labeled("Best guess", confidenceLabel));
        AddRow(side, Labeled("Label (0-9, '.', '-')", labelInput));
        AddRow(side, acceptButton);
        AddRow(side, saveLabelButton);
        AddRow(side, skipButton);

        var help = new Label {
            Text = "Enter accepts the guess when the label box is empty, otherwise it saves the typed "
                + "character. Near-identical duplicates of a label are skipped automatically.",
            Dock = DockStyle.Fill, AutoSize = true, MaximumSize = new Size(sideWidth - 24, 0),
            Padding = new Padding(0, 8, 0, 8),
        };
        AddRow(side, help);

        var addImagesButton = new Button { Text = "Add images…", Dock = DockStyle.Fill, Height = 30 };
        addImagesButton.Enabled = addImages is not null;
        addImagesButton.Click += (_, _) => OnAddImages();
        AddRow(side, addImagesButton);

        var save = new Button { Text = "Save (keep open)", Dock = DockStyle.Fill, Height = 32 };
        save.Click += (_, _) => Save(closeAfter: false);
        AddRow(side, save);

        var saveClose = new Button { Text = "Save && close", Dock = DockStyle.Fill, Height = 32 };
        saveClose.Click += (_, _) => Save(closeAfter: true);
        AddRow(side, saveClose);

        AddRow(side, status);

        acceptButton.Click += (_, _) => AcceptGuess();
        saveLabelButton.Click += (_, _) => SaveTypedLabel();
        skipButton.Click += (_, _) => Advance();
        labelInput.KeyDown += OnLabelKeyDown;

        Controls.Add(side);
        Controls.Add(previews);
    }

    private static void AddRow(TableLayoutPanel panel, Control control) {
        control.Margin = new Padding(0, 0, 0, 6);
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(control);
    }

    private static Control Labeled(string caption, Control control) {
        var panel = new TableLayoutPanel {
            Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1, Margin = new Padding(0),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.Controls.Add(new Label { Text = caption, Dock = DockStyle.Fill, AutoSize = true });
        control.Margin = new Padding(0, 2, 0, 0);
        panel.Controls.Add(control);
        return panel;
    }

    private void OnLabelKeyDown(object? sender, KeyEventArgs e) {
        if (e.KeyCode != Keys.Enter) {
            return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
        if (labelInput.Text.Trim().Length == 0) {
            AcceptGuess();
        } else {
            SaveTypedLabel();
        }
    }

    private PendingGlyph? Current => index >= 0 && index < pending.Count ? pending[index] : null;

    private void ShowCurrent() {
        originalBox.Image?.Dispose();
        normalizedBox.Image?.Dispose();
        originalBox.Image = null;
        normalizedBox.Image = null;
        labelInput.Clear();

        if (Current is not { } glyph) {
            contextLabel.Text = pending.Count == 0
                ? "No glyphs queued. Use \u2018Add images\u2026\u2019 to load screenshots."
                : "All queued glyphs handled. Add more images or save.";
            confidenceLabel.Text = string.Empty;
            SetInputEnabled(false);
            return;
        }

        SetInputEnabled(true);
        contextLabel.Text =
            $"{glyph.ImageName}\nregion '{glyph.RegionKey}'  glyph {glyph.GlyphIndex + 1}/{glyph.GlyphCount}"
            + $"\nqueued {index + 1}/{pending.Count}";
        originalBox.Image = GlyphImaging.FromPng(glyph.OriginalCropPng);
        normalizedBox.Image = GlyphImaging.Render(glyph.Normalized);

        if (recognizer is null) {
            confidenceLabel.Text = "no templates yet";
            confidenceLabel.ForeColor = Color.DimGray;
        } else {
            var match = recognizer.Classify(glyph.Normalized);
            confidenceLabel.Text = $"'{match.Label}'  d={match.Score:0.###}  m={match.Margin:0.###}  "
                + (match.Confident ? "confident" : "low");
            confidenceLabel.ForeColor = match.Confident ? Color.ForestGreen : Color.DarkOrange;
        }

        labelInput.Focus();
    }

    private void SetInputEnabled(bool enabled) {
        labelInput.Enabled = enabled;
        acceptButton.Enabled = enabled && recognizer is not null;
        saveLabelButton.Enabled = enabled;
        skipButton.Enabled = enabled;
    }

    private void AcceptGuess() {
        if (Current is not { } glyph || recognizer is null) {
            return;
        }

        Apply(recognizer.Classify(glyph.Normalized).Label);
    }

    private void SaveTypedLabel() {
        if (Current is null) {
            return;
        }

        var label = labelInput.Text.Trim();
        if (label.Length == 0) {
            return;
        }

        Apply(label[..1]);
    }

    private void Apply(string label) {
        if (Current is not { } glyph) {
            return;
        }

        if (set.TryAdd(label, glyph.Normalized)) {
            added++;
            recognizer = new TemplateDigitRecognizer(set);
            status.ForeColor = Color.ForestGreen;
            status.Text = $"Added '{label}'.  {StatsText()}";
        } else {
            skippedDuplicates++;
            status.ForeColor = Color.DimGray;
            status.Text = $"Duplicate '{label}' skipped.  {StatsText()}";
        }

        Advance();
    }

    private void Advance() {
        index++;
        ShowCurrent();
    }

    private string StatsText() =>
        $"added {added}, dup-skipped {skippedDuplicates}, remaining {Math.Max(0, pending.Count - index - 1)}, "
        + $"templates {set.Templates.Count}.";

    private void OnAddImages() {
        if (addImages is null) {
            return;
        }

        using var dialog = new OpenFileDialog {
            Title = "Add screenshots to segment",
            Filter = "PNG images (*.png)|*.png|All files (*.*)|*.*",
            Multiselect = true,
        };
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.FileNames.Length == 0) {
            return;
        }

        var files = dialog.FileNames.Select(f => new FileInfo(f)).ToList();
        IReadOnlyList<PendingGlyph> found;
        var previousCursor = Cursor;
        Cursor = Cursors.WaitCursor;
        try {
            found = addImages(files);
        } catch (Exception ex) {
            Cursor = previousCursor;
            MessageBox.Show(this, $"Could not segment the added images:\n{ex.Message}",
                "Add images", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        } finally {
            Cursor = previousCursor;
        }

        if (found.Count == 0) {
            status.ForeColor = Color.DarkOrange;
            status.Text = "No numeric glyphs found in the added image(s) — check the profile matches.";
            return;
        }

        var wasFinished = Current is null;
        pending.AddRange(found);
        status.ForeColor = Color.ForestGreen;
        status.Text = $"Added {found.Count} glyph(s) from {files.Count} image(s).  {StatsText()}";
        if (wasFinished) {
            ShowCurrent();
        }
    }

    private void Save(bool closeAfter) {
        try {
            set.Dedup();
            DigitTemplateStore.SaveAsync(templatesFile, set, CancellationToken.None).GetAwaiter().GetResult();
        } catch (Exception ex) {
            MessageBox.Show(this, $"Could not save templates:\n{ex.Message}",
                "Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        SavedTemplateCount = set.Templates.Count;
        if (closeAfter) {
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        status.ForeColor = Color.ForestGreen;
        status.Text = $"Saved {set.Templates.Count} template(s) to {templatesFile.Name} at {DateTime.Now:HH:mm:ss}.";
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            originalBox.Image?.Dispose();
            normalizedBox.Image?.Dispose();
        }

        base.Dispose(disposing);
    }
}
