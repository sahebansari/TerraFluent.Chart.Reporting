using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;

namespace TerraFluent.AutoAnalytics.Profiling;

/// <summary>Aggregated profile of an entire dataset: per-column statistics plus quick accessors.</summary>
public sealed class DatasetProfile
{
    public string Name { get; init; } = "Dataset";
    public int RowCount { get; init; }
    public int ColumnCount { get; init; }

    public IReadOnlyList<ColumnStatistics> Columns { get; init; } = new List<ColumnStatistics>();

    /// <summary>Numeric measure columns (Numeric/Currency/Percentage).</summary>
    public IEnumerable<ColumnStatistics> Measures =>
        Columns.Where(c => c.Profile.IsMeasure);

    /// <summary>Grouping dimensions (Category/Boolean).</summary>
    public IEnumerable<ColumnStatistics> Categories =>
        Columns.Where(c => c.Profile.Type is ColumnType.Category or ColumnType.Boolean);

    /// <summary>Date/time dimensions.</summary>
    public IEnumerable<ColumnStatistics> DateColumns =>
        Columns.Where(c => c.Profile.Type == ColumnType.Date);

    public ColumnStatistics? ByName(string name) =>
        Columns.FirstOrDefault(c => string.Equals(c.Name, name, System.StringComparison.OrdinalIgnoreCase));
}
