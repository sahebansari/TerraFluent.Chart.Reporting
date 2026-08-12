using System;
using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Data;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Schema;
using TerraFluent.AutoAnalytics.Statistics;

namespace TerraFluent.AutoAnalytics.Profiling;

/// <summary>
/// Phase 3 — computes numeric, categorical and date summaries for every column and assembles
/// them into a <see cref="DatasetProfile"/>.
/// </summary>
public sealed class DataProfilingEngine
{
    /// <summary>Profiles the dataset given its already-discovered schema.</summary>
    public DatasetProfile Profile(Dataset dataset, IReadOnlyList<ColumnProfile> schema)
    {
        if (dataset is null) throw new ArgumentNullException(nameof(dataset));
        if (schema is null) throw new ArgumentNullException(nameof(schema));

        var columns = new List<ColumnStatistics>(schema.Count);
        foreach (var profile in schema)
            columns.Add(ProfileColumn(dataset.Columns[profile.Index], profile));

        return new DatasetProfile
        {
            Name = dataset.Name,
            RowCount = dataset.RowCount,
            ColumnCount = dataset.ColumnCount,
            Columns = columns
        };
    }

    private static ColumnStatistics ProfileColumn(DataColumn column, ColumnProfile profile)
    {
        if (profile.IsMeasure)
            return ProfileNumeric(column, profile);
        if (profile.Type == ColumnType.Date)
            return ProfileDate(column, profile);
        return ProfileCategorical(column, profile);
    }

    private static ColumnStatistics ProfileNumeric(DataColumn column, ColumnProfile profile)
    {
        var values = new List<double>(column.Values.Count);
        foreach (var v in column.Values)
            if (!ValueParsing.IsMissing(v) && ValueParsing.TryToDouble(v, out var d))
                values.Add(d);

        NumericSummary? summary = null;
        if (values.Count > 0)
        {
            var sorted = DescriptiveStatistics.Sorted(values);
            summary = new NumericSummary
            {
                Count    = values.Count,
                Min      = sorted[0],
                Max      = sorted[sorted.Length - 1],
                Sum      = values.Sum(),
                Mean     = DescriptiveStatistics.Mean(values),
                Median   = DescriptiveStatistics.Percentile(sorted, 50),
                Mode     = DescriptiveStatistics.Mode(values),
                Variance = DescriptiveStatistics.Variance(values),
                StdDev   = DescriptiveStatistics.StdDev(values),
                Q1       = DescriptiveStatistics.Percentile(sorted, 25),
                Q3       = DescriptiveStatistics.Percentile(sorted, 75),
                Skewness = DescriptiveStatistics.Skewness(values),
                Kurtosis = DescriptiveStatistics.Kurtosis(values)
            };
        }

        return new ColumnStatistics { Profile = profile, Numeric = summary, NumericValues = values };
    }

    private static ColumnStatistics ProfileCategorical(DataColumn column, ColumnProfile profile)
    {
        var labels = new List<string>(column.Values.Count);
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in column.Values)
        {
            if (ValueParsing.IsMissing(v)) continue;
            var s = v!.ToString()!;
            labels.Add(s);
            counts[s] = counts.TryGetValue(s, out var c) ? c + 1 : 1;
        }

        int populated = labels.Count;
        var top = counts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Select(kv => new CategoryFrequency
            {
                Value = kv.Key,
                Count = kv.Value,
                Share = populated == 0 ? 0 : (double)kv.Value / populated
            })
            .ToList();

        var summary = new CategoricalSummary { DistinctCount = counts.Count, TopCategories = top };
        return new ColumnStatistics { Profile = profile, Categorical = summary, Labels = labels };
    }

    private static ColumnStatistics ProfileDate(DataColumn column, ColumnProfile profile)
    {
        var dates = new List<DateTime>(column.Values.Count);
        foreach (var v in column.Values)
            if (!ValueParsing.IsMissing(v) && ValueParsing.TryToDate(v, out var dt))
                dates.Add(dt);

        DateSummary? summary = null;
        if (dates.Count > 0)
        {
            var ordered = dates.OrderBy(d => d).ToList();
            summary = new DateSummary
            {
                Start = ordered[0],
                End = ordered[ordered.Count - 1],
                Count = ordered.Count,
                Granularity = InferGranularity(ordered)
            };
        }

        return new ColumnStatistics { Profile = profile, Date = summary, Dates = dates };
    }

    private static DateGranularity InferGranularity(IReadOnlyList<DateTime> ordered)
    {
        if (ordered.Count < 2) return DateGranularity.Unknown;
        double totalDays = 0;
        int gaps = 0;
        for (int i = 1; i < ordered.Count; i++)
        {
            double days = (ordered[i] - ordered[i - 1]).TotalDays;
            if (days <= 0) continue;
            totalDays += days; gaps++;
        }
        if (gaps == 0) return DateGranularity.Unknown;
        double avg = totalDays / gaps;
        return avg switch
        {
            <= 1.5   => DateGranularity.Daily,
            <= 10    => DateGranularity.Weekly,
            <= 45    => DateGranularity.Monthly,
            <= 135   => DateGranularity.Quarterly,
            _        => DateGranularity.Yearly
        };
    }
}
