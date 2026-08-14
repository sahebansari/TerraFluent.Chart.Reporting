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
        // A directional read of each measure, so a narrative can connect signals the way an analyst
        // would (e.g. a correlation whose two measures are both trending).
        var trendByMeasure = findings.Trends
            .Where(t => t.Kind is not TrendKind.Unknown)
            .GroupBy(t => t.Measure, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var insights = new List<Insight>();
        insights.AddRange(TrendInsights(findings.Trends, profile));
        insights.AddRange(DominanceInsights(findings.Groups));
        insights.AddRange(CorrelationInsights(findings.Correlations, trendByMeasure));
        insights.AddRange(AnomalyInsights(findings.Anomalies, profile));
        insights.AddRange(DistributionInsights(profile));

        return insights
            .OrderByDescending(i => i.ImportanceScore)
            .ToList();
    }

    private IEnumerable<Insight> TrendInsights(IReadOnlyList<TrendResult> trends, DatasetProfile profile)
    {
        foreach (var t in trends)
        {
            if (t.Kind is TrendKind.Unknown) continue;
            int score = _scorer.ScoreTrend(Math.Abs(t.GrowthRate), t.RSquared, t.PointCount);
            if (score < 10) continue;

            string pct = Percent(t.GrowthRate);
            string overPeriod = t.OrderedBy is null ? "over the observed sequence" : $"over the observed {DisplayText.Humanize(t.OrderedBy)} period";
            var role = RoleOf(profile, t.Measure);
            string measureDisp = DisplayText.Humanize(t.Measure);

            string title = t.Kind switch
            {
                TrendKind.Rising    => $"{measureDisp} is rising ({pct})",
                TrendKind.Declining => $"{measureDisp} is declining ({pct})",
                TrendKind.Volatile  => $"{measureDisp} is volatile",
                TrendKind.Seasonal  => $"{measureDisp} shows a seasonal pattern",
                _                   => $"{measureDisp} is stable"
            };

            string desc = t.Kind switch
            {
                TrendKind.Rising =>
                    $"{measureDisp} has climbed {pct} {overPeriod} \u2014 a {Magnitude(t.GrowthRate)} upward move {TrendConfidence(t)}. " +
                    TrendImplication(role, up: true),
                TrendKind.Declining =>
                    $"{measureDisp} has fallen {pct} {overPeriod} \u2014 a {Magnitude(t.GrowthRate)} decline {TrendConfidence(t)}. " +
                    TrendImplication(role, up: false),
                TrendKind.Volatile =>
                    $"{measureDisp} swings widely with no consistent direction (volatility {t.Volatility:P0}). " +
                    "Averages will mislead here \u2014 smoothing the series or root-causing the peaks and troughs would make it far easier to plan around.",
                TrendKind.Seasonal =>
                    $"{measureDisp} follows a repeating seasonal rhythm {overPeriod}. " +
                    "Like-for-like (year-over-year) comparisons will tell you more than period-to-period moves, and staffing or inventory can be timed to the cycle.",
                _ =>
                    $"{measureDisp} has held broadly flat {overPeriod} \u2014 stability that is itself worth noting, since it can be planned around with confidence."
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

            var runnerUp = g.Buckets.Count > 1 ? g.Buckets[1] : null;
            string measureDisp = DisplayText.Humanize(g.Measure);
            string dimDisp     = DisplayText.Humanize(g.Dimension);
            string title;
            string description;
            Dictionary<string, string> evidence;
            if (g.IsAdditive)
            {
                string share = Percent(top.Share, alreadyFraction: true);
                title = $"{top.Key} leads {measureDisp} by {dimDisp}";

                // A concentrated share is both a strength and a dependency; an even field reads differently.
                string reading = top.Share >= 0.50
                    ? $"{top.Key} alone accounts for {share} of all {measureDisp} across {g.Buckets.Count} {dimDisp} groups \u2014 a heavy concentration that is as much a dependency as a strength. If {top.Key} were to soften, the headline {measureDisp} number would feel it directly, so it is worth knowing how deliberate that reliance is."
                    : top.Share >= 0.30
                        ? $"{top.Key} is the biggest contributor to {measureDisp} at {share}, across {g.Buckets.Count} {dimDisp} groups. It leads clearly, but the field is spread widely enough that no single group carries the whole number."
                        : $"{top.Key} edges ahead on {measureDisp} at {share} of the total, though contribution is fairly even across the {g.Buckets.Count} {dimDisp} groups \u2014 there is no dominant player here.";
                if (runnerUp is not null)
                    reading += $" The next-largest, {runnerUp.Key}, follows at {Percent(runnerUp.Share, alreadyFraction: true)}.";
                description = reading;
                evidence = new Dictionary<string, string>
                {
                    ["topCategory"] = top.Key,
                    ["topShare"] = top.Share.ToString("F3", CultureInfo.InvariantCulture),
                    ["groups"] = g.Buckets.Count.ToString(CultureInfo.InvariantCulture),
                    ["total"] = g.Total.ToString("F2", CultureInfo.InvariantCulture)
                };
            }
            else
            {
                title = $"{top.Key} has the highest average {measureDisp}";
                string gap = runnerUp is not null
                    ? $", ahead of {runnerUp.Key} at {DisplayText.FormatNumber(runnerUp.Value)}"
                    : string.Empty;
                description =
                    $"Across {g.Buckets.Count} {dimDisp} groups, {top.Key} shows the highest average {measureDisp} " +
                    $"at {DisplayText.FormatNumber(top.Value)}{gap}. " +
                    "As this is an average rather than a total, it reflects a difference in the underlying profile of each group \u2014 a lead worth understanding rather than simply celebrating.";
                evidence = new Dictionary<string, string>
                {
                    ["topCategory"] = top.Key,
                    ["topAverage"] = top.Value.ToString("F2", CultureInfo.InvariantCulture),
                    ["groups"] = g.Buckets.Count.ToString(CultureInfo.InvariantCulture)
                };
            }

            yield return new Insight
            {
                Kind = InsightKind.Dominance,
                Title = title,
                Description = description,
                ImportanceScore = score,
                RelatedColumns = new[] { g.Dimension, g.Measure },
                Evidence = evidence
            };
        }
    }

    private IEnumerable<Insight> CorrelationInsights(
        IReadOnlyList<CorrelationResult> correlations,
        IReadOnlyDictionary<string, TrendResult> trendByMeasure)
    {
        foreach (var c in correlations)
        {
            if (c.Strength is CorrelationStrength.None or CorrelationStrength.Weak) continue;
            int score = _scorer.ScoreCorrelation(Math.Abs(c.Pearson), c.SampleSize);
            if (score < 15) continue;

            string strength = c.Strength.ToString().ToLowerInvariant().Replace("very", "very ");
            string r = c.Pearson.ToString("F2", CultureInfo.InvariantCulture);
            string xDisp = DisplayText.Humanize(c.ColumnX);
            string yDisp = DisplayText.Humanize(c.ColumnY);
            string lead = c.IsNegative
                ? $"{xDisp} and {yDisp} tend to move in opposite directions (a {strength} inverse relationship, r = {r}) \u2014 gains in one coincide with pullbacks in the other."
                : $"{xDisp} and {yDisp} move together (a {strength} relationship, r = {r}) \u2014 as one rises, the other tends to follow.";

            // Uncertainty: significance test first (is it distinguishable from noise?), then sample size.
            string caveat;
            if (!c.IsSignificant)
                caveat = $" However, at n = {c.SampleSize} this is not statistically significant (p = {c.PValue.ToString("F2", CultureInfo.InvariantCulture)}), so it may well be noise \u2014 gather more data before relying on it.";
            else if (c.SampleSize < 8)
                caveat = $" It is statistically significant (p = {c.PValue.ToString("F3", CultureInfo.InvariantCulture)}) but rests on only {c.SampleSize} paired observations, so treat it as suggestive rather than settled.";
            else
                caveat = $" The relationship is statistically significant (p = {c.PValue.ToString("F3", CultureInfo.InvariantCulture)}).";
            string shape = Math.Abs(c.Pearson - c.Spearman) > 0.2
                ? " The link looks non-linear \u2014 the rank ordering agrees more strongly than a straight-line fit, so expect accelerating or diminishing returns rather than a constant ratio."
                : string.Empty;

            // Connect to momentum: if both sides are trending, the relationship is more actionable.
            string momentum = string.Empty;
            if (trendByMeasure.TryGetValue(c.ColumnX, out var tx) && trendByMeasure.TryGetValue(c.ColumnY, out var ty)
                && tx.Kind is TrendKind.Rising or TrendKind.Declining
                && ty.Kind is TrendKind.Rising or TrendKind.Declining)
            {
                momentum = tx.Kind == ty.Kind
                    ? $" Both are currently {(tx.Kind == TrendKind.Rising ? "rising" : "falling")}, which reinforces the connection and makes {xDisp} a plausible early read on {yDisp}."
                    : " They are trending in opposite directions right now, however, so the historical link may be weakening \u2014 worth watching.";
            }

            string causation = " Keep in mind this is association rather than proof of cause; a shared driver could sit behind both.";

            yield return new Insight
            {
                Kind = InsightKind.Correlation,
                Title = $"{xDisp} and {yDisp} are correlated ({c.Pearson:F2})",
                Description = lead + caveat + shape + momentum + causation,
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
            string measureDisp = DisplayText.Humanize(a.Measure);
            string severity = a.MaxMagnitude >= 5 ? "an extreme" : a.MaxMagnitude >= 3.5 ? "a pronounced" : a.MaxMagnitude >= 2.5 ? "a clear" : "a modest";
            string where = string.IsNullOrWhiteSpace(peak.Label) ? string.Empty : $" (at {peak.Label})";
            string howMany = a.Anomalies.Count == 1 ? "one value that stands well apart" : $"{a.Anomalies.Count} values that stand well apart";
            yield return new Insight
            {
                Kind = InsightKind.Anomaly,
                Title = $"{measureDisp} has {a.Anomalies.Count} anomaly(ies)",
                Description =
                    $"{measureDisp} contains {howMany} from the rest of the data. The most extreme, " +
                    $"{DisplayText.FormatNumber(peak.Value)}{where}, sits {Math.Abs(peak.ZScore).ToString("F1", CultureInfo.InvariantCulture)}\u03c3 " +
                    $"from the norm ({peak.Method}) \u2014 {severity} deviation. Before it distorts averages or forecasts, it is worth confirming whether this is a genuine event or a data-quality artefact.",
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

            bool right = skew > 0;
            string dir = right ? "right, with a long tail of unusually high values" : "left, with a long tail of unusually low values";
            string overUnder = right ? "overstate" : "understate";
            string measureDisp = DisplayText.Humanize(m.Name);
            yield return new Insight
            {
                Kind = InsightKind.Distribution,
                Title = $"{measureDisp} is skewed",
                Description =
                    $"{measureDisp} is not evenly spread \u2014 it leans {dir}. Because of that skew, the mean " +
                    $"({DisplayText.FormatNumber(m.Numeric.Mean)}) is pulled away from the median " +
                    $"({DisplayText.FormatNumber(m.Numeric.Median)}), so planning off the average would {overUnder} the typical case \u2014 the median is the safer number to reason with here.",
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

    // ── Analyst-voice helpers ────────────────────────────────────────────────────

    private static SemanticRole RoleOf(DatasetProfile profile, string measure)
        => profile.ByName(measure)?.Profile.Role ?? SemanticRole.Unknown;

    private static string Magnitude(double fraction) => Math.Abs(fraction) switch
    {
        >= 1.0  => "dramatic",
        >= 0.5  => "steep",
        >= 0.2  => "strong",
        >= 0.08 => "moderate",
        _       => "modest"
    };

    // Confidence hedging grounded in fit quality, sample size and noise.
    private static string TrendConfidence(TrendResult t)
    {
        if (t.PointCount < 4) return "though this rests on only a handful of points, so read it as directional";
        if (t.RSquared >= 0.85) return "and the pattern is remarkably consistent";
        if (t.RSquared >= 0.5)  return "with a fairly steady progression";
        return "though the path is choppy, so trust the direction more than the exact figure";
    }

    // The \"so what\": tailor the implication to the measure's business role.
    private static string TrendImplication(SemanticRole role, bool up) => role switch
    {
        SemanticRole.RevenueMetric or SemanticRole.ProfitMetric or SemanticRole.QuantityMetric =>
            up ? "That is momentum worth protecting \u2014 now is the time to understand what is driving it and whether capacity can keep pace."
               : "That erosion deserves attention before it compounds; isolating which segments are slipping is the natural next step.",
        SemanticRole.CostMetric =>
            up ? "Rising costs will squeeze margins unless matched by higher output or pricing."
               : "Falling costs are a tailwind for margins, provided quality and service levels hold.",
        _ =>
            up ? "It is an encouraging signal \u2014 worth confirming it holds over the next few periods before banking on it."
               : "Worth pinning down the driver before it hardens into a trend."
    };
}
