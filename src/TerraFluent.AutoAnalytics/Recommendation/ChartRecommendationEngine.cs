using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Schema;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

namespace TerraFluent.AutoAnalytics.Recommendation;

/// <summary>
/// Phase 7 — deterministic, rule-based chart selection. Each rule inspects the profile and analytics
/// findings, and when it fires it emits a <see cref="RecommendedChart"/> carrying a suitability score
/// and a self-explaining reason. Results are sorted by descending score.
/// </summary>
public sealed class ChartRecommendationEngine
{
    private readonly int _maxCategories;

    // Above this many periods a time-series line is only kept when it carries real temporal signal;
    // at or below it, the series is short enough to read at a glance so it is always charted.
    private const int DenseSeriesThreshold = 24;

    public ChartRecommendationEngine(int maxCategories = 12)
        => _maxCategories = maxCategories;

    /// <summary>Produces ranked chart recommendations.</summary>
    public IReadOnlyList<RecommendedChart> Recommend(DatasetProfile profile, AnalyticsFindings findings)
    {
        var recs = new List<RecommendedChart>();
        recs.AddRange(TimeSeries(profile, findings));
        recs.AddRange(CategoryComparison(findings));
        recs.AddRange(ShareAnalysis(findings));
        recs.AddRange(CorrelationCharts(profile, findings));
        recs.AddRange(Distribution(profile));
        recs.AddRange(SingleMetric(profile));

        return recs
            .OrderByDescending(r => r.SuitabilityScore)
            .ToList();
    }

    // ── Time series: date dimension + measure => Line / Area ──────────────────
    private IEnumerable<RecommendedChart> TimeSeries(DatasetProfile profile, AnalyticsFindings findings)
    {
        var date = profile.DateColumns.FirstOrDefault(d => d.Dates.Count >= 3);
        if (date is null) yield break;

        foreach (var measure in profile.Measures)
        {
            // Collapse to one point per calendar period (summed if additive, else averaged) so the
            // chart shows a readable trend line instead of one overlapping tick/point per raw row.
            var gran     = date.Date?.Granularity ?? DateGranularity.Daily;
            bool additive = MeasureSemantics.IsAdditive(measure.Profile, measure.Numeric?.Min, measure.Numeric?.Max);
            var ordered   = PeriodAggregator.AggregateLabeled(measure, date, gran, additive);
            int n = ordered.Count;
            if (n < 3) continue;

            var trend = findings.Trends.FirstOrDefault(t => t.Measure == measure.Name);

            // A time-series line only tells a story when time actually explains the measure. Once a
            // series is dense, a flat cloud with no trend, season or period-to-period persistence
            // (e.g. a random per-record attribute plotted over a transaction date) is just noise —
            // skip it so the genuinely informative category/distribution charts surface instead.
            if (n > DenseSeriesThreshold && (trend is null || !trend.HasTemporalSignal))
                continue;

            var labels = ordered.Select(p => FormatDate(p.Period, gran)).ToList();
            var values = ordered.Select(p => (double?)p.Value).ToList();

            int score = 78 + (trend is { RSquared: >= 0.5 } ? 15 : 0) + (n >= 12 ? 5 : 0);
            score = Math.Min(score, 98);

            string trendNote = trend?.Kind switch
            {
                TrendKind.Rising => " A clear upward trend was detected.",
                TrendKind.Declining => " A clear downward trend was detected.",
                TrendKind.Seasonal => " A seasonal pattern was detected.",
                TrendKind.Volatile => " The series is volatile.",
                _ => string.Empty
            };

            string measureDisp = DisplayText.Humanize(measure.Name);
            string dateDisp    = DisplayText.Humanize(date.Name);
            // Non-additive attributes are averaged per period, so label them as such.
            string metricLabel = additive ? measureDisp : $"Average {measureDisp}";

            yield return new RecommendedChart
            {
                ChartType = ChartType.Line,
                SuitabilityScore = score,
                Reason = $"Time-based dataset detected ({n} {dateDisp} observations). " +
                         $"Line chart best shows how {metricLabel} changes over time.{trendNote}",
                Spec = new ChartSpec
                {
                    Type = ChartType.Line,
                    Title = $"{metricLabel} over {dateDisp}",
                    XAxisTitle = dateDisp,
                    YAxisTitle = metricLabel,
                    Categories = labels,
                    Series = new[] { new SeriesSpec { Name = metricLabel, Values = values } }
                }
            };
        }
    }

