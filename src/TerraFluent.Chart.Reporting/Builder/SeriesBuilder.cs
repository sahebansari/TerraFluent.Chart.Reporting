using System;
using TerraFluent.Chart.Reporting.Models;

namespace TerraFluent.Chart.Reporting.Builder
{
    /// <summary>
    /// Fluent builder for configuring a single chart series.
    /// Obtain an instance via the <c>configure</c> lambda of
    /// <c>AddLine</c>, <c>AddColumn</c>, <c>AddArea</c>, etc.:
    /// <code>
    /// .Series(s => s
    ///     .AddLine("Revenue", data, cfg => cfg
    ///         .Color(ChartColor.ChartBlue)
    ///         .LineWidth(3)
    ///         .Label(dl => dl.Show().Format("${value}k"))))
    /// </code>
    /// </summary>
    public sealed class SeriesBuilder
    {
        private readonly Series _series;

        internal SeriesBuilder(Series series)
        {
            _series = series ?? throw new ArgumentNullException(nameof(series));
        }

        // ------------------------------------------------------------------ appearance

        /// <summary>Sets the series fill / line colour (hex or CSS colour string).</summary>
        public SeriesBuilder Color(string color)         { _series.Color = color;         return this; }

        /// <summary>Sets the line stroke width in pixels (Line, Spline, Area).</summary>
        public SeriesBuilder LineWidth(int px)           { _series.LineWidth = px;         return this; }

        /// <summary>Draws a solid unbroken line (default).</summary>
        public SeriesBuilder Solid()      { _series.DashStyle = "Solid";    return this; }

        /// <summary>Draws a dashed line.</summary>
        public SeriesBuilder Dashed()     { _series.DashStyle = "Dash";     return this; }

        /// <summary>Draws a dotted line.</summary>
        public SeriesBuilder Dotted()     { _series.DashStyle = "Dot";      return this; }

        /// <summary>Draws a dash-dot alternating line.</summary>
        public SeriesBuilder DashDotted() { _series.DashStyle = "DashDot";  return this; }

        /// <summary>Draws a long-dashed line.</summary>
        public SeriesBuilder LongDashed() { _series.DashStyle = "LongDash"; return this; }

        /// <summary>
        /// Sets the fill opacity for Area series (0.0 = transparent … 1.0 = opaque).
        /// The default when <c>null</c> is 0.25 for normal area and 0.7 for stacked area.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="opacity"/> is outside [0.0, 1.0].</exception>
        public SeriesBuilder FillOpacity(double opacity)
        {
            if (opacity < 0.0 || opacity > 1.0)
                throw new ArgumentOutOfRangeException(nameof(opacity), opacity, "FillOpacity must be between 0.0 and 1.0.");
            _series.FillOpacity = opacity;
            return this;
        }

        // ------------------------------------------------------------------ gradient / pattern fill

        /// <summary>
        /// Fills this series' shapes with a linear gradient. Stops are (offset 0..1, colour) pairs;
        /// <paramref name="angleDegrees"/> is 90 for top→bottom (default), 0 for left→right.
        /// Applies to area, column, and bar series.
        /// </summary>
        public SeriesBuilder LinearGradientFill(int angleDegrees, params (double offset, string color)[] stops)
        {
            if (stops is null || stops.Length == 0)
                throw new ArgumentException("At least one gradient stop is required.", nameof(stops));
            var fill = new SeriesFill { Kind = SeriesFillKind.LinearGradient, Angle = angleDegrees };
            foreach (var s in stops) fill.Stops.Add(new GradientStop(s.offset, s.color));
            _series.Fill = fill;
            return this;
        }

        /// <summary>
        /// Fills this series' shapes with a radial gradient (centre → edge). Stops are
        /// (offset 0..1, colour) pairs. Applies to area, column, and bar series.
        /// </summary>
        public SeriesBuilder RadialGradientFill(params (double offset, string color)[] stops)
        {
            if (stops is null || stops.Length == 0)
                throw new ArgumentException("At least one gradient stop is required.", nameof(stops));
            var fill = new SeriesFill { Kind = SeriesFillKind.RadialGradient };
            foreach (var s in stops) fill.Stops.Add(new GradientStop(s.offset, s.color));
            _series.Fill = fill;
            return this;
        }

