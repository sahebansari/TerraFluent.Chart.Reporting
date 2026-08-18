using System;
using System.Collections.Generic;
using TerraFluent.Chart.Reporting.Models;

namespace TerraFluent.Chart.Reporting.Builder
{
    /// <summary>
    /// Abstraction over <see cref="ChartBuilder"/> that enables constructor injection and
    /// unit-testing without rendering real SVG output.
    /// </summary>
    /// <remarks>
    /// Obtain a concrete instance via <see cref="ChartBuilder.Create()"/> and accept
    /// <see cref="IChartBuilder"/> in your application services so callers can substitute
    /// a test double without taking a dependency on the real renderer.
    /// <code>
    /// // Application service
    /// public class ReportService
    /// {
    ///     private readonly IChartBuilder _chart;
    ///     public ReportService(IChartBuilder chart) => _chart = chart;
    ///     public string BuildRevenueSvg(double[] data) =>
    ///         _chart.Title("Revenue").Series(s => s.AddLine("Rev", data)).RenderToSvg();
    /// }
    ///
    /// // Production wiring
    /// services.AddScoped&lt;IChartBuilder&gt;(_ => ChartBuilder.Create());
    /// </code>
    /// </remarks>
    public interface IChartBuilder
    {
        // ------------------------------------------------------------------ chart type

        /// <summary>Sets the default chart type to Pie / Donut.</summary>
        IChartBuilder AsPie();
        /// <summary>Sets the default chart type to Line.</summary>
        IChartBuilder AsLine();
        /// <summary>Sets the default chart type to Area.</summary>
        IChartBuilder AsArea();
        /// <summary>Sets the default chart type to Column (vertical bars).</summary>
        IChartBuilder AsColumn();
        /// <summary>Sets the default chart type to Bar (horizontal bars).</summary>
        IChartBuilder AsBar();
        /// <summary>Sets the default chart type to Spline (smooth Bézier curves).</summary>
        IChartBuilder AsSpline();
        /// <summary>Sets the default chart type to Scatter (dots only).</summary>
        IChartBuilder AsScatter();
        /// <summary>Sets the default chart type to Waterfall.</summary>
        IChartBuilder AsWaterfall();
        /// <summary>Sets the default chart type to Gauge.</summary>
        IChartBuilder AsGauge();
        /// <summary>Sets the default chart type to DataRing.</summary>
        IChartBuilder AsDataRing();
        /// <summary>Sets the default chart type to Bubble (scatter with size-encoded Z dimension).</summary>
        IChartBuilder AsBubble();
        /// <summary>Sets the default chart type to Heatmap (colour-coded grid matrix).</summary>
        IChartBuilder AsHeatmap();
        /// <summary>Sets the default chart type to ColumnRange (vertical bars spanning low\u2013high).</summary>
        IChartBuilder AsColumnRange();
        /// <summary>Sets the default chart type to AreaRange (filled band between low and high lines).</summary>
        IChartBuilder AsAreaRange();
        /// <summary>Sets the default chart type to Funnel (stacked trapezoid stages).</summary>
        IChartBuilder AsFunnel();
        /// <summary>Sets the default chart type to Treemap (balanced binary-split nested rectangles).</summary>
        IChartBuilder AsTreemap();
        /// <summary>Sets the default chart type to Dumbbell (dot-plot with low–high range dots).</summary>
        IChartBuilder AsDumbbell();
        /// <summary>Sets the default chart type to Stream (ThemeRiver stacked-area with centered baseline).</summary>
        IChartBuilder AsStream();
        /// <summary>Sets the default chart type to Gantt (horizontal task timeline).</summary>
        IChartBuilder AsGantt();
        /// <summary>Sets the default chart type to Sankey (node-link flow diagram).</summary>
        IChartBuilder AsSankey();

        // ------------------------------------------------------------------ render mode

