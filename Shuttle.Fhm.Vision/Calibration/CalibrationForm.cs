using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;
using Shuttle.Fhm.Vision.Layout;

namespace Shuttle.Fhm.Vision.Calibration;

/// <summary>
/// Interactive WinForms editor for authoring a <see cref="LayoutProfile"/>. The user drags
/// rectangles over a captured FHM screenshot and assigns each a key/group/kind (or marks it as a
/// screen-detection anchor). Regions are stored internally in image-pixel space and converted to
/// resolution-independent ratios on save.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class CalibrationForm : Form {
    private sealed record DraftRegion(Rectangle PixelBounds, FieldRegion? Field, AnchorMarker? Anchor) {
        public bool IsAnchor => Anchor is not null;
    }

    private readonly Bitmap screenshot;
    private readonly FileInfo profileFile;
    private readonly List<DraftRegion> drafts = [];

    private readonly PictureBox picture = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
    private readonly TextBox profileName = new() { Text = "fhm-player-screen", Dock = DockStyle.Fill };
    private readonly TextBox key = new() { Dock = DockStyle.Fill };
    private readonly ComboBox group = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox kind = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox isAnchor = new() { Text = "Anchor (screen marker)", Dock = DockStyle.Fill, AutoSize = true };
    private readonly TextBox anchorText = new() { Dock = DockStyle.Fill };
    private readonly ListBox list = new() { Dock = DockStyle.Fill };
    private readonly Label status = new() { Dock = DockStyle.Fill, AutoSize = true, ForeColor = Color.ForestGreen };

    private Point? dragStart;
    private Rectangle dragRect;

    /// <summary>The profile the user saved, or <c>null</c> if the dialog was cancelled/closed.</summary>
    public LayoutProfile? Result { get; private set; }

    public CalibrationForm(Bitmap screenshot, LayoutProfile? existing, FileInfo profileFile) {
        ArgumentNullException.ThrowIfNull(screenshot);
        ArgumentNullException.ThrowIfNull(profileFile);
        this.screenshot = screenshot;
        this.profileFile = profileFile;

        Text = "FHM Vision — Layout Calibration";
        Width = 1280;
        Height = 800;
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;

        picture.Image = screenshot;
        picture.MouseDown += OnPictureMouseDown;
        picture.MouseMove += OnPictureMouseMove;
        picture.MouseUp += OnPictureMouseUp;
        picture.Paint += OnPicturePaint;

        BuildLayout();
        LoadExisting(existing);
        RefreshList();
    }

    private void BuildLayout() {
        foreach (var value in Enum.GetNames<FieldGroup>()) {
            group.Items.Add(value);
        }

        group.SelectedIndex = 0;
        foreach (var value in Enum.GetNames<FieldKind>()) {
            kind.Items.Add(value);
        }

        kind.SelectedIndex = 0;

        const int sideWidth = 340;
        var side = new TableLayoutPanel {
            Dock = DockStyle.Right,
            Width = sideWidth,
            ColumnCount = 1,
            AutoScroll = true,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            Padding = new Padding(8),
        };
        // A single column that exactly fills the panel keeps children from growing horizontally
        // (which previously pushed controls past the right edge of the window).
        side.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        AddRow(side, Labeled("Profile name", profileName));
        AddRow(side, Labeled("Field key (e.g. name, skating, playmaker)", key));
        AddRow(side, Labeled("Group", group));
        AddRow(side, Labeled("Kind", kind));
        AddRow(side, isAnchor);
        AddRow(side, Labeled("Anchor expected text", anchorText));

        var help = new Label {
            Text = "Drag a rectangle on the screenshot to add it using the settings above. "
                + "Kind = Bio parses the fixed FHM10 bio line (position, height, weight); its key/group are ignored.",
            Dock = DockStyle.Fill,
            AutoSize = true,
            MaximumSize = new Size(sideWidth - 24, 0),
            Padding = new Padding(0, 8, 0, 8),
        };
        AddRow(side, help);

        list.Height = 220;
        AddRow(side, Labeled("Regions / anchors", list));

        var remove = new Button { Text = "Remove selected", Dock = DockStyle.Fill };
        remove.Click += (_, _) => RemoveSelected();
        AddRow(side, remove);

        var save = new Button { Text = "Save (keep open)", Dock = DockStyle.Fill, Height = 32 };
        save.Click += (_, _) => SaveToDisk(closeAfter: false);
        AddRow(side, save);

        var saveClose = new Button { Text = "Save && close", Dock = DockStyle.Fill, Height = 32 };
        saveClose.Click += (_, _) => SaveToDisk(closeAfter: true);
        AddRow(side, saveClose);

        AddRow(side, status);

        // The Fill control (picture) must be added last so it occupies the space left of the panel.
        Controls.Add(side);
        Controls.Add(picture);
    }

    private static void AddRow(TableLayoutPanel panel, Control control) {
        control.Margin = new Padding(0, 0, 0, 6);
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(control);
    }

    private static Control Labeled(string caption, Control control) {
        var panel = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Margin = new Padding(0),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.Controls.Add(new Label { Text = caption, Dock = DockStyle.Fill, AutoSize = true });
        control.Margin = new Padding(0, 2, 0, 0);
        panel.Controls.Add(control);
        return panel;
    }

    private void LoadExisting(LayoutProfile? existing) {
        if (existing is null) {
            return;
        }

        profileName.Text = existing.Name;
        foreach (var anchor in existing.Anchors) {
            drafts.Add(new DraftRegion(ToPixels(anchor.Bounds), Field: null, anchor));
        }

        foreach (var region in existing.Regions) {
            drafts.Add(new DraftRegion(ToPixels(region.Bounds), region, Anchor: null));
        }
    }

    private Rectangle ToPixels(RatioRect bounds) {
        var pixel = bounds.ToPixels(screenshot.Width, screenshot.Height);
        return new Rectangle(pixel.X, pixel.Y, pixel.Width, pixel.Height);
    }

    private void OnPictureMouseDown(object? sender, MouseEventArgs e) {
        dragStart = e.Location;
        dragRect = Rectangle.Empty;
    }

    private void OnPictureMouseMove(object? sender, MouseEventArgs e) {
        if (dragStart is not { } start) {
            return;
        }

        dragRect = Rectangle.FromLTRB(
            Math.Min(start.X, e.X), Math.Min(start.Y, e.Y),
            Math.Max(start.X, e.X), Math.Max(start.Y, e.Y));
        picture.Invalidate();
    }

    private void OnPictureMouseUp(object? sender, MouseEventArgs e) {
        if (dragStart is null) {
            return;
        }

        dragStart = null;
        if (dragRect.Width < 3 || dragRect.Height < 3) {
            return;
        }

        var imageRect = ControlToImage(dragRect);
        if (imageRect.Width <= 0 || imageRect.Height <= 0) {
            return;
        }

        AddDraft(imageRect);
        dragRect = Rectangle.Empty;
        picture.Invalidate();
    }

    private void AddDraft(Rectangle imageRect) {
        if (isAnchor.Checked) {
            if (string.IsNullOrWhiteSpace(anchorText.Text)) {
                MessageBox.Show("Enter the expected anchor text first.", "Anchor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            drafts.Add(new DraftRegion(
                imageRect,
                Field: null,
                new AnchorMarker { Bounds = ToRatio(imageRect), ExpectedText = anchorText.Text.Trim() }));
        } else {
            if (string.IsNullOrWhiteSpace(key.Text)) {
                MessageBox.Show("Enter a field key first.", "Field", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            drafts.Add(new DraftRegion(
                imageRect,
                new FieldRegion {
                    Key = key.Text.Trim(),
                    Group = Enum.Parse<FieldGroup>((string)group.SelectedItem!),
                    Kind = Enum.Parse<FieldKind>((string)kind.SelectedItem!),
                    Bounds = ToRatio(imageRect),
                },
                Anchor: null));
        }

        RefreshList();
    }

    private RatioRect ToRatio(Rectangle imageRect) =>
        RatioRect.FromPixels(
            imageRect.X, imageRect.Y, imageRect.Width, imageRect.Height,
            screenshot.Width, screenshot.Height);

    private void RemoveSelected() {
        if (list.SelectedIndex >= 0 && list.SelectedIndex < drafts.Count) {
            drafts.RemoveAt(list.SelectedIndex);
            RefreshList();
            picture.Invalidate();
        }
    }

    private void RefreshList() {
        list.BeginUpdate();
        list.Items.Clear();
        foreach (var draft in drafts) {
            list.Items.Add(draft.IsAnchor
                ? $"[anchor] '{draft.Anchor!.ExpectedText}'"
                : $"{draft.Field!.Group}/{draft.Field.Kind}: {draft.Field.Key}");
        }

        list.EndUpdate();
    }

    private void SaveToDisk(bool closeAfter) {
        if (string.IsNullOrWhiteSpace(profileName.Text)) {
            MessageBox.Show("Enter a profile name.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var profile = new LayoutProfile {
            Name = profileName.Text.Trim(),
            Anchors = [.. drafts.Where(d => d.IsAnchor).Select(d => d.Anchor!)],
            Regions = [.. drafts.Where(d => !d.IsAnchor).Select(d => d.Field!)],
        };

        try {
            profileFile.Directory?.Create();
            File.WriteAllText(profileFile.FullName, LayoutProfileStore.Serialize(profile));
        }
        catch (Exception ex) {
            MessageBox.Show($"Could not save profile:\n{ex.Message}", "Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Result = profile;

        if (closeAfter) {
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        status.Text = $"Saved {profile.Regions.Count} region(s), {profile.Anchors.Count} anchor(s) to " +
            $"{profileFile.Name} at {DateTime.Now:HH:mm:ss}.";
    }

    private void OnPicturePaint(object? sender, PaintEventArgs e) {
        using var regionPen = new Pen(Color.Lime, 2);
        using var anchorPen = new Pen(Color.OrangeRed, 2);
        using var font = new Font(FontFamily.GenericSansSerif, 8);

        foreach (var draft in drafts) {
            var display = ImageToControl(draft.PixelBounds);
            var pen = draft.IsAnchor ? anchorPen : regionPen;
            e.Graphics.DrawRectangle(pen, display);
            var label = draft.IsAnchor ? draft.Anchor!.ExpectedText : draft.Field!.Key;
            e.Graphics.DrawString(label, font, pen.Brush, display.X + 2, display.Y + 2);
        }

        if (dragRect is { Width: > 0, Height: > 0 }) {
            using var dragPen = new Pen(Color.DeepSkyBlue, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            e.Graphics.DrawRectangle(dragPen, dragRect);
        }
    }

    // --- Zoom-mode coordinate mapping between the PictureBox and the source image ---

    private (float Scale, float OffsetX, float OffsetY) GetTransform() {
        float cw = picture.ClientSize.Width;
        float ch = picture.ClientSize.Height;
        float iw = screenshot.Width;
        float ih = screenshot.Height;
        var scale = Math.Min(cw / iw, ch / ih);
        var offsetX = (cw - (iw * scale)) / 2f;
        var offsetY = (ch - (ih * scale)) / 2f;
        return (scale, offsetX, offsetY);
    }

    private Rectangle ControlToImage(Rectangle control) {
        var (scale, offsetX, offsetY) = GetTransform();
        if (scale <= 0) {
            return Rectangle.Empty;
        }

        var x = (int)Math.Round((control.X - offsetX) / scale);
        var y = (int)Math.Round((control.Y - offsetY) / scale);
        var w = (int)Math.Round(control.Width / scale);
        var h = (int)Math.Round(control.Height / scale);

        x = Math.Clamp(x, 0, screenshot.Width - 1);
        y = Math.Clamp(y, 0, screenshot.Height - 1);
        w = Math.Clamp(w, 1, screenshot.Width - x);
        h = Math.Clamp(h, 1, screenshot.Height - y);
        return new Rectangle(x, y, w, h);
    }

    private Rectangle ImageToControl(Rectangle image) {
        var (scale, offsetX, offsetY) = GetTransform();
        var x = (int)Math.Round((image.X * scale) + offsetX);
        var y = (int)Math.Round((image.Y * scale) + offsetY);
        var w = (int)Math.Round(image.Width * scale);
        var h = (int)Math.Round(image.Height * scale);
        return new Rectangle(x, y, w, h);
    }

    protected override void OnResize(EventArgs e) {
        base.OnResize(e);
        picture.Invalidate();
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            picture.Image = null;
        }

        base.Dispose(disposing);
    }
}