        /// <summary>
        /// Fills this series' shapes with a repeating geometric pattern (dots, lines, grid, …).
        /// Applies to area, column, and bar series.
        /// </summary>
        public SeriesBuilder PatternFill(PatternKind pattern, string foreground,
            string? background = null, double size = 8)
        {
            if (foreground is null) throw new ArgumentNullException(nameof(foreground));
            _series.Fill = SeriesFill.PatternFill(pattern, foreground, background, size);
            return this;
        }

        /// <summary>Assigns a pre-built <see cref="SeriesFill"/> to this series.</summary>
        public SeriesBuilder Fill(SeriesFill fill)
        {
            _series.Fill = fill ?? throw new ArgumentNullException(nameof(fill));
            return this;
        }



        /// <summary>Sets the border colour, stroke width, and corner radius in one call.</summary>
        public SeriesBuilder Border(string color, int width = 1, int radius = 0)
        {
            _series.BorderColor  = color;
            _series.BorderWidth  = width;
            _series.BorderRadius = radius;
            return this;
        }

        /// <summary>Sets the stroke colour drawn around bar, column, and scatter-marker shapes.</summary>
        public SeriesBuilder BorderColor(string color)   { _series.BorderColor  = color; return this; }

        /// <summary>Sets the border stroke width in pixels.</summary>
        public SeriesBuilder BorderWidth(int px)         { _series.BorderWidth  = px;    return this; }

        /// <summary>Sets the corner radius in pixels for bar and column rectangles.</summary>
        public SeriesBuilder BorderRadius(int px)        { _series.BorderRadius = px;    return this; }

        // ------------------------------------------------------------------ markers (line / scatter dots)

        /// <summary>
        /// Sets the radius (px) of the circular marker drawn at each data point.
        /// Applies to Line, Spline, and Scatter series.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="radiusPx"/> is less than or equal to zero.</exception>
        public SeriesBuilder MarkerSize(int radiusPx)
        {
            if (radiusPx <= 0) throw new ArgumentOutOfRangeException(nameof(radiusPx), radiusPx, "MarkerSize must be greater than zero.");
            _series.MarkerSize = radiusPx;
            return this;
        }

