namespace TerraFluent.AutoAnalytics.Enums;

/// <summary>Detected granularity of a date/time column.</summary>
public enum DateGranularity
{
    /// <summary>Could not be determined.</summary>
    Unknown,
    /// <summary>One observation per day (or finer).</summary>
    Daily,
    /// <summary>Weekly spacing.</summary>
    Weekly,
    /// <summary>Monthly spacing.</summary>
    Monthly,
    /// <summary>Quarterly spacing.</summary>
    Quarterly,
    /// <summary>Yearly spacing.</summary>
    Yearly
}
