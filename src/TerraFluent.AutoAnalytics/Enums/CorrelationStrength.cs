namespace TerraFluent.AutoAnalytics.Enums;

/// <summary>Qualitative bucket for a correlation coefficient.</summary>
public enum CorrelationStrength
{
    /// <summary>|r| &lt; 0.2 — effectively unrelated.</summary>
    None,
    /// <summary>0.2 ≤ |r| &lt; 0.4.</summary>
    Weak,
    /// <summary>0.4 ≤ |r| &lt; 0.7.</summary>
    Moderate,
    /// <summary>0.7 ≤ |r| &lt; 0.9.</summary>
    Strong,
    /// <summary>|r| ≥ 0.9 — near-perfect linear relationship.</summary>
    VeryStrong
}
