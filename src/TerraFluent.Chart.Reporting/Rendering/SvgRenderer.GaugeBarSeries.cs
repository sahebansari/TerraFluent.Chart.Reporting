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
        private static void AppendGaugeSeries(StringBuilder sb, Series series,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            StringBuilder tooltipLayer)
        {
            if (series.Data.Count == 0 || !series.Data[0].HasValue) return;

            double value    = series.Data[0]!.Value;
            var gaugeAxis   = series.YAxisIndex == 1 && options.YAxis2 != null ? options.YAxis2 : options.YAxis;
            double gMin     = gaugeAxis.Min ?? 0;
            double gMax     = gaugeAxis.Max ?? 100;
            if (Math.Abs(gMax - gMin) < double.Epsilon) gMax = gMin + 1;

            double cx     = PaddingLeft + plotWidth / 2.0;
            double cy     = PaddingTop  + plotHeight * 0.75;
            double radius = Math.Min(plotWidth, plotHeight) * 0.42;
            double inner  = radius * 0.60;

            double midR  = (radius + inner) / 2.0;   // stroke centre radius
            double ringW = radius - inner;            // stroke width = ring thickness

            double frac = Math.Max(0, Math.Min(1, (value - gMin) / (gMax - gMin)));

            // Semi-circle track path: left (180°) → right (0°) clockwise through the top
            string trackPath = $"M{F(cx - midR)},{F(cy)} A{F(midR)},{F(midR)} 0 1,1 {F(cx + midR)},{F(cy)}";

            // ── Background track ─────────────────────────────────────────────────────────
            string trackColor = options.Theme.GridLineColor;
            sb.AppendLine($"  <path d=\"{trackPath}\" fill=\"none\" stroke=\"{Escape(trackColor)}\"");
            sb.AppendLine($"        stroke-width=\"{F(ringW)}\" stroke-linecap=\"butt\"/>");

            // ── Value arc ────────────────────────────────────────────────────────────────
            string c = series.Color ?? options.Theme.Colors[0];

            if (frac > 0.001)
            {
                bool animated = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
                string pdur   = F(options.Animation.Duration.TotalSeconds) + "s";
                string pease  = SmilEasing(options.Animation.Easing);

                if (animated)
                {
                    // Sweep-in animation: stroke-dashoffset from frac (hidden) → 0 (fully drawn).
                    // With pathLength="1" and stroke-dasharray="frac 1":
                    //   dashoffset=frac → gap covers entire visible path (arc hidden)
                    //   dashoffset=0    → dash covers first frac of path  (arc fully visible)
                    sb.AppendLine($"  <path d=\"{trackPath}\" fill=\"none\" stroke=\"{Escape(c)}\"");
                    sb.AppendLine($"        stroke-width=\"{F(ringW)}\" stroke-linecap=\"butt\"");
                    sb.AppendLine($"        pathLength=\"1\" stroke-dasharray=\"{F(frac)} 2\" stroke-dashoffset=\"{F(frac)}\">");
                    sb.AppendLine($"    <animate attributeName=\"stroke-dashoffset\" from=\"{F(frac)}\" to=\"0\"");
                    sb.AppendLine($"             dur=\"{pdur}\" fill=\"freeze\"{pease}/>");
                    sb.AppendLine($"  </path>");
                }
                else
                {
                    sb.AppendLine($"  <path d=\"{trackPath}\" fill=\"none\" stroke=\"{Escape(c)}\"");
                    sb.AppendLine($"        stroke-width=\"{F(ringW)}\" stroke-linecap=\"butt\"");
                    sb.AppendLine($"        pathLength=\"1\" stroke-dasharray=\"{F(frac)} 2\"/>");
                }
            }

            // ── Off-scale indicator ──────────────────────────────────────────────────────
            // frac is clamped to [0,1]; when the true value is out of range, mark the
            // exceeded end so the arc is not misread as sitting exactly at the limit.
            double gOff = Math.Max(7.0, ringW * 0.55);
            if (value > gMax)
                AppendOffScaleMarker(sb, cx + midR + 4 + gOff, cy, 1, 0, gOff, Escape(c),
                    Escape($"Above maximum ({FormatTick(gMax)})"));
            else if (value < gMin)
                AppendOffScaleMarker(sb, cx - midR - 4 - gOff, cy, -1, 0, gOff, Escape(c),
                    Escape($"Below minimum ({FormatTick(gMin)})"));

            // ── Center labels ────────────────────────────────────────────────────────────
            string label = FormatTick(value);
            sb.AppendLine($"  <text x=\"{F(cx)}\" y=\"{F(cy + 8)}\" text-anchor=\"middle\" " +
                $"font-size=\"22\" font-weight=\"bold\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(label)}</text>");

            if (!string.IsNullOrEmpty(series.Name))
                sb.AppendLine($"  <text x=\"{F(cx)}\" y=\"{F(cy + 28)}\" text-anchor=\"middle\" " +
                    $"class=\"axis-label\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(series.Name)}</text>");

            sb.AppendLine($"  <text x=\"{F(cx - midR - 6)}\" y=\"{F(cy + 14)}\" text-anchor=\"end\" " +
                $"class=\"axis-label\" fill=\"{Escape(options.Theme.TextColor)}\">{FormatTick(gMin)}</text>");
            sb.AppendLine($"  <text x=\"{F(cx + midR + 6)}\" y=\"{F(cy + 14)}\" text-anchor=\"start\" " +
                $"class=\"axis-label\" fill=\"{Escape(options.Theme.TextColor)}\">{FormatTick(gMax)}</text>");

            // ── Hover tooltip ────────────────────────────────────────────────────────────
            // Use a transparent rectangle covering the arc area as the hit target.
            // No .hit-area class to avoid the circle r-override CSS rule.
            if (options.RenderMode != SvgMode.Static)
            {
                double hitX = cx - radius;
                double hitY = cy - radius;
                tooltipLayer.AppendLine($"  <g class=\"data-point\">");
                tooltipLayer.AppendLine($"    <rect x=\"{F(hitX)}\" y=\"{F(hitY)}\" width=\"{F(radius * 2)}\" height=\"{F(radius)}\" fill=\"transparent\" stroke=\"none\"/>");
                // Violet highlight arc — same shape as value arc, slightly wider, fades in on hover
                if (frac > 0.001)
                    tooltipLayer.AppendLine($"    <path d=\"{trackPath}\" fill=\"none\" stroke=\"{ApplyAlpha(options.Theme.AccentColor, 0.55)}\"" +
                        $" stroke-width=\"{F(ringW + 5)}\" stroke-linecap=\"butt\"" +
                        $" pathLength=\"1\" stroke-dasharray=\"{F(frac)} 2\" class=\"gauge-highlight\"/>");
                AppendTooltip(tooltipLayer, cx, cy - radius, series.Name, value, svgWidth, svgHeight, options.Tooltip, c);
                tooltipLayer.AppendLine($"  </g>");
            }
        }

        private static void AppendDataRingSeries(StringBuilder sb, Series series,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            StringBuilder tooltipLayer)
        {
            if (series.Data.Count == 0 || !series.Data[0].HasValue) return;

            double value  = series.Data[0]!.Value;
            var ringAxis  = series.YAxisIndex == 1 && options.YAxis2 != null ? options.YAxis2 : options.YAxis;
            double gMin   = ringAxis.Min ?? 0;
            double gMax   = ringAxis.Max ?? 100;
            if (Math.Abs(gMax - gMin) < double.Epsilon) gMax = gMin + 1;

            double frac = Math.Max(0, Math.Min(1, (value - gMin) / (gMax - gMin)));

            double cx = PaddingLeft + plotWidth  / 2.0;
            double cy = PaddingTop  + plotHeight / 2.0;

            // Ring geometry — wider hole than gauge to leave room for centre label
            double radius = Math.Min(plotWidth, plotHeight) / 2.0 * 0.8;
            double inner  = radius * 0.70;          // inner radius (= hole)
            double midR   = (radius + inner) / 2.0; // stroke centre radius
            double ringW  = radius - inner;          // stroke width
            double circ   = 2.0 * Math.PI * midR;   // actual circumference

            bool   animated = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string pdur     = F(options.Animation.Duration.TotalSeconds) + "s";
            string pease    = SmilEasing(options.Animation.Easing);

            string c          = series.Color ?? options.Theme.Colors[0];
            string trackColor = options.Theme.GridLineColor;

            // ── Background track (full 360° ring) ──────────────────────────────────────────
            sb.AppendLine($"  <circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(midR)}\" fill=\"none\"");
            sb.AppendLine($"          stroke=\"{Escape(trackColor)}\" stroke-width=\"{F(ringW)}\"/>");

            // ── Value arc ────────────────────────────────────────────────────────────────────
            // stroke-dasharray="C C", dashoffset=C*(1-frac): draws frac of the ring from 12 o’clock.
            // rotate(-90): shifts SVG circle start from 3 o’clock to 12 o’clock.
            if (frac > 0.001)
            {
                double targetOffset = circ * (1.0 - frac);
                if (animated)
                {
                    sb.AppendLine($"  <circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(midR)}\" fill=\"none\"");
                    sb.AppendLine($"          stroke=\"{Escape(c)}\" stroke-width=\"{F(ringW)}\"");
                    sb.AppendLine($"          stroke-dasharray=\"{F(circ)} {F(circ)}\" stroke-dashoffset=\"{F(circ)}\"");
                    sb.AppendLine($"          transform=\"rotate(-90 {F(cx)} {F(cy)})\">");
                    sb.AppendLine($"    <animate attributeName=\"stroke-dashoffset\" from=\"{F(circ)}\" to=\"{F(targetOffset)}\"");
                    sb.AppendLine($"             dur=\"{pdur}\" fill=\"freeze\"{pease}/>");
                    sb.AppendLine($"  </circle>");
                }
                else
                {
                    sb.AppendLine($"  <circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(midR)}\" fill=\"none\"");
                    sb.AppendLine($"          stroke=\"{Escape(c)}\" stroke-width=\"{F(ringW)}\"");
                    sb.AppendLine($"          stroke-dasharray=\"{F(circ)} {F(circ)}\" stroke-dashoffset=\"{F(targetOffset)}\"");
                    sb.AppendLine($"          transform=\"rotate(-90 {F(cx)} {F(cy)})\"/>");
                }
            }

            // ── Off-scale indicator ──────────────────────────────────────────────────────────
            // frac is clamped to [0,1]; mark above the ring (at 12 o'clock) when the true
            // value overflows the max (points clockwise) or underflows the min (points back).
            double rOff  = Math.Max(7.0, ringW * 0.55);
            double rTopY = cy - radius - 4;
            if (value > gMax)
                AppendOffScaleMarker(sb, cx + rOff, rTopY, 1, 0, rOff, Escape(c),
                    Escape($"Above maximum ({FormatTick(gMax)})"));
            else if (value < gMin)
                AppendOffScaleMarker(sb, cx - rOff, rTopY, -1, 0, rOff, Escape(c),
                    Escape($"Below minimum ({FormatTick(gMin)})"));

            // ── Centre label ───────────────────────────────────────────────────────────────
            var    dc           = series.DonutCenter;
            string valueText   = !string.IsNullOrEmpty(dc.CustomText) ? dc.CustomText
                                  : (Math.Abs(gMax - 100) < double.Epsilon && Math.Abs(gMin) < double.Epsilon)
                                    ? $"{FormatTick(value)}%"   // 0–100 scale → show %
                                    : FormatTick(value);
            string centreTitle = dc.CenterTitle ?? series.Name;
            string valueColor  = Escape(dc.TextColor      ?? options.Theme.TextColor);
            string titleColor  = Escape(dc.TitleTextColor ?? options.Theme.TextColor);

            int valueFontSize = dc.ValueFontSize  > 0 ? dc.ValueFontSize  : Math.Max(14, (int)(inner * 0.38));
            int titleFontSize = dc.CaptionFontSize > 0 ? dc.CaptionFontSize : Math.Max(10, (int)(inner * 0.20));

            // Clamp so the combined text block never overflows the ring hole.
            // Max safe font = 65 % of inner radius (accounts for character width + vertical offset).
            // Title is held to 32 % so both lines fit vertically inside 2*inner.
            valueFontSize = Math.Min(valueFontSize, Math.Max(8,  (int)(inner * 0.65)));
            titleFontSize = Math.Min(titleFontSize, Math.Max(6,  (int)(inner * 0.32)));

            if (!string.IsNullOrEmpty(centreTitle))
            {
                double gap      = 4.0;
                double titleCy  = cy - (gap + valueFontSize)  / 2.0;
                double valueCy  = cy + (gap + titleFontSize)  / 2.0;
                sb.AppendLine($"  <text x=\"{F(cx)}\" y=\"{F(titleCy)}\" text-anchor=\"middle\" dominant-baseline=\"central\"");
                sb.AppendLine($"        font-size=\"{titleFontSize}\" fill=\"{titleColor}\" opacity=\"0.65\">{Escape(centreTitle)}</text>");
                sb.AppendLine($"  <text x=\"{F(cx)}\" y=\"{F(valueCy)}\" text-anchor=\"middle\" dominant-baseline=\"central\"");
                sb.AppendLine($"        font-size=\"{valueFontSize}\" font-weight=\"bold\" fill=\"{valueColor}\">{Escape(valueText)}</text>");
            }
            else
            {
                sb.AppendLine($"  <text x=\"{F(cx)}\" y=\"{F(cy)}\" text-anchor=\"middle\" dominant-baseline=\"central\"");
                sb.AppendLine($"        font-size=\"{valueFontSize}\" font-weight=\"bold\" fill=\"{valueColor}\">{Escape(valueText)}</text>");
            }

            // ── Hover: transparent hit circle + violet glow arc ───────────────────────────────
            if (options.RenderMode != SvgMode.Static)
            {
                tooltipLayer.AppendLine($"  <g class=\"data-point\">");
                tooltipLayer.AppendLine($"    <circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(radius)}\" fill=\"transparent\" stroke=\"none\"/>");
                if (frac > 0.001)
                {
                    double targetOffset = circ * (1.0 - frac);
                    tooltipLayer.AppendLine($"    <circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(midR)}\" fill=\"none\"");
                    tooltipLayer.AppendLine($"            stroke=\"{ApplyAlpha(options.Theme.AccentColor, 0.55)}\" stroke-width=\"{F(ringW + 5)}\"");
                    tooltipLayer.AppendLine($"            stroke-dasharray=\"{F(circ)} {F(circ)}\" stroke-dashoffset=\"{F(targetOffset)}\"");
                    tooltipLayer.AppendLine($"            transform=\"rotate(-90 {F(cx)} {F(cy)})\" class=\"gauge-highlight\"/>");
                }
                AppendTooltip(tooltipLayer, cx, cy - radius - 10, series.Name, value, svgWidth, svgHeight, options.Tooltip, c);
                tooltipLayer.AppendLine($"  </g>");
            }
        }

        /// <summary>Builds an SVG donut-arc path from <paramref name="fromDeg"/> to <paramref name="toDeg"/>.</summary>
        private static string ArcPath(double cx, double cy, double r, double ri,
            double fromDeg, double toDeg, bool largeArc)
        {
            int lg = largeArc ? 1 : 0;
            double sx  = cx + r  * Math.Cos(fromDeg * Math.PI / 180);
            double sy  = cy + r  * Math.Sin(fromDeg * Math.PI / 180);
            double ex  = cx + r  * Math.Cos(toDeg   * Math.PI / 180);
            double ey  = cy + r  * Math.Sin(toDeg   * Math.PI / 180);
            double six = cx + ri * Math.Cos(fromDeg * Math.PI / 180);
            double siy = cy + ri * Math.Sin(fromDeg * Math.PI / 180);
            double eix = cx + ri * Math.Cos(toDeg   * Math.PI / 180);
            double eiy = cy + ri * Math.Sin(toDeg   * Math.PI / 180);
            return $"M{F(sx)},{F(sy)} A{F(r)},{F(r)} 0 {lg},1 {F(ex)},{F(ey)} " +
                   $"L{F(eix)},{F(eiy)} A{F(ri)},{F(ri)} 0 {lg},0 {F(six)},{F(siy)} Z";
        }

        /// <summary>
        /// Draws a small triangle marking that a gauge/ring value lies outside the axis
        /// range (its arc is clamped to the limit). <paramref name="tipX"/>/<paramref name="tipY"/>
        /// is the pointed vertex and (<paramref name="dirX"/>, <paramref name="dirY"/>) the
        /// unit direction it points; the base is set back one <paramref name="size"/> behind it.
        /// </summary>
        private static void AppendOffScaleMarker(StringBuilder sb, double tipX, double tipY,
            double dirX, double dirY, double size, string fill, string title)
        {
            double px = -dirY, py = dirX;              // perpendicular (base) direction
            double bx = tipX - dirX * size;            // base centre
            double by = tipY - dirY * size;
            double half = size * 0.7;
            string d = $"M{F(tipX)},{F(tipY)} " +
                       $"L{F(bx + px * half)},{F(by + py * half)} " +
                       $"L{F(bx - px * half)},{F(by - py * half)} Z";
            sb.AppendLine($"  <path d=\"{d}\" fill=\"{fill}\"><title>{title}</title></path>");
        }

        // ------------------------------------------------------------------ Sprint 3 new series renderers (Sprint 2)

        private static void AppendBarSeries(StringBuilder sb, Series series, string color,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            double yMin, double yMax, string clipId, StringBuilder tooltipLayer, int xAxisTitleY = -1,
            string? fillPaint = null)
        {
            int n = series.Data.Count;
            if (n == 0) return;

            string barFill = fillPaint ?? Escape(color);
            string barOp   = fillPaint != null ? F(series.Fill?.Opacity ?? 1.0) : "0.85";

            // Count Bar series for side-by-side grouping
            int barCount = 0, barIdx = 0;
            foreach (var s in options.Series)
                if (s.Type == ChartType.Bar) { if (s == series) barIdx = barCount; barCount++; }

            bool hasCats  = options.XAxis.Categories?.Count > 0;
            int catCount  = hasCats ? options.XAxis.Categories!.Count : n;
            double groupH = (double)plotHeight / catCount;
            double barPad = groupH * 0.1;
            double barH   = (groupH - barPad * 2) / Math.Max(barCount, 1);

            double range = Math.Abs(yMax - yMin) < double.Epsilon ? 1 : yMax - yMin;
            // X pixel position where value == 0 (the origin line for the bars)
            double x0 = PaddingLeft + (0 - yMin) / range * plotWidth;
            x0 = Math.Max(PaddingLeft, Math.Min(x0, PaddingLeft + plotWidth));

            bool   bAnim  = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string bDur   = bAnim ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string bEase  = bAnim ? SmilEasing(options.Animation.Easing) : string.Empty;

            // Vertical value grid lines + bottom labels for bar value axis
            string barGc = options.YAxis.GridLineColor ?? options.Theme.GridLineColor;
            double barTickStep = NiceStep(range / 5);
            for (double tick = Math.Floor(yMin / barTickStep) * barTickStep;
                 tick <= yMax + barTickStep * 0.5; tick += barTickStep)
            {
                double xTick = PaddingLeft + (tick - yMin) / range * plotWidth;
                if (xTick < PaddingLeft - 1 || xTick > PaddingLeft + plotWidth + 1) continue;
                if (options.YAxis.GridLineVisible)
                    sb.AppendLine($"  <line class=\"grid-line\" stroke=\"{Escape(barGc)}\" x1=\"{F(xTick)}\" y1=\"{PaddingTop}\" x2=\"{F(xTick)}\" y2=\"{PaddingTop + plotHeight}\"/>");
                sb.AppendLine($"  <text class=\"axis-label\" x=\"{F(xTick)}\" y=\"{PaddingTop + plotHeight + 16}\" text-anchor=\"middle\" fill=\"{Escape(options.Theme.TextColor)}\">{FormatAxisTick(tick, options.YAxis.LabelFormat)}</text>");
            }

            // Axis border lines
            sb.AppendLine($"  <line class=\"axis-line\" x1=\"{PaddingLeft}\" y1=\"{PaddingTop}\" x2=\"{PaddingLeft}\" y2=\"{PaddingTop + plotHeight}\"/>");
            sb.AppendLine($"  <line class=\"axis-line\" x1=\"{PaddingLeft}\" y1=\"{PaddingTop + plotHeight}\" x2=\"{PaddingLeft + plotWidth}\" y2=\"{PaddingTop + plotHeight}\"/>");

            // Category labels on the left
            if (hasCats)
                for (int i = 0; i < catCount && i < options.XAxis.Categories!.Count; i++)
                {
                    double ly = PaddingTop + groupH * i + groupH / 2.0 + 4;
                    sb.AppendLine($"  <text class=\"axis-label\" x=\"{PaddingLeft - 8}\" y=\"{F(ly)}\" text-anchor=\"end\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(options.XAxis.Categories[i])}</text>");
                }

            // Y-axis title (value axis) — shown at the bottom centre like an X-axis title.
            // When a bottom legend is present, xAxisTitleY places it between the tick labels
            // and the legend so the two never overlap.
            if (!string.IsNullOrEmpty(options.YAxis.Title))
            {
                int barTitleY = xAxisTitleY >= 0 ? xAxisTitleY : svgHeight - CanvasPadding;
                sb.AppendLine($"  <text class=\"axis-title\" x=\"{PaddingLeft + plotWidth / 2}\" y=\"{barTitleY}\" text-anchor=\"middle\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(options.YAxis.Title)}</text>");
            }

            // X-axis title (category axis) — shown rotated on the far left like a Y-axis title
            if (!string.IsNullOrEmpty(options.XAxis.Title))
                sb.AppendLine($"  <text class=\"axis-title\" transform=\"rotate(-90)\" x=\"{-(PaddingTop + plotHeight / 2)}\" y=\"{CanvasPadding}\" text-anchor=\"middle\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(options.XAxis.Title)}</text>");

            // PlotBands on YAxis (vertical bands — value axis is horizontal for bar charts)
            foreach (var band in options.YAxis.PlotBands)
            {
                double bx1 = PaddingLeft + (band.From - yMin) / range * plotWidth;
                double bx2 = PaddingLeft + (band.To   - yMin) / range * plotWidth;
                bx1 = Math.Max(PaddingLeft, Math.Min(bx1, PaddingLeft + plotWidth));
                bx2 = Math.Max(PaddingLeft, Math.Min(bx2, PaddingLeft + plotWidth));
                if (bx2 > bx1)
                {
                    sb.AppendLine($"  <rect x=\"{F(bx1)}\" y=\"{PaddingTop}\" width=\"{F(bx2 - bx1)}\" height=\"{plotHeight}\" fill=\"{Escape(band.Color)}\"/>");
                    if (!string.IsNullOrEmpty(band.Label))
                        sb.AppendLine($"  <text class=\"axis-label\" x=\"{F(bx2 - 4)}\" y=\"{PaddingTop + 12}\" text-anchor=\"end\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(band.Label)}</text>");
                }
            }
            // PlotLines on YAxis (vertical reference lines for horizontal bar charts)
            foreach (var pl in options.YAxis.PlotLines)
            {
                double lx = PaddingLeft + (pl.Value - yMin) / range * plotWidth;
                if (lx < PaddingLeft - 1 || lx > PaddingLeft + plotWidth + 1) continue;
                sb.AppendLine($"  <line x1=\"{F(lx)}\" y1=\"{PaddingTop}\" x2=\"{F(lx)}\" y2=\"{PaddingTop + plotHeight}\" stroke=\"{Escape(pl.Color)}\" stroke-width=\"{pl.Width}\"{BuildDashAttr(pl.DashStyle)}/>");
                if (!string.IsNullOrEmpty(pl.Label))
                    sb.AppendLine($"  <text class=\"axis-label\" x=\"{F(lx + 4)}\" y=\"{PaddingTop + 12}\" text-anchor=\"start\" fill=\"{Escape(pl.Color)}\">{Escape(pl.Label)}</text>");
            }

            // Bars
            for (int i = 0; i < n; i++)
            {
                if (series.Data[i] is null) continue;
                double v    = series.Data[i]!.Value;
                double barW = Math.Abs((v - yMin) / range * plotWidth - (0 - yMin) / range * plotWidth);
                double barX = v >= 0 ? x0 : x0 - barW;
                double barY = PaddingTop + groupH * i + barPad + barH * barIdx;

                sb.AppendLine($"  <g>");
                if (bAnim)
                {
                    sb.AppendLine($"    <rect clip-path=\"url(#{clipId})\" x=\"{F(x0)}\" y=\"{F(barY)}\" width=\"0\" height=\"{F(barH)}\" fill=\"{barFill}\" fill-opacity=\"{barOp}\"{BuildRectBorderAttr(series)}>");
                    sb.AppendLine($"      <animate attributeName=\"width\" from=\"0\" to=\"{F(barW)}\" dur=\"{bDur}\" fill=\"freeze\"{bEase}/>");
                    if (v < 0) sb.AppendLine($"      <animate attributeName=\"x\" from=\"{F(x0)}\" to=\"{F(barX)}\" dur=\"{bDur}\" fill=\"freeze\"{bEase}/>");
                    sb.AppendLine($"    </rect>");
                }
                else
                {
                    sb.AppendLine($"    <rect clip-path=\"url(#{clipId})\" x=\"{F(barX)}\" y=\"{F(barY)}\" width=\"{F(barW)}\" height=\"{F(barH)}\" fill=\"{barFill}\" fill-opacity=\"{barOp}\"{BuildRectBorderAttr(series)}/>");
                }
                sb.AppendLine($"  </g>");
                if (options.RenderMode != SvgMode.Static)
                {
                    tooltipLayer.AppendLine($"  <g class=\"data-point\">");
                    tooltipLayer.AppendLine($"    <rect x=\"{F(barX)}\" y=\"{F(barY)}\" width=\"{F(barW)}\" height=\"{F(barH)}\" class=\"hit-area\" stroke=\"none\"/>");
                    AppendTooltip(tooltipLayer, barX + barW / 2, barY + barH / 2, series.Name, v, svgWidth, svgHeight, options.Tooltip, color);
                    tooltipLayer.AppendLine($"  </g>");
                }
            }
        }

    }
}
