using System;
using System.Collections.Generic;
using TerraFluent.Chart.Reporting.Enums;
using TerraFluent.Chart.Reporting.Models;
using TerraFluent.Chart.Reporting.Rendering;

namespace TerraFluent.Chart.Reporting.Builder
{
    /// <summary>
    /// Fluent builder for constructing and rendering a chart.
    /// Entry point: <see cref="ChartBuilder.Create()"/>.
    /// </summary>
    /// <remarks>
    /// <b>Thread safety:</b> <see cref="ChartBuilder"/> is stateful and <b>not thread-safe</b>.
    /// Create a separate instance per render call or per thread.
    /// <see cref="SvgRenderer"/> is stateless and may be shared across threads.
    /// </remarks>
    public sealed class ChartBuilder : IChartBuilder
    {
        private readonly ChartOptions _options = new ChartOptions();
        private readonly ISvgRenderer _renderer;
        private bool _globalDataLabels;
        private bool _throwOnDataQualityErrors;
        private Models.DataQualityReport? _lastDataQualityReport;

        private ChartBuilder(ISvgRenderer renderer)
        {
            _renderer = renderer;
        }

        // private ctor used by FromJson / FromOptions to inject a pre-built options object
        private ChartBuilder(ISvgRenderer renderer, Models.ChartOptions options)
        {
            _renderer = renderer;
            _options  = options;
        }

        // ------------------------------------------------------------------ factory

        /// <summary>Creates a new <see cref="ChartBuilder"/> using the default SVG renderer.</summary>
        public static ChartBuilder Create() => new ChartBuilder(new SvgRenderer());

        /// <summary>Creates a new <see cref="ChartBuilder"/> with a custom renderer.</summary>
        public static ChartBuilder Create(ISvgRenderer renderer)
        {
            if (renderer is null) throw new ArgumentNullException(nameof(renderer));
            return new ChartBuilder(renderer);
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Deserialises a JSON string produced by <see cref="Models.ChartOptions.ToJson"/> and returns
        /// a pre-populated <see cref="ChartBuilder"/> ready to render or further configure.
        /// Only available on .NET 6 and later.
        /// </summary>
        public static ChartBuilder FromJson(string json)
        {
            var opts = Models.ChartOptions.FromJson(json);
            return new ChartBuilder(new SvgRenderer(), opts);
        }
#endif

        /// <summary>
        /// Returns a new <see cref="ChartBuilder"/> that starts from a deep copy of the current
        /// configuration. Use this to produce variants (e.g. light vs dark theme) without
        /// mutating the original builder.
        /// The forked builder shares the same renderer instance (which is stateless).
        /// </summary>
        public ChartBuilder Fork()
        {
            var fork = new ChartBuilder(_renderer);
            var snap = _options.Clone();
            // Copy all option fields from the snapshot into the fork's options
            fork._options.Width             = snap.Width;
            fork._options.Height            = snap.Height;
            fork._options.BackgroundColor   = snap.BackgroundColor;
            fork._options.Theme             = snap.Theme;
            fork._options.Stacking          = snap.Stacking;
            fork._options.PrimaryChartType  = snap.PrimaryChartType;
            fork._options.RenderMode        = snap.RenderMode;
            fork._options.YAxis2            = snap.YAxis2;
            fork._options.Title.Text        = snap.Title.Text;
            fork._options.Title.Align       = snap.Title.Align;
            fork._options.Title.Style       = snap.Title.Style;
            fork._options.Subtitle.Text     = snap.Subtitle.Text;
            fork._options.Subtitle.Align    = snap.Subtitle.Align;
            fork._options.Subtitle.Style    = snap.Subtitle.Style;
            CopyAxisTo(snap.XAxis,  fork._options.XAxis);
            CopyAxisTo(snap.YAxis,  fork._options.YAxis);
            CopyLegendTo(snap.Legend, fork._options.Legend);
            fork._options.Animation.Enabled  = snap.Animation.Enabled;
            fork._options.Animation.Duration = snap.Animation.Duration;
            fork._options.Animation.Easing   = snap.Animation.Easing;
            fork._options.Tooltip            = snap.Tooltip;
            fork._options.PointClickHandler  = snap.PointClickHandler;
            fork._options.ExportButtonEnabled = snap.ExportButtonEnabled;
            fork._options.ExportButtonLabel   = snap.ExportButtonLabel;
            fork._options.ExportMenuEnabled   = snap.ExportMenuEnabled;
            fork._options.ExportMenuFormats   = new System.Collections.Generic.List<string>(snap.ExportMenuFormats);
            foreach (var s in snap.Series) fork._options.Series.Add(s);
            foreach (var a in snap.Annotations) fork._options.Annotations.Add(a);
            fork._globalDataLabels           = _globalDataLabels;
            fork._throwOnDataQualityErrors    = _throwOnDataQualityErrors;
            fork._options.LabelLayout        = snap.LabelLayout?.Clone();
            fork._options.AnnotationLayout    = snap.AnnotationLayout?.Clone();
            return fork;
        }

        /// <summary>
        /// Returns a deep copy of this builder. All series, axes, annotations, and options are
        /// duplicated; the two builders are fully independent after this call.
        /// Alias for <see cref="Fork"/>.
        /// </summary>
        public ChartBuilder Clone() => Fork();

        private static void CopyAxisTo(Models.Axis src, Models.Axis dst)
        {
            dst.Title          = src.Title;
            dst.Min            = src.Min;
            dst.Max            = src.Max;
            dst.Visible        = src.Visible;
            dst.GridLineVisible = src.GridLineVisible;
            dst.GridLineColor  = src.GridLineColor;
            dst.LabelFormat    = src.LabelFormat;
            dst.TickInterval   = src.TickInterval;
            dst.LabelRotation  = src.LabelRotation;
            dst.Categories.AddRange(src.Categories);
            foreach (var b in src.PlotBands)
                dst.PlotBands.Add(new Models.PlotBand { From = b.From, To = b.To, Color = b.Color, Label = b.Label });
            foreach (var l in src.PlotLines)
                dst.PlotLines.Add(new Models.PlotLine { Value = l.Value, Color = l.Color, Width = l.Width, DashStyle = l.DashStyle, Label = l.Label });
        }

        private static void CopyLegendTo(Models.Legend src, Models.Legend dst)
        {
            dst.Enabled         = src.Enabled;
            dst.Align           = src.Align;
            dst.VerticalAlign   = src.VerticalAlign;
            dst.Layout          = src.Layout;
            dst.X               = src.X;
            dst.Y               = src.Y;
            dst.Padding         = src.Padding;
            dst.Margin          = src.Margin;
            dst.ItemStyle       = src.ItemStyle;
            dst.ItemFontSize    = src.ItemFontSize;
            dst.ItemFontColor   = src.ItemFontColor;
            dst.SymbolWidth     = src.SymbolWidth;
            dst.SymbolHeight    = src.SymbolHeight;
            dst.SymbolRadius    = src.SymbolRadius;
            dst.BorderColor     = src.BorderColor;
            dst.BorderWidth     = src.BorderWidth;
            dst.BorderRadius    = src.BorderRadius;
            dst.BackgroundColor = src.BackgroundColor;
        }


        /// <summary>
        /// Sets the default series type to Pie (or Donut).
        /// <para>
        /// <b>Scope:</b> this only sets the fallback type used by the generic
        /// <see cref="ChartSeriesBuilder.Add(string,System.Collections.Generic.IEnumerable{double?},System.Action{TerraFluent.Chart.Reporting.Builder.SeriesBuilder})"/> method.
        /// Series added via typed helpers (<c>AddLine</c>, <c>AddColumn</c>, etc.) always use their
        /// own explicit type and are <b>not</b> affected by this setting.
        /// </para>
        /// </summary>
        public ChartBuilder AsPie()       { _options.PrimaryChartType = ChartType.Pie;       return this; }

        /// <summary>
        /// Sets the default series type to Line — also the fallback when no primary type is configured.
        /// <para><b>Scope:</b> applies only to the generic <c>Add()</c> method; typed helpers such as
        /// <c>AddColumn()</c> or <c>AddArea()</c> always use their own explicit type.</para>
        /// </summary>
        public ChartBuilder AsLine()      { _options.PrimaryChartType = ChartType.Line;      return this; }

        /// <summary>
        /// Sets the default series type to Area.
        /// <para><b>Scope:</b> applies only to the generic <c>Add()</c> method.</para>
        /// </summary>
        public ChartBuilder AsArea()      { _options.PrimaryChartType = ChartType.Area;      return this; }

        /// <summary>
        /// Sets the default series type to Column (vertical bars).
        /// <para><b>Scope:</b> applies only to the generic <c>Add()</c> method.</para>
        /// </summary>
        public ChartBuilder AsColumn()    { _options.PrimaryChartType = ChartType.Column;    return this; }

        /// <summary>
        /// Sets the default series type to Bar (horizontal bars).
        /// <para><b>Scope:</b> applies only to the generic <c>Add()</c> method.</para>
        /// </summary>
        public ChartBuilder AsBar()       { _options.PrimaryChartType = ChartType.Bar;       return this; }

        /// <summary>
        /// Sets the default series type to Spline (smooth Bézier curves).
        /// <para><b>Scope:</b> applies only to the generic <c>Add()</c> method.</para>
        /// </summary>
        public ChartBuilder AsSpline()    { _options.PrimaryChartType = ChartType.Spline;    return this; }

        /// <summary>
        /// Sets the default series type to Scatter (dots only, no connecting line).
        /// <para><b>Scope:</b> applies only to the generic <c>Add()</c> method.</para>
        /// </summary>
        public ChartBuilder AsScatter()   { _options.PrimaryChartType = ChartType.Scatter;   return this; }

        /// <summary>
        /// Sets the default series type to Waterfall.
        /// <para><b>Scope:</b> applies only to the generic <c>Add()</c> method.</para>
        /// </summary>
        public ChartBuilder AsWaterfall() { _options.PrimaryChartType = ChartType.Waterfall; return this; }

        /// <summary>
        /// Sets the default series type to Gauge (semi-circular dial).
        /// <para><b>Scope:</b> applies only to the generic <c>Add()</c> method.</para>
        /// </summary>
        public ChartBuilder AsGauge()     { _options.PrimaryChartType = ChartType.Gauge;     return this; }

        /// <summary>
        /// Sets the default series type to DataRing (full 360° progress ring).
        /// <para><b>Scope:</b> applies only to the generic <c>Add()</c> method.</para>
        /// </summary>
        public ChartBuilder AsDataRing()  { _options.PrimaryChartType = ChartType.DataRing;  return this; }

        /// <summary>
        /// Sets the default series type to Bubble (scatter with size-encoded Z dimension).
        /// <para><b>Scope:</b> applies only to the generic <c>Add()</c> method.</para>
        /// </summary>
        public ChartBuilder AsBubble()    { _options.PrimaryChartType = ChartType.Bubble;    return this; }

        /// <summary>
        /// Sets the default series type to Heatmap (colour-coded grid matrix).
        /// <para><b>Scope:</b> applies only to the generic <c>Add()</c> method.</para>
        /// </summary>
        public ChartBuilder AsHeatmap()   { _options.PrimaryChartType = ChartType.Heatmap;   return this; }

        /// <summary>
        /// Sets the default series type to ColumnRange (vertical bars spanning low–high).
        /// <para><b>Scope:</b> applies only to the generic <c>Add()</c> method.</para>
        /// </summary>
        public ChartBuilder AsColumnRange() { _options.PrimaryChartType = ChartType.ColumnRange; return this; }

        /// <summary>
        /// Sets the default series type to AreaRange (filled band between low and high lines).
        /// <para><b>Scope:</b> applies only to the generic <c>Add()</c> method.</para>
        /// </summary>
        public ChartBuilder AsAreaRange()  { _options.PrimaryChartType = ChartType.AreaRange;  return this; }

        /// <summary>
        /// Sets the default series type to Funnel (stacked trapezoid stages).
        /// <para><b>Scope:</b> applies only to the generic <c>Add()</c> method.</para>
        /// </summary>
        public ChartBuilder AsFunnel()    { _options.PrimaryChartType = ChartType.Funnel;    return this; }

        /// <summary>
        /// Sets the default series type to Treemap (squarified nested rectangles).
        /// <para><b>Scope:</b> applies only to the generic <c>Add()</c> method.</para>
        /// </summary>
        public ChartBuilder AsTreemap()   { _options.PrimaryChartType = ChartType.Treemap;   return this; }

        /// <summary>Sets the default series type to Dumbbell (dot-plot with low–high range dots).</summary>
        public ChartBuilder AsDumbbell()  { _options.PrimaryChartType = ChartType.Dumbbell;  return this; }

        /// <summary>Sets the default series type to Stream (ThemeRiver stacked-area with centered baseline).</summary>
        public ChartBuilder AsStream()    { _options.PrimaryChartType = ChartType.Stream;    return this; }

        /// <summary>Sets the default series type to Gantt (horizontal task timeline).</summary>
        public ChartBuilder AsGantt()     { _options.PrimaryChartType = ChartType.Gantt;     return this; }

        /// <summary>Sets the default series type to Sankey (node-link flow diagram).</summary>
        public ChartBuilder AsSankey()    { _options.PrimaryChartType = ChartType.Sankey;    return this; }

        /// <summary>
        /// Sets the slice / category labels for the chart — a clean alternative to
        /// <c>WithXAxis(x =&gt; x.Categories.AddRange(...))</c>.
        /// For <b>Pie/Donut</b> charts these become the per-slice labels used in tooltips and legends.
        /// For <b>Bar/Column/Line</b> charts these become the X-axis category names.
        /// </summary>
        public ChartBuilder Labels(params string[] labels)
        {
            _options.XAxis.Categories.Clear();
            _options.XAxis.Categories.AddRange(labels);
            return this;
        }

        // ------------------------------------------------------------------ chart dimensions

        /// <summary>
        /// Sets the chart width and height in a single call.
        /// </summary>
        /// <param name="width">Width in pixels. Must be greater than zero.</param>
        /// <param name="height">Height in pixels. Must be greater than zero.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="width"/> or <paramref name="height"/> is less than or equal to zero.
        /// </exception>
        public ChartBuilder Size(int width, int height)
        {
            if (width  <= 0) throw new ArgumentOutOfRangeException(nameof(width),  width,  "Chart width must be greater than zero.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Chart height must be greater than zero.");
            _options.Width  = width;
            _options.Height = height;
            return this;
        }

        /// <summary>
        /// Sets only the chart width in pixels, leaving the height unchanged.
        /// Pass <c>null</c> to revert to responsive / fluid width (<c>width="100%"</c> in the SVG).
        /// </summary>
        /// <param name="width">Width in pixels. Must be greater than zero when non-null.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="width"/> is zero or negative.</exception>
        public ChartBuilder Width(int? width)
        {
            if (width.HasValue && width.Value <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), width.Value, "Chart width must be greater than zero.");
            _options.Width = width;
            return this;
        }

