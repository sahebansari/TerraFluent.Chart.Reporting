using System.Text;
using TerraFluent.Chart.Reporting.Models;

namespace TerraFluent.Chart.Reporting.Samples;

/// <summary>
/// Console application that generates sample SVG charts to showcase the
/// TerraFluent.Chart.Reporting library features. Individual .svg files, a self-contained index.html,
/// and an AutoAnalytics dashboard.html are written to the output folder.
/// </summary>
/// <remarks>
/// This file holds only the orchestration (theme, output folder, <see cref="Main"/> and the chart
/// registry). The individual chart factory methods live in <c>SampleCharts.cs</c> and the showcase
/// page builder lives in <c>IndexHtml.cs</c> — all three are the same <c>partial class Program</c>.
/// </remarks>
internal static partial class Program
{
    private static readonly string OutputDir = Path.Combine(
        AppContext.BaseDirectory, "output");

    /// <summary>
    /// Change this single variable to apply a different theme to every sample chart.
    /// Built-in options: ChartTheme.Default, ChartTheme.Dark, ChartTheme.Pastel, ChartTheme.Monochrome,
    ///   ChartTheme.Ocean, ChartTheme.Sunset, ChartTheme.Forest, ChartTheme.Neon, ChartTheme.Minimal,
    ///   ChartTheme.Warm, ChartTheme.Arctic, ChartTheme.Business, ChartTheme.Material,
    ///   ChartTheme.TrafficLight, ChartTheme.Accessible, ChartTheme.Vivid
    /// </summary>
    private static readonly ChartTheme GlobalTheme = ChartTheme.Minimal;

