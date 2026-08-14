namespace TerraFluent.AutoAnalytics.Agent;

/// <summary>The kind of analytic intent an <see cref="AnalyticGoal"/> expresses.</summary>
public enum GoalKind
{
    /// <summary>Open-ended investigation — run every applicable skill and follow the evidence.</summary>
    Explore,
    /// <summary>Focus on directional movement of a measure over time.</summary>
    Trend,
    /// <summary>Focus on statistical relationships between measures.</summary>
    Correlation,
    /// <summary>Focus on outliers/spikes in a measure.</summary>
    Anomaly,
    /// <summary>Focus on which category concentrates a measure (share/breakdown).</summary>
    Dominance,
    /// <summary>Project a measure's future trajectory.</summary>
    Forecast,
    /// <summary>Explain why a measure changed by attributing the change to categories.</summary>
    RootCause,
    /// <summary>Cluster rows into natural segments across the numeric measures.</summary>
    Segment,
    /// <summary>Compare a measure across calendar periods (month/quarter/year over period).</summary>
    Compare
}
