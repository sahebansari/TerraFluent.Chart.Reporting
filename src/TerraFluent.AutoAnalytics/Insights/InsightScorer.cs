using System;

namespace TerraFluent.AutoAnalytics.Insights;

/// <summary>
/// Deterministic 0..100 importance scoring. Each factor is normalised to 0..1 then weighted, so the
/// same evidence always yields the same score and the contribution of each factor is explainable.
/// </summary>
public sealed class InsightScorer
{
    /// <summary>Scores a correlation insight from the absolute coefficient and sample size.</summary>
    public int ScoreCorrelation(double absPearson, int sampleSize)
    {
        double strength = Clamp01(absPearson);                 // 0..1
        double coverage = Clamp01(sampleSize / 100.0);         // saturates at 100 pts
        return ToScore(0.8 * strength + 0.2 * coverage);
    }

    /// <summary>Scores a trend insight from growth magnitude, fit quality and coverage.</summary>
    public int ScoreTrend(double absGrowth, double rSquared, int points)
    {
        double growth = Clamp01(absGrowth / 1.0);              // +100% growth => full marks
        double fit = Clamp01(rSquared);
        double coverage = Clamp01(points / 24.0);
        return ToScore(0.5 * growth + 0.35 * fit + 0.15 * coverage);
    }

    /// <summary>Scores a dominance insight from the leading category's share.</summary>
    public int ScoreDominance(double topShare, int groupCount)
    {
        double dominance = Clamp01((topShare - 1.0 / Math.Max(2, groupCount)) / (1 - 1.0 / Math.Max(2, groupCount)));
        double magnitude = Clamp01(topShare);
        return ToScore(0.6 * magnitude + 0.4 * dominance);
    }

    /// <summary>Scores an anomaly insight from the peak z-score magnitude.</summary>
    public int ScoreAnomaly(double maxAbsZ)
    {
        double magnitude = Clamp01((maxAbsZ - 3.0) / 5.0 + 0.5); // z=3 -> 0.5, z=8 -> 1.0
        return ToScore(magnitude);
    }

    /// <summary>Scores a distribution insight from absolute skewness.</summary>
    public int ScoreDistribution(double absSkewness)
    {
        double magnitude = Clamp01(absSkewness / 2.0);
        return ToScore(0.7 * magnitude + 0.1);
    }

    private static int ToScore(double x) => (int)Math.Round(Clamp01(x) * 100);

    private static double Clamp01(double x) =>
        double.IsNaN(x) ? 0 : x < 0 ? 0 : x > 1 ? 1 : x;
}
