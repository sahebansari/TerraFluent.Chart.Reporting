using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Schema;

namespace TerraFluent.AutoAnalytics.Comparison;

/// <summary>
/// Diffs two completed analyses: structural changes, per-measure movement and category mix shifts.
/// Deterministic — the same pair of datasets always yields the same comparison.
/// </summary>
/// <remarks>
/// Columns are matched by name (case-insensitively); anything unmatched is reported as added or
/// removed rather than silently dropped, since a quietly ignored column is how a comparison starts
/// lying. Measures are compared on the statistic that is meaningful for them: totals for additive
/// measures, averages for per-row attributes.
/// </remarks>
public sealed class DatasetComparisonEngine
{
    /// <summary>Cap on reported category shifts, keeping the report readable on wide dimensions.</summary>
    private const int MaxCategoryShifts = 24;

    /// <summary>Below this share change a category's movement is mix noise, not a finding.</summary>
    private const double MinShareDelta = 0.01;

    /// <summary>
    /// Minimum overlap (shared ÷ union of categories) for a dimension to be a valid basis for a mix
    /// comparison. Below this the two datasets enumerate largely different categories — non-overlapping
    /// time periods (weeks, months) or identifier-like columns — so every category reads as new or gone,
    /// which merely restates that the partition differs rather than revealing a real shift in mix.
    /// </summary>
    private const double MinCategoryOverlap = 0.5;

