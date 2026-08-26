using TerraFluent.AutoAnalytics.Comparison;
using TerraFluent.AutoAnalytics.Insights;

namespace TerraFluent.Chart.Reporting.Api.Models;

/// <summary>
/// A request to diff two datasets. Both sides are supplied inline and analysed independently, so
/// the comparison is as stateless as every other endpoint — nothing is retained between calls.
/// </summary>
public sealed class CompareRequest
{
    /// <summary>The dataset to compare against (the "before").</summary>
    public AnalyzeRequest Baseline { get; set; } = new();

    /// <summary>The dataset being examined (the "after").</summary>
    public AnalyzeRequest Current { get; set; } = new();
}

/// <summary>A structural difference between the two datasets.</summary>
public sealed record SchemaChangeDto
{
    public required string Column { get; init; }
    public required string Kind { get; init; }
    public string? BaselineType { get; init; }
    public string? CurrentType { get; init; }
    public string? BaselineRole { get; init; }
    public string? CurrentRole { get; init; }
    public required string Description { get; init; }

    public static SchemaChangeDto From(SchemaChange c) => new()
    {
        Column = c.Column,
        Kind = c.Kind.ToString(),
        BaselineType = c.BaselineType?.ToString(),
        CurrentType = c.CurrentType?.ToString(),
        BaselineRole = c.BaselineRole?.ToString(),
        CurrentRole = c.CurrentRole?.ToString(),
        Description = c.Description
    };
}

/// <summary>How one measure moved between the two datasets.</summary>
public sealed record MeasureDeltaDto
{
    public required string Measure { get; init; }
    public bool IsAdditive { get; init; }
    public required string Statistic { get; init; }
    public double BaselineValue { get; init; }
    public double CurrentValue { get; init; }
    public double Delta { get; init; }
    public double? DeltaPct { get; init; }
    public required string BaselineTrend { get; init; }
    public required string CurrentTrend { get; init; }
    public bool TrendReversed { get; init; }

    public static MeasureDeltaDto From(MeasureDelta d) => new()
    {
        Measure = d.Measure,
        IsAdditive = d.IsAdditive,
        Statistic = d.Statistic,
        BaselineValue = d.BaselineValue,
        CurrentValue = d.CurrentValue,
        Delta = d.Delta,
        DeltaPct = d.DeltaPct,
        BaselineTrend = d.BaselineTrend.ToString(),
        CurrentTrend = d.CurrentTrend.ToString(),
        TrendReversed = d.TrendReversed
    };
}

/// <summary>How one category's contribution moved between the two datasets.</summary>
public sealed record CategoryShiftDto
{
    public required string Dimension { get; init; }
    public required string Category { get; init; }
    public required string Measure { get; init; }
    public double BaselineValue { get; init; }
    public double CurrentValue { get; init; }
    public double BaselineShare { get; init; }
    public double CurrentShare { get; init; }
    public double ShareDelta { get; init; }
    public bool IsNew { get; init; }
    public bool IsGone { get; init; }

    public static CategoryShiftDto From(CategoryShift s) => new()
    {
        Dimension = s.Dimension,
        Category = s.Category,
        Measure = s.Measure,
        BaselineValue = s.BaselineValue,
        CurrentValue = s.CurrentValue,
        BaselineShare = s.BaselineShare,
        CurrentShare = s.CurrentShare,
        ShareDelta = s.ShareDelta,
        IsNew = s.IsNew,
        IsGone = s.IsGone
    };
}

/// <summary>The full diff of two datasets, with narrative insights and supporting charts.</summary>
public sealed record CompareResponse
{
    public required string BaselineName { get; init; }
    public required string CurrentName { get; init; }
    public int BaselineRowCount { get; init; }
    public int CurrentRowCount { get; init; }

    /// <summary>False when the datasets share no measure; only <see cref="SchemaChanges"/> is meaningful.</summary>
    public bool IsComparable { get; init; }

    /// <summary>How well the two datasets line up, in plain English.</summary>
    public required string CompatibilityNote { get; init; }

    /// <summary>The single most notable difference.</summary>
    public required string Headline { get; init; }

    public required IReadOnlyList<string> SharedMeasures { get; init; }
    public required IReadOnlyList<SchemaChangeDto> SchemaChanges { get; init; }
    public required IReadOnlyList<MeasureDeltaDto> MeasureDeltas { get; init; }
    public required IReadOnlyList<CategoryShiftDto> CategoryShifts { get; init; }
    public required IReadOnlyList<InsightDto> Insights { get; init; }
    public required IReadOnlyList<RecommendationDto> Charts { get; init; }

    public static CompareResponse From(
        DatasetComparison c, IReadOnlyList<Insight> insights,
        IReadOnlyList<TerraFluent.AutoAnalytics.Recommendation.RecommendedChart> charts,
        bool includeSvg = false, ChartStyle style = default) => new()
    {
        BaselineName = c.BaselineName,
        CurrentName = c.CurrentName,
        BaselineRowCount = c.BaselineRowCount,
        CurrentRowCount = c.CurrentRowCount,
        IsComparable = c.IsComparable,
        CompatibilityNote = c.CompatibilityNote,
        Headline = c.Headline,
        SharedMeasures = c.SharedMeasures,
        SchemaChanges = c.SchemaChanges.Select(SchemaChangeDto.From).ToList(),
        MeasureDeltas = c.MeasureDeltas.Select(MeasureDeltaDto.From).ToList(),
        CategoryShifts = c.CategoryShifts.Select(CategoryShiftDto.From).ToList(),
        Insights = insights.Select(InsightDto.From).ToList(),
        Charts = charts.Select(ch => RecommendationDto.From(ch, includeSvg, style)).ToList()
    };
}
