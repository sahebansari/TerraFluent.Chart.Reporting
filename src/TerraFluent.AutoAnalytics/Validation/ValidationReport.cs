using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Enums;

namespace TerraFluent.AutoAnalytics.Validation;

/// <summary>Aggregated result of the validation phase.</summary>
public sealed class ValidationReport
{
    /// <summary>All findings, ordered by descending severity.</summary>
    public IReadOnlyList<ValidationIssue> Issues { get; }

    public ValidationReport(IEnumerable<ValidationIssue> issues)
        => Issues = issues?.OrderByDescending(i => i.Severity).ToList() ?? new List<ValidationIssue>();

    /// <summary>True when at least one <see cref="ValidationSeverity.Error"/> was found.</summary>
    public bool HasErrors => Issues.Any(i => i.Severity == ValidationSeverity.Error);

    /// <summary>True when no warnings or errors were found.</summary>
    public bool IsClean => !Issues.Any(i => i.Severity >= ValidationSeverity.Warning);

    /// <summary>Count of issues at the given severity.</summary>
    public int CountOf(ValidationSeverity severity) => Issues.Count(i => i.Severity == severity);
}
