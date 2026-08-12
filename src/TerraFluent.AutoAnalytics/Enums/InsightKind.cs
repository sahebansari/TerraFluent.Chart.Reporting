namespace TerraFluent.AutoAnalytics.Enums;

/// <summary>The category of a generated insight, used for grouping and iconography.</summary>
public enum InsightKind
{
    /// <summary>A general descriptive observation.</summary>
    Observation,
    /// <summary>A trend over time (growth, decline).</summary>
    Trend,
    /// <summary>A dominant category or concentration.</summary>
    Dominance,
    /// <summary>A statistical relationship between two measures.</summary>
    Correlation,
    /// <summary>An outlier or anomalous value.</summary>
    Anomaly,
    /// <summary>A distribution/spread characteristic.</summary>
    Distribution,
    /// <summary>A data-quality finding surfaced as an insight.</summary>
    DataQuality
}
