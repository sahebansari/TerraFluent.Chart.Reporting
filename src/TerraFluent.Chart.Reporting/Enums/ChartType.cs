namespace TerraFluent.Chart.Reporting.Enums
{
    /// <summary>
    /// The type of chart to render.
    /// </summary>
    public enum ChartType
    {
        /// <summary>Data points connected by straight line segments.</summary>
        Line,
        /// <summary>Horizontal bar chart — bars grow left-to-right from the value origin.</summary>
        Bar,
        /// <summary>Vertical column (bar) chart — bars grow upward from the baseline.</summary>
        Column,
        /// <summary>Area chart — like a line chart with the area below the line filled.</summary>
        Area,
        /// <summary>Pie / donut chart — data rendered as circular slices.</summary>
        Pie,
        /// <summary>Scatter chart — individual data points with no connecting line.</summary>
        Scatter,
        /// <summary>Smooth spline chart — data points connected by cubic Bézier curves.</summary>
        Spline,
        /// <summary>Running-total / increment bar chart — positive increments green, negative red, totals neutral.</summary>
        Waterfall,
        /// <summary>Semi-circular gauge dial showing a single value against a min/max range.</summary>
        Gauge,
        /// <summary>Full-circle progress ring with the value displayed prominently in the centre.</summary>
        DataRing,
        /// <summary>Bubble chart — scatter points with a third dimension (Z) encoded as circle radius.</summary>
        Bubble,
        /// <summary>Heatmap — colour-coded matrix of cells; column labels from <c>XAxis.Categories</c>, row labels from <c>Series.HeatmapRowLabels</c>.</summary>
        Heatmap,
        /// <summary>Column-range chart — each category shows a vertical bar spanning from a low to a high value.</summary>
        ColumnRange,
        /// <summary>Area-range chart — a filled band between a lower and an upper data line per category.</summary>
        AreaRange,
        /// <summary>Funnel chart — stacked trapezoids showing a value at each conversion stage; labels from <c>XAxis.Categories</c>.</summary>
        Funnel,
        /// <summary>Treemap — nested rectangles sized proportionally to value; uses a balanced binary-split layout; labels from <c>XAxis.Categories</c>.</summary>
        Treemap,
        /// <summary>
        /// Parliament (hemicycle) chart — a semicircular seating diagram where every dot represents one
        /// legislative member and colour identifies the party or faction.
        /// Data is supplied as a collection of <see cref="TerraFluent.Chart.Reporting.Models.ParliamentGroup"/> objects
        /// via <c>AddParliament()</c>.
        /// </summary>
        Parliament,

        /// <summary>
        /// Radar (spider / polar) chart — each category is a spoke radiating from a shared centre and
        /// the series values are connected into a closed polygon. Category labels come from
        /// <c>XAxis.Categories</c>. Add via <c>AddRadar()</c>.
        /// </summary>
        Radar,

        /// <summary>
        /// Box-and-whisker plot — each category shows a distribution's five-number summary
        /// (min, lower quartile, median, upper quartile, max). Add via <c>AddBoxPlot()</c>.
        /// </summary>
        BoxPlot,

        /// <summary>
        /// Error-bar series — an I-beam whisker spanning a low/high range per category, typically
        /// overlaid on a column or scatter series to show uncertainty. Add via <c>AddErrorBar()</c>.
        /// </summary>
        ErrorBar,

        /// <summary>
        /// Candlestick (financial) chart — each category shows an open/high/low/close bar: a thin
        /// high-low wick with a thick open-close body, coloured up (close ≥ open) or down.
        /// Add via <c>AddCandlestick()</c>.
        /// </summary>
        Candlestick,

        /// <summary>
        /// OHLC (open-high-low-close) bar chart — each category shows a vertical high-low line with a
        /// left tick for the open and a right tick for the close. Add via <c>AddOhlc()</c>.
        /// </summary>
        Ohlc,

        /// <summary>
        /// Dumbbell (dot-plot) chart — two filled dots per category connected by a thin vertical line,
        /// representing a low–high range (before/after, min/max). Uses <c>RangeData</c>. Add via <c>AddDumbbell()</c>.
        /// </summary>
        Dumbbell,

        /// <summary>
        /// Stream (ThemeRiver) graph — stacked area series with a centered wiggle baseline, producing a
        /// flowing organic look. Each series contributes a filled band. Add via <c>AddStream()</c>.
        /// </summary>
        Stream,

        /// <summary>
        /// Gantt / timeline chart — horizontal task bars spanning a numeric start–end range, with task
        /// names on the left axis. Uses <c>GanttData</c>. Add via <c>AddGantt()</c>.
        /// </summary>
        Gantt,

        /// <summary>
        /// Sankey flow diagram — rectangular nodes connected by proportionally sized curved links
        /// showing quantities flowing between stages or categories. Uses <c>SankeyNodes</c> and
        /// <c>SankeyLinks</c>. Add via <c>AddSankey()</c>.
        /// </summary>
        Sankey
    }
}