        /// <summary>
        /// Sets the chart to responsive (fluid) width by clearing any fixed pixel width.
        /// The SVG will render with <c>width="100%"</c>.
        /// </summary>
        public ChartBuilder ResponsiveWidth()
        {
            _options.Width = null;
            return this;
        }

        /// <summary>
        /// Convenience toggle for fluid / responsive width.
        /// Equivalent to calling <see cref="ResponsiveWidth"/> when <paramref name="responsive"/> is <c>true</c>.
        /// </summary>
        public ChartBuilder Responsive(bool responsive = true)
        {
            if (responsive) _options.Width = null;
            return this;
        }

        /// <summary>
        /// Sets only the chart height in pixels, leaving the width unchanged.
        /// </summary>
        /// <param name="height">Height in pixels. Must be greater than zero.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="height"/> is zero or negative.</exception>
        public ChartBuilder Height(int height)
        {
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "Chart height must be greater than zero.");
            _options.Height = height;
            return this;
        }

        /// <summary>Sets the chart background colour, overriding the active theme background.</summary>
        /// <param name="color">A CSS colour string (hex, rgb, named colour, etc.).</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="color"/> is null or whitespace.</exception>
        public ChartBuilder Background(string color)
        {
            if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Background color must not be empty.", nameof(color));
            _options.BackgroundColor = color;
            return this;
        }

        // ------------------------------------------------------------------ theme

        /// <summary>Applies a built-in or custom theme (palette, fonts, background).</summary>
        public ChartBuilder Theme(ChartTheme theme)
        {
            if (theme is null) throw new ArgumentNullException(nameof(theme));
            _options.Theme = theme;
            return this;
        }

        /// <summary>Stacks Column and Area series cumulatively — each segment starts where the previous ended.</summary>
        public ChartBuilder StackNormal()  { _options.Stacking = Enums.Stacking.Normal;  return this; }

        /// <summary>Stacks Column and Area series normalised to 100 % of the total per category.</summary>
        public ChartBuilder StackPercent() { _options.Stacking = Enums.Stacking.Percent; return this; }

        /// <summary>
        /// Overrides the series colour palette without changing the rest of the active theme.
        /// Colours are applied to series in order; the list wraps when there are more series than colours.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when <paramref name="colors"/> is null or empty.</exception>
        public ChartBuilder Colors(params string[] colors)
        {
            if (colors is null || colors.Length == 0)
                throw new ArgumentException("Provide at least one colour.", nameof(colors));
            return ApplyColors(colors);
        }

        /// <summary>
        /// Overrides the series colour palette from any enumerable source (e.g. a database-driven palette).
        /// Colours are applied to series in order; the list wraps when there are more series than colours.
        /// </summary>
        /// <param name="colors">At least one CSS colour string.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="colors"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="colors"/> yields no elements.</exception>
        public ChartBuilder Colors(IEnumerable<string> colors)
        {
            if (colors is null) throw new ArgumentNullException(nameof(colors));
            var arr = new System.Collections.Generic.List<string>(colors).ToArray();
            if (arr.Length == 0) throw new ArgumentException("Provide at least one colour.", nameof(colors));
            return ApplyColors(arr);
        }

        private ChartBuilder ApplyColors(string[] colors)
        {
            var t = _options.Theme;
            _options.Theme = new ChartTheme
            {
                BackgroundColor     = t.BackgroundColor,
                PlotBackgroundColor = t.PlotBackgroundColor,
                GridLineColor       = t.GridLineColor,
                AxisLineColor       = t.AxisLineColor,
                TextColor           = t.TextColor,
                FontFamily          = t.FontFamily,
                FontScale           = t.FontScale,
                Colors              = colors
            };
            return this;
        }

        // ------------------------------------------------------------------ title

        /// <summary>Sets the chart title text.</summary>
        /// <param name="text">The title string to display. Pass <c>null</c> or empty to hide the title.</param>
        public ChartBuilder Title(string text)
        {
            _options.Title.Text = text;
            return this;
        }

        /// <summary>Configures the chart title object (text, alignment, custom style).</summary>
        public ChartBuilder Title(Action<ChartTitle> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(_options.Title);
            return this;
        }

        /// <summary>Sets the chart subtitle text displayed below the main title.</summary>
        /// <param name="text">The subtitle string. Pass <c>null</c> or empty to hide the subtitle.</param>
        public ChartBuilder Subtitle(string text)
        {
            _options.Subtitle.Text = text;
            return this;
        }

        /// <summary>Configures the chart subtitle object (text, alignment, custom style).</summary>
        public ChartBuilder Subtitle(Action<ChartTitle> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(_options.Subtitle);
            return this;
        }

        // ------------------------------------------------------------------ axes

        /// <summary>Configures the X-axis using a lambda that receives the <see cref="Axis"/> object directly.</summary>
        /// <param name="configure">Configuration action. Must not be <c>null</c>.</param>
        public ChartBuilder XAxis(Action<Axis> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(_options.XAxis);
            return this;
        }

        /// <summary>Sets the X-axis title and category labels in a single call.</summary>
        public ChartBuilder XAxis(string title, params string[] categories)
        {
            _options.XAxis.Title = title;
            _options.XAxis.Categories.Clear();
            if (categories != null) _options.XAxis.Categories.AddRange(categories);
            return this;
        }

        /// <summary>Configures the primary Y-axis using a lambda that receives the <see cref="Axis"/> object directly.</summary>
        /// <param name="configure">Configuration action. Must not be <c>null</c>.</param>
        public ChartBuilder YAxis(Action<Axis> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(_options.YAxis);
            return this;
        }

        /// <summary>Configures the secondary (right-hand) Y-axis. Creates it if it does not exist yet.</summary>
        public ChartBuilder YAxis2(Action<Axis> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            if (_options.YAxis2 is null) _options.YAxis2 = new Axis();
            configure(_options.YAxis2);
            return this;
        }

