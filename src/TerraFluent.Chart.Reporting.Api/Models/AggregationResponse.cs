using TerraFluent.AutoAnalytics.Analytics;

namespace TerraFluent.Chart.Reporting.Api.Models;

/// <summary>The result of a group-by aggregation — buckets for one dimension, or a pivot for two.</summary>
public sealed record AggregationResponse
{
    public required string Measure { get; init; }
    public required string Dimension { get; init; }
    public string? SecondDimension { get; init; }
    public required string Aggregation { get; init; }
    public bool IsPivot { get; init; }

    public IReadOnlyList<AggregationBucketDto> Buckets { get; init; } = new List<AggregationBucketDto>();
    public IReadOnlyList<PivotCellDto> Cells { get; init; } = new List<PivotCellDto>();
    public IReadOnlyList<string> RowKeys { get; init; } = new List<string>();
    public IReadOnlyList<string> ColumnKeys { get; init; } = new List<string>();

    public static AggregationResponse From(AggregationResult r) => new()
    {
        Measure = r.Measure,
        Dimension = r.Dimension,
        SecondDimension = r.SecondDimension,
        Aggregation = r.Kind.ToString(),
        IsPivot = r.IsPivot,
        Buckets = r.Buckets.Select(b => new AggregationBucketDto { Key = b.Key, Value = b.Value, Count = b.Count }).ToList(),
        Cells = r.Cells.Select(c => new PivotCellDto { Row = c.Row, Column = c.Column, Value = c.Value, Count = c.Count }).ToList(),
        RowKeys = r.RowKeys,
        ColumnKeys = r.ColumnKeys
    };
}

/// <summary>A single group in a one-dimension aggregation.</summary>
public sealed record AggregationBucketDto
{
    public required string Key { get; init; }
    public double Value { get; init; }
    public int Count { get; init; }
}

/// <summary>A single cell in a two-dimension pivot.</summary>
public sealed record PivotCellDto
{
    public required string Row { get; init; }
    public required string Column { get; init; }
    public double Value { get; init; }
    public int Count { get; init; }
}
