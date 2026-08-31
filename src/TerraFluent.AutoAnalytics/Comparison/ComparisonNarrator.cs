using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Recommendation;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

namespace TerraFluent.AutoAnalytics.Comparison;

/// <summary>
/// Turns a <see cref="DatasetComparison"/> into ranked, plain-English insights and the charts that
/// evidence them. Deterministic: the same comparison always yields the same narrative and scores.
/// </summary>
public static class ComparisonNarrator
{
    /// <summary>Below this relative movement a measure has not meaningfully changed.</summary>
    private const double MinMeaningfulChange = 0.01;

    /// <summary>Most category rows to plot on a single comparison chart.</summary>
    private const int MaxPlottedCategories = 12;

    // ── Insights ──────────────────────────────────────────────────────────────

    /// <summary>Generates the comparison's insights, most important first.</summary>
    public static IReadOnlyList<Insight> Insights(DatasetComparison comparison)
    {
        if (comparison is null) throw new ArgumentNullException(nameof(comparison));

        var insights = new List<Insight>();
        insights.AddRange(SchemaInsights(comparison));
        insights.AddRange(MeasureInsights(comparison));
        insights.AddRange(MixInsights(comparison));
        insights.AddRange(VolumeInsight(comparison));

        return insights.OrderByDescending(i => i.ImportanceScore).ToList();
    }

    private static IEnumerable<Insight> SchemaInsights(DatasetComparison c)
    {
        if (c.SchemaChanges.Count == 0) yield break;

        // A retyped column is the dangerous one: totals computed either side are not comparable.
        var retyped = c.SchemaChanges.Where(s => s.Kind == SchemaChangeKind.TypeChanged).ToList();
        var added = c.SchemaChanges.Where(s => s.Kind == SchemaChangeKind.Added).ToList();
        var removed = c.SchemaChanges.Where(s => s.Kind == SchemaChangeKind.Removed).ToList();

        if (retyped.Count > 0)
            yield return new Insight
            {
                Kind = InsightKind.Comparison,
                Title = $"{retyped.Count} column(s) changed type",
                Description =
                    string.Join(" ", retyped.Select(r => r.Description)) +
                    " A column read as a different type on each side is not safely comparable — the two " +
                    "figures are measuring different things. Fix the source formatting before trusting any " +
                    "movement reported for these columns.",
                ImportanceScore = 90,
                RelatedColumns = retyped.Select(r => r.Column).ToArray(),
                Evidence = retyped.ToDictionary(
                    r => r.Column,
                    r => $"{r.BaselineType} -> {r.CurrentType}",
                    StringComparer.OrdinalIgnoreCase)
            };

        if (added.Count > 0 || removed.Count > 0)
        {
            var parts = new List<string>();
            if (added.Count > 0) parts.Add($"{added.Count} added ({Names(added)})");
            if (removed.Count > 0) parts.Add($"{removed.Count} removed ({Names(removed)})");

            yield return new Insight
            {
                Kind = InsightKind.Comparison,
                Title = $"Structure differs: {string.Join(", ", parts)}",
                Description =
                    $"The two datasets do not have the same columns — {string.Join(" and ", parts)}. " +
                    "Only the shared columns are compared below; anything listed here is reported rather " +
                    "than quietly ignored, since a dropped column is how a comparison starts misleading.",
                ImportanceScore = 62,
                RelatedColumns = added.Concat(removed).Select(s => s.Column).ToArray(),
                Evidence = new Dictionary<string, string>
                {
                    ["added"] = added.Count.ToString(CultureInfo.InvariantCulture),
                    ["removed"] = removed.Count.ToString(CultureInfo.InvariantCulture)
                }
            };
        }
    }

    private static IEnumerable<Insight> MeasureInsights(DatasetComparison c)
    {
        foreach (var d in c.MeasureDeltas)
        {
            bool moved = d.DeltaPct is double p && Math.Abs(p) >= MinMeaningfulChange;
            if (!moved && !d.TrendReversed) continue;

            string measureDisp = DisplayText.Humanize(d.Measure);
            string direction = d.Delta >= 0 ? "rose" : "fell";
            string pct = d.DeltaPct is double q
                ? Math.Abs(q).ToString("P0", CultureInfo.InvariantCulture)
                : "an unmeasurable amount";

            int score = Score(d);

            string reversal = d.TrendReversed
                ? $" Its direction also reversed: {d.BaselineTrend.ToString().ToLowerInvariant()} in {c.BaselineName}, " +
                  $"{d.CurrentTrend.ToString().ToLowerInvariant()} in {c.CurrentName}. A reversal matters more than the " +
                  "size of the move — it says the underlying behaviour changed, not just the level."
                : string.Empty;

            // Spelling out which statistic was compared stops the reader assuming a total.
            string basis = d.IsAdditive
                ? "Compared on totals, since this measure is summable."
                : "Compared on averages — this is a per-row attribute, so a total would be meaningless.";

            yield return new Insight
            {
                Kind = InsightKind.Comparison,
                Title = $"{measureDisp} {d.Statistic} {direction} {pct}",
                Description =
                    $"{measureDisp} moved from {DisplayText.FormatNumber(d.BaselineValue)} in {c.BaselineName} " +
                    $"to {DisplayText.FormatNumber(d.CurrentValue)} in {c.CurrentName} " +
                    $"({(d.Delta >= 0 ? "+" : "-")}{DisplayText.FormatNumber(Math.Abs(d.Delta))}). " +
                    basis + reversal,
                ImportanceScore = score,
                RelatedColumns = new[] { d.Measure },
                Evidence = new Dictionary<string, string>
                {
                    ["statistic"] = d.Statistic,
                    ["baseline"] = d.BaselineValue.ToString("F2", CultureInfo.InvariantCulture),
                    ["current"] = d.CurrentValue.ToString("F2", CultureInfo.InvariantCulture),
                    ["deltaPct"] = d.DeltaPct?.ToString("F3", CultureInfo.InvariantCulture) ?? "n/a",
                    ["trendReversed"] = d.TrendReversed ? "true" : "false"
                }
            };
        }
    }

