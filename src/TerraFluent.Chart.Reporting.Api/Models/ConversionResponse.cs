namespace TerraFluent.Chart.Reporting.Api.Models;

/// <summary>
/// The result of normalising a binary tabular source (currently <c>.xlsx</c>) into CSV text that any
/// other analytics endpoint accepts directly as <see cref="AnalyzeRequest.Data"/>.
/// </summary>
public sealed class ConversionResponse
{
    /// <summary>Friendly dataset name derived from the upload (or the caller-supplied name).</summary>
    public string DatasetName { get; init; } = string.Empty;

    /// <summary>The format of <see cref="Data"/> — always <see cref="AnalyzeFormat.Csv"/>.</summary>
    public AnalyzeFormat Format { get; init; } = AnalyzeFormat.Csv;

    /// <summary>The converted CSV text, header row first.</summary>
    public string Data { get; init; } = string.Empty;

    /// <summary>Number of data rows (excluding the header).</summary>
    public int RowCount { get; init; }

    /// <summary>Number of columns detected.</summary>
    public int ColumnCount { get; init; }
}
