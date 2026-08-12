namespace TerraFluent.Chart.Reporting.Api.Models;

/// <summary>Input format of the raw data supplied to an analytics endpoint.</summary>
public enum AnalyzeFormat
{
    /// <summary>Detect CSV vs JSON automatically from the payload.</summary>
    Auto = 0,
    /// <summary>Comma-separated values with a header row.</summary>
    Csv = 1,
    /// <summary>A JSON array of flat objects.</summary>
    Json = 2
}

/// <summary>
/// A request to analyse raw tabular data. Supply the raw <see cref="Data"/> as CSV text or a JSON
/// array of objects and, optionally, tuning parameters for the deterministic analysis pipeline.
/// </summary>
public sealed class AnalyzeRequest
{
    /// <summary>The raw data to analyse — CSV text (with header row) or a JSON array of objects.</summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>How to interpret <see cref="Data"/>. Defaults to <see cref="AnalyzeFormat.Auto"/>.</summary>
    public AnalyzeFormat Format { get; set; } = AnalyzeFormat.Auto;

    /// <summary>Optional friendly dataset name used in the summary and dashboard title.</summary>
    public string? DatasetName { get; set; }

    /// <summary>Maximum insights to return (highest-scoring kept). Default 25.</summary>
    public int? MaxInsights { get; set; }

    /// <summary>Maximum chart recommendations to return (highest-scoring kept). Default 12.</summary>
    public int? MaxRecommendations { get; set; }

    /// <summary>Cap on measure×dimension group combinations evaluated. Default 12.</summary>
    public int? MaxGroupCombinations { get; set; }

    /// <summary>Absolute z-score threshold for anomaly flagging. Default 3.0.</summary>
    public double? ZScoreThreshold { get; set; }

    /// <summary>When <see langword="true"/>, a data-quality error aborts the analysis (HTTP 422).</summary>
    public bool ThrowOnValidationError { get; set; }
}