    private static IEnumerable<Insight> MixInsights(DatasetComparison c)
    {
        // Report the mix shift once per dimension/measure pair, led by its largest mover.
        var byGroup = c.CategoryShifts
            .GroupBy(s => (s.Dimension, s.Measure))
            .OrderByDescending(g => g.Max(s => Math.Abs(s.ShareDelta)));

        foreach (var group in byGroup)
        {
            var top = group.OrderByDescending(s => Math.Abs(s.ShareDelta)).First();
            string dimDisp = DisplayText.Humanize(top.Dimension);
            string measureDisp = DisplayText.Humanize(top.Measure);

            var gained = group.Where(s => s.ShareDelta > 0).OrderByDescending(s => s.ShareDelta).Take(2).ToList();
            var lost = group.Where(s => s.ShareDelta < 0).OrderBy(s => s.ShareDelta).Take(2).ToList();

            var entries = new List<string>();
            entries.AddRange(gained.Select(s => $"{s.Category} {DatasetComparisonEngine.FormatPoints(s.ShareDelta)}"));
            entries.AddRange(lost.Select(s => $"{s.Category} {DatasetComparisonEngine.FormatPoints(s.ShareDelta)}"));

            string arrivals = string.Join(", ", group.Where(s => s.IsNew).Select(s => s.Category));
            string departures = string.Join(", ", group.Where(s => s.IsGone).Select(s => s.Category));

            string movement = string.Empty;
            if (!string.IsNullOrEmpty(arrivals)) movement += $" New in {c.CurrentName}: {arrivals}.";
            if (!string.IsNullOrEmpty(departures)) movement += $" Absent from {c.CurrentName}: {departures}.";

            int score = (int)Math.Round(Math.Min(85, 40 + Math.Abs(top.ShareDelta) * 120));

            yield return new Insight
            {
                Kind = InsightKind.Comparison,
                Title = $"{measureDisp} mix shifted across {dimDisp}",
                Description =
                    $"The split of {measureDisp} by {dimDisp} changed between {c.BaselineName} and {c.CurrentName}: " +
                    string.Join(", ", entries) + "." + movement +
                    " A mix shift can move a headline total without any single group changing its own behaviour, " +
                    "so it is worth separating from a genuine gain or loss.",
                ImportanceScore = score,
                RelatedColumns = new[] { top.Dimension, top.Measure },
                Evidence = new Dictionary<string, string>
                {
                    ["topCategory"] = top.Category,
                    ["shareDelta"] = top.ShareDelta.ToString("F3", CultureInfo.InvariantCulture),
                    ["baselineShare"] = top.BaselineShare.ToString("F3", CultureInfo.InvariantCulture),
                    ["currentShare"] = top.CurrentShare.ToString("F3", CultureInfo.InvariantCulture)
                }
            };
        }
    }

    private static IEnumerable<Insight> VolumeInsight(DatasetComparison c)
    {
        if (c.BaselineRowCount == 0 || c.BaselineRowCount == c.CurrentRowCount) yield break;

        double change = (double)(c.CurrentRowCount - c.BaselineRowCount) / c.BaselineRowCount;
        if (Math.Abs(change) < 0.05) yield break;

        string direction = change >= 0 ? "more" : "fewer";
        yield return new Insight
        {
            Kind = InsightKind.Comparison,
            Title = $"{c.CurrentName} has {Math.Abs(change).ToString("P0", CultureInfo.InvariantCulture)} {direction} rows",
            Description =
                $"{c.BaselineName} holds {c.BaselineRowCount} rows against {c.CurrentRowCount} in {c.CurrentName}. " +
                "Totals will move with row count alone, so read the additive measures against this — the averages " +
                "are the fairer comparison when the two datasets cover different volumes.",
            ImportanceScore = 55,
            RelatedColumns = Array.Empty<string>(),
            Evidence = new Dictionary<string, string>
            {
                ["baselineRows"] = c.BaselineRowCount.ToString(CultureInfo.InvariantCulture),
                ["currentRows"] = c.CurrentRowCount.ToString(CultureInfo.InvariantCulture)
            }
        };
    }

