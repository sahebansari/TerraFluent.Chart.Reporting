using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Schema;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;
using Stacking = TerraFluent.Chart.Reporting.Enums.Stacking;

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
        recs.AddRange(SmoothTrendCharts(findings));
        recs.AddRange(CumulativeCharts(findings));
        recs.AddRange(CompositionCharts(findings));
        recs.AddRange(ForecastCharts(findings));
        recs.AddRange(PeriodBridgeCharts(findings));
        recs.AddRange(SegmentCharts(findings));
        recs.AddRange(CompositionHeatmaps(findings));
        recs.AddRange(SpreadByCategory(profile));
        recs.AddRange(AnomalyContextCharts(findings));

        return recs
            .OrderByDescending(r => r.SuitabilityScore)
            .ToList();
    }

    // ── Anomaly in context: the outlier against each candidate segment's norm ──
    private IEnumerable<RecommendedChart> AnomalyContextCharts(AnalyticsFindings findings)
    {
        // Only the single most extreme anomaly earns a chart; one per outlier would swamp the report.
        var explanation = findings.AnomalyExplanations
            .Where(e => !e.IsEmpty)
            .OrderByDescending(e => e.Magnitude)
            .FirstOrDefault();
        if (explanation is null) yield break;

        var attributions = explanation.Attributions.Take(_maxCategories).ToList();
        string measureDisp = DisplayText.Humanize(explanation.Measure);

        // Each bar pairs a candidate segment's norm with the outlier, so a bar that reaches the
        // outlier's height is the segment that explains it.
        var categories = attributions
            .Select(a => $"{DisplayText.Humanize(a.Dimension)}: {a.Category}")
            .ToList();

        var norms = attributions.Select(a => (double?)a.CategoryMedian).ToList();
        var outlier = attributions.Select(_ => (double?)explanation.Value).ToList();

        string verdict = explanation.IsExplained
            ? "One segment's norm sits close to the outlier, so the value is explained by segment mix."
            : "No segment's norm comes close to the outlier, so it is a genuine one-off.";

        yield return new RecommendedChart
        {
            ChartType = ChartType.Column,
            SuitabilityScore = explanation.IsExplained ? 72 : 84,
            Reason = $"The most extreme {measureDisp} anomaly ({DisplayText.FormatNumber(explanation.Value)} " +
                     $"at {explanation.WhenLabel}) against the typical value of each segment it belongs to. {verdict}",
            Spec = new ChartSpec
            {
                Type = ChartType.Column,
                Title = $"{measureDisp} anomaly at {explanation.WhenLabel} vs segment norms",
                XAxisTitle = "Segment",
                YAxisTitle = measureDisp,
                Categories = categories,
                Series = new[]
                {
                    new SeriesSpec { Name = "Segment norm", Values = norms },
                    new SeriesSpec { Name = $"Anomaly ({explanation.WhenLabel})", Values = outlier }
                }
            }
        };
    }

    // ── Forecast: history line + confidence band => Line over AreaRange ───────

    // A forecast chart earns its keep by showing the projection, so the history behind it is
    // windowed to a span that still leaves the horizon legible. Plotting a full year of daily
    // periods squeezes the projection into well under 1% of the plot width.
    private const int MaxForecastHistory = 24;

    private IEnumerable<RecommendedChart> ForecastCharts(AnalyticsFindings findings)
    {
        foreach (var f in findings.Forecasts)
        {
            if (f.IsEmpty) continue;
            var points = f.Forecast.Points;
            int fitted = f.HistoryValues.Count;

            // The projection is still fitted on the whole history; only the plotted tail is trimmed.
            int skip = Math.Max(0, fitted - MaxForecastHistory);
            var historyValues = f.HistoryValues.Skip(skip).ToList();
            var historyLabels = f.HistoryLabels.Skip(skip).ToList();
            int history = historyValues.Count;

            // One category axis spanning history then horizon; the projection continues from the
            // last actual so the line joins up rather than restarting at a gap.
            var categories = historyLabels
                .Concat(points.Select(p => $"+{p.Step}"))
                .ToList();

            var actual = historyValues.Select(v => (double?)v)
                .Concat(Enumerable.Repeat((double?)null, points.Count))
                .ToList();

            var projected = Enumerable.Repeat((double?)null, history - 1)
                .Append(f.Forecast.LastActual)
                .Concat(points.Select(p => (double?)p.Value))
                .ToList();

            // History carries no uncertainty, so its band is zero-width (it hugs the actual line) and
            // only opens out across the projected span. Avoids plotting NaN into the renderer.
            var band = historyValues.Select(v => new RangeValue(v, v))
                .Concat(points.Select(p => new RangeValue(p.Lower, p.Upper)))
                .ToList();

            string measureDisp = DisplayText.Humanize(f.Measure);
            string method = f.Forecast.Method == "holt-winters" ? "Holt-Winters (seasonal)" : "Holt's linear method";
            int score = Math.Min(92, 70 + (fitted >= 12 ? 10 : 0) + (f.Forecast.SeasonLength >= 2 ? 6 : 0));

            // History length alone rewards exactly the series that forecast worst: a long, noisy,
            // trendless series scores highest while its band swamps the data it was fitted on. When
            // the widest interval is comparable to the spread of the history itself, the projection
            // carries no usable signal, so demote it rather than ranking it near the top.
            double spread = f.HistoryValues.Max() - f.HistoryValues.Min();
            double widest = points[^1].Upper - points[^1].Lower;
            bool uninformative = spread > 0 && widest >= spread;
            if (uninformative) score -= 30;

            string caveat = uninformative
                ? " The interval spans the full range of the history, so treat the projection as " +
                  "indicative only — this series carries little forecastable signal."
                : string.Empty;
            string scope = history < fitted
                ? $"Fitted on {fitted} periods of history; the latest {history} are plotted."
                : $"{fitted} periods of history support a projection.";

            yield return new RecommendedChart
            {
                ChartType = ChartType.Line,
                SuitabilityScore = score,
                Reason = $"{scope} {method} extends " +
                         $"{measureDisp} {points.Count} period(s) forward, with a shaded 95% confidence band.{caveat}",
                Spec = new ChartSpec
                {
                    Type = ChartType.Line,
                    Title = $"{measureDisp} forecast",
                    XAxisTitle = "Period",
                    YAxisTitle = measureDisp,
                    Categories = categories,
                    Series = new[]
                    {
                        new SeriesSpec
                        {
                            Name = "Confidence band",
                            TypeOverride = ChartType.AreaRange,
                            RangeValues = band
                        },
                        new SeriesSpec { Name = measureDisp, Values = actual },
                        new SeriesSpec { Name = "Projected", Values = projected }
                    }
                }
            };
        }
    }

    // ── Period-over-period: the bridge from first period to last => Waterfall ──
    private IEnumerable<RecommendedChart> PeriodBridgeCharts(AnalyticsFindings findings)
    {
        // A bridge stays readable for about a dozen steps; longer histories show the most recent
        // window rather than being dropped entirely.
        const int MaxSteps = 12;

        foreach (var c in findings.PeriodComparisons)
        {
            if (c.Periods.Count < 3) continue;

            var window = c.Periods.Count > MaxSteps + 1
                ? c.Periods.Skip(c.Periods.Count - (MaxSteps + 1)).ToList()
                : c.Periods.ToList();

            // Each step's ChangeAbs is measured against its own predecessor, and window[0] is a real
            // period, so the bridge still balances after windowing.
            var steps = window.Skip(1).ToList();
            if (steps.All(p => Math.Abs(p.ChangeAbs) < 1e-9)) continue;

            string measureDisp = DisplayText.Humanize(c.Measure);
            var categories = new List<string> { window[0].Label };
            categories.AddRange(steps.Select(p => p.Label));

            var values = new List<double?> { window[0].Value };
            values.AddRange(steps.Select(p => (double?)p.ChangeAbs));

            string scope = window.Count < c.Periods.Count
                ? $"the latest {window.Count} of {c.Periods.Count}"
                : $"{c.Periods.Count}";

            yield return new RecommendedChart
            {
                ChartType = ChartType.Waterfall,
                SuitabilityScore = 74,
                Reason = $"{scope} {c.Granularity.ToString().ToLowerInvariant()} periods of " +
                         $"{measureDisp}. A waterfall shows how each period's change bridges the opening " +
                         "value to the closing one.",
                Spec = new ChartSpec
                {
                    Type = ChartType.Waterfall,
                    Title = $"{measureDisp} period-over-period bridge",
                    XAxisTitle = "Period",
                    YAxisTitle = measureDisp,
                    Categories = categories,
                    Series = new[] { new SeriesSpec { Name = measureDisp, Values = values } }
                }
            };
        }
    }

    // ── Segmentation: how the clustered rows divide => Column of segment sizes ─
    private IEnumerable<RecommendedChart> SegmentCharts(AnalyticsFindings findings)
    {
        var segmentation = findings.Segmentation;
        if (segmentation is null || segmentation.IsEmpty) yield break;

        var ordered = segmentation.Segments.OrderByDescending(s => s.Size).ToList();
        string measures = string.Join(", ", segmentation.Measures.Select(DisplayText.Humanize));

        yield return new RecommendedChart
        {
            ChartType = ChartType.Column,
            SuitabilityScore = 68,
            Reason = $"Rows cluster into {ordered.Count} natural segments across {measures}. " +
                     "A column chart shows how the population divides between them.",
            Spec = new ChartSpec
            {
                Type = ChartType.Column,
                Title = $"Segment sizes ({segmentation.RowsClustered} rows)",
                XAxisTitle = "Segment",
                YAxisTitle = "Rows",
                Categories = ordered.Select(s => s.Label).ToList(),
                Series = new[]
                {
                    new SeriesSpec { Name = "Rows", Values = ordered.Select(s => (double?)s.Size).ToList() }
                }
            }
        };
    }

    // ── Composition across two dimensions => Heatmap ──────────────────────────
    private IEnumerable<RecommendedChart> CompositionHeatmaps(AnalyticsFindings findings)
    {
        foreach (var composition in findings.Compositions)
        {
            var columns = composition.Categories;
            var rowNames = composition.SeriesNames;
            // A heatmap earns its place only on a genuine grid — several rows and several columns.
            if (rowNames.Count < 3 || columns.Count < 3) continue;
            if (rowNames.Count > _maxCategories || columns.Count > 24) continue;

            var cells = new List<HeatCell>(rowNames.Count * columns.Count);
            for (int r = 0; r < rowNames.Count && r < composition.Values.Count; r++)
            {
                var row = composition.Values[r];
                for (int c = 0; c < columns.Count && c < row.Count; c++)
                    if (row[c] is double v)
                        cells.Add(new HeatCell(c, r, v));
            }

            if (cells.Count == 0) continue;

            string measureDisp = DisplayText.Humanize(composition.Measure);
            string rowDisp = DisplayText.Humanize(composition.SeriesDimension);
            string colDisp = DisplayText.Humanize(composition.CategoryDimension);

            yield return new RecommendedChart
            {
                ChartType = ChartType.Heatmap,
                SuitabilityScore = 72,
                Reason = $"{measureDisp} spans a {rowNames.Count}×{columns.Count} grid of {rowDisp} by {colDisp}. " +
                         "A heatmap makes the hot and cold cells visible at a glance, which a stacked chart hides.",
                Spec = new ChartSpec
                {
                    Type = ChartType.Heatmap,
                    Title = $"{measureDisp} by {rowDisp} and {colDisp}",
                    XAxisTitle = colDisp,
                    YAxisTitle = rowDisp,
                    Categories = columns.ToList(),
                    Series = new[]
                    {
                        new SeriesSpec
                        {
                            Name = measureDisp,
                            HeatCells = cells,
                            RowLabels = rowNames.ToList()
                        }
                    }
                }
            };
        }
    }

    // ── Spread of a measure within each category => BoxPlot ───────────────────
    private IEnumerable<RecommendedChart> SpreadByCategory(DatasetProfile profile)
    {
        // Comparing averages hides how wide each group actually is; a box plot shows the spread.
        const int MinPerCategory = 5;

        var dimension = profile.Categories
            .FirstOrDefault(c => c.Categorical is { DistinctCount: >= 2 and <= 8 });
        if (dimension is null) yield break;

        foreach (var measure in profile.Measures)
        {
            if (measure.Numeric is null) continue;

            var groups = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);
            int rows = Math.Min(dimension.LabelByRow.Count, measure.NumericByRow.Count);
            for (int i = 0; i < rows; i++)
            {
                string? label = dimension.LabelByRow[i];
                double? value = measure.NumericByRow[i];
                if (label is null || value is null) continue;
                if (!groups.TryGetValue(label, out var list)) groups[label] = list = new List<double>();
                list.Add(value.Value);
            }

            var usable = groups
                .Where(g => g.Value.Count >= MinPerCategory)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (usable.Count < 2) continue;

            var boxes = usable.Select(g => Summarise(g.Value)).ToList();
            // If every group has the same spread there is nothing for the reader to compare.
            if (boxes.All(b => Math.Abs(b.High - b.Low) < 1e-9)) continue;

            string measureDisp = DisplayText.Humanize(measure.Name);
            string dimDisp = DisplayText.Humanize(dimension.Name);

            yield return new RecommendedChart
            {
                ChartType = ChartType.BoxPlot,
                SuitabilityScore = 70,
                Reason = $"{usable.Count} {dimDisp} groups each hold at least {MinPerCategory} " +
                         $"{measureDisp} values. A box plot compares their medians, quartiles and full " +
                         "range together — detail a bar of averages discards.",
                Spec = new ChartSpec
                {
                    Type = ChartType.BoxPlot,
                    Title = $"{measureDisp} spread by {dimDisp}",
                    XAxisTitle = dimDisp,
                    YAxisTitle = measureDisp,
                    Categories = usable.Select(g => g.Key).ToList(),
                    Series = new[] { new SeriesSpec { Name = measureDisp, BoxValues = boxes } }
                }
            };
        }
    }

    // Five-number summary using the same type-7 percentile convention as the profiler.
    private static BoxValue Summarise(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        return new BoxValue(
            sorted[0],
            Statistics.DescriptiveStatistics.Percentile(sorted, 25),
            Statistics.DescriptiveStatistics.Percentile(sorted, 50),
            Statistics.DescriptiveStatistics.Percentile(sorted, 75),
            sorted[^1]);
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
        int targetBins = Math.Max(3, Math.Min(15, (int)Math.Ceiling(Math.Sqrt(n))));
        // Snap the edges to round numbers so bands read as 5k–10k rather than 6.6k–13.15k.
        bool integralX = IsIntegerValued(xs, n);
        var (start, width, bins) = NiceBins(min, max, targetBins, integralX);
        var sum = new double[bins];
        var count = new int[bins];
        for (int i = 0; i < n; i++)
        {
            int idx = (int)((xs[i] - start) / width);
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
            double lo = start + b * width, hi = lo + width;
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
        int targetBins = Math.Max(5, Math.Min(12, (int)Math.Ceiling(Math.Sqrt(values.Count))));
        bool integral = IsIntegerValued(values, values.Count);
        // Snap the edges to round numbers so bins read as 5k–10k rather than 6.6k–13.15k.
        var (start, width, bins) = NiceBins(min, max, targetBins, integral);
        var counts = new int[bins];
        foreach (var v in values)
        {
            int idx = (int)((v - start) / width);
            if (idx >= bins) idx = bins - 1;
            if (idx < 0) idx = 0;
            counts[idx]++;
        }
        var labels = new List<string>(bins);
        for (int i = 0; i < bins; i++)
        {
            double lo = start + i * width, hi = lo + width;
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

    // Computes clean bin edges: a rounded start plus a "nice" width (1/2/2.5/5 × 10ⁿ) so bucket
    // boundaries read as round numbers (e.g. 5k–10k) instead of raw data-driven values (6.6k–13.15k).
    private static (double start, double width, int bins) NiceBins(double min, double max, int targetBins, bool integral)
    {
        double step = NiceStep((max - min) / Math.Max(1, targetBins));
        if (integral) step = Math.Max(1, Math.Round(step));
        double start = Math.Floor(min / step) * step;
        double end   = Math.Ceiling(max / step) * step;
        int bins = Math.Max(1, (int)Math.Round((end - start) / step));
        return (start, step, bins);
    }

    // Rounds a raw step up to the nearest "nice" number: 1, 2, 2.5 or 5 × a power of ten.
    private static double NiceStep(double raw)
    {
        if (raw <= 0 || double.IsNaN(raw) || double.IsInfinity(raw)) return 1;
        double mag  = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        double norm = raw / mag; // 1 ≤ norm < 10
        double nice = norm <= 1 ? 1 : norm <= 2 ? 2 : norm <= 2.5 ? 2.5 : norm <= 5 ? 5 : 10;
        return nice * mag;
    }

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

    // ── Smooth trend: moving average series => Spline chart ──────────────────
    private IEnumerable<RecommendedChart> SmoothTrendCharts(AnalyticsFindings findings)
    {
        foreach (var ma in findings.MovingAverages)
        {
            var trend = findings.Trends.FirstOrDefault(t => t.Measure == ma.Measure);
            // Drop the warm-up periods (no smoothed value yet) so the spline starts as a clean line.
            var pts = ma.Points.Where(p => p.Smoothed.HasValue).ToList();
            if (pts.Count < 3) continue;
            var labels   = pts.Select(p => FormatDate(p.Period, ma.Granularity)).ToList();
            var smoothed = pts.Select(p => p.Smoothed).ToList();
            string measureDisp = DisplayText.Humanize(ma.Measure);
            string dateDisp    = DisplayText.Humanize(ma.DateColumn);
            int score = 75 + (trend is { HasTemporalSignal: true } ? 8 : 0);

            yield return new RecommendedChart
            {
                ChartType = ChartType.Spline,
                SuitabilityScore = score,
                Reason = $"{ma.WindowSize}-period moving average of {measureDisp} smooths short-term noise " +
                         $"to reveal the underlying trend across {pts.Count} {dateDisp} periods.",
                Spec = new ChartSpec
                {
                    Type       = ChartType.Spline,
                    Title      = $"{measureDisp} Trend ({ma.WindowSize}-Period Moving Avg)",
                    XAxisTitle = dateDisp,
                    YAxisTitle = measureDisp,
                    Categories = labels,
                    Series     = new[] { new SeriesSpec { Name = $"{ma.WindowSize}-Period Avg", Values = smoothed } }
                }
            };
        }
    }

    // ── Cumulative series: running total => Area chart ────────────────────────
    private IEnumerable<RecommendedChart> CumulativeCharts(AnalyticsFindings findings)
    {
        foreach (var cs in findings.CumulativeSeries)
        {
            var labels = cs.Points.Select(p => FormatDate(p.Period, cs.Granularity)).ToList();
            var values = cs.Points.Select(p => (double?)p.Cumulative).ToList();
            string measureDisp = DisplayText.Humanize(cs.Measure);
            string dateDisp    = DisplayText.Humanize(cs.DateColumn);
            string total       = DisplayText.FormatNumber(cs.FinalTotal);

            yield return new RecommendedChart
            {
                ChartType = ChartType.Area,
                SuitabilityScore = 72,
                Reason = $"Running total of {measureDisp} reaches {total} across {cs.Points.Count} periods. " +
                         $"An area chart communicates the accrued magnitude over time.",
                Spec = new ChartSpec
                {
                    Type       = ChartType.Area,
                    Title      = $"Cumulative {measureDisp} over {dateDisp}",
                    XAxisTitle = dateDisp,
                    YAxisTitle = $"Cumulative {measureDisp}",
                    Categories = labels,
                    Series     = new[] { new SeriesSpec { Name = $"Cumulative {measureDisp}", Values = values } }
                }
            };
        }
    }

    // ── Composition: measure broken down by two dims => Stacked Area/Column/Bar
    private IEnumerable<RecommendedChart> CompositionCharts(AnalyticsFindings findings)
    {
        foreach (var c in findings.Compositions)
        {
            string measureDisp = DisplayText.Humanize(c.Measure);
            string catDisp     = DisplayText.Humanize(c.CategoryDimension);
            string serDisp     = DisplayText.Humanize(c.SeriesDimension);
            string metricLabel = c.IsAdditive ? measureDisp : $"Average {measureDisp}";

            // Use pretty date labels for time-series categories; raw string labels otherwise.
            var categories = c.IsDateCategory
                ? c.CategoryDates.Select(d => FormatDate(d, c.Granularity)).ToList()
                : c.Categories.ToList();

            bool longLabels = categories.Any(l => l.Length > 12);
            ChartType type = c.IsDateCategory
                ? ChartType.Area
                : (longLabels || categories.Count > 8 ? ChartType.Bar : ChartType.Column);

            int score = c.IsDateCategory ? 82 : 74;
            string chartName = type == ChartType.Area ? "stacked area"
                             : type == ChartType.Bar  ? "stacked bar"
                             : "stacked column";

            var series = c.SeriesNames
                .Zip(c.Values, (name, vals) => new SeriesSpec
                {
                    Name   = DisplayText.Humanize(name),
                    Values = vals.ToList()
                })
                .ToArray();

            yield return new RecommendedChart
            {
                ChartType = type,
                SuitabilityScore = score,
                Reason = $"{metricLabel} broken down by {serDisp} across {c.Categories.Count} {catDisp} " +
                         $"values — a {chartName} shows how each {serDisp} contributes to the total.",
                Spec = new ChartSpec
                {
                    Type         = type,
                    StackingMode = Stacking.Normal,
                    Title        = $"{metricLabel} by {catDisp} and {serDisp}",
                    XAxisTitle   = catDisp,
                    YAxisTitle   = metricLabel,
                    Categories   = categories,
                    Series       = series
                }
            };
        }
    }

    private static string FormatDate(DateTime d, DateGranularity g) => g switch
    {
        DateGranularity.Yearly => d.ToString("yyyy", CultureInfo.InvariantCulture),
        DateGranularity.Quarterly => $"Q{(d.Month - 1) / 3 + 1} {d.Year}",
        DateGranularity.Monthly => d.ToString("MMM yyyy", CultureInfo.InvariantCulture),
        _ => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
    };
}