    private static void Main()
    {
        Directory.CreateDirectory(OutputDir);

        Console.WriteLine("TerraFluent.Chart.Reporting \u2014 Sample Chart Generator");
        Console.WriteLine(new string('=', 55));
        Console.WriteLine();

        var charts = new List<ChartEntry>();

        charts.Add(Run("01_line_chart", "Line Chart",
            "Single-series animated line chart with monthly visitors data.",
            LineChart));

        charts.Add(Run("02_multiseries_line", "Multi-Series Line",
            "Three lines (Revenue, Cost, Profit).",
            MultiSeriesLine));

        charts.Add(Run("03_area_chart", "Area Chart",
            "Filled area chart showing daily active users over 10 days.",
            AreaChart));

        charts.Add(Run("04_column_chart", "Column Chart",
            "Single-series column chart \u2014 product sales by category.",
            ColumnChart));

        charts.Add(Run("05_grouped_columns", "Grouped Columns",
            "Two column series side-by-side for Budget vs Actual spend.",
            GroupedColumns));

        charts.Add(Run("06_pie_chart", "Pie Chart",
            "Pie chart with legend showing market share by vendor.",
            PieChart));

        charts.Add(Run("07_spline_chart", "Spline Chart",
            "Smooth spline curves \u2014 temperature trends for three cities.",
            SplineChart));

        charts.Add(Run("08_mixed_line_area", "Mixed Line + Area",
            "Area series (Volume) combined with a Line series (Price).",
            MixedLineAndArea));

        charts.Add(Run("09_static_mode", "Static Mode (PDF / Email)",
            "SvgMode.Static \u2014 no CSS hover rules, no embedded JS.",
            StaticMode));

        charts.Add(Run("10_interactive_mode", "Interactive Mode (Browser)",
            "SvgMode.Interactive \u2014 CSS hover tooltips + embedded JS.",
            InteractiveMode));

        charts.Add(Run("11_spline_smooth", "True Spline (B\u00e9zier)",
            "Catmull-Rom cubic B\u00e9zier curves.",
            SplineSmooth));

        charts.Add(Run("12_horizontal_bar", "Horizontal Bar Chart",
            "ChartType.Bar \u2014 bars grow left-to-right.",
            HorizontalBar));

        charts.Add(Run("13_scatter_chart", "Scatter Chart",
            "ChartType.Scatter \u2014 dots only, no connecting line.",
            ScatterChart));

        charts.Add(Run("14_animated_column", "Animated Column (SMIL)",
            "Bars grow from baseline on load via SMIL.",
            AnimatedColumn));

        charts.Add(Run("15_dark_theme", "Dark Theme",
            "ChartTheme.Dark \u2014 navy background, light text.",
            DarkTheme));

        charts.Add(Run("16_pastel_theme", "Pastel Theme",
            "ChartTheme.Pastel \u2014 soft off-white background.",
            PastelTheme));

        charts.Add(Run("17_data_labels", "Data Labels",
            "ShowDataLabels \u2014 values rendered on bars and line points.",
            DataLabels));

        charts.Add(Run("18_donut_chart", "Donut Chart",
            "Pie chart with DonutHolePercent = 0.55 creating a donut hole.",
            DonutChart));

        charts.Add(Run("19_stacked_columns", "Stacked Columns (Normal)",
            "Stacking.Normal \u2014 bars are stacked so totals are visible.",
            StackedColumns));

        charts.Add(Run("20_stacked_percent", "Stacked Columns (100 %)",
            "Stacking.Percent \u2014 each column normalised to 100 %.",
            StackedPercent));

        charts.Add(Run("21_stacked_area", "Stacked Area",
            "Stacking.Normal on Area series.",
            StackedArea));

        charts.Add(Run("22_secondary_y_axis", "Secondary Y-Axis",
            "WithYAxis2() \u2014 columns on left axis, line on right axis.",
            SecondaryYAxis));

        charts.Add(Run("23_plot_bands_lines", "Plot Bands & Reference Lines",
            "YAxis.PlotBands and YAxis.PlotLines.",
            PlotBandsAndLines));

        charts.Add(Run("24_waterfall", "Waterfall Chart",
            "ChartType.Waterfall \u2014 incremental running-total chart.",
            WaterfallDemo));

        charts.Add(Run("25_gauge", "Gauge / Radial Chart",
            "ChartType.Gauge \u2014 semi-circular dial.",
            GaugeDemo));

        charts.Add(Run("26_label_rotation", "X-Axis Label Rotation",
            "Axis.LabelRotation = -45 \u2014 diagonal labels.",
            LabelRotationDemo));

        charts.Add(Run("27_datalabel_showcase", "Data Label Showcase",
            "DataLabel: FontSize \u00b7 Format \u2014 three distinct styles on one chart.",
            DataLabelShowcase));

        charts.Add(Run("28_pie_label_placement", "Pie \u2014 Label Placement",
            "DataLabelRadius: inside, at edge, and outside with connector.",
            PieLabelPlacement));

        charts.Add(Run("29_legend_vertical_right", "Legend \u2014 Vertical Right",
            "LegendBuilder: Vertical() \u00b7 AlignRight() \u00b7 SymbolRadius.",
            LegendVerticalRight));

        charts.Add(Run("30_legend_top_center", "Legend \u2014 Top Center",
            "LegendBuilder: AtTop() \u00b7 AlignCenter() \u00b7 Horizontal() \u00b7 Padding.",
            LegendTopCenter));

        charts.Add(Run("31_legend_bottom_left", "Legend \u2014 Bottom Left",
            "LegendBuilder: AtBottom() \u00b7 AlignLeft() \u00b7 SymbolSize \u00b7 Offset.",
            LegendBottomLeft));

        charts.Add(Run("32_column_border_radius", "Column \u2014 Border & Corner Radius",
            "Series.BorderWidth \u00b7 BorderRadius per column series.",
            ColumnBorderRadius));

        charts.Add(Run("33_scatter_marker_border", "Scatter \u2014 Marker Borders",
            "Series.BorderWidth on scatter dot markers.",
            ScatterMarkerBorder));

        charts.Add(Run("34_area_fill_opacity", "Area \u2014 Fill Opacity",
            "Series.FillOpacity: 0.15 \u00b7 0.40 \u00b7 0.70 \u2014 three overlapping area series.",
            AreaFillOpacity));

        charts.Add(Run("35_tooltip_custom_style", "Tooltip \u2014 Custom Style",
            "TooltipBuilder: FontSize \u00b7 Padding \u00b7 Format \u00b7 TransitionDuration.",
            TooltipCustomStyle));

        charts.Add(Run("36_tooltip_no_arrow", "Tooltip \u2014 No Arrow",
            "TooltipBuilder: HideArrow() \u00b7 custom Format template.",
            TooltipNoArrow));

        charts.Add(Run("37_donut_center_label", "Donut \u2014 Center Label",
            "DonutCenter: auto total \u00b7 title caption \u00b7 font size.",
            DonutCenterLabel));

        charts.Add(Run("38_monthly_sales_donut", "Monthly Sales \u2014 USD Donut",
            "Donut chart with USD data labels and DonutCenter total.",
            MonthlySalesDonut));

        charts.Add(Run("39_data_ring_kpi", "Data Ring \u2014 KPI",
            "ChartType.DataRing \u2014 full 360\u00b0 progress ring.",
            DataRingKpi));

        charts.Add(Run("40_data_ring_dashboard", "Data Ring \u2014 Dashboard Trio",
            "Three DataRing charts side-by-side as a KPI dashboard.",
            DataRingDashboard));

        charts.Add(Run("41_fluent_api_showcase", "Fluent API Showcase",
            "Size \u00b7 AsInteractive \u00b7 Animate \u00b7 StackNormal \u00b7 YAxisFormat \u00b7 ShowDataLabels \u00b7 SeriesBuilder.",
            FluentApiShowcase));

        charts.Add(Run("42_fixed_width", "Fixed Width Chart",
            "Width(600) \u2014 pins the SVG to exactly 600 px wide.",
            FixedWidthDemo));

        charts.Add(Run("43_responsive_width", "Responsive Width Chart",
            "ResponsiveWidth() \u2014 SVG fills its container.",
            ResponsiveWidthDemo));

        charts.Add(Run("44_x_axis_tick_interval", "X-Axis Numeric Tick Interval",
            "XAxisTickInterval \u2014 numeric X-axis with explicit tick spacing.",
            XAxisTickIntervalDemo));

        charts.Add(Run("45_fork_variants", "Fork \u2014 Chart Variants",
            "Fork() \u2014 produce a Static and an Animated variant.",
            ForkVariants));

        charts.Add(Run("46_bubble", "Bubble Chart",
            "AddBubble() \u2014 scatter with a third dimension (Z).",
            BubbleDemo));

        charts.Add(Run("47_heatmap", "Heatmap",
            "AddHeatmap() \u2014 colour-coded matrix.",
            HeatmapDemo));

        charts.Add(Run("48_column_range", "Column Range",
            "AddColumnRange() \u2014 each category spans from low to high.",
            ColumnRangeDemo));

        charts.Add(Run("49_area_range", "Area Range",
            "AddAreaRange() \u2014 filled band between a lower and upper line.",
            AreaRangeDemo));

        charts.Add(Run("50_funnel", "Funnel Chart",
            "AddFunnel() \u2014 stacked trapezoid stages.",
            FunnelDemo));

        charts.Add(Run("51_treemap", "Treemap",
            "AddTreemap() \u2014 nested rectangles sized proportionally.",
            TreemapDemo));

        charts.Add(Run("52_click_line", "Data Point Click \u2014 Line Chart",
            "OnPointClick() \u2014 click any data point to see its details.",
            ClickLineChart));

        charts.Add(Run("53_click_column", "Data Point Click \u2014 Column Chart",
            "OnPointClick() \u2014 two column series; click any bar.",
            ClickColumnChart));

        charts.Add(Run("54_click_shared", "Data Point Click \u2014 Shared Tooltip",
            "OnPointClick() combined with shared tooltip.",
            ClickSharedTooltip));

        charts.Add(Run("55_legend_toggle", "Legend Series Toggle",
            "SvgMode.Interactive \u2014 click any legend item to hide/show that series.",
            LegendToggleDemo));

        charts.Add(Run("56_export_button", "SVG Export Button",
            "ShowExportButton() \u2014 download button appears top-right.",
            ExportButtonDemo));

        charts.Add(Run("57_export_menu", "Multi-Format Export Menu",
            "ShowExportMenu() \u2014 SVG, PNG, JPEG, and PDF options.",
            ExportMenuDemo));

        charts.Add(Run("58_autoinsight_line", "AutoInsight \u2014 Line Chart",
            "AnomalyBands \u00b7 TrendLine \u00b7 MovingAverage \u00b7 HighlightPeaks \u00b7 NarrativeSummary.",
            AutoInsightLine));

        charts.Add(Run("59_autoinsight_area", "AutoInsight \u2014 Area Chart",
            "AutoInsight on an Area series \u2014 pure SVG, no JavaScript.",
            AutoInsightArea));

        charts.Add(Run("60_parliament_westoria", "Parliament Chart",
            "AddParliament() \u2014 semicircular hemicycle seating diagram.",
            ParliamentWestoria));

        charts.Add(Run("61_log_axis", "Logarithmic Y Axis",
            "YAxisLogarithmic() \u2014 base-10 log scale.",
            LogarithmicAxis));

        charts.Add(Run("62_datetime_axis", "Date/Time X Axis",
            "XAxisDateTime() \u2014 auto-formatted by span.",
            DateTimeAxis));

        charts.Add(Run("63_radar", "Radar / Spider Chart",
            "AddRadar() \u2014 closed polygon, great for multivariate comparisons.",
            RadarChart));

        charts.Add(Run("64_boxplot", "Box-and-Whisker Plot",
            "AddBoxPlot() \u2014 five-number summary per category.",
            BoxPlotChart));

        charts.Add(Run("65_errorbar", "Column + Error Bars",
            "AddErrorBar() \u2014 I-beam uncertainty whiskers.",
            ErrorBarChart));

        charts.Add(Run("66_gradient_fill", "Gradient-Filled Area",
            "LinearGradientFill() \u2014 vertical gradient via SVG <linearGradient>.",
            GradientFillChart));

        charts.Add(Run("67_pattern_fill", "Pattern-Filled Columns",
            "PatternFill() \u2014 diagonal lines, dots, grid via SVG <pattern>.",
            PatternFillChart));

        charts.Add(Run("68_marker_symbols", "Marker Symbol Shapes",
            "MarkerSymbol() \u2014 circle, square, diamond, and triangle.",
            MarkerSymbolChart));

        charts.Add(Run("69_zones", "Threshold Zones",
            "Zones() \u2014 recolour a line by value band.",
            ZonesChart));

        charts.Add(Run("70_candlestick", "Candlestick",
            "AddCandlestick() \u2014 OHLC bodies with wicks.",
            CandlestickChart));

        charts.Add(Run("71_ohlc", "OHLC Bars",
            "AddOhlc() \u2014 high-low bar with open/close ticks.",
            OhlcChart));

        charts.Add(Run("72_annotations", "Annotations",
            "Annotations() \u2014 labels, lines, rectangles and circles.",
            AnnotationsChart));

        charts.Add(Run("73_vivid_theme", "Vivid Theme",
            "ChartTheme.Vivid \u2014 HighCharts-inspired full-spectrum palette.",
            VividTheme));

        charts.Add(Run("74_label_layout", "Label Layout Builder",
            "LabelLayout() \u2014 rotation, wrap, stagger, font scaling, collision detection.",
            LabelLayoutChart));

        charts.Add(Run("75_high_contrast_theme", "High-Contrast Theme",
            "ChartTheme.HighContrast \u2014 WCAG AA palette, all colours \u2265 4.5:1 on white.",
            HighContrastThemeChart));

        charts.Add(Run("76_null_gap_policy", "Null-Gap Policy",
            "NullGap() \u2014 Break (gap), Connect (bridge), Zero (baseline) for missing values.",
            NullGapPolicyChart));

        charts.Add(Run("77_target_lines", "Per-Series Target Lines",
            "TargetLine() \u2014 horizontal reference lines scoped to a single series.",
            TargetLinesChart));

        charts.Add(Run("78_render_to_data_uri", "RenderToDataUri()",
            "Embeds the chart as a Base64 data: URI inside an HTML img tag.",
            DataUriEmbedChart));

        // ---- Phase 2 samples -----------------------------------------------

        charts.Add(Run("79_percent_stacked", "100% Stacked Columns",
            "StackPercent() — Y-axis automatically labels 0% … 100%.",
            PercentStackedChart));

        charts.Add(Run("80_dual_axis_combo", "Dual-Axis Combo Chart",
            "Column (primary Y) + Line (secondary Y) on one chart.",
            DualAxisComboChart));

        charts.Add(Run("81_combo_chart", "Mixed-Type Combo Chart",
            "Column + Line + Area series rendered together.",
            ComboChartDemo));

        charts.Add(Run("82_inverted_axis", "Inverted Y-Axis",
            "YAxisInverted() — minimum at the top, ranking style.",
            InvertedAxisChart));

        // ---- Phase 3 samples -----------------------------------------------

        charts.Add(Run("83_linear_regression", "Linear Regression Overlay",
            "AddLinearRegression() — least-squares trend line over raw data.",
            LinearRegressionChart));

        charts.Add(Run("84_moving_average", "Moving Average Overlay",
            "AddMovingAverage() — simple 3-period moving average over a line series.",
            MovingAverageChart));

        charts.Add(Run("85_exponential_smoothing", "Exponential Smoothing Overlay",
            "AddExponentialSmoothing() — EMA overlay (α = 0.4) for noisy data.",
            ExponentialSmoothingChart));

        charts.Add(Run("86_data_table", "Data Table Toggle",
            "ShowDataTable() — appends a per-category value grid below the chart.",
            DataTableChart));

        // ---- Phase 4 samples -----------------------------------------------

        charts.Add(Run("87_aria_labels", "Custom ARIA Labels",
            "AriaLabel() + AriaDescription() — custom accessible title and description.",
            AriaLabelsChart));

        charts.Add(Run("88_culture_de", "German Culture (de-DE)",
            "Culture(\"de-DE\") — number formatting uses comma decimal separator.",
            GermanCultureChart));

        charts.Add(Run("89_rtl_chart", "Right-to-Left Chart",
            "RightToLeft() — dir=\"rtl\" on SVG root, direction:rtl CSS on all text.",
            RtlChart));

        // ---- Phase 5 samples -----------------------------------------------

        charts.Add(Run("90_template_revenue", "Template: Revenue",
            "ChartTemplate.Revenue — column chart with N0-formatted Y-axis.",
            TemplateRevenueChart));

        charts.Add(Run("91_template_kpi", "Template: KPI Dashboard",
            "ChartTemplate.KpiDashboard — dark theme, interactive, no grid/legend.",
            TemplateKpiChart));

        charts.Add(Run("92_template_timeseries", "Template: Time Series",
            "ChartTemplate.TimeSeries — spline, animated, 1 s entry.",
            TemplateTimeSeriesChart));

        charts.Add(Run("93_template_exec", "Template: Executive Summary",
            "ChartTemplate.ExecutiveSummary — pastel bar chart, static/PDF-safe.",
            TemplateExecSummaryChart));

        charts.Add(Run("94_json_roundtrip", "JSON Round-Trip",
            "ChartOptions.ToJson() / ChartBuilder.FromJson() — persist and restore a chart.",
            JsonRoundTripChart));

        // ---- Phase 6 samples -----------------------------------------------

        charts.Add(Run("95_dumbbell", "Dumbbell / Dot-Plot",
            "AddDumbbell() — two dots per category connected by a vertical line.",
            DumbbellChart));

        charts.Add(Run("96_stream", "Stream Graph (ThemeRiver)",
            "AddStream() — stacked areas with a centered wiggle baseline.",
            StreamChart));

        charts.Add(Run("97_gantt", "Gantt / Timeline",
            "AddGantt() — horizontal task bars on a numeric time axis.",
            GanttChart));

        charts.Add(Run("98_sankey", "Sankey Flow Diagram",
            "AddSankey() — node-link flow diagram with cubic-bezier links.",
            SankeyChart));

        // ---- Phase 7 samples -----------------------------------------------

        charts.Add(Run("99_range_selector", "Range Selector (Navigator)",
            "RangeSelector() — interactive brush strip below the chart fires tf:rangechange.",
            RangeSelectorChart));

        charts.Add(Run("100_sync_tooltips", "Synchronized Tooltips",
            "SyncGroup() — two charts share the same hover group so tooltips mirror each other.",
            SyncTooltipPage));

        charts.Add(Run("101_drilldown", "Drill-Down Chart",
            "WithDrilldown() — click a column to open a child detail chart; Back returns to overview.",
            DrilldownChart));

        charts.Add(Run("102_grid_toggle", "Grid Lines Toggle",
            "GridLines(false) / HideGridLines() — show or hide the background plot grid.",
            GridLinesToggleChart));

        Console.Write("  Generating index.html ... ");
        try
        {
            GenerateIndexHtml(charts);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("OK");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAILED \u2014 {ex.Message}");
        }
        finally { Console.ResetColor(); }

        Console.Write("  Generating auto-dashboard ... ");
        
        try
        {
            AutoAnalyticsDashboardSample.Generate(OutputDir);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("OK");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAILED \u2014 {ex.Message}");
        }
        finally { Console.ResetColor(); }

        Console.Write("  Generating agent report ... ");
        try
        {
            AutoAnalyticsAgentSample.Generate(OutputDir);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("OK");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAILED \u2014 {ex.Message}");
        }
        finally { Console.ResetColor(); }

        Console.WriteLine();
        Console.WriteLine($"Output folder : {OutputDir}");
        Console.WriteLine($"Showcase page : {Path.Combine(OutputDir, "index.html")}");
        Console.WriteLine($"Smart dashboard: {Path.Combine(OutputDir, "dashboard.html")}");
        Console.WriteLine($"Agent report  : {Path.Combine(OutputDir, "agent-report.html")}");
        Console.WriteLine("Open index.html in a browser to view all charts.");
    }

    // ------------------------------------------------------------------ runner

    private record ChartEntry(string FileName, string Title, string Description, string Svg);

    private static ChartEntry Run(string name, string title, string description, Func<string> generator)
    {
        Console.Write($"  Generating {name} ... ");
        string svg = string.Empty;
        try
        {
            svg = generator();
            File.WriteAllText(Path.Combine(OutputDir, $"{name}.svg"), svg, Encoding.UTF8);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("OK");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAILED \u2014 {ex.Message}");
        }
        finally { Console.ResetColor(); }
        return new ChartEntry(name, title, description, svg);
    }
}