        /// <summary>Renders in static mode — no CSS hover, no JS, no SMIL animation. Safe for PDF and email. Also sets <c>Animation.Enabled = false</c>.</summary>
        IChartBuilder AsStatic();
        /// <summary>Renders with SMIL/CSS animations and hover tooltips. No JS.</summary>
        IChartBuilder AsAnimated();
        /// <summary>Renders with CSS hover effects and embedded JS. Browser only.</summary>
        IChartBuilder AsInteractive();

        // ------------------------------------------------------------------ labels / categories

        /// <summary>Sets slice or X-axis category labels.</summary>
        IChartBuilder Labels(params string[] labels);

        // ------------------------------------------------------------------ dimensions

        /// <summary>Sets chart width and height in a single call.</summary>
        IChartBuilder Size(int width, int height);
        /// <summary>Sets only the chart width. Pass <c>null</c> for responsive (fluid) width.</summary>
        IChartBuilder Width(int? width);
        /// <summary>Sets only the chart height.</summary>
        IChartBuilder Height(int height);
        /// <summary>Sets the chart background colour.</summary>
        IChartBuilder Background(string color);

        // ------------------------------------------------------------------ theme / palette

        /// <summary>Applies a built-in or custom theme.</summary>
        IChartBuilder Theme(ChartTheme theme);
        /// <summary>Overrides the series colour palette (params overload).</summary>
        IChartBuilder Colors(params string[] colors);
        /// <summary>Overrides the series colour palette from any enumerable source.</summary>
        IChartBuilder Colors(IEnumerable<string> colors);

        // ------------------------------------------------------------------ stacking

        /// <summary>Stacks Column / Area series cumulatively.</summary>
        IChartBuilder StackNormal();
        /// <summary>Stacks Column / Area series normalised to 100 %.</summary>
        IChartBuilder StackPercent();

        // ------------------------------------------------------------------ title / subtitle

        /// <summary>Sets the chart title text.</summary>
        IChartBuilder Title(string text);
        /// <summary>Configures the chart title via a lambda.</summary>
        IChartBuilder Title(Action<ChartTitle> configure);
        /// <summary>Sets the chart subtitle text.</summary>
        IChartBuilder Subtitle(string text);
        /// <summary>Configures the chart subtitle object (text, alignment, custom style).</summary>
        IChartBuilder Subtitle(Action<ChartTitle> configure);

        // ------------------------------------------------------------------ axes

        /// <summary>Configures the X-axis via a lambda.</summary>
        IChartBuilder XAxis(Action<Axis> configure);
        /// <summary>Sets the X-axis title and category labels.</summary>
        IChartBuilder XAxis(string title, params string[] categories);
        /// <summary>Configures the primary Y-axis via a lambda.</summary>
        IChartBuilder YAxis(Action<Axis> configure);
        /// <summary>Sets the Y-axis title and optional min/max.</summary>
        IChartBuilder YAxis(string title, double? min = null, double? max = null);
        /// <summary>Sets the Y-axis tick-label format string. Throws <see cref="ArgumentException"/> when null or whitespace.</summary>
        IChartBuilder YAxisFormat(string format);
        /// <summary>Sets the X-axis tick-label format string.</summary>
        IChartBuilder XAxisFormat(string format);
        /// <summary>Shows or hides the background grid lines across the plot area for both axes.</summary>
        IChartBuilder GridLines(bool visible = true);
        /// <summary>Hides the background grid lines across the plot area for both axes.</summary>
        IChartBuilder HideGridLines();
        /// <summary>Sets the Y-axis tick interval in data units. Must be greater than zero.</summary>
        IChartBuilder YAxisTickInterval(double interval);
        /// <summary>Sets the X-axis tick interval in data units. Must be greater than zero.</summary>
        IChartBuilder XAxisTickInterval(double interval);
        /// <summary>Configures the secondary (right-hand) Y-axis.</summary>
        IChartBuilder YAxis2(Action<Axis> configure);

        // ------------------------------------------------------------------ plot annotations

