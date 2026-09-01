namespace TerraFluent.Chart.Reporting.Enums
{
    /// <summary>
    /// The scale type applied to an axis.
    /// </summary>
    public enum AxisType
    {
        /// <summary>Standard evenly-spaced linear scale (default).</summary>
        Linear,

        /// <summary>
        /// Logarithmic (base-10) scale. Tick marks and data positions are spaced by their
        /// logarithm so wide-ranging values compress into a readable range.
        /// Only positive values can be plotted; non-positive data is clamped to the axis floor.
        /// </summary>
        Logarithmic,

        /// <summary>
        /// Date/time scale for the X axis. Category labels are generated from
        /// <see cref="Models.Axis.DateTimeValues"/> using an automatically chosen (or explicit)
        /// date format, and dense labels are thinned so the axis stays readable.
        /// </summary>
        DateTime
    }
}