    /// <summary>Compares a current analysis against a baseline.</summary>
    public DatasetComparison Compare(AnalyticsResult baseline, AnalyticsResult current)
    {
        if (baseline is null) throw new ArgumentNullException(nameof(baseline));
        if (current is null) throw new ArgumentNullException(nameof(current));

        var schemaChanges = DiffSchema(baseline.Profile, current.Profile);

        var sharedMeasures = baseline.Profile.Measures
            .Select(m => m.Name)
            .Intersect(current.Profile.Measures.Select(m => m.Name), StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        bool comparable = sharedMeasures.Count > 0;

        var measureDeltas = comparable
            ? DiffMeasures(baseline, current, sharedMeasures)
            : new List<MeasureDelta>();

        var categoryShifts = comparable
            ? DiffCategories(baseline, current)
            : new List<CategoryShift>();

        return new DatasetComparison
        {
            BaselineName = baseline.Summary.DatasetName,
            CurrentName = current.Summary.DatasetName,
            BaselineRowCount = baseline.Profile.RowCount,
            CurrentRowCount = current.Profile.RowCount,
            SchemaChanges = schemaChanges,
            MeasureDeltas = measureDeltas,
            CategoryShifts = categoryShifts,
            SharedMeasures = sharedMeasures,
            IsComparable = comparable,
            CompatibilityNote = BuildCompatibilityNote(comparable, sharedMeasures, schemaChanges),
            Headline = BuildHeadline(comparable, measureDeltas, categoryShifts, schemaChanges,
                                     baseline.Profile.RowCount, current.Profile.RowCount)
        };
    }

    // ── Schema ────────────────────────────────────────────────────────────────

    private static List<SchemaChange> DiffSchema(DatasetProfile baseline, DatasetProfile current)
    {
        var changes = new List<SchemaChange>();

        var baseByName = baseline.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        var currentByName = current.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var column in current.Columns)
        {
            if (baseByName.TryGetValue(column.Name, out var before))
            {
                string disp = DisplayText.Humanize(column.Name);

                if (before.Profile.Type != column.Profile.Type)
                    changes.Add(new SchemaChange
                    {
                        Column = column.Name,
                        Kind = SchemaChangeKind.TypeChanged,
                        BaselineType = before.Profile.Type,
                        CurrentType = column.Profile.Type,
                        BaselineRole = before.Profile.Role,
                        CurrentRole = column.Profile.Role,
                        Description = $"{disp} changed type from {before.Profile.Type} to {column.Profile.Type}."
                    });
                else if (before.Profile.Role != column.Profile.Role)
                    changes.Add(new SchemaChange
                    {
                        Column = column.Name,
                        Kind = SchemaChangeKind.RoleChanged,
                        BaselineType = before.Profile.Type,
                        CurrentType = column.Profile.Type,
                        BaselineRole = before.Profile.Role,
                        CurrentRole = column.Profile.Role,
                        Description = $"{disp} is now read as {column.Profile.Role} rather than {before.Profile.Role}."
                    });
            }
            else
            {
                changes.Add(new SchemaChange
                {
                    Column = column.Name,
                    Kind = SchemaChangeKind.Added,
                    CurrentType = column.Profile.Type,
                    CurrentRole = column.Profile.Role,
                    Description = $"{DisplayText.Humanize(column.Name)} ({column.Profile.Type}) is new."
                });
            }
        }

        foreach (var column in baseline.Columns.Where(c => !currentByName.ContainsKey(c.Name)))
            changes.Add(new SchemaChange
            {
                Column = column.Name,
                Kind = SchemaChangeKind.Removed,
                BaselineType = column.Profile.Type,
                BaselineRole = column.Profile.Role,
                Description = $"{DisplayText.Humanize(column.Name)} ({column.Profile.Type}) is no longer present."
            });

        return changes
            .OrderBy(c => c.Kind)
            .ThenBy(c => c.Column, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // ── Measures ──────────────────────────────────────────────────────────────

    private static List<MeasureDelta> DiffMeasures(
        AnalyticsResult baseline, AnalyticsResult current, IReadOnlyList<string> sharedMeasures)
    {
        var deltas = new List<MeasureDelta>();

        foreach (var name in sharedMeasures)
        {
            var before = baseline.Profile.ByName(name);
            var after = current.Profile.ByName(name);
            if (before?.Numeric is null || after?.Numeric is null) continue;

            // Additivity is decided from the current dataset, and only when BOTH agree is a total
            // used — if either side reads as a per-row attribute, summing would misstate the change.
            bool additive =
                MeasureSemantics.IsAdditive(after.Profile, after.Numeric.Min, after.Numeric.Max) &&
                MeasureSemantics.IsAdditive(before.Profile, before.Numeric.Min, before.Numeric.Max);

            double baseValue = additive ? before.Numeric.Sum : before.Numeric.Mean;
            double currentValue = additive ? after.Numeric.Sum : after.Numeric.Mean;
            double delta = currentValue - baseValue;

            var baseTrend = TrendOf(baseline.Findings, name);
            var currentTrend = TrendOf(current.Findings, name);

            deltas.Add(new MeasureDelta
            {
                Measure = name,
                IsAdditive = additive,
                Statistic = additive ? "total" : "average",
                BaselineValue = baseValue,
                CurrentValue = currentValue,
                Delta = delta,
                DeltaPct = Math.Abs(baseValue) > 1e-9 ? delta / Math.Abs(baseValue) : null,
                BaselineTrend = baseTrend,
                CurrentTrend = currentTrend,
                TrendReversed = IsReversal(baseTrend, currentTrend)
            });
        }

        return deltas
            .OrderByDescending(d => Math.Abs(d.DeltaPct ?? 0))
            .ThenByDescending(d => Math.Abs(d.Delta))
            .ToList();
    }

    private static TrendKind TrendOf(AnalyticsFindings findings, string measure) =>
        findings.Trends.FirstOrDefault(t =>
            string.Equals(t.Measure, measure, StringComparison.OrdinalIgnoreCase))?.Kind
        ?? TrendKind.Unknown;

    // Only a genuine direction flip counts; moving to or from Stable/Volatile is not a reversal.
    private static bool IsReversal(TrendKind before, TrendKind after) =>
        (before is TrendKind.Rising && after is TrendKind.Declining)
        || (before is TrendKind.Declining && after is TrendKind.Rising);

    // ── Category mix ──────────────────────────────────────────────────────────

    private static List<CategoryShift> DiffCategories(AnalyticsResult baseline, AnalyticsResult current)
    {
        var shifts = new List<CategoryShift>();

        // Index the baseline's group analyses so each current one can find its counterpart.
        var baselineGroups = baseline.Findings.Groups
            .GroupBy(g => (g.Dimension, g.Measure), TupleComparer.Instance)
            .ToDictionary(g => g.Key, g => g.First(), TupleComparer.Instance);

        foreach (var group in current.Findings.Groups)
        {
            if (!baselineGroups.TryGetValue((group.Dimension, group.Measure), out var before)) continue;

            var beforeBuckets = before.Buckets.ToDictionary(b => b.Key, StringComparer.OrdinalIgnoreCase);
            var afterBuckets = group.Buckets.ToDictionary(b => b.Key, StringComparer.OrdinalIgnoreCase);

            // Skip dimensions whose categories barely overlap: the two sides enumerate different
            // things (e.g. non-overlapping weeks), so their shifts would only restate the partition.
            int sharedCategories = beforeBuckets.Keys.Count(afterBuckets.ContainsKey);
            int unionCategories = beforeBuckets.Keys
                .Union(afterBuckets.Keys, StringComparer.OrdinalIgnoreCase).Count();
            if (unionCategories == 0 || (double)sharedCategories / unionCategories < MinCategoryOverlap)
                continue;

            var categories = beforeBuckets.Keys
                .Union(afterBuckets.Keys, StringComparer.OrdinalIgnoreCase)
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase);

            foreach (var category in categories)
            {
                beforeBuckets.TryGetValue(category, out var b);
                afterBuckets.TryGetValue(category, out var a);

                double baseShare = b?.Share ?? 0;
                double currentShare = a?.Share ?? 0;
                double shareDelta = currentShare - baseShare;

                bool appeared = b is null && a is not null;
                bool vanished = b is not null && a is null;

                // A category that came or went is always worth reporting; otherwise require a
                // movement large enough not to be rounding.
                if (!appeared && !vanished && Math.Abs(shareDelta) < MinShareDelta) continue;

                shifts.Add(new CategoryShift
                {
                    Dimension = group.Dimension,
                    Category = category,
                    Measure = group.Measure,
                    BaselineValue = b?.Value ?? 0,
                    CurrentValue = a?.Value ?? 0,
                    BaselineShare = baseShare,
                    CurrentShare = currentShare,
                    ShareDelta = shareDelta,
                    IsNew = appeared,
                    IsGone = vanished
                });
            }
        }

        return shifts
            .OrderByDescending(s => Math.Abs(s.ShareDelta))
            .ThenBy(s => s.Category, StringComparer.OrdinalIgnoreCase)
            .Take(MaxCategoryShifts)
            .ToList();
    }

    // Case-insensitive key comparer for the (dimension, measure) pairing.
    private sealed class TupleComparer : IEqualityComparer<(string Dimension, string Measure)>
    {
        public static readonly TupleComparer Instance = new();

        public bool Equals((string Dimension, string Measure) x, (string Dimension, string Measure) y) =>
            string.Equals(x.Dimension, y.Dimension, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.Measure, y.Measure, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Dimension, string Measure) obj) =>
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Dimension) * 397
            ^ StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Measure);
    }

    // ── Narrative ─────────────────────────────────────────────────────────────

    private static string BuildCompatibilityNote(
        bool comparable, IReadOnlyList<string> sharedMeasures, IReadOnlyList<SchemaChange> changes)
    {
        if (!comparable)
            return "These datasets share no measure column, so only the structural differences can be " +
                   "compared. Check that the two files describe the same thing and use the same column names.";

        int structural = changes.Count(c => c.Kind is SchemaChangeKind.Added or SchemaChangeKind.Removed);
        string measures = $"{sharedMeasures.Count} shared measure(s): {string.Join(", ", sharedMeasures.Select(DisplayText.Humanize))}.";

        return structural == 0
            ? $"The two datasets have matching structure. Comparing {measures}"
            : $"{structural} column(s) differ between the two datasets; the comparison covers {measures}";
    }

    private static string BuildHeadline(
        bool comparable, IReadOnlyList<MeasureDelta> deltas, IReadOnlyList<CategoryShift> shifts,
        IReadOnlyList<SchemaChange> changes, int baselineRows, int currentRows)
    {
        if (!comparable)
            return changes.Count > 0
                ? $"No shared measures — {changes.Count} structural difference(s) found."
                : "The two datasets have nothing in common to compare.";

        var top = deltas.FirstOrDefault(d => d.DeltaPct is not null);
        if (top?.DeltaPct is double pct && Math.Abs(pct) >= 0.01)
        {
            string direction = pct >= 0 ? "up" : "down";
            return $"{DisplayText.Humanize(top.Measure)} {top.Statistic} is {direction} " +
                   $"{Math.Abs(pct).ToString("P0", CultureInfo.InvariantCulture)}.";
        }

        var reversal = deltas.FirstOrDefault(d => d.TrendReversed);
        if (reversal is not null)
            return $"{DisplayText.Humanize(reversal.Measure)} reversed direction — " +
                   $"{reversal.BaselineTrend} became {reversal.CurrentTrend}.";

        var shift = shifts.FirstOrDefault();
        if (shift is not null)
            return $"{shift.Category} moved {FormatPoints(shift.ShareDelta)} of " +
                   $"{DisplayText.Humanize(shift.Measure)} share.";

        return baselineRows == currentRows
            ? "The two datasets are materially the same."
            : $"Row count moved from {baselineRows} to {currentRows} with no material change in the measures.";
    }

    internal static string FormatPoints(double shareDelta) =>
        (shareDelta >= 0 ? "+" : "-") +
        (Math.Abs(shareDelta) * 100).ToString("0.#", CultureInfo.InvariantCulture) + " pts";
}
