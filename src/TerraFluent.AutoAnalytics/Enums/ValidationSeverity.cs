namespace TerraFluent.AutoAnalytics.Enums;

/// <summary>Severity of a validation finding.</summary>
public enum ValidationSeverity
{
    /// <summary>Informational — no action required.</summary>
    Info,
    /// <summary>Warning — analysis can proceed but results may be affected.</summary>
    Warning,
    /// <summary>Error — the data is malformed and should be corrected.</summary>
    Error
}
