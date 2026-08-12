using System.Collections.Generic;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

namespace TerraFluent.AutoAnalytics.Recommendation;

/// <summary>A named data series within a <see cref="ChartSpec"/>.</summary>
public sealed class SeriesSpec
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<double?> Values { get; init; } = new List<double?>();

    /// <summary>Single scalar for KPI/gauge specs.</summary>
    public double? ScalarValue { get; init; }
}

/// <summary>
/// A rendering-library-agnostic description of a chart: the type, title, category labels and one
/// or more data series. Converted into a real <c>ChartBuilder</c> by <see cref="ChartConfigBuilder"/>.
/// </summary>
public sealed class ChartSpec
{
    public ChartType Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public IReadOnlyList<string> Categories { get; init; } = new List<string>();
    public IReadOnlyList<SeriesSpec> Series { get; init; } = new List<SeriesSpec>();

    public int Width { get; init; } = 720;
    public int Height { get; init; } = 420;

    /// <summary>Optional value-axis lower bound (used for gauges/rings).</summary>
    public double? AxisMin { get; init; }
    /// <summary>Optional value-axis upper bound (used for gauges/rings).</summary>
    public double? AxisMax { get; init; }
}
