using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Enums;

namespace TerraFluent.AutoAnalytics.Comparison;

/// <summary>How a column differs between the baseline and the current dataset.</summary>
public enum SchemaChangeKind
{
    /// <summary>Present in the current dataset but not the baseline.</summary>
    Added,
    /// <summary>Present in the baseline but not the current dataset.</summary>
    Removed,
    /// <summary>Present in both, but discovery inferred a different <see cref="ColumnType"/>.</summary>
    TypeChanged,
    /// <summary>Present in both with the same type, but a different <see cref="SemanticRole"/>.</summary>
    RoleChanged
}

/// <summary>A single structural difference between the two datasets.</summary>
public sealed class SchemaChange
{
    public string Column { get; init; } = string.Empty;
    public SchemaChangeKind Kind { get; init; }

    public ColumnType? BaselineType { get; init; }
    public ColumnType? CurrentType { get; init; }
    public SemanticRole? BaselineRole { get; init; }
    public SemanticRole? CurrentRole { get; init; }

    /// <summary>Plain-English statement of the change.</summary>
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// How one measure moved between the two datasets. Additive measures are compared on their totals
/// and per-row attributes on their averages — a summed average score would be meaningless.
/// </summary>
public sealed class MeasureDelta
{
    public string Measure { get; init; } = string.Empty;

    /// <summary>True when the measure is summable; false when it is a per-row attribute.</summary>
    public bool IsAdditive { get; init; }

    /// <summary>Which statistic was compared: <c>total</c> or <c>average</c>.</summary>
    public string Statistic { get; init; } = "total";

    public double BaselineValue { get; init; }
    public double CurrentValue { get; init; }
    public double Delta { get; init; }

    /// <summary>Fractional change (0.12 = +12%); <see langword="null"/> when the baseline is zero.</summary>
    public double? DeltaPct { get; init; }

    public TrendKind BaselineTrend { get; init; }
    public TrendKind CurrentTrend { get; init; }

    /// <summary>True when the measure reversed direction (e.g. rising became declining).</summary>
    public bool TrendReversed { get; init; }
}

/// <summary>How one category's contribution to a measure moved between the two datasets.</summary>
public sealed class CategoryShift
{
    public string Dimension { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Measure { get; init; } = string.Empty;

    public double BaselineValue { get; init; }
    public double CurrentValue { get; init; }

    /// <summary>The category's share 0..1 of the measure in the baseline.</summary>
    public double BaselineShare { get; init; }
    /// <summary>The category's share 0..1 of the measure in the current dataset.</summary>
    public double CurrentShare { get; init; }

    /// <summary>Change in share (<c>CurrentShare − BaselineShare</c>), in share points.</summary>
    public double ShareDelta { get; init; }

    /// <summary>True when the category appears only in the current dataset.</summary>
    public bool IsNew { get; init; }
    /// <summary>True when the category appeared only in the baseline.</summary>
    public bool IsGone { get; init; }
}

/// <summary>
/// A deterministic diff of two analysed datasets: what changed structurally, how each shared measure
/// moved, and how the mix within each dimension shifted.
/// </summary>
public sealed class DatasetComparison
{
    public string BaselineName { get; init; } = "Baseline";
    public string CurrentName { get; init; } = "Current";

    public int BaselineRowCount { get; init; }
    public int CurrentRowCount { get; init; }

    /// <summary>Columns added, removed, retyped or re-roled.</summary>
    public IReadOnlyList<SchemaChange> SchemaChanges { get; init; } = new List<SchemaChange>();

    /// <summary>Per-measure movement, ordered by descending absolute relative change.</summary>
    public IReadOnlyList<MeasureDelta> MeasureDeltas { get; init; } = new List<MeasureDelta>();

    /// <summary>Category mix shifts, ordered by descending absolute share change.</summary>
    public IReadOnlyList<CategoryShift> CategoryShifts { get; init; } = new List<CategoryShift>();

    /// <summary>Measure columns present in both datasets, so genuinely comparable.</summary>
    public IReadOnlyList<string> SharedMeasures { get; init; } = new List<string>();

    /// <summary>
    /// False when the two datasets share no measure, in which case only the schema diff is
    /// meaningful and every numeric section is empty.
    /// </summary>
    public bool IsComparable { get; init; }

    /// <summary>Plain-English note on how well the two datasets line up.</summary>
    public string CompatibilityNote { get; init; } = string.Empty;

    /// <summary>The single most notable difference.</summary>
    public string Headline { get; init; } = string.Empty;
}
