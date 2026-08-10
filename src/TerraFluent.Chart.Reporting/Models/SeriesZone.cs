namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// A value band ("zone") that recolours the portion of a series whose y-value falls within it.
    /// Zones are evaluated in ascending <see cref="Value"/> order: a point is painted with the first
    /// zone whose <see cref="Value"/> is <c>null</c> (open-ended) or greater-than-or-equal to the
    /// point's value. Enables threshold / negative-value colouring on line and column series.
    /// </summary>
    public sealed class SeriesZone
    {
        /// <summary>
        /// Inclusive upper bound of this zone. A point with value ≤ <see cref="Value"/> belongs to
        /// this zone. <c>null</c> means the zone extends to positive infinity (the final zone).
        /// </summary>
        public double? Value { get; set; }

        /// <summary>CSS colour applied to the series within this zone.</summary>
        public string Color { get; set; } = "#000000";

        /// <summary>Creates an empty zone.</summary>
        public SeriesZone() { }

        /// <summary>Creates a zone covering values up to (and including) <paramref name="value"/>.</summary>
        public SeriesZone(double? value, string color)
        {
            Value = value;
            Color = color;
        }
    }
}