    // ── Category comparison: dimension + measure => Column / Bar ───────────────
    private IEnumerable<RecommendedChart> CategoryComparison(AnalyticsFindings findings)
    {
        foreach (var g in findings.Groups)
        {
            int count = g.Buckets.Count;
            if (count is < 2 or > 60) continue;

            var top = g.Buckets.Take(_maxCategories).ToList();
            var labels = top.Select(b => b.Key).ToList();
            var values = top.Select(b => (double?)b.Value).ToList();

            bool useBar = labels.Any(l => l.Length > 12) || count > 8;
            var type = useBar ? ChartType.Bar : ChartType.Column;
            int score = 72 + (count is >= 3 and <= 10 ? 12 : 0);

            string measureDisp = DisplayText.Humanize(g.Measure);
            string dimDisp     = DisplayText.Humanize(g.Dimension);
            // Non-additive measures are averaged per group, so label them as such.
            string metricLabel = g.IsAdditive ? measureDisp : $"Average {measureDisp}";

            yield return new RecommendedChart
            {
                ChartType = type,
                SuitabilityScore = Math.Min(score, 92),
                Reason = $"{metricLabel} compared across {count} '{dimDisp}' categories. " +
                         $"{(useBar ? "A horizontal bar" : "A column")} chart ranks the categories clearly.",
                Spec = new ChartSpec
                {
                    Type = type,
                    Title = $"{metricLabel} by {dimDisp}",
                    XAxisTitle = dimDisp,
                    YAxisTitle = metricLabel,
                    Categories = labels,
                    Series = new[] { new SeriesSpec { Name = metricLabel, Values = values } }
                }
            };
        }
    }

    // ── Share analysis: few categories => Pie ─────────────────────────────────
    private IEnumerable<RecommendedChart> ShareAnalysis(AnalyticsFindings findings)
    {
        foreach (var g in findings.Groups)
        {
            // A part-to-whole (share of total) view only makes sense for additive measures —
            // averaging attributes like age has no meaningful "total" to take a share of.
            if (!g.IsAdditive) continue;

            int count = g.Buckets.Count;
            if (count is < 2 or > 6) continue;
            if (g.Buckets.Any(b => b.Value < 0)) continue; // pies require non-negative parts

            var labels = g.Buckets.Select(b => b.Key).ToList();
            var values = g.Buckets.Select(b => (double?)b.Value).ToList();
            int score = 68 + (count <= 4 ? 10 : 0);
            string measureDisp = DisplayText.Humanize(g.Measure);
            string dimDisp     = DisplayText.Humanize(g.Dimension);

            yield return new RecommendedChart
            {
                ChartType = ChartType.Pie,
                SuitabilityScore = Math.Min(score, 88),
                Reason = $"Only {count} '{dimDisp}' categories with a part-to-whole relationship — " +
                         $"a pie chart highlights each category's share of total {measureDisp}.",
                Spec = new ChartSpec
                {
                    Type = ChartType.Pie,
                    Title = $"{measureDisp} share by {dimDisp}",
                    Categories = labels,
                    Series = new[] { new SeriesSpec { Name = measureDisp, Values = values } }
                }
            };
        }
    }

