using System;
using System.Collections.Generic;

namespace TerraFluent.AutoAnalytics.Profiling;

/// <summary>
/// Complete-case row alignment for cross-column analytics. Each column stores its non-missing
/// values row-aligned (index = original dataset row) via <see cref="ColumnStatistics.NumericByRow"/>,
/// <see cref="ColumnStatistics.LabelByRow"/> and <see cref="ColumnStatistics.DateByRow"/>. These
/// helpers pair columns by row, dropping only rows where a required cell is missing — so a missing
/// value in one column never shifts every subsequent pairing (the classic "compacted list" bug).
/// </summary>
internal static class RowAlignment
{
    /// <summary>Numeric (x, y) pairs for rows where BOTH values are present.</summary>
    public static (List<double> X, List<double> Y) NumericPairs(ColumnStatistics x, ColumnStatistics y)
    {
        var xr = x.NumericByRow;
        var yr = y.NumericByRow;
        int n = Math.Min(xr.Count, yr.Count);
        var xs = new List<double>(n);
        var ys = new List<double>(n);
        for (int i = 0; i < n; i++)
            if (xr[i].HasValue && yr[i].HasValue)
            {
                xs.Add(xr[i]!.Value);
                ys.Add(yr[i]!.Value);
            }
        return (xs, ys);
    }

    /// <summary>(label, value) pairs for rows where both the dimension label and measure are present.</summary>
    public static List<(string Label, double Value)> LabelValues(ColumnStatistics dimension, ColumnStatistics measure)
    {
        var lr = dimension.LabelByRow;
        var mr = measure.NumericByRow;
        int n = Math.Min(lr.Count, mr.Count);
        var list = new List<(string, double)>(n);
        for (int i = 0; i < n; i++)
            if (lr[i] is not null && mr[i].HasValue)
                list.Add((lr[i]!, mr[i]!.Value));
        return list;
    }

    /// <summary>(label1, label2, value) triples for rows where all three cells are present.</summary>
    public static List<(string Row, string Column, double Value)> LabelLabelValues(
        ColumnStatistics dim1, ColumnStatistics dim2, ColumnStatistics measure)
    {
        var r1 = dim1.LabelByRow;
        var r2 = dim2.LabelByRow;
        var mr = measure.NumericByRow;
        int n = Math.Min(mr.Count, Math.Min(r1.Count, r2.Count));
        var list = new List<(string, string, double)>(n);
        for (int i = 0; i < n; i++)
            if (r1[i] is not null && r2[i] is not null && mr[i].HasValue)
                list.Add((r1[i]!, r2[i]!, mr[i]!.Value));
        return list;
    }

    /// <summary>
    /// (date, value) pairs for rows where both are present, ascending by date. When
    /// <paramref name="date"/> is <see langword="null"/>, returns the measure's present values in
    /// row order paired with <see cref="DateTime.MinValue"/> (caller ignores the date).
    /// </summary>
    public static List<(DateTime Date, double Value)> DateValues(ColumnStatistics measure, ColumnStatistics? date)
    {
        var mr = measure.NumericByRow;
        if (date is null)
        {
            var seq = new List<(DateTime, double)>(mr.Count);
            for (int i = 0; i < mr.Count; i++)
                if (mr[i].HasValue) seq.Add((DateTime.MinValue, mr[i]!.Value));
            return seq;
        }

        var dr = date.DateByRow;
        int n = Math.Min(dr.Count, mr.Count);
        var list = new List<(DateTime Date, double Value)>(n);
        for (int i = 0; i < n; i++)
            if (dr[i].HasValue && mr[i].HasValue)
                list.Add((dr[i]!.Value, mr[i]!.Value));
        list.Sort((a, b) => a.Date.CompareTo(b.Date));
        return list;
    }

    /// <summary>
    /// (label, value) pairs for rows where both are present, ordered by <paramref name="date"/> when
    /// supplied (rows lacking a date are excluded from the ordered result), else in row order.
    /// Used by driver/root-cause analysis to split an ordered series into earlier/later halves.
    /// </summary>
    public static List<(string Label, double Value)> LabelValuesOrderedByDate(
        ColumnStatistics dimension, ColumnStatistics measure, ColumnStatistics? date)
    {
        var lr = dimension.LabelByRow;
        var mr = measure.NumericByRow;

        if (date is null)
        {
            int m = Math.Min(lr.Count, mr.Count);
            var seq = new List<(string, double)>(m);
            for (int i = 0; i < m; i++)
                if (lr[i] is not null && mr[i].HasValue)
                    seq.Add((lr[i]!, mr[i]!.Value));
            return seq;
        }

        var dr = date.DateByRow;
        int n = Math.Min(dr.Count, Math.Min(lr.Count, mr.Count));
        var rows = new List<(DateTime Date, string Label, double Value)>(n);
        for (int i = 0; i < n; i++)
            if (dr[i].HasValue && lr[i] is not null && mr[i].HasValue)
                rows.Add((dr[i]!.Value, lr[i]!, mr[i]!.Value));
        rows.Sort((a, b) => a.Date.CompareTo(b.Date));

        var list = new List<(string, double)>(rows.Count);
        foreach (var r in rows) list.Add((r.Label, r.Value));
        return list;
    }
}