        /// <summary>Sets the Y-axis title and optional min/max scale in a single call.</summary>
        /// <param name="title">Y-axis title text.</param>
        /// <param name="min">Minimum scale value. Must be less than <paramref name="max"/> when both are provided.</param>
        /// <param name="max">Maximum scale value. Must be greater than <paramref name="min"/> when both are provided.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when both <paramref name="min"/> and <paramref name="max"/> are specified and <paramref name="min"/> is
        /// greater than or equal to <paramref name="max"/>.
        /// </exception>
        public ChartBuilder YAxis(string title, double? min = null, double? max = null)
        {
            if (min.HasValue && max.HasValue && min.Value >= max.Value)
                throw new ArgumentException(
                    $"Y-axis min ({min.Value}) must be less than max ({max.Value}).",
                    nameof(min));
            _options.YAxis.Title = title;
            if (min.HasValue) _options.YAxis.Min = min;
            if (max.HasValue) _options.YAxis.Max = max;
            return this;
        }

        /// <summary>
        /// Sets the Y-axis tick-label format string. Use <c>{value}</c> for the compact
        /// abbreviated number (e.g. <c>1.5k</c>), or <c>{value:fmt}</c> with a standard .NET
        /// numeric format specifier for an exact number — e.g. <c>"{value}%"</c>,
        /// <c>"${value:N0}"</c>, or <c>"{value:F2}"</c>.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when <paramref name="format"/> is null or whitespace.</exception>
        public ChartBuilder YAxisFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
                throw new ArgumentException("Format must not be empty.", nameof(format));
            _options.YAxis.LabelFormat = format;
            return this;
        }

        /// <summary>Sets the X-axis tick-label format string. See <see cref="YAxisFormat"/> for the token syntax.</summary>
        public ChartBuilder XAxisFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
                throw new ArgumentException("Format must not be empty.", nameof(format));
            _options.XAxis.LabelFormat = format;
            return this;
        }

        /// <summary>
        /// Switches the primary Y-axis to a base-10 logarithmic scale. Only positive values plot;
        /// non-positive data is clamped to the axis floor.
        /// </summary>
        public ChartBuilder YAxisLogarithmic()
        {
            _options.YAxis.Type = Enums.AxisType.Logarithmic;
            return this;
        }

        /// <summary>Switches the secondary (right-hand) Y-axis to a base-10 logarithmic scale.</summary>
        public ChartBuilder YAxis2Logarithmic()
        {
            _options.YAxis2 ??= new Axis();
            _options.YAxis2.Type = Enums.AxisType.Logarithmic;
            return this;
        }

        /// <summary>
        /// Inverts the primary Y-axis direction: minimum value at top, maximum at bottom.
        /// Useful for ranking charts (rank 1 at top) and depth charts.
        /// </summary>
        public ChartBuilder ShowDataTable(int rowHeight = 20, int fontSize = 10)
        {
            _options.DataTable.Visible   = true;
            _options.DataTable.RowHeight = rowHeight;
            _options.DataTable.FontSize  = fontSize;
            return this;
        }

        // ------------------------------------------------------------------ accessibility / localization

        /// <summary>Overrides the auto-generated accessible name on the SVG root and its &lt;title&gt; element.</summary>
        public ChartBuilder AriaLabel(string label)
        {
            if (label is null) throw new ArgumentNullException(nameof(label));
            _options.AriaLabel = label;
            return this;
        }

        /// <summary>Overrides the auto-generated accessible description in the SVG &lt;desc&gt; element.</summary>
        public ChartBuilder AriaDescription(string description)
        {
            if (description is null) throw new ArgumentNullException(nameof(description));
            _options.AriaDescription = description;
            return this;
        }

        /// <summary>Sets the display culture for axis tick-label number formatting and the SVG <c>lang</c> attribute.</summary>
        public ChartBuilder Culture(System.Globalization.CultureInfo culture)
        {
            _options.DisplayCulture = culture ?? throw new ArgumentNullException(nameof(culture));
            return this;
        }

        /// <summary>Sets the display culture by name (e.g. "de-DE", "ar-SA", "fr-FR").</summary>
        public ChartBuilder Culture(string cultureName)
            => Culture(new System.Globalization.CultureInfo(
                    cultureName ?? throw new ArgumentNullException(nameof(cultureName))));

        /// <summary>Adds <c>dir="rtl"</c> to the SVG root and <c>direction:rtl</c> CSS to all text elements.</summary>
        public ChartBuilder RightToLeft(bool rtl = true)
        {
            _options.RightToLeft = rtl;
            return this;
        }

        // ------------------------------------------------------------------ template

        /// <summary>Applies a template preset. Only the settings the template touches are changed; all other settings are preserved.</summary>
        public ChartBuilder ApplyTemplate(IChartTemplate template)
        {
            if (template is null) throw new ArgumentNullException(nameof(template));
            template.Apply(this);
            return this;
        }

        // ------------------------------------------------------------------ range selector / sync group

        /// <summary>Configures the interactive range-selector navigator strip (Interactive mode only).</summary>
        public ChartBuilder RangeSelector(Action<Models.RangeSelectorOptions> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(_options.RangeSelector);
            return this;
        }

        /// <summary>Assigns this chart to a synchronised-tooltip group.</summary>
        public ChartBuilder SyncGroup(string groupId)
        {
            _options.SyncGroup = groupId ?? throw new ArgumentNullException(nameof(groupId));
            return this;
        }

        /// <summary>Inverts the primary Y-axis direction (minimum at top, maximum at bottom).</summary>
        public ChartBuilder YAxisInverted(bool inverted = true)
        {
            _options.YAxis.Inverted = inverted;
            return this;
        }

        /// <summary>Inverts the secondary (right-hand) Y-axis direction.</summary>
        public ChartBuilder YAxis2Inverted(bool inverted = true)
        {
            _options.YAxis2 ??= new Axis();
            _options.YAxis2.Inverted = inverted;
            return this;
        }

        /// <summary>
        /// Configures the X axis as a date/time axis. Each supplied value is formatted into a
        /// category label using <paramref name="format"/>, or an automatically chosen format based
        /// on the overall time span when <paramref name="format"/> is <c>null</c>. Dense labels are
        /// thinned by the renderer so the axis stays readable.
        /// </summary>
        /// <param name="values">The date/time value for each data point, in series order.</param>
        /// <param name="format">Optional .NET date format string (e.g. <c>"MMM yyyy"</c>).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="values"/> is empty.</exception>
        public ChartBuilder XAxisDateTime(IEnumerable<DateTime> values, string? format = null)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var list = new List<DateTime>(values);
            if (list.Count == 0)
                throw new ArgumentException("At least one date/time value is required.", nameof(values));

            string fmt = format ?? AutoDateFormat(list);
            _options.XAxis.Type = Enums.AxisType.DateTime;
            _options.XAxis.DateTimeLabelFormat = fmt;
            _options.XAxis.DateTimeValues.Clear();
            _options.XAxis.DateTimeValues.AddRange(list);
            _options.XAxis.Categories.Clear();
            foreach (var dt in list)
                _options.XAxis.Categories.Add(dt.ToString(fmt, System.Globalization.CultureInfo.InvariantCulture));
            return this;
        }

        /// <summary>
        /// Picks a sensible date format based on the span between the earliest and latest value:
        /// time-of-day for &lt;= 2 days, day+month for &lt;= 90 days, month+year for &lt;= 2 years,
        /// otherwise year only.
        /// </summary>
        private static string AutoDateFormat(List<DateTime> values)
        {
            DateTime min = values[0], max = values[0];
            foreach (var d in values) { if (d < min) min = d; if (d > max) max = d; }
            TimeSpan span = max - min;
            if (span <= TimeSpan.FromDays(2))   return "HH:mm";
            if (span <= TimeSpan.FromDays(90))  return "d MMM";
            if (span <= TimeSpan.FromDays(730)) return "MMM yyyy";
            return "yyyy";
        }

        /// <summary>
        /// Sets the Y-axis tick interval (distance between ticks in data units).
        /// </summary>
        /// <param name="interval">Must be greater than zero.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="interval"/> is zero or negative.</exception>
        public ChartBuilder YAxisTickInterval(double interval)
        {
            if (interval <= 0)
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "Tick interval must be greater than zero.");
            _options.YAxis.TickInterval = interval;
            return this;
        }

        /// <summary>
        /// Sets the X-axis tick interval (distance between ticks in data units).
        /// </summary>
        /// <param name="interval">Must be greater than zero.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="interval"/> is zero or negative.</exception>
        public ChartBuilder XAxisTickInterval(double interval)
        {
            if (interval <= 0)
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "Tick interval must be greater than zero.");
            _options.XAxis.TickInterval = interval;
            return this;
        }

        /// <summary>
        /// Adds a horizontal reference band to the primary Y-axis.
        /// Shorthand for <c>YAxis(y =&gt; y.PlotBands.Add(new PlotBand { ... }))</c>.
        /// </summary>
        /// <param name="from">Lower Y value of the band.</param>
        /// <param name="to">Upper Y value of the band.</param>
        /// <param name="color">Fill colour (CSS colour string). Defaults to a semi-transparent blue.</param>
        /// <param name="label">Optional label shown at the right edge of the band.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="from"/> is greater than or equal to <paramref name="to"/>.</exception>
        public ChartBuilder PlotBand(double from, double to, string color = "rgba(68,170,213,0.15)", string? label = null)
        {
            if (from >= to)
                throw new ArgumentException($"PlotBand 'from' ({from}) must be less than 'to' ({to}).", nameof(from));
            _options.YAxis.PlotBands.Add(new Models.PlotBand { From = from, To = to, Color = color, Label = label });
            return this;
        }

        /// <summary>
        /// Adds a horizontal reference line to the primary Y-axis.
        /// Shorthand for <c>YAxis(y =&gt; y.PlotLines.Add(new PlotLine { ... }))</c>.
        /// </summary>
        /// <param name="value">Y value at which the line is drawn.</param>
        /// <param name="color">Stroke colour. Defaults to red (<c>#FF0000</c>).</param>
        /// <param name="width">Stroke width in pixels. Default 1.</param>
        /// <param name="label">Optional label shown at the right edge of the line.</param>
        /// <param name="dashStyle">Optional HighCharts-style dash style (e.g. <c>"Dash"</c>, <c>"Dot"</c>).</param>
        public ChartBuilder PlotLine(double value, string color = ChartColor.Red, int width = 1, string? label = null, string? dashStyle = null)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "PlotLine width must be greater than zero.");
            _options.YAxis.PlotLines.Add(new Models.PlotLine { Value = value, Color = color, Width = width, Label = label, DashStyle = dashStyle });
            return this;
        }

        // ------------------------------------------------------------------ legend

        /// <summary>Configures the legend using the fluent <see cref="LegendBuilder"/>.</summary>
        public ChartBuilder Legend(Action<LegendBuilder> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(new LegendBuilder(_options.Legend));
            return this;
        }

        /// <summary>Hides the legend.</summary>
        public ChartBuilder HideLegend()
        {
            _options.Legend.Enabled = false;
            return this;
        }

        // ------------------------------------------------------------------ tooltip

        /// <summary>Configures the data-point tooltip using the fluent <see cref="TooltipBuilder"/>.</summary>
        public ChartBuilder Tooltip(Action<TooltipBuilder> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(new TooltipBuilder(_options.Tooltip));
            return this;
        }

        /// <summary>Disables tooltips for this chart.</summary>
        public ChartBuilder DisableTooltip()
        {
            _options.Tooltip.Enabled = false;
            return this;
        }

        // ------------------------------------------------------------------ label layout

        /// <summary>
        /// Configures dynamic X-axis label layout — rotation, word-wrap, skip stride,
        /// dynamic font scaling, bounding-box collision detection, and stagger positioning —
        /// via the fluent <see cref="LabelLayoutBuilder"/>.
        /// <para>When not called, the renderer falls back to its built-in auto-rotate heuristic.</para>
        /// </summary>
        public ChartBuilder LabelLayout(Action<LabelLayoutBuilder> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            if (_options.LabelLayout is null)
                _options.LabelLayout = new Models.LabelLayoutOptions();
            configure(new LabelLayoutBuilder(_options.LabelLayout));
            return this;
        }

        // ------------------------------------------------------------------ annotations

        /// <summary>
        /// Adds free-form annotations (text labels, connector lines, highlight rectangles and circles)
        /// over the plot area using the fluent <see cref="AnnotationBuilder"/>. Coordinates are in data
        /// space: X is a category index (when the X axis has categories) or a numeric X value; Y is a
        /// value-axis value.
        /// </summary>
        public ChartBuilder Annotations(Action<AnnotationBuilder> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(new AnnotationBuilder(_options.Annotations, _options));
            return this;
        }

        // ------------------------------------------------------------------ animation

        /// <summary>
        /// Enables animation with the specified duration (default 800 ms). Easing defaults to EaseOut.
        /// Chain <see cref="Animation"/> after this call to customise the easing curve.
        /// </summary>
        /// <param name="milliseconds">Duration in milliseconds. Must be greater than zero. Defaults to 800.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="milliseconds"/> is zero or negative.</exception>
        public ChartBuilder Animate(int milliseconds = 800)
        {
            if (milliseconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(milliseconds), milliseconds, "Animation duration must be greater than zero.");
            _options.Animation.Enabled  = true;
            _options.Animation.Duration = TimeSpan.FromMilliseconds(milliseconds);
            return this;
        }

        /// <summary>Configures animation timing and easing via the fluent <see cref="AnimationBuilder"/>.</summary>
        public ChartBuilder Animation(Action<AnimationBuilder> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(new AnimationBuilder(_options.Animation));
            return this;
        }

        /// <summary>Disables all load animation effects for this chart.</summary>
        public ChartBuilder DisableAnimation()
        {
            _options.Animation.Enabled = false;
            return this;
        }

        // ------------------------------------------------------------------ render mode

        /// <summary>
        /// Renders in static mode — no CSS hover rules, no JavaScript, no SMIL animation.
        /// Safe for PDF and email. Also disables animation (<see cref="AnimationOptions.Enabled"/> is set to
        /// <c>false</c>) because SMIL is not supported in static contexts.
        /// </summary>
        public ChartBuilder AsStatic()
        {
            _options.RenderMode = SvgMode.Static;
            _options.Animation.Enabled = false;
            return this;
        }

        /// <summary>Renders with SMIL/CSS animations, no JavaScript. Suitable for Blazor and browser embedding.</summary>
        public ChartBuilder AsAnimated()    { _options.RenderMode = SvgMode.Animated;    return this; }

        /// <summary>Renders with CSS hover effects and embedded JavaScript interactions. Browser-only.</summary>
        public ChartBuilder AsInteractive() { _options.RenderMode = SvgMode.Interactive; return this; }

        /// <summary>
        /// Adds a download button to the top-right corner of the chart (Interactive mode only).
        /// Clicking it exports the current SVG to a file named after the chart title.
        /// </summary>
        /// <param name="label">Button label. Defaults to "⬇ SVG".</param>
        public ChartBuilder ShowExportButton(string label = "\u2b07 SVG")
        {
            _options.ExportButtonEnabled = true;
            _options.ExportButtonLabel   = label;
            return this;
        }

        /// <summary>
        /// Adds a multi-format export drop-down menu to the top-right corner (Interactive mode only).
        /// The menu shows one item per format: SVG, PNG, JPEG, and/or PDF.
        /// </summary>
        /// <param name="formats">
        /// The formats to include. Pass any combination of <c>"SVG"</c>, <c>"PNG"</c>,
        /// <c>"JPEG"</c>, <c>"PDF"</c> (case-insensitive). Omit to include all four.
        /// </param>
        public ChartBuilder ShowExportMenu(params string[] formats)
        {
            _options.ExportMenuEnabled = true;
            if (formats != null && formats.Length > 0)
                _options.ExportMenuFormats = new System.Collections.Generic.List<string>(formats);
            return this;
        }

        /// <summary>Registers a JavaScript function to invoke when a data point is clicked (Interactive mode only).</summary>
        /// <remarks>
        /// <para>Supply a global function name (<c>"myHandler"</c>) or an inline expression
        /// (<c>"function(pt, e) { alert(pt.value); }"</c>). Arrow functions are also accepted.</para>
        /// <para>The callback receives a point-data object
        /// <c>{ index, value, name, color, x, y, category }</c> and the original DOM <c>MouseEvent</c>
        /// as a second argument.</para>
        /// </remarks>
        /// <param name="handler">JS function name or inline expression. Must not be empty.</param>
        public ChartBuilder OnPointClick(string handler)
        {
            if (string.IsNullOrWhiteSpace(handler)) throw new ArgumentException("handler must not be empty.", nameof(handler));
            _options.PointClickHandler = handler;
            return this;
        }

        // ------------------------------------------------------------------ series management

        /// <summary>Removes all series from the chart.</summary>
        public ChartBuilder ClearSeries() { _options.Series.Clear(); return this; }

        /// <summary>Removes the first series whose name matches <paramref name="name"/>.</summary>
        public ChartBuilder RemoveSeries(string name)
        {
            _options.Series.RemoveAll(s => s.Name == name);
            return this;
        }

        /// <summary>
        /// Enables data labels on all series — both series already added and any added afterwards via
        /// <see cref="Series"/>. Call before or after <see cref="Series"/>; both orderings work.
        /// </summary>
        public ChartBuilder ShowDataLabels()
        {
            _globalDataLabels = true;
            foreach (var s in _options.Series)
                s.DataLabel.Show();
            return this;
        }

        // ------------------------------------------------------------------ series

        /// <summary>Adds or configures series using the fluent <see cref="ChartSeriesBuilder"/>.</summary>
        /// <param name="configure">Configuration action. Must not be <c>null</c>.</param>
        public ChartBuilder Series(Action<ChartSeriesBuilder> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            int beforeCount = _options.Series.Count;
            var builder = new ChartSeriesBuilder(_options.Series, _options.PrimaryChartType);
            configure(builder);
            if (_globalDataLabels)
                for (int i = beforeCount; i < _options.Series.Count; i++)
                    _options.Series[i].DataLabel.Show();
            return this;
        }

        // ------------------------------------------------------------------ data quality

        /// <summary>
        /// When called, causes <see cref="RenderToSvg"/> (and all render overloads) to throw a
        /// <see cref="Models.DataQualityException"/> if the implicit data-quality analysis detects
        /// at least one <see cref="TerraFluent.Chart.Reporting.Enums.WarningSeverity"/> (<c>Error</c>) finding before rendering begins.
        /// <para>Warnings (<see cref="TerraFluent.Chart.Reporting.Enums.WarningSeverity"/> <c>Warning</c> and <c>Info</c>)
        /// never block rendering; retrieve them via <see cref="GetLastDataQualityReport"/>.</para>
        /// </summary>
        public ChartBuilder ThrowOnDataQualityErrors()
        {
            _throwOnDataQualityErrors = true;
            return this;
        }

        /// <summary>
        /// Returns the <see cref="Models.DataQualityReport"/> produced by the most recent implicit
        /// analysis (performed automatically inside every render call) or by an explicit call to
        /// <see cref="AnalyzeDataQuality"/>. Returns <c>null</c> before the first render or analysis.
        /// </summary>
        public Models.DataQualityReport? GetLastDataQualityReport() => _lastDataQualityReport;

        /// <summary>
        /// Explicitly runs all data-quality checks against the current chart configuration and
        /// returns a <see cref="Models.DataQualityReport"/> with every finding sorted by severity
        /// (highest first). The result is also cached and retrievable via
        /// <see cref="GetLastDataQualityReport"/>.
        /// <para>
        /// This method is also called <b>implicitly</b> before every render. You only need to call
        /// it explicitly when you want to inspect the report <em>without</em> rendering.
        /// </para>
        /// <para>Checks performed:</para>
        /// <list type="bullet">
        ///   <item>Missing values (null data points)</item>
        ///   <item>Duplicate X-axis categories</item>
        ///   <item>Negative values in Pie / Donut series</item>
        ///   <item>Broken or out-of-order DateTime sequences</item>
        ///   <item>Values outside [0, 100] in percentage-formatted series</item>
        ///   <item>Completely empty datasets</item>
        ///   <item>Extremely skewed value distributions</item>
        /// </list>
        /// </summary>
        public Models.DataQualityReport AnalyzeDataQuality()
        {
            _lastDataQualityReport = Analysis.DataQualityAnalyzer.Analyze(_options);
            return _lastDataQualityReport;
        }

        // ------------------------------------------------------------------ render

        /// <summary>
        /// Renders the chart and returns the SVG markup as a string.
        /// <para>
        /// Data quality is analysed automatically before rendering. The result is stored and
        /// retrievable via <see cref="GetLastDataQualityReport"/> at any time after this call.
        /// If <see cref="ThrowOnDataQualityErrors"/> was set and any
        /// <see cref="TerraFluent.Chart.Reporting.Enums.WarningSeverity"/> (<c>Error</c>) findings are found, a
        /// <see cref="Models.DataQualityException"/> is thrown before the SVG is generated.
        /// </para>
        /// </summary>
        public string RenderToSvg()
        {
            _lastDataQualityReport = Analysis.DataQualityAnalyzer.Analyze(_options);
            if (_throwOnDataQualityErrors && _lastDataQualityReport.HasErrors)
                throw new Models.DataQualityException(_lastDataQualityReport);
            return _renderer.Render(_options);
        }

        /// <summary>
        /// Renders the chart and returns a self-contained HTML fragment suitable for embedding in a web page or email.
        /// <para>
        /// The SVG is wrapped in a <c>&lt;figure&gt;</c> element. When <paramref name="caption"/> is provided,
        /// a <c>&lt;figcaption&gt;</c> is appended beneath the chart. The wrapper <c>div</c> is set to
        /// <c>display:inline-block</c> and the SVG width is constrained to 100 % of the container.
        /// </para>
        /// </summary>
        /// <param name="caption">
        /// Optional caption rendered below the chart as a <c>&lt;figcaption&gt;</c>.
        /// Pass <c>null</c> (default) to omit it.
        /// </param>
        /// <param name="cssClass">
        /// Optional CSS class(es) added to the outer <c>&lt;figure&gt;</c> element for styling hooks.
        /// </param>
        public string RenderToHtml(string? caption = null, string? cssClass = null)
        {
            string svg = RenderToSvg();

            var sb = new System.Text.StringBuilder();

            string classAttr = string.IsNullOrWhiteSpace(cssClass) ? string.Empty : $" class=\"{HtmlEncode(cssClass)}\"";
            sb.AppendLine($"<figure{classAttr} style=\"margin:0;padding:0;display:inline-block;width:100%;\">");
            sb.AppendLine("  <div style=\"width:100%;overflow:hidden;\">");
            sb.AppendLine(svg.TrimEnd());
            sb.AppendLine("  </div>");

            if (!string.IsNullOrEmpty(caption))
                sb.AppendLine($"  <figcaption style=\"text-align:center;font-size:0.85em;color:#666;margin-top:4px;\">{HtmlEncode(caption)}</figcaption>");

            sb.Append("</figure>");
            return sb.ToString();
        }

        /// <summary>
        /// Renders the chart and writes the HTML fragment to <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">Absolute or relative path of the output file (e.g. <c>report.html</c>).</param>
        /// <param name="caption">Optional caption beneath the chart.</param>
        /// <param name="cssClass">Optional CSS class on the outer <c>&lt;figure&gt;</c>.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or whitespace.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Thrown when the parent directory does not exist.</exception>
        public void RenderToHtmlFile(string filePath, string? caption = null, string? cssClass = null)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path must not be empty.", nameof(filePath));
            string? dir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(filePath));
            if (dir != null && !System.IO.Directory.Exists(dir))
                throw new System.IO.DirectoryNotFoundException(
                    $"The output directory does not exist: \"{dir}\". Create it before calling RenderToHtmlFile.");
            System.IO.File.WriteAllText(filePath, RenderToHtml(caption, cssClass), System.Text.Encoding.UTF8);
        }

        // XML-encodes characters that are meaningful in HTML attribute values and content.
        private static string HtmlEncode(string text) =>
            text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

        /// <summary>Renders the chart and writes the SVG file to <paramref name="filePath"/>.</summary>
        /// <param name="filePath">Absolute or relative path of the output file.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or whitespace.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">
        /// Thrown when the parent directory of <paramref name="filePath"/> does not exist.
        /// Create the directory before calling this method, or use <see cref="RenderToSvg"/> and write the file yourself.
        /// </exception>
        public void RenderToFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path must not be empty.", nameof(filePath));

            string? dir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(filePath));
            if (dir != null && !System.IO.Directory.Exists(dir))
                throw new System.IO.DirectoryNotFoundException(
                    $"The output directory does not exist: \"{dir}\". Create it before calling RenderToFile.");

            System.IO.File.WriteAllText(filePath, RenderToSvg(), System.Text.Encoding.UTF8);
        }

        /// <summary>Renders the chart and writes UTF-8 encoded SVG bytes to <paramref name="stream"/>.</summary>
        public void RenderToStream(System.IO.Stream stream)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(RenderToSvg());
            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Renders the chart and returns a Base64-encoded <c>data:</c> URI suitable for use as an
        /// HTML <c>&lt;img src="..."&gt;</c> attribute or a CSS <c>background-image</c> value.
        /// <para>Format: <c>data:image/svg+xml;base64,{base64}</c></para>
        /// </summary>
        public string RenderToDataUri()
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(RenderToSvg());
            return "data:image/svg+xml;base64," + Convert.ToBase64String(bytes);
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Asynchronously renders the chart and writes UTF-8 encoded SVG bytes to <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">A writable stream. Must not be null.</param>
        /// <param name="cancellationToken">Token to cancel the write operation.</param>
        public async System.Threading.Tasks.Task RenderToStreamAsync(
            System.IO.Stream stream,
            System.Threading.CancellationToken cancellationToken = default)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(RenderToSvg());
            await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously renders the chart and writes the SVG file to <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">Absolute or relative path of the output file.</param>
        /// <param name="cancellationToken">Token to cancel the write operation.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or whitespace.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">
        /// Thrown when the parent directory of <paramref name="filePath"/> does not exist.
        /// </exception>
        public async System.Threading.Tasks.Task RenderToFileAsync(
            string filePath,
            System.Threading.CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path must not be empty.", nameof(filePath));
            string? dir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(filePath));
            if (dir != null && !System.IO.Directory.Exists(dir))
                throw new System.IO.DirectoryNotFoundException(
                    $"The output directory does not exist: \"{dir}\". Create it before calling RenderToFileAsync.");
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(RenderToSvg());
            await System.IO.File.WriteAllBytesAsync(filePath, bytes, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously renders the chart HTML fragment and writes it to <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">Absolute or relative path of the output file.</param>
        /// <param name="caption">Optional caption beneath the chart.</param>
        /// <param name="cssClass">Optional CSS class on the outer <c>&lt;figure&gt;</c>.</param>
        /// <param name="cancellationToken">Token to cancel the write operation.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or whitespace.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">Thrown when the parent directory does not exist.</exception>
        public async System.Threading.Tasks.Task RenderToHtmlFileAsync(
            string filePath,
            string? caption = null,
            string? cssClass = null,
            System.Threading.CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path must not be empty.", nameof(filePath));
            string? dir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(filePath));
            if (dir != null && !System.IO.Directory.Exists(dir))
                throw new System.IO.DirectoryNotFoundException(
                    $"The output directory does not exist: \"{dir}\". Create it before calling RenderToHtmlFileAsync.");
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(RenderToHtml(caption, cssClass));
            await System.IO.File.WriteAllBytesAsync(filePath, bytes, cancellationToken).ConfigureAwait(false);
        }