    // A reversal is a stronger signal than a large move, so it floors the score high.
    private static int Score(MeasureDelta d)
    {
        double magnitude = Math.Min(1, Math.Abs(d.DeltaPct ?? 0) / 0.5);
        int score = (int)Math.Round(45 + magnitude * 45);
        return d.TrendReversed ? Math.Max(score, 80) : score;
    }

    private static string Names(IEnumerable<SchemaChange> changes) =>
        string.Join(", ", changes.Select(s => DisplayText.Humanize(s.Column)));

    // ── Charts ────────────────────────────────────────────────────────────────

    /// <summary>Builds the charts that evidence the comparison, most useful first.</summary>
    public static IReadOnlyList<RecommendedChart> Charts(DatasetComparison comparison)
    {
        if (comparison is null) throw new ArgumentNullException(nameof(comparison));
        if (!comparison.IsComparable) return Array.Empty<RecommendedChart>();

        var charts = new List<RecommendedChart>();
        if (MeasureChart(comparison) is { } measures) charts.Add(measures);
        charts.AddRange(MixCharts(comparison));
        return charts;
    }

    // Every shared measure side by side. Values sit on one axis, so this is honest only when the
    // measures are of comparable magnitude — hence the grouped column rather than a shared index.
    private static RecommendedChart? MeasureChart(DatasetComparison c)
    {
        var deltas = c.MeasureDeltas.Take(MaxPlottedCategories).ToList();
        if (deltas.Count == 0) return null;

        return new RecommendedChart
        {
            ChartType = ChartType.Column,
            SuitabilityScore = 86,
            Reason = $"Each shared measure in {c.BaselineName} against {c.CurrentName}. " +
                     "Additive measures are compared on totals and per-row attributes on averages.",
            Spec = new ChartSpec
            {
                Type = ChartType.Column,
                Title = $"{c.BaselineName} vs {c.CurrentName} — measures",
                XAxisTitle = "Measure",
                YAxisTitle = "Value",
                Categories = deltas.Select(d => $"{DisplayText.Humanize(d.Measure)} ({d.Statistic})").ToList(),
                Series = new[]
                {
                    new SeriesSpec { Name = c.BaselineName, Values = deltas.Select(d => (double?)d.BaselineValue).ToList() },
                    new SeriesSpec { Name = c.CurrentName, Values = deltas.Select(d => (double?)d.CurrentValue).ToList() }
                }
            }
        };
    }

    // A dumbbell draws each category as a line from its old value to its new one — the shape of a
    // before/after comparison, and far easier to scan than two adjacent bars per category.
    private static IEnumerable<RecommendedChart> MixCharts(DatasetComparison c)
    {
        var byGroup = c.CategoryShifts
            .GroupBy(s => (s.Dimension, s.Measure))
            .OrderByDescending(g => g.Max(s => Math.Abs(s.ShareDelta)));

        foreach (var group in byGroup)
        {
            var rows = group
                .OrderByDescending(s => Math.Abs(s.ShareDelta))
                .Take(MaxPlottedCategories)
                .ToList();
            if (rows.Count < 2) continue;

            string dimDisp = DisplayText.Humanize(group.Key.Dimension);
            string measureDisp = DisplayText.Humanize(group.Key.Measure);

            yield return new RecommendedChart
            {
                ChartType = ChartType.Dumbbell,
                SuitabilityScore = 80,
                Reason = $"How each {dimDisp} category's {measureDisp} moved between {c.BaselineName} and " +
                         $"{c.CurrentName}. Each dumbbell runs from the old value to the new one, so the " +
                         "length is the change and the direction is its sign.",
                Spec = new ChartSpec
                {
                    Type = ChartType.Dumbbell,
                    Title = $"{measureDisp} by {dimDisp} — {c.BaselineName} to {c.CurrentName}",
                    XAxisTitle = dimDisp,
                    YAxisTitle = measureDisp,
                    Categories = rows.Select(s => s.Category).ToList(),
                    Series = new[]
                    {
                        new SeriesSpec
                        {
                            Name = $"{c.BaselineName} to {c.CurrentName}",
                            // Low/High is an extent, not a direction, so order the pair by value and
                            // let the insight text carry the sign of the movement.
                            RangeValues = rows
                                .Select(s => new RangeValue(
                                    Math.Min(s.BaselineValue, s.CurrentValue),
                                    Math.Max(s.BaselineValue, s.CurrentValue)))
                                .ToList()
                        }
                    }
                }
            };
        }
    }
}