        /// <summary>Adds a reference band to the primary Y-axis.</summary>
        IChartBuilder PlotBand(double from, double to, string color = "rgba(68,170,213,0.15)", string? label = null);
        /// <summary>Adds a reference line to the primary Y-axis.</summary>
        IChartBuilder PlotLine(double value, string color = ChartColor.Red, int width = 1, string? label = null, string? dashStyle = null);

        // ------------------------------------------------------------------ legend / tooltip

        /// <summary>Configures the legend via a lambda.</summary>
        IChartBuilder Legend(Action<LegendBuilder> configure);
        /// <summary>Hides the legend.</summary>
        IChartBuilder HideLegend();
        /// <summary>Configures the tooltip via a lambda.</summary>
        IChartBuilder Tooltip(Action<TooltipBuilder> configure);
        /// <summary>Disables tooltips.</summary>
        IChartBuilder DisableTooltip();

        // ------------------------------------------------------------------ animation

        /// <summary>Enables animation with the specified duration in milliseconds.</summary>
        IChartBuilder Animate(int milliseconds = 800);
        /// <summary>Configures animation timing and easing via a lambda.</summary>
        IChartBuilder Animation(Action<AnimationBuilder> configure);
        /// <summary>Disables all load animations.</summary>
        IChartBuilder DisableAnimation();

        // ------------------------------------------------------------------ series management

        /// <summary>Adds and configures chart series.</summary>
        IChartBuilder Series(Action<ChartSeriesBuilder> configure);
        /// <summary>Removes all series.</summary>
        IChartBuilder ClearSeries();
        /// <summary>Removes the first series matching <paramref name="name"/>.</summary>
        IChartBuilder RemoveSeries(string name);
        /// <summary>Enables data labels on all series.</summary>
        IChartBuilder ShowDataLabels();

        /// <summary>Adds an SVG download button to the top-right corner (Interactive mode only).</summary>
        IChartBuilder ShowExportButton(string label = "\u2b07 SVG");

        /// <summary>
        /// Adds a multi-format export drop-down menu to the top-right corner (Interactive mode only).
        /// Supported format strings: <c>"SVG"</c>, <c>"PNG"</c>, <c>"JPEG"</c>, <c>"PDF"</c>.
        /// Omit <paramref name="formats"/> to include all four.
        /// </summary>
        IChartBuilder ShowExportMenu(params string[] formats);

        /// <summary>
        /// Sets the chart to responsive (fluid) width by clearing any fixed pixel width.
        /// The SVG will render with <c>width="100%"</c>.
        /// </summary>
        IChartBuilder ResponsiveWidth();

        // ------------------------------------------------------------------ data quality

        /// <summary>
        /// Configures the builder to throw a <see cref="Models.DataQualityException"/> when
        /// <see cref="RenderToSvg"/> is called and at least one
        /// <see cref="TerraFluent.Chart.Reporting.Enums.WarningSeverity"/> (<c>Error</c>) finding is detected.
        /// </summary>
        IChartBuilder ThrowOnDataQualityErrors();

        /// <summary>
        /// Returns the <see cref="Models.DataQualityReport"/> from the most recent implicit
        /// analysis (run automatically inside every render call) or from an explicit
        /// <see cref="AnalyzeDataQuality"/> call. Returns <c>null</c> before the first render
        /// or analysis.
        /// </summary>
        Models.DataQualityReport? GetLastDataQualityReport();

        /// <summary>
        /// Explicitly analyses data quality without rendering. The report is also cached and
        /// returned by <see cref="GetLastDataQualityReport"/>.
        /// </summary>
        Models.DataQualityReport AnalyzeDataQuality();

        // ------------------------------------------------------------------ render

