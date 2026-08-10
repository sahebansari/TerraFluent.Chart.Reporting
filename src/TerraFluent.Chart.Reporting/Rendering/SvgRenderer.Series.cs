using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TerraFluent.Chart.Reporting.Analysis;
using TerraFluent.Chart.Reporting.Enums;
using TerraFluent.Chart.Reporting.Models;

namespace TerraFluent.Chart.Reporting.Rendering
{
    public partial class SvgRenderer
    {
        private static void AppendSeries(StringBuilder sb, ChartOptions options,
            int svgWidth, int svgHeight, int plotWidth, int plotHeight, string clipId,
            StringBuilder tooltipLayer, int xAxisTitleY = -1)
        {
            // Pre-compute primary Y range (used for series on axis 0, or when no secondary axis)
            var primarySeries = options.YAxis2 != null
                ? options.Series.FindAll(s => s.Visible && s.YAxisIndex == 0)
                : options.Series;
            var (yMinPrimaryRaw, yMaxPrimaryRaw) = ResolveYBounds(options.YAxis, primarySeries);
            bool primaryInv = options.YAxis.Inverted;
            double yMinPrimary = primaryInv ? yMaxPrimaryRaw : yMinPrimaryRaw;
            double yMaxPrimary = primaryInv ? yMinPrimaryRaw : yMaxPrimaryRaw;

            // Pre-compute secondary Y range if YAxis2 is configured
            double yMinSecondary = 0, yMaxSecondary = 1;
            if (options.YAxis2 != null)
            {
                var secSeries = options.Series.FindAll(s => s.Visible && s.YAxisIndex == 1);
                var (yMinSecRaw, yMaxSecRaw) = ResolveYBounds(options.YAxis2, secSeries);
                bool secInv = options.YAxis2.Inverted;
                yMinSecondary = secInv ? yMaxSecRaw : yMinSecRaw;
                yMaxSecondary = secInv ? yMinSecRaw : yMaxSecRaw;
            }

            // colourIdx tracks visible series so hidden/empty series don't shift the palette
            int colourIdx = 0;

            for (int si = 0; si < options.Series.Count; si++)
            {
                var series = options.Series[si];
                if (!series.Visible) continue;
                bool hasData = series.Data.Count > 0
                    || series.BubbleData.Count > 0
                    || series.RangeData.Count > 0
                    || series.HeatmapData.Count > 0
                    || series.ParliamentData.Count > 0
                    || series.BoxPlotData.Count > 0
                    || series.OhlcData.Count > 0
                    || series.GanttData.Count > 0
                    || series.SankeyLinks.Count > 0;
                if (!hasData) continue;

                // Select the correct Y range for this series
                double yMin = series.YAxisIndex == 1 && options.YAxis2 != null ? yMinSecondary : yMinPrimary;
                double yMax = series.YAxisIndex == 1 && options.YAxis2 != null ? yMaxSecondary : yMaxPrimary;

                string color = series.Color ?? options.Theme.Colors[colourIdx % options.Theme.Colors.Length];
                colourIdx++;  // advance AFTER assigning colour so hidden series don't consume a palette slot

                // Resolve gradient/pattern fill paint (falls back to the flat colour). When the
                // series carries a SeriesFill, emit its <defs> definition once and reference it.
                string? fillPaint = null;
                if (series.Fill != null)
                {
                    string fillId = $"{clipId}-fill-{si}";
                    AppendFillDef(sb, series.Fill, fillId);
                    fillPaint = $"url(#{fillId})";
                }

                int n = series.Data.Count;
                double step = n > 1 ? (double)plotWidth / (n - 1) : plotWidth / 2.0;
                if (options.XAxis.Categories?.Count > 0)
                    step = (double)plotWidth / options.XAxis.Categories.Count;

                // In Interactive mode wrap each series in a named group so the legend toggle can
                // show/hide it.  The wrapper is emitted only for Interactive so Static/Animated SVGs
                // stay exactly as before.
                if (options.RenderMode == SvgMode.Interactive)
                {
                    string ddAttr = series.DrilldownChart != null ? $" data-dd=\"{si}\"" : "";
                    sb.AppendLine($"  <g class=\"tf-sg\" data-si=\"{si}\" role=\"group\" aria-label=\"{Escape(series.Name)}\"{ddAttr}>");
                }

                switch (series.Type)
                {
                    case ChartType.Line:
                    case ChartType.Spline:
                    {
                        // Supply a stable path ID when RS is active so JS can redraw the line.
                        string? rsLpId = (options.RangeSelector.Enabled && options.RenderMode == SvgMode.Interactive)
                            ? $"{clipId.Substring(0, clipId.Length - 5)}-rs-lp-{si}"
                            : null;
                        AppendLineSeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, yMin, yMax, step, clipId, tooltipLayer, rsLpId);
                        break;
                    }
                    case ChartType.Column:
                        if (options.Stacking != Stacking.None)
                            AppendStackedColumnSeries(sb, series, si, color, options, svgWidth, svgHeight, plotWidth, plotHeight, clipId, tooltipLayer);
                        else
                            AppendColumnSeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, yMin, yMax, clipId, tooltipLayer, fillPaint);
                        break;
                    case ChartType.Bar:
                        if (options.Stacking != Stacking.None)
                            AppendStackedBarSeries(sb, series, si, color, options, svgWidth, svgHeight, plotWidth, plotHeight, clipId, tooltipLayer);
                        else
                            AppendBarSeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, yMin, yMax, clipId, tooltipLayer, xAxisTitleY, fillPaint);
                        break;
                    case ChartType.Scatter:
                        AppendScatterSeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, yMin, yMax, step, clipId, tooltipLayer);
                        break;
                    case ChartType.Area:
                        if (options.Stacking != Stacking.None)
                            AppendStackedAreaSeries(sb, series, si, color, options, svgWidth, svgHeight, plotWidth, plotHeight, step, clipId, tooltipLayer);
                        else
                            AppendAreaSeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, yMin, yMax, step, clipId, tooltipLayer, fillPaint);
                        break;
                    case ChartType.Pie:
                        AppendPieSeries(sb, series, options, svgWidth, svgHeight, plotWidth, plotHeight, tooltipLayer);
                        break;
                    case ChartType.Waterfall:
                        AppendWaterfallSeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, yMin, yMax, clipId, tooltipLayer);
                        break;
                    case ChartType.Gauge:
                        AppendGaugeSeries(sb, series, options, svgWidth, svgHeight, plotWidth, plotHeight, tooltipLayer);
                        break;
                    case ChartType.DataRing:
                        AppendDataRingSeries(sb, series, options, svgWidth, svgHeight, plotWidth, plotHeight, tooltipLayer);
                        break;
                    case ChartType.Bubble:
                        AppendBubbleSeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, clipId, tooltipLayer);
                        break;
                    case ChartType.Heatmap:
                        AppendHeatmapSeries(sb, series, options, svgWidth, svgHeight, plotWidth, plotHeight, clipId);
                        break;
                    case ChartType.ColumnRange:
                        AppendColumnRangeSeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, clipId, tooltipLayer);
                        break;
                    case ChartType.AreaRange:
                        AppendAreaRangeSeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, clipId, tooltipLayer);
                        break;
                    case ChartType.Funnel:
                        AppendFunnelSeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, tooltipLayer);
                        break;
                    case ChartType.Treemap:
                        AppendTreemapSeries(sb, series, options, svgWidth, svgHeight, plotWidth, plotHeight, tooltipLayer);
                        break;
                    case ChartType.Parliament:
                        AppendParliamentSeries(sb, series, options, svgWidth, svgHeight, plotWidth, clipId, tooltipLayer);
                        break;
                    case ChartType.Radar:
                        AppendRadarSeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, tooltipLayer);
                        break;
                    case ChartType.BoxPlot:
                        AppendBoxPlotSeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, yMin, yMax, clipId, tooltipLayer);
                        break;
                    case ChartType.ErrorBar:
                        AppendErrorBarSeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, yMin, yMax, clipId, tooltipLayer);
                        break;
                    case ChartType.Candlestick:
                        AppendCandlestickSeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, yMin, yMax, clipId, tooltipLayer);
                        break;
                    case ChartType.Ohlc:
                        AppendOhlcSeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, yMin, yMax, clipId, tooltipLayer);
                        break;
                    case ChartType.Dumbbell:
                        AppendDumbbellSeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, yMin, yMax, clipId, tooltipLayer);
                        break;
                    case ChartType.Stream:
                        AppendStreamSeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, clipId, tooltipLayer);
                        break;
                    case ChartType.Gantt:
                        AppendGanttSeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, clipId, tooltipLayer);
                        break;
                    case ChartType.Sankey:
                        AppendSankeySeries(sb, series, color, options, svgWidth, svgHeight, plotWidth, plotHeight, tooltipLayer);
                        break;
                }

                // Per-series target lines (horizontal rules at a specific Y value, scoped to this series).
                if (series.TargetLines.Count > 0)
                    AppendSeriesTargetLines(sb, series, color, options, plotWidth, plotHeight, yMin, yMax, clipId);

                if (options.RenderMode == SvgMode.Interactive)
                    sb.AppendLine("  </g>");
            }
        }

        private static void AppendSeriesTargetLines(StringBuilder sb, Series series, string seriesColor,
            ChartOptions options, int plotWidth, int plotHeight, double yMin, double yMax, string clipId)
        {
            // Target lines are only meaningful on cartesian charts with a vertical Y axis.
            // Skip non-cartesian types (Pie, Gauge, DataRing, Radar, Parliament, Heatmap, Funnel, Treemap)
            // and horizontal-bar types where the Y mapping is not vertical.
            switch (series.Type)
            {
                case ChartType.Pie:
                case ChartType.Gauge:
                case ChartType.DataRing:
                case ChartType.Radar:
                case ChartType.Parliament:
                case ChartType.Heatmap:
                case ChartType.Funnel:
                case ChartType.Treemap:
                case ChartType.Bar:
                    return;
            }

            bool yLog = IsLog(series.YAxisIndex == 1 ? options.YAxis2 : options.YAxis);
            foreach (var tl in series.TargetLines)
            {
                double lineY = PaddingTop + plotHeight - Frac(tl.Value, yMin, yMax, yLog) * plotHeight;
                if (lineY < PaddingTop - 1 || lineY > PaddingTop + plotHeight + 1) continue;

                string color = tl.Color ?? seriesColor;
                string dash  = BuildDashAttr(tl.DashStyle);
                sb.AppendLine($"  <line clip-path=\"url(#{clipId})\" x1=\"{PaddingLeft}\" y1=\"{F(lineY)}\" x2=\"{PaddingLeft + plotWidth}\" y2=\"{F(lineY)}\" stroke=\"{Escape(color)}\" stroke-width=\"{tl.LineWidth}\"{dash}/>");
                if (!string.IsNullOrEmpty(tl.Label))
                    sb.AppendLine($"  <text class=\"axis-label\" x=\"{PaddingLeft + plotWidth - 4}\" y=\"{F(lineY - 3)}\" text-anchor=\"end\" fill=\"{Escape(color)}\">{Escape(tl.Label)}</text>");
            }
        }

    }
}
