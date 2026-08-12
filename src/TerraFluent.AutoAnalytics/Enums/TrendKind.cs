namespace TerraFluent.AutoAnalytics.Enums;

/// <summary>Direction/shape of a detected trend over an ordered (usually time) series.</summary>
public enum TrendKind
{
    /// <summary>Not enough data to determine a trend.</summary>
    Unknown,
    /// <summary>Values are increasing over the observed period.</summary>
    Rising,
    /// <summary>Values are decreasing over the observed period.</summary>
    Declining,
    /// <summary>Values are broadly flat.</summary>
    Stable,
    /// <summary>Values show a repeating periodic pattern.</summary>
    Seasonal,
    /// <summary>Values fluctuate strongly with no consistent direction.</summary>
    Volatile
}