    // ── Correlation: scatter for small samples, binned mean-trend for large ───
    private IEnumerable<RecommendedChart> CorrelationCharts(DatasetProfile profile, AnalyticsFindings findings)
    {
        foreach (var c in findings.Correlations)
        {
            if (c.Strength is CorrelationStrength.None or CorrelationStrength.Weak) continue;
            var x = profile.ByName(c.ColumnX);
            var y = profile.ByName(c.ColumnY);
            if (x is null || y is null) continue;

            // Pair the measures by ORIGINAL ROW (complete-case) — the same rows the correlation
            // coefficient was computed from — so the binned trend can't be built on misaligned data.
            var (xs, ys) = RowAlignment.NumericPairs(x, y);
            int n = xs.Count;
            if (n < 3) continue;

            int score = 70 + c.Strength switch
            {
                CorrelationStrength.VeryStrong => 18,
                CorrelationStrength.Strong => 10,
                _ => 4
            };
            // Use the stronger of the linear/monotonic coefficients to describe the relationship.
            bool useSpearman = Math.Abs(c.Spearman) > Math.Abs(c.Pearson);
            double dominant  = useSpearman ? c.Spearman : c.Pearson;
            string direction = dominant < 0 ? "negative" : "positive";
            string rValue    = dominant.ToString("F2", CultureInfo.InvariantCulture);
            string xDisp     = DisplayText.Humanize(c.ColumnX);
            string yDisp     = DisplayText.Humanize(c.ColumnY);

            // Two numeric measures have no natural category axis, and the renderer positions
            // scatter points by category index (not by their real X value) — so a raw point-per-
            // value scatter spaces unequal X's evenly and misrepresents the relationship. Instead
            // bin X into equal-width bands and plot the mean of Y per band: the bands ARE evenly
            // spaced, so the trend (and its direction) reads correctly at any sample size.
            var (bandLabels, bandMeans) = BinnedMean(xs, ys, n);
            if (bandLabels.Count < 2) continue;

            string reason = $"{char.ToUpperInvariant(direction[0])}{direction.Substring(1)} correlation " +
                            $"(r = {rValue}) between {xDisp} and {yDisp}. {yDisp} is averaged across " +
                            $"{bandLabels.Count} equal-width {xDisp} bands to show the relationship clearly.";

            yield return new RecommendedChart
            {
                ChartType = ChartType.Line,
                SuitabilityScore = Math.Min(score, 94),
                Reason = reason,
                Spec = new ChartSpec
                {
                    Type = ChartType.Line,
                    Title = $"Average {yDisp} by {xDisp}",
                    XAxisTitle = xDisp,
                    YAxisTitle = $"Average {yDisp}",
                    Categories = bandLabels,
                    Series = new[] { new SeriesSpec { Name = $"Avg {yDisp}", Values = bandMeans } }
                }
            };
        }
    }

    // Bins X into equal-width bands and returns the mean of Y within each non-empty band,
    // turning a large point cloud into a compact, readable trend.
    private static (List<string> labels, List<double?> means) BinnedMean(
        IReadOnlyList<double> xs, IReadOnlyList<double> ys, int n)
    {
        double min = double.MaxValue, max = double.MinValue;
        for (int i = 0; i < n; i++)
        {
            double v = xs[i];
            if (v < min) min = v;
            if (v > max) max = v;
        }
        if (max <= min) return (new List<string>(), new List<double?>());

        // Target ≈ √n bands, clamped to a sensible range. No hard floor of 6 so small samples
        // (e.g. 10–24 points) form a few well-populated bands instead of many sparse ones.
        int bins = Math.Max(3, Math.Min(15, (int)Math.Ceiling(Math.Sqrt(n))));
        // Whole-number fields (years, age, counts) get integer-aligned bands so the x-axis reads as
        // coherent integers (0–3, 3–5, …) instead of 2.6–5.2. Clamp bins so band width stays >= 1.
        bool integralX = IsIntegerValued(xs, n);
        if (integralX)
        {
            int span = (int)Math.Round(max - min);
            if (span >= 2) bins = Math.Min(bins, span);
        }
        double width = (max - min) / bins;
        var sum = new double[bins];
        var count = new int[bins];
        for (int i = 0; i < n; i++)
        {
            int idx = (int)((xs[i] - min) / width);
            if (idx >= bins) idx = bins - 1;
            if (idx < 0) idx = 0;
            sum[idx] += ys[i];
            count[idx]++;
        }

        var labels = new List<string>(bins);
        var means = new List<double?>(bins);
        for (int b = 0; b < bins; b++)
        {
            if (count[b] == 0) continue; // drop empty bands so the trend stays continuous
            double lo = min + b * width, hi = lo + width;
            labels.Add($"{FormatBound(lo, integralX)}\u2013{FormatBound(hi, integralX)}");
            means.Add(sum[b] / count[b]);
        }
        return (labels, means);
    }

    // ── Distribution: single measure => histogram (binned column) ─────────────
    private IEnumerable<RecommendedChart> Distribution(DatasetProfile profile)
    {
        foreach (var m in profile.Measures)
        {
            if (m.Numeric is null || m.NumericValues.Count < 8) continue;

            var (labels, counts) = Histogram(m.NumericValues, m.Numeric.Min, m.Numeric.Max);
            if (labels.Count == 0) continue;

            int score = 60 + (Math.Abs(m.Numeric.Skewness) > 0.8 ? 12 : 0);
            string measureDisp = DisplayText.Humanize(m.Name);
            yield return new RecommendedChart
            {
                ChartType = ChartType.Column,
                SuitabilityScore = Math.Min(score, 82),
                Reason = $"Distribution analysis of {measureDisp}: values grouped into {labels.Count} bins " +
                         $"shows how the measure is spread.",
                Spec = new ChartSpec
                {
                    Type = ChartType.Column,
                    Title = $"Distribution of {measureDisp}",
                    XAxisTitle = measureDisp,
                    YAxisTitle = "Frequency",
                    Categories = labels,
                    Series = new[] { new SeriesSpec { Name = "Frequency", Values = counts.Select(c => (double?)c).ToList() } }
                }
            };
        }
    }

