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
        private static void AppendBubbleSeries(StringBuilder sb, Series series, string color,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            string clipId, StringBuilder tooltipLayer)
        {
            var pts = series.BubbleData;
            if (pts.Count == 0) return;

            // Compute X and Y ranges
            double xMin = options.XAxis.Min ?? double.MaxValue;
            double xMax = options.XAxis.Max ?? double.MinValue;
            double yMin = options.YAxis.Min ?? double.MaxValue;
            double yMax = options.YAxis.Max ?? double.MinValue;
            double zMax = 0;
            foreach (var p in pts)
            {
                if (options.XAxis.Min == null && p.X < xMin) xMin = p.X;
                if (options.XAxis.Max == null && p.X > xMax) xMax = p.X;
                if (options.YAxis.Min == null && p.Y < yMin) yMin = p.Y;
                if (options.YAxis.Max == null && p.Y > yMax) yMax = p.Y;
                if (p.Z > zMax) zMax = p.Z;
            }
            if (xMin == double.MaxValue) xMin = 0; if (xMax == double.MinValue) xMax = 1;
            if (yMin == double.MaxValue) yMin = 0; if (yMax == double.MinValue) yMax = 1;
            if (Math.Abs(xMax - xMin) < double.Epsilon) xMax = xMin + 1;
            if (Math.Abs(yMax - yMin) < double.Epsilon) yMax = yMin + 1;
            if (zMax <= 0) zMax = 1;

            double maxBubbleR = Math.Min(plotWidth, plotHeight) * 0.08;
            double minBubbleR = 4;

            bool   bAnim = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string bDur  = bAnim ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string bEase = bAnim ? SmilEasing(options.Animation.Easing) : string.Empty;

            string scStroke  = !string.IsNullOrEmpty(series.BorderColor) ? Escape(series.BorderColor!) : Escape(options.ResolvedBackgroundColor);
            string scStrokeW = series.BorderWidth > 0 ? series.BorderWidth.ToString(CultureInfo.InvariantCulture) : "1.5";

            foreach (var p in pts)
            {
                double px = PaddingLeft + (p.X - xMin) / (xMax - xMin) * plotWidth;
                double py = PaddingTop  + plotHeight - (p.Y - yMin) / (yMax - yMin) * plotHeight;
                // Area-proportional sizing: bubble AREA ∝ Z (radius ∝ √Z), the perceptually
                // correct encoding. Scaling radius linearly with Z would make area ∝ Z²,
                // exaggerating larger values. Negative Z is clamped to the minimum radius.
                double zNorm = Math.Sqrt(Math.Max(0.0, p.Z) / zMax);
                double r     = minBubbleR + zNorm * (maxBubbleR - minBubbleR);

                if (bAnim)
                {
                    sb.AppendLine($"  <circle clip-path=\"url(#{clipId})\" cx=\"{F(px)}\" cy=\"{F(py)}\" r=\"0\" fill=\"{Escape(color)}\" fill-opacity=\"0.7\" stroke=\"{scStroke}\" stroke-width=\"{scStrokeW}\">");
                    sb.AppendLine($"    <animate attributeName=\"r\" from=\"0\" to=\"{F(r)}\" dur=\"{bDur}\" fill=\"freeze\"{bEase}/>");
                    sb.AppendLine($"  </circle>");
                }
                else
                {
                    sb.AppendLine($"  <circle clip-path=\"url(#{clipId})\" cx=\"{F(px)}\" cy=\"{F(py)}\" r=\"{F(r)}\" fill=\"{Escape(color)}\" fill-opacity=\"0.7\" stroke=\"{scStroke}\" stroke-width=\"{scStrokeW}\"/>");
                }

                if (series.DataLabel.Enabled)
                    AppendDataLabel(sb, px, py + 4, FormatDataLabel(p.Z, series.DataLabel.FormatString),
                        series.DataLabel.TextColor ?? options.Theme.TextColor, series.DataLabel.BackgroundColor, series.DataLabel.TextFontSize);

                if (options.RenderMode != SvgMode.Static)
                {
                    tooltipLayer.AppendLine($"  <g class=\"data-point\">");
                    tooltipLayer.AppendLine($"    <circle cx=\"{F(px)}\" cy=\"{F(py)}\" r=\"{F(r + 4)}\" class=\"hit-area\" stroke=\"none\"/>");
                    AppendTooltip(tooltipLayer, px, py - r - 8, series.Name, p.Z, svgWidth, svgHeight, options.Tooltip,
                        color, PaddingTop, PaddingTop + plotHeight);
                    tooltipLayer.AppendLine($"  </g>");
                }
            }
        }

        // ------------------------------------------------------------------ Heatmap

        private static void AppendHeatmapSeries(StringBuilder sb, Series series,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            string clipId)
        {
            var cells = series.HeatmapData;
            if (cells.Count == 0) return;

            // Determine grid dimensions
            int numCols = 0, numRows = 0;
            foreach (var c in cells)
            {
                if (c.Col + 1 > numCols) numCols = c.Col + 1;
                if (c.Row + 1 > numRows) numRows = c.Row + 1;
            }
            if (options.XAxis.Categories.Count > numCols) numCols = options.XAxis.Categories.Count;
            if (series.HeatmapRowLabels.Count > numRows)  numRows = series.HeatmapRowLabels.Count;
            if (numCols == 0 || numRows == 0) return;

            // Value range for colour scale
            double valMin = double.MaxValue, valMax = double.MinValue;
            foreach (var c in cells)
            {
                if (c.Value < valMin) valMin = c.Value;
                if (c.Value > valMax) valMax = c.Value;
            }
            if (Math.Abs(valMax - valMin) < double.Epsilon) { valMin = 0; valMax = 1; }

            // Allocate left margin for row labels
            int rowLabelW = series.HeatmapRowLabels.Count > 0 ? 60 : 0;
            double cellW = (plotWidth - rowLabelW) / (double)numCols;
            double cellH = plotHeight / (double)numRows;

            string hotHex  = series.Color ?? options.Theme.Colors[0];
            // Cold end: blend 15 % of the hot colour into the chart background so the
            // gradient always harmonises with the current theme (dark → near-black tint,
            // light → near-white tint of the series colour).
            string coldHex = HeatmapColor(0.15, options.ResolvedBackgroundColor, hotHex);

            // Draw row labels
            for (int r = 0; r < numRows; r++)
            {
                string rowLabel = r < series.HeatmapRowLabels.Count ? series.HeatmapRowLabels[r] : string.Empty;
                if (string.IsNullOrEmpty(rowLabel)) continue;
                double ly = PaddingTop + r * cellH + cellH / 2 + 4;
                sb.AppendLine($"  <text class=\"axis-label\" x=\"{PaddingLeft + rowLabelW - 6}\" y=\"{F(ly)}\" text-anchor=\"end\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(rowLabel)}</text>");
            }

            // Draw column labels (from XAxis.Categories)
            double colOriginX = PaddingLeft + rowLabelW;
            for (int c = 0; c < numCols; c++)
            {
                string colLabel = c < options.XAxis.Categories.Count ? options.XAxis.Categories[c] : string.Empty;
                if (string.IsNullOrEmpty(colLabel)) continue;
                double lx = colOriginX + c * cellW + cellW / 2;
                sb.AppendLine($"  <text class=\"axis-label\" x=\"{F(lx)}\" y=\"{PaddingTop + plotHeight + 16}\" text-anchor=\"middle\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(colLabel)}</text>");
            }

            // Draw cell rectangles
            foreach (var cell in cells)
            {
                double t  = (cell.Value - valMin) / (valMax - valMin);
                string fc = HeatmapColor(t, coldHex, hotHex);
                double cx = colOriginX + cell.Col * cellW;
                double cy = PaddingTop + cell.Row * cellH;

                sb.AppendLine($"  <rect clip-path=\"url(#{clipId})\" x=\"{F(cx)}\" y=\"{F(cy)}\" width=\"{F(cellW)}\" height=\"{F(cellH)}\" fill=\"{fc}\" stroke=\"{Escape(options.ResolvedBackgroundColor)}\" stroke-width=\"1\"/>");

                if (series.DataLabel.Enabled)
                {
                    string labelText = FormatDataLabel(cell.Value, series.DataLabel.FormatString);
                    AppendDataLabel(sb, cx + cellW / 2, cy + cellH / 2 + 4, labelText,
                        series.DataLabel.TextColor ?? options.Theme.TextColor, null, series.DataLabel.TextFontSize);
                }
            }

            // Colour-axis legend (gradient scale) — unless the legend is explicitly disabled.
            if (options.Legend == null || options.Legend.Enabled)
                AppendColorAxisLegend(sb, options, series, valMin, valMax, coldHex, hotHex,
                    plotWidth, plotHeight, clipId);
        }

        /// <summary>
        /// Draws a horizontal colour-axis legend (a min→max gradient bar with tick labels) beneath a
        /// heatmap, mapping cell colour back to value. The gradient mirrors the heatmap's cold→hot scale.
        /// </summary>
        private static void AppendColorAxisLegend(StringBuilder sb, ChartOptions options, Series series,
            double valMin, double valMax, string coldHex, string hotHex,
            int plotWidth, int plotHeight, string clipId)
        {
            string gid = $"{clipId}-colorlegend";
            double barW = Math.Min(240.0, plotWidth * 0.6);
            double barH = 12.0;
            double barX = PaddingLeft + (plotWidth - barW) / 2.0;
            double barY = PaddingTop + plotHeight + 34.0;

            sb.AppendLine("  <defs>");
            sb.AppendLine($"    <linearGradient id=\"{Escape(gid)}\" x1=\"0\" y1=\"0\" x2=\"1\" y2=\"0\">");
            sb.AppendLine($"      <stop offset=\"0\" stop-color=\"{coldHex}\"/>");
            sb.AppendLine($"      <stop offset=\"0.5\" stop-color=\"{HeatmapColor(0.5, coldHex, hotHex)}\"/>");
            sb.AppendLine($"      <stop offset=\"1\" stop-color=\"{hotHex}\"/>");
            sb.AppendLine("    </linearGradient>");
            sb.AppendLine("  </defs>");

            // Optional title (series name) centred above the bar.
            if (!string.IsNullOrEmpty(series.Name))
                sb.AppendLine($"  <text class=\"legend-label\" x=\"{F(barX + barW / 2)}\" y=\"{F(barY - 6)}\" text-anchor=\"middle\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(series.Name)}</text>");

            sb.AppendLine($"  <rect x=\"{F(barX)}\" y=\"{F(barY)}\" width=\"{F(barW)}\" height=\"{F(barH)}\" fill=\"url(#{Escape(gid)})\" stroke=\"{Escape(options.Theme.AxisLineColor)}\" stroke-width=\"0.5\"/>");

            // Min / mid / max tick labels below the bar.
            double mid = (valMin + valMax) / 2.0;
            double lblY = barY + barH + 12.0;
            sb.AppendLine($"  <text class=\"axis-label\" x=\"{F(barX)}\" y=\"{F(lblY)}\" text-anchor=\"middle\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(FormatTick(valMin))}</text>");
            sb.AppendLine($"  <text class=\"axis-label\" x=\"{F(barX + barW / 2)}\" y=\"{F(lblY)}\" text-anchor=\"middle\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(FormatTick(mid))}</text>");
            sb.AppendLine($"  <text class=\"axis-label\" x=\"{F(barX + barW)}\" y=\"{F(lblY)}\" text-anchor=\"middle\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(FormatTick(valMax))}</text>");
        }

        // ------------------------------------------------------------------ ColumnRange

        private static void AppendColumnRangeSeries(StringBuilder sb, Series series, string color,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            string clipId, StringBuilder tooltipLayer)
        {
            var pts = series.RangeData;
            if (pts.Count == 0) return;
            int n = pts.Count;

            bool yLog = IsLog(options.YAxis);
            var (yMin, yMax) = ResolveYBounds(options.YAxis, options.Series);

            bool hasCats = options.XAxis.Categories?.Count > 0;
            int catCount = hasCats ? options.XAxis.Categories!.Count : n;
            double groupW = (double)plotWidth / catCount;
            double barPad = groupW * 0.15;
            double barW   = groupW - barPad * 2;

            bool   anim  = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string dur   = anim ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string ease  = anim ? SmilEasing(options.Animation.Easing) : string.Empty;

            for (int i = 0; i < n; i++)
            {
                var p  = pts[i];
                // Normalise range order so an inverted point (Low > High) is treated as the
                // same range rather than rendering a misleading 1%-tall sliver.
                double rawLo = Math.Min(p.Low, p.High);
                double rawHi = Math.Max(p.Low, p.High);
                double lo = Math.Max(rawLo, yMin);
                double hi = Math.Min(rawHi, yMax);
                if (hi <= lo) hi = lo + (yMax - yMin) * 0.01; // degenerate (Low == High) → min visibility

                double yTop  = PaddingTop + plotHeight - Frac(hi, yMin, yMax, yLog) * plotHeight;
                double yBot  = PaddingTop + plotHeight - Frac(lo, yMin, yMax, yLog) * plotHeight;
                double barH  = yBot - yTop;
                double x     = PaddingLeft + groupW * i + barPad;

                // Plain group — tooltip hit-area lives in tooltipLayer only
                sb.AppendLine($"  <g>");
                if (anim)
                {
                    double baseYPx = PaddingTop + plotHeight;
                    sb.AppendLine($"    <rect clip-path=\"url(#{clipId})\" x=\"{F(x)}\" y=\"{F(baseYPx)}\" width=\"{F(barW)}\" height=\"0\" fill=\"{Escape(color)}\" fill-opacity=\"0.85\"{BuildRectBorderAttr(series, options)}>");
                    sb.AppendLine($"      <animate attributeName=\"height\" from=\"0\" to=\"{F(barH)}\" dur=\"{dur}\" fill=\"freeze\"{ease}/>");
                    sb.AppendLine($"      <animate attributeName=\"y\" from=\"{F(baseYPx)}\" to=\"{F(yTop)}\" dur=\"{dur}\" fill=\"freeze\"{ease}/>");
                    sb.AppendLine($"    </rect>");
                }
                else
                {
                    sb.AppendLine($"    <rect clip-path=\"url(#{clipId})\" x=\"{F(x)}\" y=\"{F(yTop)}\" width=\"{F(barW)}\" height=\"{F(barH)}\" fill=\"{Escape(color)}\" fill-opacity=\"0.85\"{BuildRectBorderAttr(series, options)}/>");
                }
                sb.AppendLine($"  </g>");

                if (series.DataLabel.Enabled)
                {
                    string txt = FormatDataLabel(rawHi, series.DataLabel.FormatString);
                    AppendDataLabel(sb, x + barW / 2, yTop - 4 + series.DataLabel.VerticalOffset.GetValueOrDefault(), txt,
                        series.DataLabel.TextColor ?? options.Theme.TextColor, series.DataLabel.BackgroundColor, series.DataLabel.TextFontSize);
                }

                if (options.RenderMode != SvgMode.Static)
                {
                    tooltipLayer.AppendLine($"  <g class=\"data-point\">");
                    AppendHoverBandV(tooltipLayer, options, PaddingLeft + groupW * i + groupW / 2.0, groupW, plotHeight);
                    tooltipLayer.AppendLine($"    <rect x=\"{F(x)}\" y=\"{F(yTop)}\" width=\"{F(barW)}\" height=\"{F(barH)}\" class=\"hit-area\" stroke=\"none\"/>");
                    // Tooltip shows Low–High range
                    string rangeLabel = $"{series.Name}: {FormatTick(rawLo)}\u2013{FormatTick(rawHi)}";
                    double mid = (rawLo + rawHi) / 2;
                    AppendTooltip(tooltipLayer, x + barW / 2, yTop - 10, rangeLabel, mid, svgWidth, svgHeight, options.Tooltip,
                        color, PaddingTop, PaddingTop + plotHeight);
                    tooltipLayer.AppendLine($"  </g>");
                }
            }
        }

        // ------------------------------------------------------------------ AreaRange

        private static void AppendAreaRangeSeries(StringBuilder sb, Series series, string color,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            string clipId, StringBuilder tooltipLayer)
        {
            var pts = series.RangeData;
            if (pts.Count == 0) return;
            int n = pts.Count;

            bool yLog = IsLog(options.YAxis);
            var (yMin, yMax) = ResolveYBounds(options.YAxis, options.Series);

            bool hasCats  = options.XAxis.Categories?.Count > 0;
            int catCount  = hasCats ? options.XAxis.Categories!.Count : n;
            double step   = (double)plotWidth / Math.Max(catCount - 1, 1);
            double xOff   = hasCats ? (double)plotWidth / catCount / 2.0 : 0;
            if (hasCats) step = (double)plotWidth / catCount;

            var highPts = new List<(double x, double y)>();
            var lowPts  = new List<(double x, double y)>();

            for (int i = 0; i < n; i++)
            {
                var p  = pts[i];
                double px = PaddingLeft + xOff + i * step;
                double pyHi = PaddingTop + plotHeight - Frac(p.High, yMin, yMax, yLog) * plotHeight;
                double pyLo = PaddingTop + plotHeight - Frac(p.Low,  yMin, yMax, yLog) * plotHeight;
                highPts.Add((px, pyHi));
                lowPts.Add((px, pyLo));
            }
            if (highPts.Count == 0) return;

            // Fill band: high line forward, low line reversed
            var fill = new StringBuilder();
            fill.Append($"M{F(highPts[0].x)},{F(highPts[0].y)}");
            for (int i = 1; i < highPts.Count; i++) fill.Append($" L{F(highPts[i].x)},{F(highPts[i].y)}");
            for (int i = lowPts.Count - 1; i >= 0; i--) fill.Append($" L{F(lowPts[i].x)},{F(lowPts[i].y)}");
            fill.Append(" Z");

            // Upper and lower stroke paths
            var highPath = new StringBuilder();
            highPath.Append($"M{F(highPts[0].x)},{F(highPts[0].y)}");
            for (int i = 1; i < highPts.Count; i++) highPath.Append($" L{F(highPts[i].x)},{F(highPts[i].y)}");

            var lowPath = new StringBuilder();
            lowPath.Append($"M{F(lowPts[0].x)},{F(lowPts[0].y)}");
            for (int i = 1; i < lowPts.Count; i++) lowPath.Append($" L{F(lowPts[i].x)},{F(lowPts[i].y)}");

            bool   anim  = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string dur   = anim ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string ease  = anim ? SmilEasing(options.Animation.Easing) : string.Empty;
            double fillOp = series.FillOpacity ?? 0.3;

            if (anim)
            {
                sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{fill}\" fill=\"{Escape(color)}\" fill-opacity=\"0\" stroke=\"none\">");
                sb.AppendLine($"    <animate attributeName=\"fill-opacity\" from=\"0\" to=\"{F(fillOp)}\" dur=\"{dur}\" fill=\"freeze\"{ease}/>");
                sb.AppendLine($"  </path>");
                sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{highPath}\" fill=\"none\" stroke=\"{Escape(color)}\" stroke-width=\"{series.LineWidth}\" pathLength=\"1\" stroke-dasharray=\"1\" stroke-dashoffset=\"1\">");
                sb.AppendLine($"    <animate attributeName=\"stroke-dashoffset\" from=\"1\" to=\"0\" dur=\"{dur}\" fill=\"freeze\"{ease}/>");
                sb.AppendLine($"  </path>");
                sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{lowPath}\" fill=\"none\" stroke=\"{Escape(color)}\" stroke-width=\"{series.LineWidth}\" stroke-opacity=\"0.5\" pathLength=\"1\" stroke-dasharray=\"1\" stroke-dashoffset=\"1\">");
                sb.AppendLine($"    <animate attributeName=\"stroke-dashoffset\" from=\"1\" to=\"0\" dur=\"{dur}\" fill=\"freeze\"{ease}/>");
                sb.AppendLine($"  </path>");
            }
            else
            {
                sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{fill}\" fill=\"{Escape(color)}\" fill-opacity=\"{F(fillOp)}\" stroke=\"none\"/>");
                sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{highPath}\" fill=\"none\" stroke=\"{Escape(color)}\" stroke-width=\"{series.LineWidth}\"/>");
                sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{lowPath}\" fill=\"none\" stroke=\"{Escape(color)}\" stroke-width=\"{series.LineWidth}\" stroke-opacity=\"0.5\"/>");
            }

            // Tooltip hit-points at high boundary
            if (options.RenderMode != SvgMode.Static)
            {
                for (int i = 0; i < n; i++)
                {
                    var p = pts[i];
                    string rangeLabel = $"{series.Name}: {FormatTick(p.Low)}\u2013{FormatTick(p.High)}";
                    double mid = (p.Low + p.High) / 2;
                    tooltipLayer.AppendLine($"  <g class=\"data-point\">");
                    tooltipLayer.AppendLine($"    <circle cx=\"{F(highPts[i].x)}\" cy=\"{F(highPts[i].y)}\" r=\"8\" class=\"hit-area\" stroke=\"none\"/>");
                    AppendTooltip(tooltipLayer, highPts[i].x, highPts[i].y - 14, rangeLabel, mid, svgWidth, svgHeight, options.Tooltip,
                        color, PaddingTop, PaddingTop + plotHeight);
                    tooltipLayer.AppendLine($"  </g>");
                }
            }
        }

        // ------------------------------------------------------------------ Funnel

    }
}
