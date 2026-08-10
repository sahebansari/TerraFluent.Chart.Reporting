namespace TerraFluent.Chart.Reporting.Enums
{
    /// <summary>
    /// Controls how null (missing) data points are treated in line, spline, and area series.
    /// </summary>
    public enum GapPolicy
    {
        /// <summary>A null point breaks the line/area into separate segments, leaving a visible gap (default).</summary>
        Break,

        /// <summary>Null points are skipped and the line is drawn straight through to the next valid point.</summary>
        Connect,

        /// <summary>Null points are treated as zero and are plotted on the baseline.</summary>
        Zero
    }
}
