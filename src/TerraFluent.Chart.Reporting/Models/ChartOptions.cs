using System.Collections.Generic;

namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// Root options object for a chart.
    /// </summary>
    public class ChartOptions
    {
        /// <summary>Primary chart title (text, alignment, optional CSS style).</summary>
        public ChartTitle Title { get; set; } = new ChartTitle();
        /// <summary>Secondary chart subtitle rendered below the main title.</summary>
        public ChartTitle Subtitle { get; set; } = new ChartTitle();
        /// <summary>Horizontal axis configuration.</summary>
        public Axis XAxis { get; set; } = new Axis();
        /// <summary>Primary (left-hand) vertical axis configuration.</summary>
        public Axis YAxis { get; set; } = new Axis();
        /// <summary>Legend (series key) configuration.</summary>
        public Legend Legend { get; set; } = new Legend();
        /// <summary>Tooltip (hover popup) configuration.</summary>
        public TooltipOptions Tooltip { get; set; } = new TooltipOptions();
        /// <summary>Collection of data series to render.</summary>
        public List<Series> Series { get; set; } = new List<Series>();
        /// <summary>Free-form annotations (labels, lines, rectangles, circles) drawn over the plot area.</summary>
        public List<Annotation> Annotations { get; set; } = new List<Annotation>();
        /// <summary>
        /// Dynamic axis label layout configuration (rotation, wrap, skip, font scaling,
        /// collision detection, stagger). <c>null</c> = legacy auto-behaviour.
        /// </summary>
        public LabelLayoutOptions? LabelLayout { get; set; }

        /// <summary>
        /// Smart label layout for free-form annotations — bounding-box collision detection,
        /// automatic repositioning, and leader-line connectors. <c>null</c> = disabled.
        /// Enable via <c>AnnotationBuilder.SmartLayout()</c>.
        /// </summary>
        public AnnotationLayoutOptions? AnnotationLayout { get; set; }

        /// <summary>Entry/load animation configuration.</summary>
        public AnimationOptions Animation { get; set; } = new AnimationOptions();

        /// <summary>Visual theme (palette, fonts, background). Defaults to <see cref="ChartTheme.Default"/>.</summary>
        public ChartTheme Theme { get; set; } = ChartTheme.Default;

        /// <summary>
        /// Optional primary chart type for the whole chart.
        /// When set, <see cref="TerraFluent.Chart.Reporting.Builder.ChartSeriesBuilder.Add(string,System.Collections.Generic.IEnumerable{double?},System.Action{TerraFluent.Chart.Reporting.Builder.SeriesBuilder})"/> uses this as
        /// the default series type, and the renderer applies type-appropriate layout (e.g. no axes for Pie).
        /// </summary>
        public Enums.ChartType? PrimaryChartType { get; set; }

        /// <summary>Stacking mode for Column and Area series.</summary>
        public Enums.Stacking Stacking { get; set; } = Enums.Stacking.None;

        /// <summary>
        /// Optional secondary (right-hand) Y-axis. When set, series with
        /// <see cref="Series.YAxisIndex"/> = 1 use this axis for their scale.
        /// </summary>
        public Axis? YAxis2 { get; set; }

        /// <summary>Background color of the chart area. Overrides <see cref="Theme"/> background when set explicitly.</summary>
        public string? BackgroundColor { get; set; }

        /// <summary>Width in pixels. <c>null</c> = responsive / fluid (SVG emits <c>width="100%"</c>).</summary>
        public int? Width { get; set; }

        /// <summary>Height in pixels. Default is 400.</summary>
        public int Height { get; set; } = 400;

        /// <summary>
        /// Controls what is emitted in the SVG output.
        /// <list type="bullet">
        ///   <item><see cref="Enums.SvgMode.Static"/> — no CSS hover, no JS. Safe for PDF and email.</item>
        ///   <item><see cref="Enums.SvgMode.Animated"/> — SMIL + CSS hover tooltips. Safe for browsers and Blazor.</item>
        ///   <item><see cref="Enums.SvgMode.Interactive"/> — CSS hover + embedded JS. Browser only.</item>
        /// </list>
        /// Default is <see cref="Enums.SvgMode.Animated"/>.
        /// </summary>
        public Enums.SvgMode RenderMode { get; set; } = Enums.SvgMode.Animated;

        /// <summary>
        /// Optional JavaScript callback invoked when a data point is clicked (Interactive mode only).
        /// Provide a global function name (<c>"myHandler"</c>) or an inline expression
        /// (<c>"function(pt, e) { alert(pt.value); }"</c>).
        /// <para>The callback receives a point-data object: <c>{ index, value, name, color, x, y, category }</c>
        /// and the original DOM MouseEvent as the second argument.</para>
        /// </summary>
        public string? PointClickHandler { get; set; }

        /// <summary>
        /// When <c>true</c>, a download button is rendered in the top-right corner of the chart
        /// that exports the SVG to a file when clicked. Only active in
        /// <see cref="Enums.SvgMode.Interactive"/> mode.
        /// </summary>
        public bool ExportButtonEnabled { get; set; }

        /// <summary>Label displayed on the SVG export button. Defaults to "⬇ SVG".</summary>
        public string ExportButtonLabel { get; set; } = "\u2b07 SVG";

        /// <summary>
        /// When <see langword="true"/> a drop-down export menu is rendered at the top-right corner
        /// of the chart (Interactive mode only). The menu offers one item per entry in
        /// <see cref="ExportMenuFormats"/>.
        /// </summary>
        public bool ExportMenuEnabled { get; set; }

        /// <summary>
        /// The export formats shown in the drop-down menu when <see cref="ExportMenuEnabled"/> is
        /// <see langword="true"/>. Supported values: <c>"SVG"</c>, <c>"PNG"</c>, <c>"JPEG"</c>,
        /// <c>"PDF"</c>. Defaults to all four.
        /// </summary>
        public List<string> ExportMenuFormats { get; set; } = new List<string> { "SVG", "PNG", "JPEG", "PDF" };

        /// <summary>Optional data table appended below the chart showing raw series values per category.</summary>
        public DataTableOptions DataTable { get; set; } = new DataTableOptions();

        // ------------------------------------------------------------------ accessibility / localization

        /// <summary>Overrides the auto-generated accessible name on the SVG root element and the &lt;title&gt; element.</summary>
        public string? AriaLabel { get; set; }

        /// <summary>Overrides the auto-generated accessible description in the SVG &lt;desc&gt; element.</summary>
        public string? AriaDescription { get; set; }

        /// <summary>
        /// Range-selector (navigator) strip rendered below the chart.
        /// Only active when <see cref="RenderMode"/> is <see cref="Enums.SvgMode.Interactive"/>.
        /// </summary>
        public RangeSelectorOptions RangeSelector { get; set; } = new RangeSelectorOptions();

        /// <summary>
        /// When non-null, this chart joins a synchronised-tooltip group.  All interactive
        /// SVGs on the same HTML page that share the same group identifier will
        /// mirror hover highlights when the user moves the cursor over any member chart.
        /// </summary>
        public string? SyncGroup { get; set; }

        /// <summary>
        /// Culture used for axis tick-label number formatting. <c>null</c> = <see cref="System.Globalization.CultureInfo.InvariantCulture"/>.
        /// Affects decimal separators and group separators in displayed numbers.
        /// Also sets the SVG <c>lang</c> attribute.
        /// </summary>
        public System.Globalization.CultureInfo? DisplayCulture { get; set; }

        /// <summary>
        /// When <see langword="true"/>, adds <c>dir="rtl"</c> to the SVG root and applies
        /// <c>direction:rtl</c> to all text elements. Use for Arabic, Hebrew, and Farsi content.
        /// </summary>
        public bool RightToLeft { get; set; }

        /// <summary>Returns the resolved background colour (explicit override or theme default).</summary>
        internal string ResolvedBackgroundColor =>
            BackgroundColor ?? Theme.BackgroundColor;

        /// <summary>
        /// Returns a deep-copy snapshot of this <see cref="ChartOptions"/>.
        /// The snapshot is independent — modifying the original does not affect it.
        /// </summary>
        public ChartOptions Clone()
        {
            var c = new ChartOptions
            {
                Width           = this.Width,
                Height          = this.Height,
                BackgroundColor = this.BackgroundColor,
                Theme           = this.Theme,       // theme is designed to be shared
                Stacking        = this.Stacking,
                PrimaryChartType = this.PrimaryChartType,
                RenderMode      = this.RenderMode
            };

            // Title / Subtitle
            c.Title.Text   = this.Title.Text;
            c.Title.Align  = this.Title.Align;
            c.Title.Style  = this.Title.Style;
            c.Subtitle.Text   = this.Subtitle.Text;
            c.Subtitle.Align  = this.Subtitle.Align;
            c.Subtitle.Style  = this.Subtitle.Style;

            // Axes
            CopyAxis(this.XAxis, c.XAxis);
            CopyAxis(this.YAxis, c.YAxis);
            if (this.YAxis2 != null) { c.YAxis2 = new Axis(); CopyAxis(this.YAxis2, c.YAxis2); }

            // Legend
            c.Legend.Enabled         = this.Legend.Enabled;
            c.Legend.Align           = this.Legend.Align;
            c.Legend.VerticalAlign   = this.Legend.VerticalAlign;
            c.Legend.Layout          = this.Legend.Layout;
            c.Legend.X               = this.Legend.X;
            c.Legend.Y               = this.Legend.Y;
            c.Legend.Padding         = this.Legend.Padding;
            c.Legend.Margin          = this.Legend.Margin;
            c.Legend.ItemStyle       = this.Legend.ItemStyle;
            c.Legend.ItemFontSize    = this.Legend.ItemFontSize;
            c.Legend.ItemFontColor   = this.Legend.ItemFontColor;
            c.Legend.SymbolWidth     = this.Legend.SymbolWidth;
            c.Legend.SymbolHeight    = this.Legend.SymbolHeight;
            c.Legend.SymbolRadius    = this.Legend.SymbolRadius;
            c.Legend.BorderColor     = this.Legend.BorderColor;
            c.Legend.BorderWidth     = this.Legend.BorderWidth;
            c.Legend.BorderRadius    = this.Legend.BorderRadius;
            c.Legend.BackgroundColor = this.Legend.BackgroundColor;

            // Animation
            c.Animation.Enabled  = this.Animation.Enabled;
            c.Animation.Duration = this.Animation.Duration;
            c.Animation.Easing   = this.Animation.Easing;

            // Tooltip
            c.Tooltip = this.Tooltip.Clone();

            // Point click handler
            c.PointClickHandler = this.PointClickHandler;

            // Export button
            c.ExportButtonEnabled = this.ExportButtonEnabled;
            c.ExportButtonLabel   = this.ExportButtonLabel;

            // Export menu
            c.ExportMenuEnabled = this.ExportMenuEnabled;
            c.ExportMenuFormats = new List<string>(this.ExportMenuFormats);

            // Annotations (deep copy)
            foreach (var a in this.Annotations)
                c.Annotations.Add(a.Clone());

            // Label layout
            c.LabelLayout = this.LabelLayout?.Clone();

            // Annotation smart layout
            c.AnnotationLayout = this.AnnotationLayout?.Clone();

            // Data table
            c.DataTable = this.DataTable.Clone();

            // Accessibility / localization
            c.AriaLabel        = this.AriaLabel;
            c.AriaDescription  = this.AriaDescription;
            c.DisplayCulture   = this.DisplayCulture;
            c.RightToLeft      = this.RightToLeft;

            // Phase 7
            c.RangeSelector = this.RangeSelector.Clone();
            c.SyncGroup     = this.SyncGroup;

            // Series (deep copy)
            foreach (var s in this.Series)
            {
                var cs = new Series
                {
                    Name             = s.Name,
                    Type             = s.Type,
                    Color            = s.Color,
                    Visible          = s.Visible,
                    DashStyle        = s.DashStyle,
                    LineWidth        = s.LineWidth,
                    ShowInLegend     = s.ShowInLegend,
                    DonutHolePercent = s.DonutHolePercent,
                    YAxisIndex       = s.YAxisIndex,
                    FillOpacity      = s.FillOpacity,
                    BorderColor      = s.BorderColor,
                    BorderWidth      = s.BorderWidth,
                    BorderRadius     = s.BorderRadius,
                    MarkerSize       = s.MarkerSize,
                    MarkerEnabled    = s.MarkerEnabled,
                    MarkerSymbol     = s.MarkerSymbol
                };
                cs.Fill = s.Fill?.Clone();
                foreach (var z in s.Zones)
                    cs.Zones.Add(new SeriesZone(z.Value, z.Color));
                cs.NullGapPolicy = s.NullGapPolicy;
                foreach (var tl in s.TargetLines)
                    cs.TargetLines.Add(new TargetLineOptions { Value = tl.Value, Label = tl.Label, Color = tl.Color, LineWidth = tl.LineWidth, DashStyle = tl.DashStyle });
                cs.Data.AddRange(s.Data);
                cs.WaterfallTotals.AddRange(s.WaterfallTotals);
                cs.RangeData.AddRange(s.RangeData);
                cs.BoxPlotData.AddRange(s.BoxPlotData);
                cs.OhlcData.AddRange(s.OhlcData);
                cs.BubbleData.AddRange(s.BubbleData);
                cs.HeatmapData.AddRange(s.HeatmapData);
                cs.HeatmapRowLabels.AddRange(s.HeatmapRowLabels);
                cs.ParliamentData.AddRange(s.ParliamentData);
                cs.ParliamentCenter = s.ParliamentCenter.Clone();
                cs.AutoInsight      = s.AutoInsight?.Clone();
                cs.DataLabel   = s.DataLabel.Clone();
                cs.DonutCenter = s.DonutCenter.Clone();
                cs.GanttData.AddRange(s.GanttData);
                cs.SankeyNodes.AddRange(s.SankeyNodes);
                cs.SankeyLinks.AddRange(s.SankeyLinks);
                cs.DrilldownChart = s.DrilldownChart?.Clone();
                foreach (var kvp in s.DrilldownCharts)
                    cs.DrilldownCharts[kvp.Key] = kvp.Value.Clone();
                c.Series.Add(cs);
            }

            return c;
        }

        private static void CopyAxis(Axis src, Axis dst)
        {
            dst.Title          = src.Title;
            dst.Type           = src.Type;            dst.Min            = src.Min;
            dst.Max            = src.Max;
            dst.Visible        = src.Visible;
            dst.GridLineVisible = src.GridLineVisible;
            dst.GridLineColor  = src.GridLineColor;
            dst.LabelFormat    = src.LabelFormat;
            dst.TickInterval   = src.TickInterval;
            dst.LabelRotation  = src.LabelRotation;
            dst.Inverted       = src.Inverted;
            dst.DateTimeLabelFormat = src.DateTimeLabelFormat;
            dst.Categories.AddRange(src.Categories);
            dst.DateTimeValues.AddRange(src.DateTimeValues);
            foreach (var b in src.PlotBands)
                dst.PlotBands.Add(new PlotBand
                {
                    From  = b.From,
                    To    = b.To,
                    Color = b.Color,
                    Label = b.Label
                });
            foreach (var l in src.PlotLines)
                dst.PlotLines.Add(new PlotLine
                {
                    Value     = l.Value,
                    Color     = l.Color,
                    Width     = l.Width,
                    DashStyle = l.DashStyle,
                    Label     = l.Label
                });
        }

        // ------------------------------------------------------------------ JSON round-trip

#if NET6_0_OR_GREATER
        /// <summary>
        /// Serialises this <see cref="ChartOptions"/> to an indented JSON string.
        /// Use <see cref="FromJson"/> or <see cref="Builder.ChartBuilder.FromJson"/> to restore it.
        /// Only available on .NET 6 and later.
        /// </summary>
        public string ToJson()
        {
            return System.Text.Json.JsonSerializer.Serialize(this, CreateJsonOptions());
        }

        /// <summary>
        /// Deserialises a JSON string previously produced by <see cref="ToJson"/> back into a
        /// <see cref="ChartOptions"/> instance.
        /// Only available on .NET 6 and later.
        /// </summary>
        public static ChartOptions FromJson(string json)
        {
            if (json is null) throw new System.ArgumentNullException(nameof(json));
            return System.Text.Json.JsonSerializer.Deserialize<ChartOptions>(json, CreateJsonOptions())
                ?? throw new System.Text.Json.JsonException("Deserialisation produced null.");
        }

        private static System.Text.Json.JsonSerializerOptions CreateJsonOptions()
        {
            var opts = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true,
            };
            opts.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            opts.Converters.Add(new CultureInfoJsonConverter());
            return opts;
        }

        private sealed class CultureInfoJsonConverter
            : System.Text.Json.Serialization.JsonConverter<System.Globalization.CultureInfo>
        {
            public override System.Globalization.CultureInfo? Read(
                ref System.Text.Json.Utf8JsonReader reader,
                System.Type typeToConvert,
                System.Text.Json.JsonSerializerOptions options)
            {
                var name = reader.GetString();
                return name is null ? null : new System.Globalization.CultureInfo(name);
            }

            public override void Write(
                System.Text.Json.Utf8JsonWriter writer,
                System.Globalization.CultureInfo value,
                System.Text.Json.JsonSerializerOptions options)
                => writer.WriteStringValue(value.Name);
        }
#else
        /// <summary>JSON serialisation is not supported on this target framework.</summary>
        public string ToJson() =>
            throw new System.NotSupportedException("ToJson requires .NET 6 or later.");

        /// <summary>JSON deserialisation is not supported on this target framework.</summary>
        public static ChartOptions FromJson(string json) =>
            throw new System.NotSupportedException("FromJson requires .NET 6 or later.");
#endif
    }
}