    // ── Single metric: lone measure, no dimension => Gauge / DataRing ─────────
    private IEnumerable<RecommendedChart> SingleMetric(DatasetProfile profile)
    {
        var measures = profile.Measures.ToList();
        bool noDimensions = !profile.Categories.Any() && !profile.DateColumns.Any();
        if (!noDimensions || measures.Count != 1) yield break;

        var m = measures[0];
        if (m.Numeric is null) yield break;
        string measureDisp = DisplayText.Humanize(m.Name);

        yield return new RecommendedChart
        {
            ChartType = ChartType.Gauge,
            SuitabilityScore = 74,
            Reason = $"A single numeric measure ({measureDisp}) with no grouping dimension — " +
                     $"a gauge communicates the current value against its range.",
            Spec = new ChartSpec
            {
                Type = ChartType.Gauge,
                Title = measureDisp,
                AxisMin = Math.Min(0, m.Numeric.Min),
                AxisMax = m.Numeric.Max,
                Series = new[] { new SeriesSpec { Name = measureDisp, ScalarValue = m.Numeric.Mean } }
            }
        };
    }

    private static (List<string> labels, List<int> counts) Histogram(IReadOnlyList<double> values, double min, double max)
    {
        if (max <= min) return (new List<string>(), new List<int>());
        int bins = Math.Max(5, Math.Min(12, (int)Math.Ceiling(Math.Sqrt(values.Count))));
        bool integral = IsIntegerValued(values, values.Count);
        if (integral)
        {
            int span = (int)Math.Round(max - min);
            if (span >= 2) bins = Math.Min(bins, span);
        }
        double width = (max - min) / bins;
        var counts = new int[bins];
        foreach (var v in values)
        {
            int idx = (int)((v - min) / width);
            if (idx >= bins) idx = bins - 1;
            if (idx < 0) idx = 0;
            counts[idx]++;
        }
        var labels = new List<string>(bins);
        for (int i = 0; i < bins; i++)
        {
            double lo = min + i * width, hi = lo + width;
            labels.Add($"{FormatBound(lo, integral)}–{FormatBound(hi, integral)}");
        }
        return (labels, counts.ToList());
    }

    private static bool IsIntegerValued(IReadOnlyList<double> values, int n)
    {
        for (int i = 0; i < n; i++)
            if (values[i] != Math.Floor(values[i])) return false;
        return true;
    }

    // Rounds a bin boundary to a whole number for integer-valued fields before compact-formatting.
    private static string FormatBound(double value, bool integral) =>
        FormatCompact(integral ? Math.Round(value, MidpointRounding.AwayFromZero) : value);

    // Compact, human-readable numeric formatting (avoids "G3"/"G4" scientific notation like 4.77E+04).
    private static string FormatCompact(double value)
    {
        double abs = Math.Abs(value);
        return abs switch
        {
            >= 1_000_000_000 => (value / 1_000_000_000).ToString("0.##", CultureInfo.InvariantCulture) + "B",
            >= 1_000_000     => (value / 1_000_000).ToString("0.##", CultureInfo.InvariantCulture) + "M",
            >= 1_000         => (value / 1_000).ToString("0.##", CultureInfo.InvariantCulture) + "k",
            >= 1             => value.ToString("0.##", CultureInfo.InvariantCulture),
            _                => value.ToString("0.###", CultureInfo.InvariantCulture)
        };
    }

    private static string FormatDate(DateTime d, DateGranularity g) => g switch
    {
        DateGranularity.Yearly => d.ToString("yyyy", CultureInfo.InvariantCulture),
        DateGranularity.Quarterly => $"Q{(d.Month - 1) / 3 + 1} {d.Year}",
        DateGranularity.Monthly => d.ToString("MMM yyyy", CultureInfo.InvariantCulture),
        _ => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
    };
}