#endif

        /// <summary>Renders the chart and returns the SVG as a UTF-8 encoded byte array.</summary>
        public byte[] RenderToBytes() =>
            System.Text.Encoding.UTF8.GetBytes(RenderToSvg());

        /// <summary>Returns the raw <see cref="ChartOptions"/> for advanced customisation.</summary>
        public ChartOptions GetOptions() => _options;

        /// <summary>
        /// Returns an independent snapshot of the current configuration.
        /// Subsequent calls to the builder do not affect the snapshot.
        /// </summary>
        public ChartOptions Build() => _options.Clone();

        // ------------------------------------------------------------------ explicit IChartBuilder implementation
        // Concrete methods return ChartBuilder (for callers holding the concrete type).
        // Explicit implementations return IChartBuilder (for callers holding the interface).

        IChartBuilder IChartBuilder.AsPie()        => AsPie();
        IChartBuilder IChartBuilder.AsLine()       => AsLine();
        IChartBuilder IChartBuilder.AsArea()       => AsArea();
        IChartBuilder IChartBuilder.AsColumn()     => AsColumn();
        IChartBuilder IChartBuilder.AsBar()        => AsBar();
        IChartBuilder IChartBuilder.AsSpline()     => AsSpline();
        IChartBuilder IChartBuilder.AsScatter()    => AsScatter();
        IChartBuilder IChartBuilder.AsWaterfall()  => AsWaterfall();
        IChartBuilder IChartBuilder.AsGauge()      => AsGauge();
        IChartBuilder IChartBuilder.AsDataRing()   => AsDataRing();
        IChartBuilder IChartBuilder.AsBubble()     => AsBubble();
        IChartBuilder IChartBuilder.AsHeatmap()    => AsHeatmap();
        IChartBuilder IChartBuilder.AsColumnRange() => AsColumnRange();
        IChartBuilder IChartBuilder.AsAreaRange()  => AsAreaRange();
        IChartBuilder IChartBuilder.AsFunnel()     => AsFunnel();
        IChartBuilder IChartBuilder.AsTreemap()    => AsTreemap();
        IChartBuilder IChartBuilder.AsDumbbell()   => AsDumbbell();
        IChartBuilder IChartBuilder.AsStream()     => AsStream();
        IChartBuilder IChartBuilder.AsGantt()      => AsGantt();
        IChartBuilder IChartBuilder.AsSankey()     => AsSankey();
        IChartBuilder IChartBuilder.AsStatic()     => AsStatic();
        IChartBuilder IChartBuilder.AsAnimated()   => AsAnimated();
        IChartBuilder IChartBuilder.AsInteractive() => AsInteractive();
        IChartBuilder IChartBuilder.Labels(params string[] labels)                              => Labels(labels);
        IChartBuilder IChartBuilder.Size(int width, int height)                                 => Size(width, height);
        IChartBuilder IChartBuilder.Width(int? width)                                           => Width(width);
        IChartBuilder IChartBuilder.Height(int height)                                          => Height(height);
        IChartBuilder IChartBuilder.Background(string color)                                    => Background(color);
        IChartBuilder IChartBuilder.Theme(Models.ChartTheme theme)                             => Theme(theme);
        IChartBuilder IChartBuilder.Colors(params string[] colors)                              => Colors(colors);
        IChartBuilder IChartBuilder.Colors(IEnumerable<string> colors)                         => Colors(colors);
        IChartBuilder IChartBuilder.StackNormal()                                               => StackNormal();
        IChartBuilder IChartBuilder.StackPercent()                                              => StackPercent();
        IChartBuilder IChartBuilder.Title(string text)                                          => Title(text);
        IChartBuilder IChartBuilder.Title(Action<Models.ChartTitle> configure)                  => Title(configure);
        IChartBuilder IChartBuilder.Subtitle(string text)                                       => Subtitle(text);
        IChartBuilder IChartBuilder.Subtitle(Action<Models.ChartTitle> configure)               => Subtitle(configure);
        IChartBuilder IChartBuilder.XAxis(Action<Models.Axis> configure)                        => XAxis(configure);
        IChartBuilder IChartBuilder.XAxis(string title, params string[] categories)             => XAxis(title, categories);
        IChartBuilder IChartBuilder.YAxis(Action<Models.Axis> configure)                        => YAxis(configure);
        IChartBuilder IChartBuilder.YAxis(string title, double? min, double? max)               => YAxis(title, min, max);
        IChartBuilder IChartBuilder.YAxisFormat(string format)                                  => YAxisFormat(format);
        IChartBuilder IChartBuilder.XAxisFormat(string format)                                  => XAxisFormat(format);
        IChartBuilder IChartBuilder.YAxisTickInterval(double interval)                          => YAxisTickInterval(interval);
        IChartBuilder IChartBuilder.XAxisTickInterval(double interval)                          => XAxisTickInterval(interval);
        IChartBuilder IChartBuilder.YAxis2(Action<Models.Axis> configure)                       => YAxis2(configure);
        IChartBuilder IChartBuilder.PlotBand(double from, double to, string color, string? label) => PlotBand(from, to, color, label);
        IChartBuilder IChartBuilder.PlotLine(double value, string color, int width, string? label, string? dashStyle) => PlotLine(value, color, width, label, dashStyle);
        IChartBuilder IChartBuilder.Legend(Action<LegendBuilder> configure)                     => Legend(configure);
        IChartBuilder IChartBuilder.HideLegend()                                                => HideLegend();
        IChartBuilder IChartBuilder.Tooltip(Action<TooltipBuilder> configure)                   => Tooltip(configure);
        IChartBuilder IChartBuilder.DisableTooltip()                                            => DisableTooltip();
        IChartBuilder IChartBuilder.Animate(int milliseconds)                                   => Animate(milliseconds);
        IChartBuilder IChartBuilder.Animation(Action<AnimationBuilder> configure)               => Animation(configure);
        IChartBuilder IChartBuilder.DisableAnimation()                                          => DisableAnimation();
        IChartBuilder IChartBuilder.Series(Action<ChartSeriesBuilder> configure)                => Series(configure);
        IChartBuilder IChartBuilder.ClearSeries()                                               => ClearSeries();
        IChartBuilder IChartBuilder.RemoveSeries(string name)                                   => RemoveSeries(name);
        IChartBuilder IChartBuilder.ShowDataLabels()                                            => ShowDataLabels();
        IChartBuilder IChartBuilder.ShowExportButton(string label)                              => ShowExportButton(label);
        IChartBuilder IChartBuilder.ShowExportMenu(params string[] formats)                     => ShowExportMenu(formats);
        IChartBuilder IChartBuilder.ResponsiveWidth()                                           => ResponsiveWidth();
        IChartBuilder IChartBuilder.ShowDataTable(int rowHeight, int fontSize)                  => ShowDataTable(rowHeight, fontSize);
        IChartBuilder IChartBuilder.AriaLabel(string label)                                     => AriaLabel(label);
        IChartBuilder IChartBuilder.AriaDescription(string desc)                                => AriaDescription(desc);
        IChartBuilder IChartBuilder.Culture(System.Globalization.CultureInfo c)                 => Culture(c);
        IChartBuilder IChartBuilder.Culture(string name)                                        => Culture(name);
        IChartBuilder IChartBuilder.RightToLeft(bool rtl)                                       => RightToLeft(rtl);
        IChartBuilder IChartBuilder.ApplyTemplate(IChartTemplate template)                      => ApplyTemplate(template);
        IChartBuilder IChartBuilder.RangeSelector(Action<Models.RangeSelectorOptions> configure) => RangeSelector(configure);
        IChartBuilder IChartBuilder.SyncGroup(string groupId)                                    => SyncGroup(groupId);
        IChartBuilder IChartBuilder.Fork()                                                      => Fork();
        IChartBuilder IChartBuilder.ThrowOnDataQualityErrors()                                  => ThrowOnDataQualityErrors();
        Models.DataQualityReport? IChartBuilder.GetLastDataQualityReport()                      => GetLastDataQualityReport();
        Models.DataQualityReport IChartBuilder.AnalyzeDataQuality()                             => AnalyzeDataQuality();
