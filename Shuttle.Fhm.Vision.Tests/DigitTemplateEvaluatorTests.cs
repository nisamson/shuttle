using Shuttle.Fhm.Vision.Recognition;

namespace Shuttle.Fhm.Vision.Tests;

public sealed class DigitTemplateEvaluatorTests {
    private static GlyphBitmap Glyph(int width, int height, params (int X, int Y)[] ink) {
        var pixels = new bool[width * height];
        foreach (var (x, y) in ink) {
            pixels[(y * width) + x] = true;
        }

        return new GlyphBitmap(width, height, pixels);
    }

    [Fact]
    public void Evaluate_scores_a_clean_separable_set_as_fully_correct() {
        var set = new DigitTemplateSet { Width = 3, Height = 1 };
        // Two well-separated clusters: '0' lights column 0, '1' lights column 2.
        set.Add("0", Glyph(3, 1, (0, 0)));
        set.Add("0", Glyph(3, 1, (0, 0)));
        set.Add("1", Glyph(3, 1, (2, 0)));
        set.Add("1", Glyph(3, 1, (2, 0)));

        var eval = DigitTemplateEvaluator.Evaluate(set);

        Assert.Equal(4, eval.TemplateCount);
        Assert.Equal(4, eval.Correct);
        Assert.Equal(1.0, eval.Accuracy);
        Assert.Empty(eval.Confusions);
        Assert.Empty(eval.SingleSampleLabels);

        var zero = eval.PerLabel.Single(l => l.Label == "0");
        Assert.Equal(2, zero.Count);
        Assert.Equal(0.0, zero.MeanSameDistance); // identical same-label neighbour
        Assert.True(zero.Margin > 0.0);
    }

    [Fact]
    public void Evaluate_reports_confusion_between_indistinguishable_labels() {
        var set = new DigitTemplateSet { Width = 3, Height = 1 };
        // '6' and '8' share the exact same bitmap, so held-out each nearest neighbour is the other label.
        set.Add("6", Glyph(3, 1, (1, 0)));
        set.Add("8", Glyph(3, 1, (1, 0)));

        var eval = DigitTemplateEvaluator.Evaluate(set);

        Assert.Equal(0, eval.Correct);
        Assert.Contains(eval.Confusions, c => c.Actual == "6" && c.Predicted == "8");
        Assert.Contains(eval.Confusions, c => c.Actual == "8" && c.Predicted == "6");
    }

    [Fact]
    public void Evaluate_flags_single_sample_labels() {
        var set = new DigitTemplateSet { Width = 3, Height = 1 };
        set.Add("0", Glyph(3, 1, (0, 0)));
        set.Add("0", Glyph(3, 1, (0, 0)));
        set.Add("7", Glyph(3, 1, (2, 0)));

        var eval = DigitTemplateEvaluator.Evaluate(set);

        Assert.Contains("7", eval.SingleSampleLabels);
        Assert.DoesNotContain("0", eval.SingleSampleLabels);
    }
}
