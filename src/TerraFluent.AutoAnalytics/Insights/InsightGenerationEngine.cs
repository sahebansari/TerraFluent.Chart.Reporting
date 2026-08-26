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
        insights.AddRange(AnomalyInsights(findings.Anomalies, profile, findings.AnomalyExplanations));
        insights.AddRange(DistributionInsights(profile));
        insights.AddRange(ForecastInsights(findings.Forecasts, profile));
        insights.AddRange(PeriodComparisonInsights(findings.PeriodComparisons));
        insights.AddRange(SegmentationInsights(findings.Segmentation));

        return insights
            .OrderByDescending(i => i.ImportanceScore)
            .ToList();
    }

    private IEnumerable<Insight> ForecastInsights(IReadOnlyList<MeasureForecast> forecasts, DatasetProfile profile)
    {
        foreach (var f in forecasts)
        {
            if (f.IsEmpty) continue;
            var result = f.Forecast;
            int score = _scorer.ScoreForecast(Math.Abs(result.ProjectedChange), f.HistoryValues.Count);
            if (score < 15) continue;

            var last = result.Points[^1];
            string measureDisp = DisplayText.Humanize(f.Measure);
            string direction = result.ProjectedChange >= 0 ? "rise" : "fall";
            string pct = Percent(result.ProjectedChange);
            var role = RoleOf(profile, f.Measure);

            // Band width relative to the projection tells the reader how much to trust it.
            double spread = Math.Abs(last.Upper - last.Lower);
            double reference = Math.Max(Math.Abs(last.Value), 1e-9);
            string confidence = (spread / reference) switch
            {
                <= 0.2 => "The band around that path is tight, so the direction is well supported by the history.",
                <= 0.6 => "The band is moderately wide — treat the direction as sound and the exact figure as indicative.",
                _      => "The band is wide, so read this as a direction of travel rather than a number to plan against."
            };

            string method = result.Method == "holt-winters"
                ? $" A seasonal pattern of {result.SeasonLength} period(s) was detected and carried into the projection."
                : string.Empty;

            yield return new Insight
            {
                Kind = InsightKind.Forecast,
                Title = $"{measureDisp} is projected to {direction} {pct}",
                Description =
                    $"Extending {measureDisp} {result.Points.Count} period(s) beyond the last observed " +
                    $"{DisplayText.FormatNumber(result.LastActual)} points to about " +
                    $"{DisplayText.FormatNumber(last.Value)} " +
                    $"({(result.ProjectedChange >= 0 ? "+" : "-")}{pct}), within a range of " +
                    $"{DisplayText.FormatNumber(last.Lower)}–{DisplayText.FormatNumber(last.Upper)}. " +
                    confidence + method + " " + ForecastImplication(role, up: result.ProjectedChange >= 0),
                ImportanceScore = score,
                RelatedColumns = new[] { f.Measure },
                Evidence = new Dictionary<string, string>
                {
                    ["projectedChange"] = result.ProjectedChange.ToString("F3", CultureInfo.InvariantCulture),
                    ["finalValue"] = last.Value.ToString("F2", CultureInfo.InvariantCulture),
                    ["lower"] = last.Lower.ToString("F2", CultureInfo.InvariantCulture),
                    ["upper"] = last.Upper.ToString("F2", CultureInfo.InvariantCulture),
                    ["method"] = result.Method,
                    ["seasonLength"] = result.SeasonLength.ToString(CultureInfo.InvariantCulture),
                    ["historyPoints"] = f.HistoryValues.Count.ToString(CultureInfo.InvariantCulture)
                }
            };
        }
    }

    private IEnumerable<Insight> PeriodComparisonInsights(IReadOnlyList<PeriodComparisonResult> comparisons)
    {
        foreach (var c in comparisons)
        {
            if (c.IsEmpty) continue;
            int score = _scorer.ScorePeriodChange(Math.Abs(c.LatestChangePct), c.Periods.Count);
            if (score < 15) continue;

            string measureDisp = DisplayText.Humanize(c.Measure);
            string cadence = c.Granularity.ToString().ToLowerInvariant();
            string latest = c.Periods[^1].Label;
            string previous = c.Periods[^2].Label;
            string direction = c.LatestChangePct >= 0 ? "up" : "down";
            string pct = Percent(c.LatestChangePct);

            // Year-over-year is the like-for-like read; call out when it disagrees with the last step.
            string yoy = string.Empty;
            if (c.YearOverYearPct is double y)
            {
                string yoyDir = y >= 0 ? "up" : "down";
                bool diverges = Math.Sign(y) != Math.Sign(c.LatestChangePct);
                yoy = $" Year-over-year, {latest} is {yoyDir} {Percent(y)}." +
                      (diverges
                          ? " That runs counter to the latest period-on-period move, so one of the two is a timing effect rather than a change in the underlying run-rate."
                          : " The two readings agree, which strengthens the signal.");
            }

            yield return new Insight
            {
                Kind = InsightKind.PeriodChange,
                Title = $"{measureDisp} is {direction} {pct} in {latest}",
                Description =
                    $"On a {cadence} basis, {measureDisp} moved from " +
                    $"{DisplayText.FormatNumber(c.Periods[^2].Value)} in {previous} to " +
                    $"{DisplayText.FormatNumber(c.Periods[^1].Value)} in {latest} — " +
                    $"{direction} {pct} ({(c.LatestChangeAbs >= 0 ? "+" : "-")}{DisplayText.FormatNumber(Math.Abs(c.LatestChangeAbs))}) " +
                    $"across {c.Periods.Count} observed periods." + yoy,
                ImportanceScore = score,
                RelatedColumns = new[] { c.Measure },
                Evidence = new Dictionary<string, string>
                {
                    ["latestPeriod"] = latest,
                    ["latestChangePct"] = c.LatestChangePct.ToString("F3", CultureInfo.InvariantCulture),
                    ["latestChangeAbs"] = c.LatestChangeAbs.ToString("F2", CultureInfo.InvariantCulture),
                    ["yearOverYearPct"] = c.YearOverYearPct?.ToString("F3", CultureInfo.InvariantCulture) ?? "n/a",
                    ["periods"] = c.Periods.Count.ToString(CultureInfo.InvariantCulture),
                    ["granularity"] = c.Granularity.ToString()
                }
            };
        }
    }

    private IEnumerable<Insight> SegmentationInsights(SegmentationResult? segmentation)
    {
        if (segmentation is null || segmentation.IsEmpty) yield break;

        var largest = segmentation.Segments.OrderByDescending(s => s.Size).First();
        int score = _scorer.ScoreSegmentation(largest.Share, segmentation.Segments.Count, segmentation.RowsClustered);
        if (score < 15) yield break;

        string measures = string.Join(", ", segmentation.Measures.Select(DisplayText.Humanize));
        string breakdown = string.Join("; ", segmentation.Segments
            .OrderByDescending(s => s.Size)
            .Select(s => $"{s.Label} ({Percent(s.Share)})"));

        // An even split means the measures genuinely separate rows; one giant cluster means they don't.
        string reading = largest.Share >= 0.8
            ? "One group swallows nearly every row, so these measures do not separate the population much — a different combination of measures would likely segment it better."
            : $"The groups are distinct enough to act on: {breakdown}.";

        yield return new Insight
        {
            Kind = InsightKind.Segmentation,
            Title = $"Rows fall into {segmentation.Segments.Count} natural segments",
            Description =
                $"Clustering {segmentation.RowsClustered} rows across {measures} separates them into " +
                $"{segmentation.Segments.Count} groups. {reading} " +
                "Segments are derived from the measures alone, so they describe how rows differ numerically rather than by any label already in the data.",
            ImportanceScore = score,
            RelatedColumns = segmentation.Measures.ToArray(),
            Evidence = new Dictionary<string, string>
            {
                ["segments"] = segmentation.Segments.Count.ToString(CultureInfo.InvariantCulture),
                ["rowsClustered"] = segmentation.RowsClustered.ToString(CultureInfo.InvariantCulture),
                ["largestShare"] = largest.Share.ToString("F3", CultureInfo.InvariantCulture),
                ["largestLabel"] = largest.Label
            }
        };
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
                string share = Percent(top.Share);
                title = $"{top.Key} leads {measureDisp} by {dimDisp}";

                // A concentrated share is both a strength and a dependency; an even field reads differently.
                string reading = top.Share >= 0.50
                    ? $"{top.Key} alone accounts for {share} of all {measureDisp} across {g.Buckets.Count} {dimDisp} groups \u2014 a heavy concentration that is as much a dependency as a strength. If {top.Key} were to soften, the headline {measureDisp} number would feel it directly, so it is worth knowing how deliberate that reliance is."
                    : top.Share >= 0.30
                        ? $"{top.Key} is the biggest contributor to {measureDisp} at {share}, across {g.Buckets.Count} {dimDisp} groups. It leads clearly, but the field is spread widely enough that no single group carries the whole number."
                        : $"{top.Key} edges ahead on {measureDisp} at {share} of the total, though contribution is fairly even across the {g.Buckets.Count} {dimDisp} groups \u2014 there is no dominant player here.";
                if (runnerUp is not null)
                    reading += $" The next-largest, {runnerUp.Key}, follows at {Percent(runnerUp.Share)}.";
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

    private IEnumerable<Insight> AnomalyInsights(
        IReadOnlyList<AnomalyResult> anomalies, DatasetProfile profile,
        IReadOnlyList<AnomalyExplanation> explanations)
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

            var evidence = new Dictionary<string, string>
            {
                ["count"] = a.Anomalies.Count.ToString(CultureInfo.InvariantCulture),
                ["maxZ"] = a.MaxMagnitude.ToString("F2", CultureInfo.InvariantCulture),
                ["method"] = peak.Method
            };

            // Fold the attribution into this insight rather than emitting a second one about the same
            // event: the reader wants one account of the outlier, not a detection and an explanation.
            var explanation = explanations.FirstOrDefault(e =>
                string.Equals(e.Measure, a.Measure, StringComparison.OrdinalIgnoreCase)
                && e.RowIndex == peak.RowIndex);

            string title = $"{measureDisp} has {a.Anomalies.Count} anomaly(ies)";
            string closing =
                "Before it distorts averages or forecasts, it is worth confirming whether this is a genuine event or a data-quality artefact.";

            if (explanation?.BestExplanation is { } best)
            {
                string dimDisp = DisplayText.Humanize(best.Dimension);
                string explainedPct = best.ExplainedFraction.ToString("P0", CultureInfo.InvariantCulture);

                evidence["explainedBy"] = $"{best.Dimension}={best.Category}";
                evidence["explainedFraction"] = best.ExplainedFraction.ToString("F3", CultureInfo.InvariantCulture);
                evidence["categoryMedian"] = best.CategoryMedian.ToString("F2", CultureInfo.InvariantCulture);
                evidence["categoryRows"] = best.CategoryCount.ToString(CultureInfo.InvariantCulture);

                if (explanation.IsExplained)
                {
                    title = $"{measureDisp} spike at {best.Category} is normal for that {dimDisp}";
                    closing =
                        $"That said, it is ordinary for its segment: rows where {dimDisp} is {best.Category} " +
                        $"typically run {DisplayText.FormatNumber(best.CategoryMedian)} across {best.CategoryCount} rows, " +
                        $"which accounts for {explainedPct} of the gap. This is segment mix rather than a genuine outlier \u2014 " +
                        $"compare within {dimDisp} rather than across the whole dataset.";
                }
                else
                {
                    title = $"{measureDisp} has an unexplained anomaly";
                    closing =
                        $"No dimension accounts for it: even against its own {dimDisp} ({best.Category}, which typically runs " +
                        $"{DisplayText.FormatNumber(best.CategoryMedian)}), the value is still " +
                        $"{FormatRatio(best.ResidualRatio)} the norm \u2014 the closest dimension explains only {explainedPct} of the gap. " +
                        "That makes it a genuine one-off event or a data-quality artefact, and worth confirming before it distorts averages or forecasts.";
                }
            }

            yield return new Insight
            {
                Kind = InsightKind.Anomaly,
                Title = title,
                Description =
                    $"{measureDisp} contains {howMany} from the rest of the data. The most extreme, " +
                    $"{DisplayText.FormatNumber(peak.Value)}{where}, sits {peak.Magnitude.ToString("F1", CultureInfo.InvariantCulture)}\u03c3 " +
                    $"from the norm ({peak.Method}) \u2014 {severity} deviation. " + closing,
                ImportanceScore = score,
                RelatedColumns = explanation?.BestExplanation is { } b
                    ? new[] { a.Measure, b.Dimension }
                    : new[] { a.Measure },
                Evidence = evidence
            };
        }
    }

    // "3.4x" reads better than a percentage once a value is a multiple of its norm.
    private static string FormatRatio(double ratio) =>
        double.IsNaN(ratio) || double.IsInfinity(ratio)
            ? "far above"
            : Math.Abs(ratio).ToString("0.#", CultureInfo.InvariantCulture) + "\u00d7";

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

    private static string Percent(double fraction) =>
        Math.Abs(fraction).ToString("P0", CultureInfo.InvariantCulture);

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

    // The forward-looking counterpart of TrendImplication: what a projection means for this role.
    private static string ForecastImplication(SemanticRole role, bool up) => role switch
    {
        SemanticRole.RevenueMetric or SemanticRole.ProfitMetric or SemanticRole.QuantityMetric =>
            up ? "If it holds, plan for the capacity and working capital that growth will demand."
               : "Worth acting on now rather than waiting for the shortfall to land in the actuals.",
        SemanticRole.CostMetric =>
            up ? "Budget for the increase, or find the lever that bends the curve before it lands."
               : "That relief should show up in margin, provided volumes hold.",
        _ =>
            up ? "Confirm it against the next period's actuals before committing to it."
               : "Confirm it against the next period's actuals before treating it as settled."
    };

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
