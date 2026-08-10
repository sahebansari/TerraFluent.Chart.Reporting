namespace TerraFluent.Chart.Reporting.Api.Models;

/// <summary>A single item inside a batch render request.</summary>
public sealed class BatchRenderItem
{
    /// <summary>
    /// Target output format: <c>svg</c> (default), <c>html</c>, or <c>datauri</c>.
    /// Case-insensitive.
    /// </summary>
    public string Format { get; init; } = "svg";

    /// <summary>
    /// Chart configuration serialised as a JSON object (the same schema accepted by the
    /// individual <c>/api/charts/svg</c>, <c>/html</c>, and <c>/datauri</c> endpoints).
    /// </summary>
    public System.Text.Json.JsonElement Options { get; init; }
}
