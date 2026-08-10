namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// Represents one party or faction in a <see cref="Enums.ChartType.Parliament"/> (hemicycle) chart.
    /// Each group contributes a contiguous block of coloured dots to the semicircular seating layout.
    /// </summary>
    public sealed class ParliamentGroup
    {
        /// <summary>Display name of the party or faction shown in the chart legend.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Fill colour of the dots that represent this group's members.
        /// Accepts any CSS colour string (hex, rgb, named colour).
        /// </summary>
        public string Color { get; set; } = "#888888";

        /// <summary>Number of seats (members) held by this group.</summary>
        public int Seats { get; set; }

        /// <summary>Initialises an empty <see cref="ParliamentGroup"/>.</summary>
        public ParliamentGroup() { }

        /// <summary>Initialises a <see cref="ParliamentGroup"/> with all required fields.</summary>
        /// <param name="name">Party or faction name.</param>
        /// <param name="color">Dot fill colour (CSS colour string).</param>
        /// <param name="seats">Number of seats held.</param>
        public ParliamentGroup(string name, string color, int seats)
        {
            Name  = name;
            Color = color;
            Seats = seats;
        }
    }
}
