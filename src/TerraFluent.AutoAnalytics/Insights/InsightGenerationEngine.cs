using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;

namespace TerraFluent.AutoAnalytics.Insights;

/// <summary>
/// Phase 6 — turns analytics findings into ranked, natural-language <see cref="Insight"/> objects.
/// Deterministic: identical findings always yield identical narratives and scores.
/// </summary>
public sealed class InsightGenerationEngine
{
    private readonly InsightScorer _scorer;

    public InsightGenerationEngine(InsightScorer? scorer = null)
        => _scorer = scorer ?? new InsightScorer();

    /// <summary>Generates all insights, ordered by descending importance.</summary>
    public IReadOnlyList<Insight> Generate(DatasetProfile profile, AnalyticsFindings findings)
    {
        var insights = new List<Insight>();
        insights.AddRange(TrendInsights(findings.Trends));
        insights.AddRange(DominanceInsights(findings.Groups));
        insights.AddRange(CorrelationInsights(findings.Correlations));
        insights.AddRange(AnomalyInsights(findings.Anomalies, profile));
        insights.AddRange(DistributionInsights(profile));

        return insights
            .OrderByDescending(i => i.ImportanceScore)
            .ToList();
    }

    private IEnumerable<Insight> TrendInsights(IReadOnlyList<TrendResult> trends)
    {
        foreach (var t in trends)
        {
            if (t.Kind is TrendKind.Unknown) continue;
            int score = _scorer.ScoreTrend(Math.Abs(t.GrowthRate), t.RSquared, t.PointCount);
            if (score < 10) continue;

            string pct = Percent(t.GrowthRate);
            string overPeriod = t.OrderedBy is null ? "over the observed sequence" : $"over the observed {t.OrderedBy} period";

            string title = t.Kind switch
            {
                TrendKind.Rising    => $"{t.Measure} is rising ({pct})",
                TrendKind.Declining => $"{t.Measure} is declining ({pct})",
                TrendKind.Volatile  => $"{t.Measure} is volatile",
                TrendKind.Seasonal  => $"{t.Measure} shows a seasonal pattern",
                _                   => $"{t.Measure} is stable"
            };

            string desc = t.Kind switch
            {
                TrendKind.Rising    => $"{t.Measure} increased by {pct} {overPeriod}.",
                TrendKind.Declining => $"{t.Measure} decreased by {pct} {overPeriod}.",
                TrendKind.Volatile  => $"{t.Measure} fluctuates strongly with no consistent direction (volatility {t.Volatility:P0}).",
                TrendKind.Seasonal  => $"{t.Measure} exhibits a repeating up-and-down seasonal pattern {overPeriod}.",
                _                   => $"{t.Measure} remained broadly flat {overPeriod}."
            };

            yield return new Insight
            {
                Kind = InsightKind.Trend,
                Title = title,
                Description = desc,
                ImportanceScore = score,
                RelatedColumns = new[] { t.Measure },
                Evidence = new Dictionary<string, string>
                {
                    ["growthRate"] = t.GrowthRate.ToString("F3", CultureInfo.InvariantCulture),
                    ["slope"] = t.Slope.ToString("F3", CultureInfo.InvariantCulture),
                    ["rSquared"] = t.RSquared.ToString("F3", CultureInfo.InvariantCulture),
                    ["points"] = t.PointCount.ToString(CultureInfo.InvariantCulture)
                }
            };
        }
    }

    private IEnumerable<Insight> DominanceInsights(IReadOnlyList<GroupAnalysisResult> groups)
    {
        foreach (var g in groups)
        {
            var top = g.Top;
            if (top is null || g.Buckets.Count < 2) continue;
            int score = _scorer.ScoreDominance(top.Share, g.Buckets.Count);
            if (score < 15) continue;

            string share = Percent(top.Share, alreadyFraction: true);
            yield return new Insight
            {
                Kind = InsightKind.Dominance,
                Title = $"{top.Key} leads {g.Measure} by {g.Dimension}",
                Description = $"{top.Key} contributes {share} of total {g.Measure} across {g.Dimension} " +
                              $"({g.Buckets.Count} groups).",
                ImportanceScore = score,
                RelatedColumns = new[] { g.Dimension, g.Measure },
                Evidence = new Dictionary<string, string>
                {
                    ["topCategory"] = top.Key,
                    ["topShare"] = top.Share.ToString("F3", CultureInfo.InvariantCulture),
                    ["groups"] = g.Buckets.Count.ToString(CultureInfo.InvariantCulture),
                    ["total"] = g.Total.ToString("F2", CultureInfo.InvariantCulture)
                }
            };
        }
    }

