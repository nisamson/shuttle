using FastDtw.CSharp;

namespace Shuttle.Api.Services.DevelopmentProjections;

/// <summary>Exact constrained DTW and development-curve compatibility scoring.</summary>
public static class DevelopmentCurveSimilarity {
    public const double EndpointPenaltyWeight = 0.25;
    public const double RatePenaltyWeight = 2;
    public const double SimilarityScale = 100;

    /// <summary>
    /// Computes exact path-length-normalized DTW inside a Sakoe-Chiba band.
    /// </summary>
    /// <remarks>
    /// FastDtw.CSharp 2.3.0 exposes constrained and path-normalized scores as separate overloads,
    /// but has no overload combining both. Its constrained implementation supplies the exact raw
    /// score; the rolling dynamic program below obtains the corresponding constrained path length.
    /// </remarks>
    public static double GetPathLengthNormalizedDtw(
        double[] left,
        double[] right,
        int bandWidth) {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Length == 0 || right.Length == 0) {
            throw new ArgumentException("DTW series must not be empty.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(bandWidth);
        bandWidth = Math.Max(bandWidth, Math.Abs(left.Length - right.Length));

        var rawScore = Dtw.GetScore(left, right, bandWidth);
        var pathLength = GetOptimalPathLength(left, right, bandWidth);
        return rawScore / pathLength;
    }

    public static (double Distance, double Score) Compare(
        IReadOnlyList<double> left,
        IReadOnlyList<double> right,
        int bandWidth) {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Count != right.Count || left.Count < 2) {
            throw new ArgumentException("Development curves must have equal lengths of at least two points.");
        }

        var leftGain = left.Select(value => value - left[0]).ToArray();
        var rightGain = right.Select(value => value - right[0]).ToArray();
        var dtw = GetPathLengthNormalizedDtw(leftGain, rightGain, bandWidth);
        var endpointPenalty = Math.Abs(left[^1] - right[^1]);
        var leftRate = leftGain[^1] / (leftGain.Length - 1);
        var rightRate = rightGain[^1] / (rightGain.Length - 1);
        var ratePenalty = Math.Abs(leftRate - rightRate);
        var distance = dtw
            + (EndpointPenaltyWeight * endpointPenalty)
            + (RatePenaltyWeight * ratePenalty);
        var score = Math.Exp(-distance / SimilarityScale);
        return (distance, score);
    }

    private static int GetOptimalPathLength(
        IReadOnlyList<double> left,
        IReadOnlyList<double> right,
        int bandWidth) {
        var previousCosts = Enumerable.Repeat(double.PositiveInfinity, right.Count).ToArray();
        var currentCosts = new double[right.Count];
        var previousLengths = new int[right.Count];
        var currentLengths = new int[right.Count];

        for (var i = 0; i < left.Count; i++) {
            Array.Fill(currentCosts, double.PositiveInfinity);
            Array.Clear(currentLengths);
            var minColumn = Math.Max(0, i - bandWidth);
            var maxColumn = Math.Min(right.Count - 1, i + bandWidth);

            for (var j = minColumn; j <= maxColumn; j++) {
                var localCost = Math.Abs(left[i] - right[j]);
                if (i == 0 && j == 0) {
                    currentCosts[j] = localCost;
                    currentLengths[j] = 1;
                    continue;
                }

                var bestCost = double.PositiveInfinity;
                var bestLength = 0;

                // Diagonal wins equal-cost ties, matching the shortest natural warp path.
                if (i > 0 && j > 0) {
                    bestCost = previousCosts[j - 1];
                    bestLength = previousLengths[j - 1];
                }

                if (i > 0 && previousCosts[j] < bestCost) {
                    bestCost = previousCosts[j];
                    bestLength = previousLengths[j];
                }

                if (j > 0 && currentCosts[j - 1] < bestCost) {
                    bestCost = currentCosts[j - 1];
                    bestLength = currentLengths[j - 1];
                }

                if (!double.IsPositiveInfinity(bestCost)) {
                    currentCosts[j] = bestCost + localCost;
                    currentLengths[j] = bestLength + 1;
                }
            }

            (previousCosts, currentCosts) = (currentCosts, previousCosts);
            (previousLengths, currentLengths) = (currentLengths, previousLengths);
        }

        var result = previousLengths[^1];
        return result > 0
            ? result
            : throw new InvalidOperationException("No DTW path exists inside the requested band.");
    }
}
