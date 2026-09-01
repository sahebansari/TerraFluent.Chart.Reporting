using System.Collections.Generic;

namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// Represents a single data series in the chart (e.g. one line, one bar group, one pie).
    /// </summary>
    public class Series
    {
        /// <summary>Display name of the series, shown in the legend and tooltip.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Chart type for this series (overrides the chart-level primary type).</summary>
        public Enums.ChartType Type { get; set; } = Enums.ChartType.Line;
        /// <summary>Data values. Use <c>null</c> entries to represent missing / gap points.</summary>
        public List<double?> Data { get; set; } = new List<double?>();
        /// <summary>Series colour as a CSS string. <c>null</c> picks from the active theme palette.</summary>
        public string? Color { get; set; }
        /// <summary>Whether the series participates in rendering. Hidden series are excluded from layout calculations.</summary>
        public bool Visible { get; set; } = true;
        /// <summary>SVG stroke-dasharray pattern, e.g. <c>"6,3"</c>. <c>null</c> = solid line.</summary>
        public string? DashStyle { get; set; }
        /// <summary>Stroke width in pixels for line and spline series. Default <c>2</c>.</summary>
        public int LineWidth { get; set; } = 2;
        /// <summary>Whether this series appears in the chart legend. Default <c>true</c>.</summary>
        public bool ShowInLegend { get; set; } = true;

        /// <summary>
        /// Fluent data-label configuration. Chain methods to configure appearance:
        /// <code>cfg.DataLabel.Show().Format("{value}%").Color("#fff").FontSize(11).Background("#7cb5ec")</code>
        /// </summary>
        public DataLabelOptions DataLabel { get; set; } = new DataLabelOptions();

        /// <summary>
        /// For <see cref="Enums.ChartType.Pie"/> series: fraction of the outer radius
        /// left empty as a donut hole (0 = solid pie, 0.5 = half-radius hole).
        /// </summary>
        public double DonutHolePercent { get; set; } = 0.0;

        /// <summary>
        /// Fluent configuration for the label shown in the centre of the donut hole.
        /// Only rendered when <see cref="DonutHolePercent"/> is greater than 0.
        /// Chain methods to configure: <code>cfg.DonutCenter.Show().Title("Total")</code>
        /// </summary>
        public DonutCenterOptions DonutCenter { get; set; } = new DonutCenterOptions();

        /// <summary>
        /// 0 = primary (left) Y-axis; 1 = secondary (right) Y-axis.
        /// Only relevant when <see cref="Models.ChartOptions.YAxis2"/> is configured.
        /// </summary>
        public int YAxisIndex { get; set; } = 0;

        /// <summary>
        /// For <see cref="Enums.ChartType.Waterfall"/> series: marks each data point as a
        /// cumulative "total" bar (drawn from the axis baseline) when <c>true</c>.
        /// When empty the last data point is auto-treated as a total and the first is an
        /// absolute starting bar.
        /// </summary>
        public List<bool> WaterfallTotals { get; set; } = new List<bool>();

        /// <summary>
        /// Opacity of the filled area for <see cref="Enums.ChartType.Area"/> and stacked-area series
        /// (0.0 = transparent … 1.0 = opaque).
        /// Defaults to <c>0.25</c> for normal area and <c>0.7</c> for stacked area when <c>null</c>.
        /// </summary>
        public double? FillOpacity { get; set; }

        /// <summary>
        /// Optional gradient or pattern paint for this series' filled shapes (area, column, bar).
        /// When null, a flat colour is used. Configure via the fluent <c>SeriesBuilder</c> fill helpers.
        /// </summary>
        public SeriesFill? Fill { get; set; }
        // ---- bar / column / scatter border ----
        /// <summary>Stroke color drawn around bar, column, and scatter-marker shapes. No border when null.</summary>
        public string? BorderColor { get; set; }
        /// <summary>Stroke width (px) of the border. Default 0 = no border.</summary>
        public int BorderWidth { get; set; } = 0;
        /// <summary>Corner radius (px) of bar and column rectangles. Default 0 = square corners.</summary>
        public int BorderRadius { get; set; } = 0;

        // ---- marker (line / scatter dots) ----
        /// <summary>
        /// Radius in pixels of the circular marker drawn at each data point for Line, Spline, and Scatter series.
        /// Default <c>null</c> = use renderer default (5 px).
        /// </summary>
        public int? MarkerSize { get; set; }

        /// <summary>
        /// When <c>false</c> the per-point circular markers are suppressed for Line and Spline series.
        /// Has no effect on Scatter (which is markers-only). Default <c>true</c>.
        /// </summary>
        public bool MarkerEnabled { get; set; } = true;

        /// <summary>
        /// Shape of the per-point marker (circle, square, diamond, triangle, …) for Line, Spline,
        /// Area, and Scatter series. Default <see cref="Enums.MarkerSymbol.Circle"/>.
        /// </summary>
        public Enums.MarkerSymbol MarkerSymbol { get; set; } = Enums.MarkerSymbol.Circle;

        /// <summary>
        /// Value-band zones that recolour this series by threshold (e.g. red below zero, green above).
        /// Applies to Line, Spline, and Column series. Empty = single flat colour.
        /// </summary>
        public List<SeriesZone> Zones { get; set; } = new List<SeriesZone>();

        /// <summary>
        /// How null (missing) values are treated in Line, Spline, and Area series.
        /// <see cref="Enums.GapPolicy.Break"/> (default) leaves a visible gap;
        /// <see cref="Enums.GapPolicy.Connect"/> draws a straight line through the gap;
        /// <see cref="Enums.GapPolicy.Zero"/> plots nulls at zero.
        /// </summary>
        public Enums.GapPolicy NullGapPolicy { get; set; } = Enums.GapPolicy.Break;

        /// <summary>
        /// Per-series horizontal reference lines drawn at specific Y values within this series' plot area.
        /// Unlike axis-level <see cref="PlotLine"/>, these are scoped to the series' own Y scale
        /// and are labelled with the series context (e.g. a budget or target for this series only).
        /// Add via <c>SeriesBuilder.TargetLine()</c>.
        /// </summary>
        public List<TargetLineOptions> TargetLines { get; set; } = new List<TargetLineOptions>();

        // ---- new chart type data collections --------------------------------

        /// <summary>
        /// Data points for <see cref="Enums.ChartType.Bubble"/> series.
        /// Each point carries an (X, Y, Z) triplet where Z drives the bubble radius.
        /// </summary>
        public List<BubblePoint> BubbleData { get; set; } = new List<BubblePoint>();

        /// <summary>
        /// Data points for <see cref="Enums.ChartType.ColumnRange"/> and
        /// <see cref="Enums.ChartType.AreaRange"/> series.
        /// Each point carries a (Low, High) pair that defines the bar or band extent.
        /// </summary>
        public List<RangePoint> RangeData { get; set; } = new List<RangePoint>();

        /// <summary>
        /// Data points for a <see cref="Enums.ChartType.BoxPlot"/> series. Each point carries the
        /// five-number summary (Low, Q1, Median, Q3, High) for one category.
        /// </summary>
        public List<BoxPlotPoint> BoxPlotData { get; set; } = new List<BoxPlotPoint>();

        /// <summary>
        /// Data points for a <see cref="Enums.ChartType.Candlestick"/> or
        /// <see cref="Enums.ChartType.Ohlc"/> series. Each point carries the
        /// (Open, High, Low, Close) prices for one trading period.
        /// </summary>
        public List<OhlcPoint> OhlcData { get; set; } = new List<OhlcPoint>();

        /// <summary>
        /// Data points for a <see cref="Enums.ChartType.Heatmap"/> series.
        /// Each point addresses a grid cell via (Col, Row) indices and carries a Value.
        /// </summary>
        public List<HeatmapPoint> HeatmapData { get; set; } = new List<HeatmapPoint>();

        /// <summary>
        /// Row labels for a <see cref="Enums.ChartType.Heatmap"/> series.
        /// Maps zero-based row indices to display strings shown on the left axis of the heatmap.
        /// When empty, rows are labelled "Row 1", "Row 2", …
        /// </summary>
        public List<string> HeatmapRowLabels { get; set; } = new List<string>();

        /// <summary>
        /// Optional AutoInsight configuration for this series.
        /// When non-<c>null</c>, the renderer computes and draws statistical overlays
        /// (trend line, moving average, anomaly bands, peak highlights, narrative summary)
        /// entirely server-side as pure SVG elements.
        /// Applies to <see cref="Enums.ChartType.Line"/>, <see cref="Enums.ChartType.Spline"/>,
        /// and <see cref="Enums.ChartType.Area"/> series.
        /// </summary>
        public AutoInsightOptions? AutoInsight { get; set; }

        /// <summary>
        /// Party/faction groups for a <see cref="Enums.ChartType.Parliament"/> (hemicycle) series.
        /// Each <see cref="ParliamentGroup"/> specifies a name, colour, and seat count; groups are
        /// rendered as contiguous coloured dot blocks ordered left-to-right across the semicircle.
        /// </summary>
        public List<ParliamentGroup> ParliamentData { get; set; } = new List<ParliamentGroup>();

        /// <summary>
        /// Centre-label options for a <see cref="Enums.ChartType.Parliament"/> series.
        /// Controls the text, font size, and colour displayed in the open centre of the hemicycle.
        /// </summary>
        public ParliamentCenterOptions ParliamentCenter { get; set; } = new ParliamentCenterOptions();

        // ---- Phase 6 chart types ----------------------------------------

        /// <summary>
        /// Task bars for a <see cref="Enums.ChartType.Gantt"/> series.
        /// Each <see cref="GanttTask"/> carries a name, numeric start/end, and optional colour.
        /// </summary>
        public List<GanttTask> GanttData { get; set; } = new List<GanttTask>();

        /// <summary>
        /// Named stage nodes for a <see cref="Enums.ChartType.Sankey"/> series.
        /// Index positions must match the <c>From</c>/<c>To</c> values in <see cref="SankeyLinks"/>.
        /// </summary>
        public List<SankeyNode> SankeyNodes { get; set; } = new List<SankeyNode>();

        /// <summary>Directed flow links between <see cref="SankeyNodes"/> for a Sankey series.</summary>
        public List<SankeyLink> SankeyLinks { get; set; } = new List<SankeyLink>();

        /// <summary>
        /// Optional child chart shown when ANY data-point in this series is clicked
        /// (Interactive mode only). Use <see cref="DrilldownCharts"/> for per-point children.
        /// </summary>
        public ChartOptions? DrilldownChart { get; set; }

        /// <summary>
        /// Per-data-point child charts keyed by data index (0-based).
        /// When both this and <see cref="DrilldownChart"/> are set, the per-point entry takes
        /// precedence for that specific index; <see cref="DrilldownChart"/> acts as the fallback.
        /// </summary>
        public System.Collections.Generic.Dictionary<int, ChartOptions> DrilldownCharts { get; set; }
            = new System.Collections.Generic.Dictionary<int, ChartOptions>();
    }
}