    private IEnumerable<Insight> CorrelationInsights(IReadOnlyList<CorrelationResult> correlations)
    {
        foreach (var c in correlations)
        {
            if (c.Strength is CorrelationStrength.None or CorrelationStrength.Weak) continue;
            int score = _scorer.ScoreCorrelation(Math.Abs(c.Pearson), c.SampleSize);
            if (score < 15) continue;

            string dir = c.IsNegative ? "negative" : "positive";
            string strength = c.Strength.ToString().ToLowerInvariant().Replace("very", "very ");
            yield return new Insight
            {
                Kind = InsightKind.Correlation,
                Title = $"{c.ColumnX} and {c.ColumnY} are correlated ({c.Pearson:F2})",
                Description = $"{c.ColumnX} and {c.ColumnY} exhibit a {strength} {dir} correlation " +
                              $"(r = {c.Pearson.ToString("F2", CultureInfo.InvariantCulture)}).",
                ImportanceScore = score,
                RelatedColumns = new[] { c.ColumnX, c.ColumnY },
                Evidence = new Dictionary<string, string>
                {
                    ["pearson"] = c.Pearson.ToString("F3", CultureInfo.InvariantCulture),
                    ["spearman"] = c.Spearman.ToString("F3", CultureInfo.InvariantCulture),
                    ["sampleSize"] = c.SampleSize.ToString(CultureInfo.InvariantCulture)
                }
            };
        }
    }

    private IEnumerable<Insight> AnomalyInsights(IReadOnlyList<AnomalyResult> anomalies, DatasetProfile profile)
    {
        foreach (var a in anomalies)
        {
            if (a.Anomalies.Count == 0) continue;
            int score = _scorer.ScoreAnomaly(a.MaxMagnitude);
            if (score < 15) continue;

            var peak = a.Anomalies[0];
            yield return new Insight
            {
                Kind = InsightKind.Anomaly,
                Title = $"{a.Measure} has {a.Anomalies.Count} anomaly(ies)",
                Description = $"{a.Measure} contains {a.Anomalies.Count} outlier(s); the most extreme value " +
                              $"{peak.Value.ToString("N2", CultureInfo.InvariantCulture)} is {Math.Abs(peak.ZScore).ToString("F1", CultureInfo.InvariantCulture)} " +
                              $"standard deviations from the mean ({peak.Method}).",
                ImportanceScore = score,
                RelatedColumns = new[] { a.Measure },
                Evidence = new Dictionary<string, string>
                {
                    ["count"] = a.Anomalies.Count.ToString(CultureInfo.InvariantCulture),
                    ["maxZ"] = a.MaxMagnitude.ToString("F2", CultureInfo.InvariantCulture),
                    ["method"] = peak.Method
                }
            };
        }
    }

    private IEnumerable<Insight> DistributionInsights(DatasetProfile profile)
    {
        foreach (var m in profile.Measures)
        {
            if (m.Numeric is null || m.Numeric.Count < 8) continue;
            double skew = m.Numeric.Skewness;
            if (Math.Abs(skew) < 0.8) continue;
            int score = _scorer.ScoreDistribution(Math.Abs(skew));
            if (score < 15) continue;

            string dir = skew > 0 ? "right (a long tail of high values)" : "left (a long tail of low values)";
            yield return new Insight
            {
                Kind = InsightKind.Distribution,
                Title = $"{m.Name} is skewed",
                Description = $"{m.Name} is skewed to the {dir}; the mean " +
                              $"({m.Numeric.Mean.ToString("N2", CultureInfo.InvariantCulture)}) differs from the median " +
                              $"({m.Numeric.Median.ToString("N2", CultureInfo.InvariantCulture)}).",
                ImportanceScore = score,
                RelatedColumns = new[] { m.Name },
                Evidence = new Dictionary<string, string>
                {
                    ["skewness"] = skew.ToString("F2", CultureInfo.InvariantCulture),
                    ["mean"] = m.Numeric.Mean.ToString("F2", CultureInfo.InvariantCulture),
                    ["median"] = m.Numeric.Median.ToString("F2", CultureInfo.InvariantCulture)
                }
            };
        }
    }

    private static string Percent(double value, bool alreadyFraction = false)
    {
        double pct = alreadyFraction ? value : value;
        return Math.Abs(pct).ToString("P0", CultureInfo.InvariantCulture);
    }
}
