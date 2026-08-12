namespace TerraFluent.AutoAnalytics.Profiling;

/// <summary>Full numeric five-number summary plus shape statistics for a measure column.</summary>
public sealed class NumericSummary
{
    public int Count { get; init; }
    public double Min { get; init; }
    public double Max { get; init; }
    public double Mean { get; init; }
    public double Median { get; init; }
    public double Mode { get; init; }
    public double Variance { get; init; }
    public double StdDev { get; init; }
    public double Q1 { get; init; }
    public double Q3 { get; init; }
    /// <summary>Interquartile range (Q3 − Q1).</summary>
    public double Iqr => Q3 - Q1;
    public double Sum { get; init; }
    public double Skewness { get; init; }
    public double Kurtosis { get; init; }
}
