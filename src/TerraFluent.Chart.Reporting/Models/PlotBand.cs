namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// A horizontal reference band drawn across the plot area between two Y values.
    /// Add to <see cref="Axis.PlotBands"/>.
    /// </summary>
    public class PlotBand
    {
        /// <summary>Lower Y value of the band.</summary>
        public double From { get; set; }

        /// <summary>Upper Y value of the band.</summary>
        public double To { get; set; }

        /// <summary>Fill colour (CSS colour string). Defaults to a semi-transparent blue.</summary>
        public string Color { get; set; } = "rgba(68,170,213,0.15)";

        /// <summary>Optional label shown at the right edge of the band.</summary>
        public string? Label { get; set; }
    }
}
