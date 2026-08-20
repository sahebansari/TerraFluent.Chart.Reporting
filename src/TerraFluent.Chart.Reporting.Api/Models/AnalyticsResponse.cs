using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Recommendation;
using TerraFluent.AutoAnalytics.Validation;

namespace TerraFluent.Chart.Reporting.Api.Models;

/// <summary>Clean JSON response returned by the analytics endpoints.</summary>
public sealed record AnalyticsResponse
{
    public required SummaryDto Summary { get; init; }
    public required ValidationDto Validation { get; init; }
    public required IReadOnlyList<InsightDto> Insights { get; init; }
    public required IReadOnlyList<RecommendationDto> Recommendations { get; init; }
    public required IReadOnlyList<ColumnProfileDto> Columns { get; init; }

    /// <summary>Maps a domain <see cref="AnalyticsResult"/> to the API response DTO.</summary>
    public static AnalyticsResponse From(AnalyticsResult r, bool includeSvg = false, ChartStyle style = default) => new()
    {
        Summary = SummaryDto.From(r.Summary),
        Validation = ValidationDto.From(r.Validation),
        Insights = r.Insights.Select(InsightDto.From).ToList(),
        Recommendations = r.Recommendations.Select(rec => RecommendationDto.From(rec, includeSvg, style)).ToList(),
        Columns = r.Profile.Columns.Select(ColumnProfileDto.From).ToList()
    };
}

/// <summary>Executive summary of the analysis.</summary>
public sealed record SummaryDto
{
    public required string DatasetName { get; init; }
    public int RowCount { get; init; }
    public int ColumnCount { get; init; }
    public int MeasureCount { get; init; }
    public int DimensionCount { get; init; }
    public int InsightCount { get; init; }
    public int RecommendationCount { get; init; }
    public required string Headline { get; init; }
    public required IReadOnlyList<string> KeyFindings { get; init; }
    public required string DataQuality { get; init; }

    public static SummaryDto From(AnalyticsSummary s) => new()
    {
        DatasetName = s.DatasetName,
        RowCount = s.RowCount,
        ColumnCount = s.ColumnCount,
        MeasureCount = s.MeasureCount,
        DimensionCount = s.DimensionCount,
        InsightCount = s.InsightCount,
        RecommendationCount = s.RecommendationCount,
        Headline = s.Headline,
        KeyFindings = s.KeyFindings,
        DataQuality = s.DataQuality
    };
}

/// <summary>Aggregated data-quality verdict.</summary>
public sealed record ValidationDto
{
    public bool IsClean { get; init; }
    public bool HasErrors { get; init; }
    public required IReadOnlyList<ValidationIssueDto> Issues { get; init; }

    public static ValidationDto From(ValidationReport v) => new()
    {
        IsClean = v.IsClean,
        HasErrors = v.HasErrors,
        Issues = v.Issues.Select(i => new ValidationIssueDto
        {
            Severity = i.Severity.ToString(),
            Code = i.Code,
            Column = i.Column,
            Message = i.Message,
            AffectedCount = i.AffectedCount
        }).ToList()
    };
}

/// <summary>A single data-quality finding.</summary>
public sealed record ValidationIssueDto
{
    public required string Severity { get; init; }
    public required string Code { get; init; }
    public string? Column { get; init; }
    public required string Message { get; init; }
    public int AffectedCount { get; init; }
}

/// <summary>A ranked, natural-language insight.</summary>
public sealed record InsightDto
{
    public required string Kind { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public int ImportanceScore { get; init; }
    public required IReadOnlyDictionary<string, string> Evidence { get; init; }
    public required IReadOnlyList<string> RelatedColumns { get; init; }

    public static InsightDto From(Insight i) => new()
    {
        Kind = i.Kind.ToString(),
        Title = i.Title,
        Description = i.Description,
        ImportanceScore = i.ImportanceScore,
        Evidence = i.Evidence,
        RelatedColumns = i.RelatedColumns
    };
}

/// <summary>A ranked chart recommendation with an optional pre-rendered SVG.</summary>
public sealed record RecommendationDto
{
    public required string ChartType { get; init; }
    public int SuitabilityScore { get; init; }
    public required string Reason { get; init; }
    public required string Title { get; init; }
    public required IReadOnlyList<string> Categories { get; init; }
    public required IReadOnlyList<SeriesDto> Series { get; init; }

    /// <summary>Rendered SVG markup (only populated when the caller requests it).</summary>
    public string? Svg { get; init; }

    public static RecommendationDto From(RecommendedChart rec, bool includeSvg, ChartStyle style = default) => new()
    {
        ChartType = rec.ChartName,
        SuitabilityScore = rec.SuitabilityScore,
        Reason = rec.Reason,
        Title = rec.Spec.Title,
        Categories = rec.Spec.Categories,
        Series = rec.Spec.Series.Select(s => new SeriesDto
        {
            Name = s.Name,
            Values = s.Values,
            ScalarValue = s.ScalarValue
        }).ToList(),
        // Match the dashboard's chart size (560×340) so text scales to the same on-page size —
        // both are displayed at width:100%, so a wider intrinsic chart would render smaller text.
        Svg = includeSvg ? style.Apply(ChartConfigBuilder.ToChartBuilder(rec.Spec).Size(560, 340)).RenderToSvg() : null
    };
}

/// <summary>A named data series within a recommended chart.</summary>
public sealed record SeriesDto
{
    public required string Name { get; init; }
    public required IReadOnlyList<double?> Values { get; init; }
    public double? ScalarValue { get; init; }
}

/// <summary>Per-column inferred schema and key statistics.</summary>
public sealed record ColumnProfileDto
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required string Role { get; init; }
    public int DistinctCount { get; init; }
    public int MissingCount { get; init; }
    public double Completeness { get; init; }
    public double? Min { get; init; }
    public double? Max { get; init; }
    public double? Mean { get; init; }
    public double? Sum { get; init; }

    /// <summary>True when the measure can be meaningfully summed; false for per-row attributes
    /// (age, tenure, ratios, rates) where only average/min/max/count make sense.</summary>
    public bool Additive { get; init; }

    public static ColumnProfileDto From(ColumnStatistics c) => new()
    {
        Name = c.Profile.Name,
        Type = c.Profile.Type.ToString(),
        Role = c.Profile.Role.ToString(),
        DistinctCount = c.Profile.DistinctCount,
        MissingCount = c.Profile.MissingCount,
        Completeness = c.Profile.Completeness,
        Min = c.Numeric?.Min,
        Max = c.Numeric?.Max,
        Mean = c.Numeric?.Mean,
        Sum = c.Numeric?.Sum,
        Additive = TerraFluent.AutoAnalytics.Schema.MeasureSemantics.IsAdditive(c.Profile, c.Numeric?.Min, c.Numeric?.Max)
    };
}