        /// <summary>Renders the chart and returns the SVG markup as a string.</summary>
        string RenderToSvg();
        /// <summary>Renders the chart and returns a self-contained HTML fragment.</summary>
        string RenderToHtml(string? caption = null, string? cssClass = null);
        /// <summary>Renders the chart and returns the SVG as a UTF-8 byte array.</summary>
        byte[] RenderToBytes();
        /// <summary>Renders the chart and writes the SVG to <paramref name="stream"/>.</summary>
        void RenderToStream(System.IO.Stream stream);
        /// <summary>Renders the chart and writes the SVG file to <paramref name="filePath"/>.</summary>
        void RenderToFile(string filePath);
        /// <summary>Renders the chart and writes the HTML fragment to <paramref name="filePath"/>.</summary>
        void RenderToHtmlFile(string filePath, string? caption = null, string? cssClass = null);

#if NET6_0_OR_GREATER
        /// <summary>Asynchronously renders the chart and writes UTF-8 SVG bytes to <paramref name="stream"/>.</summary>
        System.Threading.Tasks.Task RenderToStreamAsync(
            System.IO.Stream stream,
            System.Threading.CancellationToken cancellationToken = default);

        /// <summary>Asynchronously renders the chart and writes the SVG file to <paramref name="filePath"/>.</summary>
        System.Threading.Tasks.Task RenderToFileAsync(
            string filePath,
            System.Threading.CancellationToken cancellationToken = default);

        /// <summary>Asynchronously renders the chart HTML fragment and writes it to <paramref name="filePath"/>.</summary>
        System.Threading.Tasks.Task RenderToHtmlFileAsync(
            string filePath,
            string? caption = null,
            string? cssClass = null,
            System.Threading.CancellationToken cancellationToken = default);
#endif

        /// <summary>Appends a data table below the chart showing the raw series values per category.</summary>
        IChartBuilder ShowDataTable(int rowHeight = 20, int fontSize = 10);

        // ------------------------------------------------------------------ accessibility / localization

        /// <summary>Overrides the auto-generated accessible name (SVG title and aria-label).</summary>
        IChartBuilder AriaLabel(string label);

        /// <summary>Overrides the auto-generated accessible description (SVG &lt;desc&gt; element).</summary>
        IChartBuilder AriaDescription(string description);

        /// <summary>Sets the display culture for axis tick-label number formatting and the SVG lang attribute.</summary>
        IChartBuilder Culture(System.Globalization.CultureInfo culture);

        /// <summary>Sets the display culture by name (e.g. "de-DE", "ar-SA", "fr-FR").</summary>
        IChartBuilder Culture(string cultureName);

        /// <summary>
        /// Enables right-to-left text direction. Adds <c>dir="rtl"</c> to the SVG root
        /// and <c>direction:rtl</c> CSS to all text elements.
        /// </summary>
        IChartBuilder RightToLeft(bool rtl = true);

        // ------------------------------------------------------------------ template

        /// <summary>Applies an <see cref="IChartTemplate"/> preset, overwriting only the settings the template touches.</summary>
        IChartBuilder ApplyTemplate(IChartTemplate template);

        // ------------------------------------------------------------------ range selector / sync / drill-down

        /// <summary>
        /// Enables the interactive range-selector (navigator) strip below the chart.
        /// Only rendered in <see cref="TerraFluent.Chart.Reporting.Enums.SvgMode.Interactive"/> mode.
        /// </summary>
        IChartBuilder RangeSelector(Action<Models.RangeSelectorOptions> configure);

        /// <summary>
        /// Assigns this chart to a synchronised-tooltip group.  All interactive SVGs on the
        /// same HTML page sharing the same <paramref name="groupId"/> will mirror hover
        /// highlights in real time.
        /// </summary>
        IChartBuilder SyncGroup(string groupId);

        // ------------------------------------------------------------------ snapshot / advanced

        /// <summary>Returns the raw <see cref="ChartOptions"/> for advanced customisation.</summary>
        ChartOptions GetOptions();
        /// <summary>Returns an independent deep-copy snapshot of the current configuration.</summary>
        ChartOptions Build();
        /// <summary>
        /// Returns a new <see cref="IChartBuilder"/> that starts from a deep copy of the current
        /// configuration, allowing independent variant charts to be produced from the same data.
        /// </summary>
        IChartBuilder Fork();
    }
}
