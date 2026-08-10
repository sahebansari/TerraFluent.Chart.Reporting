namespace TerraFluent.Chart.Reporting.Api.Models;

/// <summary>Payload returned by the batch render endpoint for each item in the input array.</summary>
public sealed class BatchRenderResult
{
    /// <summary>Zero-based index of the request in the batch.</summary>
    public int Index { get; init; }

    /// <summary><c>true</c> when the chart rendered successfully.</summary>
    public bool Success { get; init; }

    /// <summary>Rendered output (SVG, HTML, or data URI). <c>null</c> when <see cref="Success"/> is <c>false</c>.</summary>
    public string? Output { get; init; }

    /// <summary>Error message when <see cref="Success"/> is <c>false</c>.</summary>
    public string? Error { get; init; }
}