#if NET6_0_OR_GREATER
        System.Threading.Tasks.Task IChartBuilder.RenderToStreamAsync(
            System.IO.Stream stream,
            System.Threading.CancellationToken cancellationToken)
            => RenderToStreamAsync(stream, cancellationToken);

        System.Threading.Tasks.Task IChartBuilder.RenderToFileAsync(
            string filePath,
            System.Threading.CancellationToken cancellationToken)
            => RenderToFileAsync(filePath, cancellationToken);

        System.Threading.Tasks.Task IChartBuilder.RenderToHtmlFileAsync(
            string filePath,
            string? caption,
            string? cssClass,
            System.Threading.CancellationToken cancellationToken)
            => RenderToHtmlFileAsync(filePath, caption, cssClass, cancellationToken);
#endif
    }

    // ======================================================================= SeriesBuilder

    /// <summary>
    /// Fluent builder for adding and configuring chart series.
    /// </summary>
    public sealed class ChartSeriesBuilder
    {
        private readonly List<Series> _series;
        private readonly ChartType?   _defaultType;

        internal ChartSeriesBuilder(List<Series> series, ChartType? defaultType = null)
        {
            _series      = series;
            _defaultType = defaultType;
        }

        // ---- explicit-type helpers — nullable data (supports sparse / missing points) ----

        /// <summary>Adds a <see cref="ChartType.Line"/> series. Supports <c>null</c> values for gap points.</summary>
        public ChartSeriesBuilder AddLine(string name, IEnumerable<double?> data, Action<SeriesBuilder>? configure = null)
            => AddTyped(name, ChartType.Line, data, configure);

        /// <summary>Adds a <see cref="ChartType.Spline"/> (smooth curve) series. Supports <c>null</c> gap values.</summary>
        public ChartSeriesBuilder AddSpline(string name, IEnumerable<double?> data, Action<SeriesBuilder>? configure = null)
            => AddTyped(name, ChartType.Spline, data, configure);

        /// <summary>Adds a <see cref="ChartType.Bar"/> (horizontal) series. Supports <c>null</c> gap values.</summary>
        public ChartSeriesBuilder AddBar(string name, IEnumerable<double?> data, Action<SeriesBuilder>? configure = null)
            => AddTyped(name, ChartType.Bar, data, configure);

        /// <summary>Adds a <see cref="ChartType.Column"/> (vertical bar) series. Supports <c>null</c> gap values.</summary>
        public ChartSeriesBuilder AddColumn(string name, IEnumerable<double?> data, Action<SeriesBuilder>? configure = null)
            => AddTyped(name, ChartType.Column, data, configure);

        /// <summary>Adds an <see cref="ChartType.Area"/> series. Supports <c>null</c> gap values.</summary>
        public ChartSeriesBuilder AddArea(string name, IEnumerable<double?> data, Action<SeriesBuilder>? configure = null)
            => AddTyped(name, ChartType.Area, data, configure);

        /// <summary>Adds a <see cref="ChartType.Pie"/> (or donut) series. Supports <c>null</c> gap values.</summary>
        public ChartSeriesBuilder AddPie(string name, IEnumerable<double?> data, Action<SeriesBuilder>? configure = null)
            => AddTyped(name, ChartType.Pie, data, configure);

        /// <summary>Adds a <see cref="ChartType.Scatter"/> series. Supports <c>null</c> gap values.</summary>
        public ChartSeriesBuilder AddScatter(string name, IEnumerable<double?> data, Action<SeriesBuilder>? configure = null)
            => AddTyped(name, ChartType.Scatter, data, configure);

        /// <summary>
        /// Adds a <see cref="ChartType.Radar"/> (spider / polar) series. One value per category spoke;
        /// category labels come from <c>XAxis.Categories</c>. Supports <c>null</c> gap values.
        /// </summary>
        public ChartSeriesBuilder AddRadar(string name, IEnumerable<double?> data, Action<SeriesBuilder>? configure = null)
            => AddTyped(name, ChartType.Radar, data, configure);

        // ---- non-nullable convenience overloads (most common case — no gaps in data) ----

        /// <summary>Adds a <see cref="ChartType.Line"/> series from non-nullable data.</summary>
        public ChartSeriesBuilder AddLine(string name, IEnumerable<double> data, Action<SeriesBuilder>? configure = null)
            => AddLine(name, ToNullable(data), configure);

        /// <summary>Adds a <see cref="ChartType.Spline"/> series from non-nullable data.</summary>
        public ChartSeriesBuilder AddSpline(string name, IEnumerable<double> data, Action<SeriesBuilder>? configure = null)
            => AddSpline(name, ToNullable(data), configure);

        /// <summary>Adds a <see cref="ChartType.Bar"/> (horizontal) series from non-nullable data.</summary>
        public ChartSeriesBuilder AddBar(string name, IEnumerable<double> data, Action<SeriesBuilder>? configure = null)
            => AddBar(name, ToNullable(data), configure);

        /// <summary>Adds a <see cref="ChartType.Column"/> (vertical bar) series from non-nullable data.</summary>
        public ChartSeriesBuilder AddColumn(string name, IEnumerable<double> data, Action<SeriesBuilder>? configure = null)
            => AddColumn(name, ToNullable(data), configure);

        /// <summary>Adds an <see cref="ChartType.Area"/> series from non-nullable data.</summary>
        public ChartSeriesBuilder AddArea(string name, IEnumerable<double> data, Action<SeriesBuilder>? configure = null)
            => AddArea(name, ToNullable(data), configure);

        /// <summary>Adds a <see cref="ChartType.Pie"/> series from non-nullable data.</summary>
        public ChartSeriesBuilder AddPie(string name, IEnumerable<double> data, Action<SeriesBuilder>? configure = null)
            => AddPie(name, ToNullable(data), configure);

        /// <summary>Adds a <see cref="ChartType.Scatter"/> series from non-nullable data.</summary>
        public ChartSeriesBuilder AddScatter(string name, IEnumerable<double> data, Action<SeriesBuilder>? configure = null)
            => AddScatter(name, ToNullable(data), configure);

        /// <summary>Adds a <see cref="ChartType.Radar"/> (spider / polar) series from non-nullable data.</summary>
        public ChartSeriesBuilder AddRadar(string name, IEnumerable<double> data, Action<SeriesBuilder>? configure = null)
            => AddRadar(name, ToNullable(data), configure);

        /// <summary>
        /// Adds a waterfall (running-total) series. Positive values are increases (green),
        /// negatives are decreases (red), and total bars can be marked via
        /// <paramref name="totals"/>. When <paramref name="totals"/> is null the first and
        /// last data points are automatically treated as cumulative totals.
        /// </summary>
        public ChartSeriesBuilder AddWaterfall(string name, IEnumerable<double?> data,
            IEnumerable<bool>? totals = null, Action<SeriesBuilder>? configure = null)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (data is null) throw new ArgumentNullException(nameof(data));
            var series = new Series { Name = name, Type = ChartType.Waterfall };
            series.Data.AddRange(data);
            if (totals != null)
            {
                series.WaterfallTotals.AddRange(totals);
                if (series.WaterfallTotals.Count != series.Data.Count)
                    throw new ArgumentException(
                        $"The 'totals' list length ({series.WaterfallTotals.Count}) must match the 'data' length ({series.Data.Count}).",
                        nameof(totals));
            }
            configure?.Invoke(new SeriesBuilder(series));
            _series.Add(series);
            return this;
        }

        /// <summary>Adds a waterfall series from non-nullable data.</summary>
        public ChartSeriesBuilder AddWaterfall(string name, IEnumerable<double> data,
            IEnumerable<bool>? totals = null, Action<SeriesBuilder>? configure = null)
            => AddWaterfall(name, ToNullable(data), totals, configure);

        /// <summary>
        /// Adds a gauge (dial) series. Provide a single data value representing
        /// the current reading; <c>YAxis.Min</c>/<c>YAxis.Max</c> define the scale.
        /// </summary>
        public ChartSeriesBuilder AddGauge(string name, double value, Action<SeriesBuilder>? configure = null)
            => AddTyped(name, ChartType.Gauge, new double?[] { value }, configure);

        /// <summary>
        /// Adds a <see cref="ChartType.DataRing"/> series — a full 360° progress ring with the value displayed in the
        /// centre. <c>YAxis.Min</c>/<c>YAxis.Max</c> define the scale (default 0–100 for
        /// percentages). Customise the centre label via <c>cfg.DonutCenter</c>.
        /// </summary>
        public ChartSeriesBuilder AddDataRing(string name, double value, Action<SeriesBuilder>? configure = null)
            => AddTyped(name, ChartType.DataRing, new double?[] { value }, configure);

        // ---- new chart type add methods ----

        /// <summary>
        /// Adds a <see cref="ChartType.Bubble"/> series. Each point carries (X, Y, Z)
        /// where Z drives the bubble radius relative to the largest Z in the series.
        /// </summary>
        public ChartSeriesBuilder AddBubble(string name, IEnumerable<BubblePoint> data,
            Action<SeriesBuilder>? configure = null)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (data is null) throw new ArgumentNullException(nameof(data));
            var series = new Series { Name = name, Type = ChartType.Bubble };
            series.BubbleData.AddRange(data);
            configure?.Invoke(new SeriesBuilder(series));
            _series.Add(series);
            return this;
        }

        /// <summary>
        /// Adds a <see cref="ChartType.Heatmap"/> series.
        /// Column indices map to <c>XAxis.Categories</c>; row indices map to
        /// <c>Series.HeatmapRowLabels</c> (set via the configure lambda).
        /// </summary>
        public ChartSeriesBuilder AddHeatmap(string name, IEnumerable<HeatmapPoint> data,
            Action<SeriesBuilder>? configure = null)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (data is null) throw new ArgumentNullException(nameof(data));
            var series = new Series { Name = name, Type = ChartType.Heatmap };
            series.HeatmapData.AddRange(data);
            configure?.Invoke(new SeriesBuilder(series));
            _series.Add(series);
            return this;
        }

        /// <summary>
        /// Adds a <see cref="ChartType.ColumnRange"/> series. Each point is a (Low, High) pair
        /// rendered as a vertical bar spanning from the low to the high value.
        /// </summary>
        public ChartSeriesBuilder AddColumnRange(string name, IEnumerable<RangePoint> data,
            Action<SeriesBuilder>? configure = null)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (data is null) throw new ArgumentNullException(nameof(data));
            var series = new Series { Name = name, Type = ChartType.ColumnRange };
            series.RangeData.AddRange(data);
            configure?.Invoke(new SeriesBuilder(series));
            _series.Add(series);
            return this;
        }

        /// <summary>
        /// Adds an <see cref="ChartType.AreaRange"/> series. Each point is a (Low, High) pair
        /// rendered as a filled band between the lower and upper boundary lines.
        /// </summary>
        public ChartSeriesBuilder AddAreaRange(string name, IEnumerable<RangePoint> data,
            Action<SeriesBuilder>? configure = null)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (data is null) throw new ArgumentNullException(nameof(data));
            var series = new Series { Name = name, Type = ChartType.AreaRange };
            series.RangeData.AddRange(data);
            configure?.Invoke(new SeriesBuilder(series));
            _series.Add(series);
            return this;
        }

        /// <summary>
        /// Adds a <see cref="ChartType.BoxPlot"/> series. Each <see cref="Models.BoxPlotPoint"/>
        /// supplies a five-number summary (Low, Q1, Median, Q3, High) for one category, rendered as
        /// a box-and-whisker glyph. Category labels come from <c>XAxis.Categories</c>.
        /// </summary>
        public ChartSeriesBuilder AddBoxPlot(string name, IEnumerable<BoxPlotPoint> data,
            Action<SeriesBuilder>? configure = null)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (data is null) throw new ArgumentNullException(nameof(data));
            var series = new Series { Name = name, Type = ChartType.BoxPlot };
            series.BoxPlotData.AddRange(data);
            configure?.Invoke(new SeriesBuilder(series));
            _series.Add(series);
            return this;
        }

        /// <summary>
        /// Adds an <see cref="ChartType.ErrorBar"/> series. Each <see cref="Models.RangePoint"/> is a
        /// (Low, High) uncertainty interval drawn as an I-beam whisker per category — typically
        /// overlaid on a column or scatter series.
        /// </summary>
        public ChartSeriesBuilder AddErrorBar(string name, IEnumerable<RangePoint> data,
            Action<SeriesBuilder>? configure = null)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (data is null) throw new ArgumentNullException(nameof(data));
            var series = new Series { Name = name, Type = ChartType.ErrorBar };
            series.RangeData.AddRange(data);
            configure?.Invoke(new SeriesBuilder(series));
            _series.Add(series);
            return this;
        }

        /// <summary>
        /// Adds a <see cref="ChartType.Candlestick"/> series. Each <see cref="Models.OhlcPoint"/>
        /// supplies (Open, High, Low, Close) prices for one period, rendered as a high-low wick with
        /// a filled open-close body coloured up (close ≥ open) or down. Category labels come from
        /// <c>XAxis.Categories</c>.
        /// </summary>
        public ChartSeriesBuilder AddCandlestick(string name, IEnumerable<OhlcPoint> data,
            Action<SeriesBuilder>? configure = null)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (data is null) throw new ArgumentNullException(nameof(data));
            var series = new Series { Name = name, Type = ChartType.Candlestick };
            series.OhlcData.AddRange(data);
            configure?.Invoke(new SeriesBuilder(series));
            _series.Add(series);
            return this;
        }

        /// <summary>
        /// Adds an <see cref="ChartType.Ohlc"/> series. Each <see cref="Models.OhlcPoint"/> supplies
        /// (Open, High, Low, Close) prices for one period, rendered as a vertical high-low bar with a
        /// left tick for the open and a right tick for the close. Category labels come from
        /// <c>XAxis.Categories</c>.
        /// </summary>
        public ChartSeriesBuilder AddOhlc(string name, IEnumerable<OhlcPoint> data,
            Action<SeriesBuilder>? configure = null)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (data is null) throw new ArgumentNullException(nameof(data));
            var series = new Series { Name = name, Type = ChartType.Ohlc };
            series.OhlcData.AddRange(data);
            configure?.Invoke(new SeriesBuilder(series));
            _series.Add(series);
            return this;
        }

        /// <summary>
        /// Adds a <see cref="ChartType.Funnel"/> series. Values represent the relative size
        /// of each conversion stage; stage labels come from <c>XAxis.Categories</c>.
        /// </summary>
        public ChartSeriesBuilder AddFunnel(string name, IEnumerable<double?> data,
            Action<SeriesBuilder>? configure = null)
            => AddTyped(name, ChartType.Funnel, data, configure);

        /// <summary>Adds a funnel series from non-nullable data.</summary>
        public ChartSeriesBuilder AddFunnel(string name, IEnumerable<double> data,
            Action<SeriesBuilder>? configure = null)
            => AddFunnel(name, ToNullable(data), configure);

        /// <summary>
        /// Adds a <see cref="ChartType.Treemap"/> series. Values drive rectangle area; labels
        /// come from <c>XAxis.Categories</c>. Uses a balanced binary-split layout.
        /// </summary>
        public ChartSeriesBuilder AddTreemap(string name, IEnumerable<double?> data,
            Action<SeriesBuilder>? configure = null)
            => AddTyped(name, ChartType.Treemap, data, configure);

        /// <summary>Adds a treemap series from non-nullable data.</summary>
        public ChartSeriesBuilder AddTreemap(string name, IEnumerable<double> data,
            Action<SeriesBuilder>? configure = null)
            => AddTreemap(name, ToNullable(data), configure);

        /// <summary>
        /// Adds a <see cref="ChartType.Parliament"/> (hemicycle) series — a semicircular seating
        /// diagram where every dot represents one legislative member and colour identifies the party.
        /// Supply one <see cref="Models.ParliamentGroup"/> per party with its name, CSS colour, and
        /// seat count. Groups are rendered as contiguous coloured blocks ordered left-to-right.
        /// </summary>
        public ChartSeriesBuilder AddParliament(
            string name,
            IEnumerable<Models.ParliamentGroup> groups,
            Action<SeriesBuilder>? configure = null)
        {
            if (name   is null) throw new ArgumentNullException(nameof(name));
            if (groups is null) throw new ArgumentNullException(nameof(groups));
            var series = new Models.Series { Name = name, Type = ChartType.Parliament };
            series.ParliamentData.AddRange(groups);
            configure?.Invoke(new SeriesBuilder(series));
            _series.Add(series);
            return this;
        }

        // ---- trendline overlays (computed from source data, rendered as Line series) ----

        /// <summary>
        /// Adds a least-squares linear-regression line computed from <paramref name="sourceData"/>.
        /// Null values in the source are ignored; the line spans the full index range.
        /// </summary>
        public ChartSeriesBuilder AddLinearRegression(string name, IEnumerable<double?> sourceData,
            Action<SeriesBuilder>? configure = null)
        {
            if (name is null)       throw new ArgumentNullException(nameof(name));
            if (sourceData is null) throw new ArgumentNullException(nameof(sourceData));
            return AddLine(name, ComputeLinearRegression(new List<double?>(sourceData)), configure);
        }

        /// <summary>Adds a linear-regression overlay from non-nullable source data.</summary>
        public ChartSeriesBuilder AddLinearRegression(string name, IEnumerable<double> sourceData,
            Action<SeriesBuilder>? configure = null)
            => AddLinearRegression(name, ToNullable(sourceData), configure);

        /// <summary>
        /// Adds a simple moving-average line computed from <paramref name="sourceData"/>.
        /// The first <paramref name="period"/> − 1 values are emitted as null (the window is not yet full).
        /// </summary>
        public ChartSeriesBuilder AddMovingAverage(string name, IEnumerable<double?> sourceData,
            int period = 3, Action<SeriesBuilder>? configure = null)
        {
            if (name is null)       throw new ArgumentNullException(nameof(name));
            if (sourceData is null) throw new ArgumentNullException(nameof(sourceData));
            if (period <= 0)        throw new ArgumentOutOfRangeException(nameof(period), period, "Period must be > 0.");
            return AddLine(name, ComputeMovingAverage(new List<double?>(sourceData), period), configure);
        }

        /// <summary>Adds a moving-average overlay from non-nullable source data.</summary>
        public ChartSeriesBuilder AddMovingAverage(string name, IEnumerable<double> sourceData,
            int period = 3, Action<SeriesBuilder>? configure = null)
            => AddMovingAverage(name, ToNullable(sourceData), period, configure);

        /// <summary>
        /// Adds an exponential-smoothing line (EMA) computed from <paramref name="sourceData"/>.
        /// <paramref name="alpha"/> controls responsiveness: 0 → very smooth, 1 → tracks raw data.
        /// </summary>
        public ChartSeriesBuilder AddExponentialSmoothing(string name, IEnumerable<double?> sourceData,
            double alpha = 0.3, Action<SeriesBuilder>? configure = null)
        {
            if (name is null)       throw new ArgumentNullException(nameof(name));
            if (sourceData is null) throw new ArgumentNullException(nameof(sourceData));
            if (alpha <= 0 || alpha > 1)
                throw new ArgumentOutOfRangeException(nameof(alpha), alpha, "Alpha must be in (0, 1].");
            return AddLine(name, ComputeExponentialSmoothing(new List<double?>(sourceData), alpha), configure);
        }

        /// <summary>Adds an exponential-smoothing overlay from non-nullable source data.</summary>
        public ChartSeriesBuilder AddExponentialSmoothing(string name, IEnumerable<double> sourceData,
            double alpha = 0.3, Action<SeriesBuilder>? configure = null)
            => AddExponentialSmoothing(name, ToNullable(sourceData), alpha, configure);

        // ---- trendline computation helpers ----

        private static List<double?> ComputeLinearRegression(IList<double?> data)
        {
            double sumX = 0, sumY = 0, sumXY = 0, sumXX = 0;
            int n = 0;
            for (int i = 0; i < data.Count; i++)
            {
                if (!data[i].HasValue) continue;
                double y = data[i]!.Value;
                sumX += i; sumY += y; sumXY += (double)i * y; sumXX += (double)i * i;
                n++;
            }
            var result = new List<double?>(data.Count);
            if (n < 2) { for (int i = 0; i < data.Count; i++) result.Add(null); return result; }
            double denom = n * sumXX - sumX * sumX;
            if (System.Math.Abs(denom) < 1e-10) { for (int i = 0; i < data.Count; i++) result.Add(null); return result; }
            double slope     = (n * sumXY - sumX * sumY) / denom;
            double intercept = (sumY - slope * sumX) / n;
            for (int i = 0; i < data.Count; i++) result.Add(intercept + slope * i);
            return result;
        }

        private static List<double?> ComputeMovingAverage(IList<double?> data, int period)
        {
            var result = new List<double?>(data.Count);
            var buf = new List<double>(period + 1);
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].HasValue)
                {
                    buf.Add(data[i]!.Value);
                    if (buf.Count > period) buf.RemoveAt(0);
                }
                if (buf.Count == period)
                {
                    double s = 0;
                    foreach (double v in buf) s += v;
                    result.Add(s / period);
                }
                else result.Add(null);
            }
            return result;
        }

        private static List<double?> ComputeExponentialSmoothing(IList<double?> data, double alpha)
        {
            var result = new List<double?>(data.Count);
            double? ema = null;
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].HasValue)
                    ema = ema.HasValue ? alpha * data[i]!.Value + (1.0 - alpha) * ema!.Value : data[i]!.Value;
                result.Add(ema);
            }
            return result;
        }

        // ---- chart-type-aware shorthand ----

        /// <summary>
        /// Adds a series using the chart's primary type (set via e.g.
        /// <see cref="ChartBuilder.AsPie()"/>). Falls back to
        /// <see cref="ChartType.Line"/> when no primary type is configured.
        /// </summary>
        public ChartSeriesBuilder Add(string name, IEnumerable<double?> data, Action<SeriesBuilder>? configure = null)
            => AddTyped(name, _defaultType ?? ChartType.Line, data, configure);

        /// <summary>Adds a series using the chart's primary type from non-nullable data.</summary>
        public ChartSeriesBuilder Add(string name, IEnumerable<double> data, Action<SeriesBuilder>? configure = null)
            => Add(name, ToNullable(data), configure);

        // ---- Phase 6 chart types ----

        /// <summary>
        /// Adds a <see cref="ChartType.Dumbbell"/> (dot-plot) series.
        /// Each <see cref="RangePoint"/> renders as two filled dots connected by a vertical line.
        /// </summary>
        public ChartSeriesBuilder AddDumbbell(string name, IEnumerable<RangePoint> data,
            Action<SeriesBuilder>? configure = null)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (data is null) throw new ArgumentNullException(nameof(data));
            var series = new Series { Name = name, Type = ChartType.Dumbbell };
            series.RangeData.AddRange(data);
            configure?.Invoke(new SeriesBuilder(series));
            _series.Add(series);
            return this;
        }

        /// <summary>
        /// Adds a <see cref="ChartType.Stream"/> (ThemeRiver) series.
        /// Multiple stream series are stacked with a centered wiggle baseline.
        /// </summary>
        public ChartSeriesBuilder AddStream(string name, IEnumerable<double?> data,
            Action<SeriesBuilder>? configure = null)
            => AddTyped(name, ChartType.Stream, data, configure);

        /// <summary>Adds a stream series from non-nullable data.</summary>
        public ChartSeriesBuilder AddStream(string name, IEnumerable<double> data,
            Action<SeriesBuilder>? configure = null)
            => AddStream(name, ToNullable(data), configure);

        /// <summary>
        /// Adds a <see cref="ChartType.Gantt"/> (timeline) series.
        /// Each <see cref="Models.GanttTask"/> renders as a horizontal bar spanning
        /// its numeric <c>Start</c>–<c>End</c> range on the X (time) axis.
        /// </summary>
        public ChartSeriesBuilder AddGantt(string name, IEnumerable<Models.GanttTask> tasks,
            Action<SeriesBuilder>? configure = null)
        {
            if (name is null)  throw new ArgumentNullException(nameof(name));
            if (tasks is null) throw new ArgumentNullException(nameof(tasks));
            var series = new Series { Name = name, Type = ChartType.Gantt };
            series.GanttData.AddRange(tasks);
            configure?.Invoke(new SeriesBuilder(series));
            _series.Add(series);
            return this;
        }

        /// <summary>
        /// Adds a <see cref="ChartType.Sankey"/> (flow diagram) series.
        /// <paramref name="nodes"/> define the named stages; <paramref name="links"/> define
        /// the directed flow quantities between them.
        /// </summary>
        public ChartSeriesBuilder AddSankey(string name,
            IEnumerable<Models.SankeyNode> nodes, IEnumerable<Models.SankeyLink> links,
            Action<SeriesBuilder>? configure = null)
        {
            if (name is null)  throw new ArgumentNullException(nameof(name));
            if (nodes is null) throw new ArgumentNullException(nameof(nodes));
            if (links is null) throw new ArgumentNullException(nameof(links));
            var series = new Series { Name = name, Type = ChartType.Sankey };
            series.SankeyNodes.AddRange(nodes);
            series.SankeyLinks.AddRange(links);
            configure?.Invoke(new SeriesBuilder(series));
            _series.Add(series);
            return this;
        }

        // ---- internal ----

        private ChartSeriesBuilder AddTyped(string name, ChartType type, IEnumerable<double?> data, Action<SeriesBuilder>? configure)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (data is null) throw new ArgumentNullException(nameof(data));

            var series = new Series { Name = name, Type = type };
            series.Data.AddRange(data);
            configure?.Invoke(new SeriesBuilder(series));
            _series.Add(series);
            return this;
        }

        private static IEnumerable<double?> ToNullable(IEnumerable<double> data)
        {
            foreach (var v in data) yield return v;
        }
    }
}
