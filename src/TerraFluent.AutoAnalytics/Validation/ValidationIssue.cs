using TerraFluent.AutoAnalytics.Enums;

namespace TerraFluent.AutoAnalytics.Validation;

/// <summary>A single data-quality finding produced by the validation engine.</summary>
public sealed class ValidationIssue
{
    /// <summary>Severity bucket.</summary>
    public ValidationSeverity Severity { get; init; }

    /// <summary>Machine-readable code (e.g. <c>MISSING_VALUES</c>, <c>DUPLICATE_ROWS</c>).</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Column the issue relates to, or <see langword="null"/> for row/table-level issues.</summary>
    public string? Column { get; init; }

    /// <summary>Human-readable description.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Number of affected cells/rows, when applicable.</summary>
    public int AffectedCount { get; init; }
}
