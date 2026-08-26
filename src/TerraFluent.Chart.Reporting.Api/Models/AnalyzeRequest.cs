using System.ComponentModel.DataAnnotations;

namespace TerraFluent.Chart.Reporting.Api.Models;

/// <summary>Input format of the raw data supplied to an analytics endpoint.</summary>
public enum AnalyzeFormat
{
    /// <summary>Detect CSV, JSON or base64 XLSX automatically from the payload.</summary>
    Auto = 0,
    /// <summary>Comma-separated values with a header row.</summary>
    Csv = 1,
    /// <summary>A JSON array of flat objects.</summary>
    Json = 2,
    /// <summary>A base64-encoded <c>.xlsx</c> workbook; the first worksheet is read.</summary>
    Xlsx = 3
}

/// <summary>
/// A request to analyse raw tabular data. Supply the raw <see cref="Data"/> as CSV text or a JSON
/// array of objects and, optionally, tuning parameters for the deterministic analysis pipeline.
/// </summary>
public sealed class AnalyzeRequest
{
    /// <summary>
    /// The raw data to analyse — CSV text (with header row), a JSON array of objects, or a
    /// base64-encoded <c>.xlsx</c> workbook when <see cref="Format"/> is <see cref="AnalyzeFormat.Xlsx"/>.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>How to interpret <see cref="Data"/>. Defaults to <see cref="AnalyzeFormat.Auto"/>.</summary>
    public AnalyzeFormat Format { get; set; } = AnalyzeFormat.Auto;

    /// <summary>
    /// Name of a server-configured connection to pull the data from instead of supplying it inline.
    /// Takes precedence over <see cref="Data"/>. List the available names with
    /// <c>GET /api/analytics/connections</c>.
    /// </summary>
    /// <remarks>
    /// Only the <em>name</em> travels: the URL and any credentials stay in server configuration and
    /// are never accepted from, or returned to, a caller. Data is pulled fresh on each request and
    /// discarded once the response is written.
    /// </remarks>
    public string? ConnectionName { get; set; }

    /// <summary>Optional friendly dataset name used in the summary and dashboard title.</summary>
    public string? DatasetName { get; set; }

    /// <summary>
    /// Optional natural-language question for the analytic agent (e.g. "why did revenue change?",
    /// "forecast revenue", "which region leads?"). Ignored by non-agent endpoints.
    /// </summary>
    public string? Question { get; set; }

    /// <summary>
    /// Optional row filter applied before analysis, e.g. <c>region = EU and revenue &gt; 10000</c>.
    /// Conditions are combined with logical AND. Unknown columns are ignored.
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>Maximum insights to return (highest-scoring kept). Default 25.</summary>
    [Range(1, 500)]
    public int? MaxInsights { get; set; }

    /// <summary>Maximum chart recommendations to return (highest-scoring kept). Default 12.</summary>
    [Range(1, 200)]
    public int? MaxRecommendations { get; set; }

    /// <summary>Cap on measure×dimension group combinations evaluated. Default 12.</summary>
    [Range(1, 200)]
    public int? MaxGroupCombinations { get; set; }

    /// <summary>Absolute z-score threshold for anomaly flagging. Default 3.0.</summary>
    [Range(0.1, 10.0)]
    public double? ZScoreThreshold { get; set; }

    /// <summary>When <see langword="true"/>, a data-quality error aborts the analysis (HTTP 422).</summary>
    public bool ThrowOnValidationError { get; set; }
}