        /// <summary>
        /// Shows or hides the per-point circular markers for Line and Spline series.
        /// Scatter series are always marker-based and are unaffected.
        /// </summary>
        public SeriesBuilder MarkerEnabled(bool enabled = true)
        {
            _series.MarkerEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Sets the marker shape (circle, square, diamond, triangle, …) drawn at each data point.
        /// Applies to Line, Spline, Area, and Scatter series.
        /// </summary>
        public SeriesBuilder MarkerSymbol(Enums.MarkerSymbol symbol)
        {
            _series.MarkerSymbol = symbol;
            return this;
        }

        // ------------------------------------------------------------------ zones / threshold colour

        /// <summary>
        /// Adds a threshold zone: the series is painted <paramref name="color"/> for values up to and
        /// including <paramref name="upTo"/> (use <c>null</c> for the open-ended final zone).
        /// Add zones in ascending order. Applies to Line, Spline, and Column series.
        /// </summary>
        public SeriesBuilder Zone(double? upTo, string color)
        {
            if (color is null) throw new ArgumentNullException(nameof(color));
            _series.Zones.Add(new SeriesZone(upTo, color));
            return this;
        }

        /// <summary>
        /// Replaces this series' zones with the supplied (upTo, colour) bands. Bands are sorted by
        /// bound ascending; a <c>null</c> bound is treated as the open-ended final zone.
        /// </summary>
        public SeriesBuilder Zones(params (double? upTo, string color)[] zones)
        {
            if (zones is null || zones.Length == 0)
                throw new ArgumentException("At least one zone is required.", nameof(zones));
            _series.Zones.Clear();
            foreach (var z in zones)
            {
                if (z.color is null) throw new ArgumentNullException(nameof(zones));
                _series.Zones.Add(new SeriesZone(z.upTo, z.color));
            }
            return this;
        }

        // ------------------------------------------------------------------ null-gap policy

        /// <summary>
        /// Sets how null (missing) values are treated when drawing the line or area.
        /// <see cref="Enums.GapPolicy.Break"/> (default) leaves a visible gap;
        /// <see cref="Enums.GapPolicy.Connect"/> bridges the gap with a straight segment;
        /// <see cref="Enums.GapPolicy.Zero"/> plots null points at zero.
        /// </summary>
        public SeriesBuilder NullGap(Enums.GapPolicy policy)
        {
            _series.NullGapPolicy = policy;
            return this;
        }

        // ------------------------------------------------------------------ target lines

        /// <summary>
        /// Adds a horizontal reference line at <paramref name="value"/> on this series' Y axis.
        /// Unlike a chart-level <c>PlotLine</c>, the target line is scoped to this series so it
        /// respects the series' Y-axis binding (primary vs secondary).
        /// </summary>
        /// <param name="value">Y value at which the line is drawn.</param>
        /// <param name="label">Optional label shown at the right edge of the line.</param>
        /// <param name="color">Stroke colour. <c>null</c> = inherits the series colour at render time.</param>
        /// <param name="dashStyle">HighCharts-style dash pattern (e.g. <c>"Dash"</c>). <c>null</c> = solid.</param>
        /// <param name="lineWidth">Stroke width in pixels. Default <c>1</c>.</param>
        public SeriesBuilder TargetLine(double value, string? label = null, string? color = null,
            string? dashStyle = null, int lineWidth = 1)
        {
            if (lineWidth <= 0) throw new ArgumentOutOfRangeException(nameof(lineWidth), lineWidth, "Line width must be greater than zero.");
            _series.TargetLines.Add(new Models.TargetLineOptions
            {
                Value     = value,
                Label     = label,
                Color     = color,
                LineWidth = lineWidth,
                DashStyle = dashStyle
            });
            return this;
        }

        // ------------------------------------------------------------------ visibility / legend

        /// <summary>Shows or hides the series in the plot area.</summary>
        public SeriesBuilder Visible(bool visible = true) { _series.Visible = visible; return this; }

        /// <summary>Hides the series from the plot area (data is still included in tooltips).</summary>
        public SeriesBuilder Hide() => Visible(false);

        /// <summary>Shows or hides this series in the legend.</summary>
        public SeriesBuilder ShowInLegend(bool show = true) { _series.ShowInLegend = show; return this; }

        /// <summary>Removes this series from the legend while keeping it visible in the chart.</summary>
        public SeriesBuilder HideFromLegend() => ShowInLegend(false);

        // ------------------------------------------------------------------ AutoInsight

        /// <summary>
        /// Attaches statistical insight overlays to this series.
        /// The renderer computes and renders them server-side as pure SVG elements — zero JS required.
        /// <code>
        /// cfg.AutoInsight(ai => ai
        ///     .AnomalyBands(sigma: 1.5)
        ///     .TrendLine()
        ///     .MovingAverage(period: 3)
        ///     .HighlightPeaks()
        ///     .NarrativeSummary())
        /// </code>
        /// </summary>
        /// <param name="configure">Delegate that configures the <see cref="AutoInsightBuilder"/>.</param>
        public SeriesBuilder AutoInsight(Action<AutoInsightBuilder> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            var builder = new AutoInsightBuilder();
            configure(builder);
            _series.AutoInsight = builder.Build();
            return this;
        }

        // ------------------------------------------------------------------ axis binding

        /// <summary>
        /// Assigns the series to a Y-axis by index. <c>0</c> = primary (left), <c>1</c> = secondary (right).
        /// Requires <see cref="ChartBuilder.YAxis2"/> to be configured when using index 1.
        /// </summary>
        public SeriesBuilder OnYAxis(int index)          { _series.YAxisIndex = index; return this; }

        /// <summary>Binds the series to the secondary (right-hand) Y-axis. Shorthand for <c>OnYAxis(1)</c>.</summary>
        public SeriesBuilder OnSecondaryAxis()            => OnYAxis(1);

        // ------------------------------------------------------------------ pie / donut

        /// <summary>
        /// Cuts a donut hole of the given radius fraction (0 = solid pie, 0.5 = half-radius hole).
        /// Values are clamped to [0, 0.95].
        /// </summary>
        public SeriesBuilder DonutHole(double fraction)
        {
            _series.DonutHolePercent = Math.Max(0, Math.Min(0.95, fraction));
            return this;
        }

        // ------------------------------------------------------------------ data labels

        /// <summary>
        /// Provides direct access to the <see cref="DataLabelOptions"/> so you can chain its own
        /// fluent methods: <c>cfg.DataLabel.Show().Format("{value}%").Color("#fff")</c>.
        /// </summary>
        public DataLabelOptions DataLabel => _series.DataLabel;

        /// <summary>
        /// Configures data labels via a lambda, keeping the <see cref="SeriesBuilder"/> chain intact:
        /// <code>cfg.Label(dl => dl.Show().Format("${value}k").Color("#fff"))</code>
        /// </summary>
        public SeriesBuilder Label(Action<DataLabelOptions> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(_series.DataLabel);
            return this;
        }

        // ------------------------------------------------------------------ donut center

        /// <summary>
        /// Provides direct access to the <see cref="DonutCenterOptions"/> so you can chain its
        /// own fluent methods: <c>cfg.DonutCenter.Show().Title("Total")</c>.
        /// </summary>
        public DonutCenterOptions DonutCenter => _series.DonutCenter;

        /// <summary>
        /// Configures the donut-center label via a lambda, keeping the <see cref="SeriesBuilder"/> chain intact:
        /// <code>cfg.Center(c => c.Show().Title("Total").Color("#333"))</code>
        /// </summary>
        public SeriesBuilder Center(Action<DonutCenterOptions> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(_series.DonutCenter);
            return this;
        }

        // ------------------------------------------------------------------ parliament centre

        /// <summary>
        /// Provides direct access to the <see cref="Models.ParliamentCenterOptions"/> so you
        /// can chain its fluent methods: <c>cfg.ParliamentCenter.Text("300").Subtitle("seats")</c>.
        /// </summary>
        public Models.ParliamentCenterOptions ParliamentCenter => _series.ParliamentCenter;

        /// <summary>
        /// Configures the Parliament centre label via a lambda, keeping the builder chain intact:
        /// <code>cfg.CenterLabel(c => c.Text("300").Subtitle("members").Color("#333"))</code>
        /// </summary>
        public SeriesBuilder CenterLabel(Action<Models.ParliamentCenterOptions> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(_series.ParliamentCenter);
            return this;
        }

        // ------------------------------------------------------------------ heatmap

        /// <summary>
        /// Provides direct access to the row-label list for a <see cref="Models.Series.HeatmapRowLabels"/> series.
        /// Add labels in row order: <c>cfg.HeatmapRowLabels.AddRange(new[]{"North","South"})</c>.
        /// </summary>
        public System.Collections.Generic.List<string> HeatmapRowLabels => _series.HeatmapRowLabels;

        // ------------------------------------------------------------------ drill-down

        /// <summary>
        /// Attaches a child chart that replaces the current view when any data-point in this
        /// series is clicked (Interactive mode only). A ◀ Back button returns to the parent.
        /// </summary>
        public SeriesBuilder WithDrilldown(Models.ChartOptions childChart)
        {
            if (childChart is null) throw new ArgumentNullException(nameof(childChart));
            _series.DrilldownChart = childChart;
            return this;
        }
        /// <summary>
        /// Attaches a per-point child chart at <paramref name="dataIndex"/>.
        /// Overrides the whole-series <see cref="WithDrilldown(Models.ChartOptions)"/> fallback for that index.
        /// </summary>
        public SeriesBuilder WithDrilldown(int dataIndex, Models.ChartOptions childChart)
        {
            if (childChart is null) throw new ArgumentNullException(nameof(childChart));
            _series.DrilldownCharts[dataIndex] = childChart;
            return this;
        }    }
}
