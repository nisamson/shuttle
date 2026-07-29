namespace Shuttle.Fhm.Vision.Recognition;

/// <summary>Per-label result of a leave-one-out evaluation of a <see cref="DigitTemplateSet"/>.</summary>
/// <param name="Label">The glyph label these stats describe.</param>
/// <param name="Count">How many templates carry this label.</param>
/// <param name="Correct">How many were classified correctly when held out.</param>
/// <param name="MeanSameDistance">
/// Mean normalized distance to the nearest <em>same-label</em> template (over samples that have one).
/// Lower is better — it is what a correct match scores.
/// </param>
/// <param name="MeanNearestOtherDistance">
/// Mean normalized distance to the nearest <em>different-label</em> template. The gap between this and
/// <see cref="MeanSameDistance"/> is the confidence margin.
/// </param>
public readonly record struct LabelStats(
    string Label,
    int Count,
    int Correct,
    double MeanSameDistance,
    double MeanNearestOtherDistance
) {
    public double Accuracy => Count == 0 ? 0 : (double)Correct / Count;

    /// <summary>Separation between the nearest wrong template and the nearest right one; higher is safer.</summary>
    public double Margin => MeanNearestOtherDistance - MeanSameDistance;
}

/// <summary>A held-out template that was classified as the wrong label.</summary>
public readonly record struct ConfusionPair(string Actual, string Predicted, int Count);

/// <summary>Aggregate result of leave-one-out evaluation over a whole template set.</summary>
public readonly record struct DigitEvaluation(
    int TemplateCount,
    int Correct,
    IReadOnlyList<LabelStats> PerLabel,
    IReadOnlyList<ConfusionPair> Confusions,
    IReadOnlyList<string> SingleSampleLabels
) {
    public double Accuracy => TemplateCount == 0 ? 0 : (double)Correct / TemplateCount;
}

/// <summary>
/// Scores how well a <see cref="DigitTemplateSet"/> identifies its own glyphs using leave-one-out
/// cross-validation: each template is classified against every <em>other</em> template. This measures
/// coverage/robustness of the collected samples without needing any extra labelled data, and surfaces
/// the confusion pairs and confidence margins that tell you which digits still need more samples.
/// </summary>
public static class DigitTemplateEvaluator {
    public static DigitEvaluation Evaluate(DigitTemplateSet set) {
        ArgumentNullException.ThrowIfNull(set);
        var glyphs = set.Materialize();
        var total = set.Width * set.Height;

        var correctByLabel = new Dictionary<string, int>(StringComparer.Ordinal);
        var countByLabel = new Dictionary<string, int>(StringComparer.Ordinal);
        var sameDistSum = new Dictionary<string, double>(StringComparer.Ordinal);
        var sameDistCount = new Dictionary<string, int>(StringComparer.Ordinal);
        var otherDistSum = new Dictionary<string, double>(StringComparer.Ordinal);
        var confusions = new Dictionary<(string Actual, string Predicted), int>();

        var correct = 0;
        for (var i = 0; i < glyphs.Count; i++) {
            var actual = glyphs[i].Label;
            countByLabel[actual] = countByLabel.GetValueOrDefault(actual) + 1;

            var bestLabel = string.Empty;
            var bestDistance = int.MaxValue;
            var bestSame = int.MaxValue;
            var bestOther = int.MaxValue;

            for (var j = 0; j < glyphs.Count; j++) {
                if (i == j) {
                    continue;
                }

                var distance = glyphs[i].Glyph.Distance(glyphs[j].Glyph);
                if (distance < bestDistance) {
                    bestDistance = distance;
                    bestLabel = glyphs[j].Label;
                }

                if (glyphs[j].Label == actual) {
                    bestSame = Math.Min(bestSame, distance);
                } else {
                    bestOther = Math.Min(bestOther, distance);
                }
            }

            if (bestDistance == int.MaxValue) {
                // Only one template in the whole set; nothing to compare against.
                continue;
            }

            if (bestLabel == actual) {
                correct++;
                correctByLabel[actual] = correctByLabel.GetValueOrDefault(actual) + 1;
            } else {
                var key = (actual, bestLabel);
                confusions[key] = confusions.GetValueOrDefault(key) + 1;
            }

            if (bestSame != int.MaxValue) {
                sameDistSum[actual] = sameDistSum.GetValueOrDefault(actual) + ((double)bestSame / total);
                sameDistCount[actual] = sameDistCount.GetValueOrDefault(actual) + 1;
            }

            if (bestOther != int.MaxValue) {
                otherDistSum[actual] = otherDistSum.GetValueOrDefault(actual) + ((double)bestOther / total);
            }
        }

        var perLabel = countByLabel.Keys
            .OrderBy(l => l, StringComparer.Ordinal)
            .Select(label => {
                var count = countByLabel[label];
                var sameCount = sameDistCount.GetValueOrDefault(label);
                var meanSame = sameCount > 0 ? sameDistSum.GetValueOrDefault(label) / sameCount : 0.0;
                var meanOther = count > 0 ? otherDistSum.GetValueOrDefault(label) / count : 0.0;
                return new LabelStats(label, count, correctByLabel.GetValueOrDefault(label), meanSame, meanOther);
            })
            .ToList();

        var confusionPairs = confusions
            .Select(kv => new ConfusionPair(kv.Key.Actual, kv.Key.Predicted, kv.Value))
            .OrderByDescending(c => c.Count)
            .ThenBy(c => c.Actual, StringComparer.Ordinal)
            .ToList();

        var singleSample = countByLabel
            .Where(kv => kv.Value < 2)
            .Select(kv => kv.Key)
            .OrderBy(l => l, StringComparer.Ordinal)
            .ToList();

        return new DigitEvaluation(glyphs.Count, correct, perLabel, confusionPairs, singleSample);
    }
}
