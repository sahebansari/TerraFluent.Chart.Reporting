using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;
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
            int n = Math.Min(date.Dates.Count, measure.NumericValues.Count);
            if (n < 3) continue;

            var ordered = Enumerable.Range(0, n)
                .OrderBy(i => date.Dates[i])
                .ToList();
            var labels = ordered.Select(i => FormatDate(date.Dates[i], date.Date?.Granularity ?? DateGranularity.Daily)).ToList();
            var values = ordered.Select(i => (double?)measure.NumericValues[i]).ToList();

            var trend = findings.Trends.FirstOrDefault(t => t.Measure == measure.Name);
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

            yield return new RecommendedChart
            {
                ChartType = ChartType.Line,
                SuitabilityScore = score,
                Reason = $"Time-based dataset detected ({n} {date.Name} observations). " +
                         $"Line chart best shows how {measure.Name} changes over time.{trendNote}",
                Spec = new ChartSpec
                {
                    Type = ChartType.Line,
                    Title = $"{measure.Name} over {date.Name}",
                    Categories = labels,
                    Series = new[] { new SeriesSpec { Name = measure.Name, Values = values } }
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

            yield return new RecommendedChart
            {
                ChartType = type,
                SuitabilityScore = Math.Min(score, 92),
                Reason = $"{g.Measure} compared across {count} '{g.Dimension}' categories. " +
                         $"{(useBar ? "A horizontal bar" : "A column")} chart ranks the categories clearly.",
                Spec = new ChartSpec
                {
                    Type = type,
                    Title = $"{g.Measure} by {g.Dimension}",
                    Categories = labels,
                    Series = new[] { new SeriesSpec { Name = g.Measure, Values = values } }
                }
            };
        }
    }

    // ── Share analysis: few categories => Pie ─────────────────────────────────
    private IEnumerable<RecommendedChart> ShareAnalysis(AnalyticsFindings findings)
    {
        foreach (var g in findings.Groups)
        {
            int count = g.Buckets.Count;
            if (count is < 2 or > 6) continue;
            if (g.Buckets.Any(b => b.Value < 0)) continue; // pies require non-negative parts

            var labels = g.Buckets.Select(b => b.Key).ToList();
            var values = g.Buckets.Select(b => (double?)b.Value).ToList();
            int score = 68 + (count <= 4 ? 10 : 0);

            yield return new RecommendedChart
            {
                ChartType = ChartType.Pie,
                SuitabilityScore = Math.Min(score, 88),
                Reason = $"Only {count} '{g.Dimension}' categories with a part-to-whole relationship — " +
                         $"a pie chart highlights each category's share of total {g.Measure}.",
                Spec = new ChartSpec
                {
                    Type = ChartType.Pie,
                    Title = $"{g.Measure} share by {g.Dimension}",
                    Categories = labels,
                    Series = new[] { new SeriesSpec { Name = g.Measure, Values = values } }
                }
            };
        }
    }

    // ── Correlation: two measures => Scatter ──────────────────────────────────
    private IEnumerable<RecommendedChart> CorrelationCharts(DatasetProfile profile, AnalyticsFindings findings)
    {
        foreach (var c in findings.Correlations)
        {
            if (c.Strength is CorrelationStrength.None or CorrelationStrength.Weak) continue;
            var x = profile.ByName(c.ColumnX);
            var y = profile.ByName(c.ColumnY);
            if (x is null || y is null) continue;

            int n = Math.Min(x.NumericValues.Count, y.NumericValues.Count);
            if (n < 3) continue;

            // Order by X so the scatter reveals the relationship shape.
            var order = Enumerable.Range(0, n).OrderBy(i => x.NumericValues[i]).ToList();
            var labels = order.Select(i => x.NumericValues[i].ToString("G4", CultureInfo.InvariantCulture)).ToList();
            var values = order.Select(i => (double?)y.NumericValues[i]).ToList();

            int score = 70 + c.Strength switch
            {
                CorrelationStrength.VeryStrong => 18,
                CorrelationStrength.Strong => 10,
                _ => 4
            };

            yield return new RecommendedChart
            {
                ChartType = ChartType.Scatter,
                SuitabilityScore = Math.Min(score, 94),
                Reason = $"Strong {(c.IsNegative ? "negative" : "positive")} correlation " +
                         $"(r = {c.Pearson.ToString("F2", CultureInfo.InvariantCulture)}) between {c.ColumnX} and {c.ColumnY}. " +
                         $"A scatter plot reveals the relationship.",
                Spec = new ChartSpec
                {
                    Type = ChartType.Scatter,
                    Title = $"{c.ColumnY} vs {c.ColumnX}",
                    Categories = labels,
                    Series = new[] { new SeriesSpec { Name = c.ColumnY, Values = values } }
                }
            };
        }
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
            yield return new RecommendedChart
            {
                ChartType = ChartType.Column,
                SuitabilityScore = Math.Min(score, 82),
                Reason = $"Distribution analysis of {m.Name}: values grouped into {labels.Count} bins " +
                         $"shows how the measure is spread.",
                Spec = new ChartSpec
                {
                    Type = ChartType.Column,
                    Title = $"Distribution of {m.Name}",
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

        yield return new RecommendedChart
        {
            ChartType = ChartType.Gauge,
            SuitabilityScore = 74,
            Reason = $"A single numeric measure ({m.Name}) with no grouping dimension — " +
                     $"a gauge communicates the current value against its range.",
            Spec = new ChartSpec
            {
                Type = ChartType.Gauge,
                Title = m.Name,
                AxisMin = Math.Min(0, m.Numeric.Min),
                AxisMax = m.Numeric.Max,
                Series = new[] { new SeriesSpec { Name = m.Name, ScalarValue = m.Numeric.Mean } }
            }
        };
    }

    private static (List<string> labels, List<int> counts) Histogram(IReadOnlyList<double> values, double min, double max)
    {
        if (max <= min) return (new List<string>(), new List<int>());
        int bins = Math.Max(5, Math.Min(12, (int)Math.Ceiling(Math.Sqrt(values.Count))));
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
            labels.Add($"{FormatBinBound(lo)}–{FormatBinBound(hi)}");
        }
        return (labels, counts.ToList());
    }

    // Compact, human-readable bin-bound formatting (avoids "G3" scientific notation like 4.77E+04).
    private static string FormatBinBound(double value)
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
