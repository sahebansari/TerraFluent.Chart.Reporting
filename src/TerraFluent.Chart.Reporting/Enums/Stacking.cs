namespace TerraFluent.Chart.Reporting.Enums
{
    /// <summary>
    /// Controls how multiple Column or Area series are stacked on the same axis.
    /// </summary>
    public enum Stacking
    {
        /// <summary>Series are plotted side-by-side (default).</summary>
        None,

        /// <summary>Series values are stacked; each bar/area starts where the previous one ended.</summary>
        Normal,

        /// <summary>Like Normal, but each column is normalised to 100 % of the total.</summary>
        Percent
    }
}
