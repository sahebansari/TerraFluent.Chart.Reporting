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
        // Chart types that draw no cartesian axes; their category labels are slice/segment names,
        // not X-axis ticks, so no bottom space should be reserved for rotated axis labels.
        private static readonly ChartType[] AxisFreeChartTypes =
        {
            ChartType.Pie, ChartType.Gauge, ChartType.DataRing,
            ChartType.Funnel, ChartType.Treemap, ChartType.Heatmap,
            ChartType.Parliament, ChartType.Radar,
            ChartType.Stream, ChartType.Sankey
        };

        /// <summary>True when every visible series is an axis-free type (pie, gauge, donut, …).</summary>
        private static bool AllVisibleSeriesAxisFree(ChartOptions options) =>
            !options.Series.Exists(s => s.Visible && System.Array.IndexOf(AxisFreeChartTypes, s.Type) < 0);

        private static void AppendAxes(StringBuilder sb, ChartOptions options,
            int svgWidth, int svgHeight, int plotWidth, int plotHeight, int xAxisTitleY = -1,
            System.Globalization.CultureInfo? displayCulture = null, string? svgId = null)
        {
            // Horizontal bar charts and Gantt charts handle their own axis rendering
            if (options.Series.Exists(s => (s.Type == ChartType.Bar || s.Type == ChartType.Gantt) && s.Visible)) return;

            // Chart types that have no axes — skip entirely when ALL visible series are axis-free
            if (AllVisibleSeriesAxisFree(options)) return;

            // ---- Y axis -------------------------------------------------------
            if (options.YAxis.Visible)
            {
                bool yLog = IsLog(options.YAxis);
                var (yMinRaw, yMaxRaw) = ResolveYBounds(options.YAxis, options.Series);
                // Swap pixel-mapping bounds when inverted; tick generation always uses raw (ascending) range.
                bool yInv  = options.YAxis.Inverted;
                double yMinP = yInv ? yMaxRaw : yMinRaw;
                double yMaxP = yInv ? yMinRaw : yMaxRaw;
                // Auto-apply "%" suffix on the Y-axis when using Percent stacking and no explicit format.
                string? yTickFmt = options.YAxis.LabelFormat
                    ?? (options.Stacking == Stacking.Percent ? "{value}%" : null);

                // Build the ordered list of tick values (log decades or linear nice-steps).
                var yTicks = new List<double>();
                if (yLog)
                {
                    yTicks = GenerateLogTicks(yMinRaw, yMaxRaw);
                }
                else
                {
                    double tickStep = options.YAxis.TickInterval.HasValue && options.YAxis.TickInterval.Value > 0
                        ? options.YAxis.TickInterval.Value
                        : NiceStep((yMaxRaw - yMinRaw) / 5);
                    double firstTick = Math.Floor(yMinRaw / tickStep) * tickStep;
                    for (double tick = firstTick; tick <= yMaxRaw + tickStep * 0.5; tick += tickStep)
                        yTicks.Add(tick);
                }

                foreach (double tick in yTicks)
                {
                    double y = PaddingTop + plotHeight - Frac(tick, yMinP, yMaxP, yLog) * plotHeight;
                    if (y < PaddingTop - 1 || y > PaddingTop + plotHeight + 1) continue;

                    if (options.YAxis.GridLineVisible)
                    {
                        string gc = options.YAxis.GridLineColor ?? options.Theme.GridLineColor;
                        string yp = F(y);
                        string gridOp = options.Theme.ModernStyle ? " stroke-opacity=\"0.45\"" : string.Empty;
                        sb.AppendLine($"  <line aria-hidden=\"true\" class=\"grid-line\" stroke=\"{Escape(gc)}\"{gridOp} x1=\"{PaddingLeft}\" y1=\"{yp}\" x2=\"{PaddingLeft + plotWidth}\" y2=\"{yp}\"/>");
                    }

                    sb.AppendLine($"  <text class=\"axis-label\" x=\"{PaddingLeft - 6}\" y=\"{F(y + 4)}\" text-anchor=\"end\" fill=\"{Escape(options.Theme.TextColor)}\">{FormatAxisTick(tick, yTickFmt, displayCulture)}</text>");
                }

                sb.AppendLine($"  <line class=\"axis-line\" x1=\"{PaddingLeft}\" y1=\"{PaddingTop}\" x2=\"{PaddingLeft}\" y2=\"{PaddingTop + plotHeight}\"/>");

                if (!string.IsNullOrEmpty(options.YAxis.Title))
                    sb.AppendLine($"  <text class=\"axis-title\" transform=\"rotate(-90)\" x=\"{-(PaddingTop + plotHeight / 2)}\" y=\"{CanvasPadding}\" text-anchor=\"middle\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(options.YAxis.Title)}</text>");
            }

            // ---- X axis -------------------------------------------------------
            if (options.XAxis.Visible)
            {
                var cats = options.XAxis.Categories;
                if (cats != null && cats.Count > 0)
                {
                    double step = (double)plotWidth / cats.Count;

                    // Resolve label layout via collision-detection algorithm
                    var ll = ComputeLabelLayout(options, cats, step);
                    int rotation    = ll.Rotation;
                    int labelStride = ll.Stride;
                    // Date/time axes: preserve legacy thinning when LabelLayout is not configured
                    bool xIsDate = options.XAxis.Type == Enums.AxisType.DateTime;
                    if (options.LabelLayout == null && xIsDate && cats.Count > 12)
                        labelStride = Math.Max(labelStride, (int)Math.Ceiling(cats.Count / 12.0));
                    string fontSizeAttr = (ll.FontSize != 11) ? $" font-size=\"{ll.FontSize}\"" : string.Empty;

                    bool rsXGroup = svgId != null
                        && options.RangeSelector.Enabled
                        && options.RenderMode == SvgMode.Interactive;
                    if (rsXGroup)
                        sb.AppendLine($"  <g id=\"{svgId}-rs-xg\">");

                    string? lastShownLabel = null;
                    for (int i = 0; i < cats.Count; i++)
                    {
                        double x     = PaddingLeft + step * i + step / 2.0;
                        double tickY = PaddingTop + plotHeight;
                        // Stagger: every other label drops by StaggerOffset pixels
                        double staggerY = (ll.Stagger && i % 2 == 1) ? ll.StaggerOffset : 0.0;

                        bool showLabel = (i % labelStride == 0) || i == cats.Count - 1;
                        if (!showLabel) continue;

                        // For date/time axes, suppress a repeated label (e.g. two "Dec 2024"
                        // ticks produced when the forced last tick lands in the same month as
                        // the previous strided tick).
                        if (xIsDate && cats[i] == lastShownLabel) continue;
                        lastShownLabel = cats[i];

                        if (options.XAxis.GridLineVisible && !options.Theme.ModernStyle)
                        {
                            string gc = options.XAxis.GridLineColor ?? options.Theme.GridLineColor;
                            string xp = F(x);
                            sb.AppendLine($"  <line class=\"grid-line\" stroke=\"{Escape(gc)}\" x1=\"{xp}\" y1=\"{PaddingTop}\" x2=\"{xp}\" y2=\"{F(tickY)}\"/>");
                        }

                        if (ll.WordWrap)
                        {
                            var lines = WrapLabel(cats[i], ll.MaxCharsPerLine);
                            double lineH   = ll.FontSize + 2;
                            double baseY   = tickY + 14 + staggerY;
                            string anchor  = rotation != 0 ? "end" : "middle";
                            string rotAttr = rotation != 0
                                ? $" transform=\"rotate({rotation}, {F(x)}, {F(baseY)})\""
                                : string.Empty;
                            sb.Append($"  <text class=\"axis-label\"{fontSizeAttr}{rotAttr} x=\"{F(x)}\" y=\"{F(baseY)}\" text-anchor=\"{anchor}\" fill=\"{Escape(options.Theme.TextColor)}\">");
                            for (int li = 0; li < lines.Count; li++)
                                sb.Append($"<tspan x=\"{F(x)}\" dy=\"{F(li == 0 ? 0 : lineH)}\">{Escape(lines[li])}</tspan>");
                            sb.AppendLine("</text>");
                        }
                        else if (rotation != 0)
                        {
                            // Rotated label: anchor at tick, rotate around that point
                            sb.AppendLine($"  <text class=\"axis-label\"{fontSizeAttr} transform=\"rotate({rotation}, {F(x)}, {F(tickY + 14 + staggerY)})\" x=\"{F(x)}\" y=\"{F(tickY + 14 + staggerY)}\" text-anchor=\"end\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(cats[i])}</text>");
                        }
                        else
                        {
                            sb.AppendLine($"  <text class=\"axis-label\"{fontSizeAttr} x=\"{F(x)}\" y=\"{F(tickY + 16 + staggerY)}\" text-anchor=\"middle\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(cats[i])}</text>");
                        }
                    }

                    if (rsXGroup)
                        sb.AppendLine("  </g>");
                }
                else if (options.XAxis.TickInterval.HasValue && options.XAxis.TickInterval.Value > 0)
                {
                    double xTickStep = options.XAxis.TickInterval.Value;
                    // Numeric X-axis ticks driven by XAxisTickInterval
                    double xMin = options.XAxis.Min ?? 0;
                    double xMax = options.XAxis.Max ?? xTickStep * 10;
                    if (Math.Abs(xMax - xMin) < double.Epsilon) xMax = xMin + xTickStep;

                    double firstXTick = Math.Floor(xMin / xTickStep) * xTickStep;
                    double tickY = PaddingTop + plotHeight;

                    for (double tick = firstXTick; tick <= xMax + xTickStep * 0.5; tick += xTickStep)
                    {
                        double x = PaddingLeft + (tick - xMin) / (xMax - xMin) * plotWidth;
                        if (x < PaddingLeft - 1 || x > PaddingLeft + plotWidth + 1) continue;

                        if (options.XAxis.GridLineVisible && !options.Theme.ModernStyle)
                        {
                            string gc = options.XAxis.GridLineColor ?? options.Theme.GridLineColor;
                            sb.AppendLine($"  <line aria-hidden=\"true\" class=\"grid-line\" stroke=\"{Escape(gc)}\" x1=\"{F(x)}\" y1=\"{PaddingTop}\" x2=\"{F(x)}\" y2=\"{F(tickY)}\"/>");
                        }

                        sb.AppendLine($"  <text class=\"axis-label\" x=\"{F(x)}\" y=\"{F(tickY + 16)}\" text-anchor=\"middle\" fill=\"{Escape(options.Theme.TextColor)}\">{FormatAxisTick(tick, options.XAxis.LabelFormat, displayCulture)}</text>");
                    }
                }

                sb.AppendLine($"  <line class=\"axis-line\" x1=\"{PaddingLeft}\" y1=\"{PaddingTop + plotHeight}\" x2=\"{PaddingLeft + plotWidth}\" y2=\"{PaddingTop + plotHeight}\"/>");

                if (!string.IsNullOrEmpty(options.XAxis.Title))
                {
                    int titleY = xAxisTitleY >= 0 ? xAxisTitleY : svgHeight - CanvasPadding;
                    sb.AppendLine($"  <text class=\"axis-title\" x=\"{PaddingLeft + plotWidth / 2}\" y=\"{titleY}\" text-anchor=\"middle\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(options.XAxis.Title)}</text>");
                }
            }

            // ---- Plot bands (filled rectangles behind data) -------------------
            if (options.YAxis.Visible && options.YAxis.PlotBands.Count > 0)
            {
                bool yLogPB = IsLog(options.YAxis);
                var (yMinPBRaw, yMaxPBRaw) = ResolveYBounds(options.YAxis, options.Series);
                bool yInvPB = options.YAxis.Inverted;
                double yMinPB = yInvPB ? yMaxPBRaw : yMinPBRaw;
                double yMaxPB = yInvPB ? yMinPBRaw : yMaxPBRaw;

                foreach (var band in options.YAxis.PlotBands)
                {
                    double bandTop = PaddingTop + plotHeight - Frac(band.To,   yMinPB, yMaxPB, yLogPB) * plotHeight;
                    double bandBot = PaddingTop + plotHeight - Frac(band.From, yMinPB, yMaxPB, yLogPB) * plotHeight;
                    bandTop = Math.Max(PaddingTop, bandTop);
                    bandBot = Math.Min(PaddingTop + plotHeight, bandBot);
                    double bandH = bandBot - bandTop;
                    if (bandH <= 0) continue;

                    sb.AppendLine($"  <rect x=\"{PaddingLeft}\" y=\"{F(bandTop)}\" width=\"{plotWidth}\" height=\"{F(bandH)}\" fill=\"{Escape(band.Color)}\"/>");
                    if (!string.IsNullOrEmpty(band.Label))
                        sb.AppendLine($"  <text class=\"axis-label\" x=\"{PaddingLeft + plotWidth - 4}\" y=\"{F(bandTop + 12)}\" text-anchor=\"end\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(band.Label)}</text>");
                }
            }

            // ---- Plot lines (reference lines over data) -----------------------
            if (options.YAxis.Visible && options.YAxis.PlotLines.Count > 0)
            {
                bool yLogPL = IsLog(options.YAxis);
                var (yMinPLRaw, yMaxPLRaw) = ResolveYBounds(options.YAxis, options.Series);
                bool yInvPL = options.YAxis.Inverted;
                double yMinPL = yInvPL ? yMaxPLRaw : yMinPLRaw;
                double yMaxPL = yInvPL ? yMinPLRaw : yMaxPLRaw;

                foreach (var pl in options.YAxis.PlotLines)
                {
                    double lineY = PaddingTop + plotHeight - Frac(pl.Value, yMinPL, yMaxPL, yLogPL) * plotHeight;
                    if (lineY < PaddingTop - 1 || lineY > PaddingTop + plotHeight + 1) continue;

                    string dash = BuildDashAttr(pl.DashStyle);
                    sb.AppendLine($"  <line x1=\"{PaddingLeft}\" y1=\"{F(lineY)}\" x2=\"{PaddingLeft + plotWidth}\" y2=\"{F(lineY)}\" stroke=\"{Escape(pl.Color)}\" stroke-width=\"{pl.Width}\"{dash}/>");
                    if (!string.IsNullOrEmpty(pl.Label))
                        sb.AppendLine($"  <text class=\"axis-label\" x=\"{PaddingLeft + plotWidth - 4}\" y=\"{F(lineY - 3)}\" text-anchor=\"end\" fill=\"{Escape(pl.Color)}\">{Escape(pl.Label)}</text>");
                }
            }

            // ---- X-axis plot bands (vertical shaded bands at category or numeric X positions) --
            if (options.XAxis.Visible && options.XAxis.PlotBands.Count > 0)
            {
                var cats = options.XAxis.Categories;
                bool hasCatBands = cats != null && cats.Count > 0;
                double xRange = Math.Abs(options.XAxis.Max.GetValueOrDefault(1) - options.XAxis.Min.GetValueOrDefault(0));
                if (xRange < double.Epsilon) xRange = 1;

                foreach (var band in options.XAxis.PlotBands)
                {
                    double bx1, bx2;
                    if (hasCatBands)
                    {
                        // Category-index based: From/To are category indices (0-based)
                        double step = (double)plotWidth / cats!.Count;
                        bx1 = PaddingLeft + band.From * step;
                        bx2 = PaddingLeft + band.To   * step;
                    }
                    else
                    {
                        double xMin = options.XAxis.Min ?? 0;
                        double xMax = options.XAxis.Max ?? xRange;
                        bx1 = PaddingLeft + (band.From - xMin) / (xMax - xMin) * plotWidth;
                        bx2 = PaddingLeft + (band.To   - xMin) / (xMax - xMin) * plotWidth;
                    }
                    bx1 = Math.Max(PaddingLeft, Math.Min(bx1, PaddingLeft + plotWidth));
                    bx2 = Math.Max(PaddingLeft, Math.Min(bx2, PaddingLeft + plotWidth));
                    if (bx2 <= bx1) continue;

                    sb.AppendLine($"  <rect x=\"{F(bx1)}\" y=\"{PaddingTop}\" width=\"{F(bx2 - bx1)}\" height=\"{plotHeight}\" fill=\"{Escape(band.Color)}\"/>");
                    if (!string.IsNullOrEmpty(band.Label))
                        sb.AppendLine($"  <text class=\"axis-label\" x=\"{F(bx1 + 4)}\" y=\"{PaddingTop + 12}\" text-anchor=\"start\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(band.Label)}</text>");
                }
            }

            // ---- X-axis plot lines (vertical reference lines at category or numeric X positions) --
            if (options.XAxis.Visible && options.XAxis.PlotLines.Count > 0)
            {
                var cats = options.XAxis.Categories;
                bool hasCatLines = cats != null && cats.Count > 0;

                foreach (var pl in options.XAxis.PlotLines)
                {
                    double lx;
                    if (hasCatLines)
                    {
                        double step = (double)plotWidth / cats!.Count;
                        lx = PaddingLeft + pl.Value * step;
                    }
                    else
                    {
                        double xMin = options.XAxis.Min ?? 0;
                        double xMax = options.XAxis.Max ?? 1;
                        if (Math.Abs(xMax - xMin) < double.Epsilon) xMax = xMin + 1;
                        lx = PaddingLeft + (pl.Value - xMin) / (xMax - xMin) * plotWidth;
                    }
                    if (lx < PaddingLeft - 1 || lx > PaddingLeft + plotWidth + 1) continue;

                    string dash = BuildDashAttr(pl.DashStyle);
                    sb.AppendLine($"  <line x1=\"{F(lx)}\" y1=\"{PaddingTop}\" x2=\"{F(lx)}\" y2=\"{PaddingTop + plotHeight}\" stroke=\"{Escape(pl.Color)}\" stroke-width=\"{pl.Width}\"{dash}/>");
                    if (!string.IsNullOrEmpty(pl.Label))
                        sb.AppendLine($"  <text class=\"axis-label\" x=\"{F(lx + 4)}\" y=\"{PaddingTop + 12}\" text-anchor=\"start\" fill=\"{Escape(pl.Color)}\">{Escape(pl.Label)}</text>");
                }
            }

            // ---- Secondary (right) Y-axis -------------------------------------
            if (options.YAxis2 != null && options.Series.Exists(s => s.Visible && s.YAxisIndex == 1))
            {
                var sec = options.YAxis2;
                var secSeries = options.Series.FindAll(s => s.Visible && s.YAxisIndex == 1);
                bool y2Log = IsLog(sec);
                var (y2MinRaw, y2MaxRaw) = ResolveYBounds(sec, secSeries);
                bool y2Inv = sec.Inverted;
                double y2Min = y2Inv ? y2MaxRaw : y2MinRaw;
                double y2Max = y2Inv ? y2MinRaw : y2MaxRaw;

                var secTicks = y2Log
                    ? GenerateLogTicks(y2MinRaw, y2MaxRaw)
                    : null;
                double secStep = NiceStep((y2MaxRaw - y2MinRaw) / 5);
                double secFirst = Math.Floor(y2MinRaw / secStep) * secStep;

                // PlotBands on YAxis2
                foreach (var band in sec.PlotBands)
                {
                    double bt = PaddingTop + plotHeight - Frac(band.To,   y2Min, y2Max, y2Log) * plotHeight;
                    double bb = PaddingTop + plotHeight - Frac(band.From, y2Min, y2Max, y2Log) * plotHeight;
                    bt = Math.Max(PaddingTop, bt); bb = Math.Min(PaddingTop + plotHeight, bb);
                    if (bb > bt) sb.AppendLine($"  <rect x=\"{PaddingLeft}\" y=\"{F(bt)}\" width=\"{plotWidth}\" height=\"{F(bb - bt)}\" fill=\"{Escape(band.Color)}\"/>");
                }
                // PlotLines on YAxis2
                foreach (var pl in sec.PlotLines)
                {
                    double ly = PaddingTop + plotHeight - Frac(pl.Value, y2Min, y2Max, y2Log) * plotHeight;
                    if (ly < PaddingTop - 1 || ly > PaddingTop + plotHeight + 1) continue;
                    sb.AppendLine($"  <line x1=\"{PaddingLeft}\" y1=\"{F(ly)}\" x2=\"{PaddingLeft + plotWidth}\" y2=\"{F(ly)}\" stroke=\"{Escape(pl.Color)}\" stroke-width=\"{pl.Width}\"{BuildDashAttr(pl.DashStyle)}/>");
                }

                var secTickValues = secTicks ?? EnumerateLinearTicks(secFirst, y2MaxRaw, secStep);
                foreach (double tick in secTickValues)
                {
                    double ty = PaddingTop + plotHeight - Frac(tick, y2Min, y2Max, y2Log) * plotHeight;
                    if (ty < PaddingTop - 1 || ty > PaddingTop + plotHeight + 1) continue;
                    sb.AppendLine($"  <text class=\"axis-label\" x=\"{PaddingLeft + plotWidth + 8}\" y=\"{F(ty + 4)}\" text-anchor=\"start\" fill=\"{Escape(options.Theme.TextColor)}\">{FormatAxisTick(tick, sec.LabelFormat, displayCulture)}</text>");
                }

                // Right axis border line
                sb.AppendLine($"  <line class=\"axis-line\" x1=\"{PaddingLeft + plotWidth}\" y1=\"{PaddingTop}\" x2=\"{PaddingLeft + plotWidth}\" y2=\"{PaddingTop + plotHeight}\"/>");

                if (!string.IsNullOrEmpty(sec.Title))
                {
                    // After rotate(90): screen_x = -ty, screen_y = tx.
                    // Place at: screen_x = svgWidth - CanvasPadding (kept off the right edge), screen_y = vertical centre.
                    double secTitleX = PaddingTop + plotHeight / 2.0;
                    double secTitleY = -(svgWidth - CanvasPadding);
                    sb.AppendLine($"  <text class=\"axis-title\" transform=\"rotate(90)\" x=\"{F(secTitleX)}\" y=\"{F(secTitleY)}\" text-anchor=\"middle\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(sec.Title)}</text>");
                }
            }
        }

        // ------------------------------------------------------------------ series

    }
}
