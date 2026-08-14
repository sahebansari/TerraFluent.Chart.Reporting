namespace TerraFluent.Chart.Reporting.Api.Models;

/// <summary>The response to creating an analytic session.</summary>
public sealed record SessionCreatedResponse
{
    public required string SessionId { get; init; }
    public required SummaryDto Summary { get; init; }
}

/// <summary>A follow-up question posted to an existing analytic session.</summary>
public sealed class SessionAskRequest
{
    /// <summary>The natural-language question. Omit for an open-ended investigation.</summary>
    public string? Question { get; set; }
}
