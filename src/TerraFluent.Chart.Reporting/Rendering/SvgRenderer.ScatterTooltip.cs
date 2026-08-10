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
        private static void AppendScatterSeries(StringBuilder sb, Series series, string color,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            double yMin, double yMax, double step, string clipId, StringBuilder tooltipLayer)
        {
            bool hasCats = options.XAxis.Categories?.Count > 0;
            double xOff  = hasCats ? step / 2.0 : (series.Data.Count == 1 ? plotWidth / 2.0 : 0);
            bool yLog = IsLog(series.YAxisIndex == 1 ? options.YAxis2 : options.YAxis);

            bool   sAnim = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string sDur  = sAnim ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string sEase = sAnim ? SmilEasing(options.Animation.Easing) : string.Empty;
            // Resolved scatter marker border — falls back to the chart background so dots get a
            // "knockout" halo separating them from the series fill on any theme colour.
            string scStroke  = !string.IsNullOrEmpty(series.BorderColor) ? Escape(series.BorderColor!) : Escape(options.ResolvedBackgroundColor);
            string scStrokeW = series.BorderWidth > 0 ? series.BorderWidth.ToString(CultureInfo.InvariantCulture) : "1.5";

            for (int i = 0; i < series.Data.Count; i++)
            {
                if (series.Data[i] is null) continue;
                double v  = series.Data[i]!.Value;
                double px = PaddingLeft + xOff + i * step;
                double py = PaddingTop + plotHeight - Frac(v, yMin, yMax, yLog) * plotHeight;

                // Scatter dot shape in sb
                int scR = series.MarkerSize ?? 5;
                string scRs = scR.ToString(CultureInfo.InvariantCulture);
                if (series.MarkerSymbol == Enums.MarkerSymbol.Circle)
                {
                    if (sAnim)
                    {
                        sb.AppendLine($"  <circle clip-path=\"url(#{clipId})\" cx=\"{F(px)}\" cy=\"{F(py)}\" r=\"0\" fill=\"{Escape(color)}\" stroke=\"{scStroke}\" stroke-width=\"{scStrokeW}\">");
                        sb.AppendLine($"    <animate attributeName=\"r\" from=\"0\" to=\"{scRs}\" dur=\"{sDur}\" fill=\"freeze\"{sEase}/>");
                        sb.AppendLine($"  </circle>");
                    }
                    else
                    {
                        sb.AppendLine($"  <circle clip-path=\"url(#{clipId})\" cx=\"{F(px)}\" cy=\"{F(py)}\" r=\"{scRs}\" fill=\"{Escape(color)}\" stroke=\"{scStroke}\" stroke-width=\"{scStrokeW}\"/>");
                    }
                }
                else
                {
                    // Non-circular symbols fade in (SMIL opacity) rather than growing a radius.
                    double scStrokeWv = series.BorderWidth > 0 ? series.BorderWidth : 1.5;
                    if (sAnim)
                    {
                        string shape = MarkerShape(series.MarkerSymbol, px, py, scR, Escape(color), scStroke, scStrokeWv, $" clip-path=\"url(#{clipId})\" opacity=\"0\"");
                        // Insert the animate child before the self-closing slash.
                        shape = shape.Substring(0, shape.Length - 2) + ">";
                        sb.AppendLine("  " + shape);
                        sb.AppendLine($"    <animate attributeName=\"opacity\" from=\"0\" to=\"1\" dur=\"{sDur}\" fill=\"freeze\"{sEase}/>");
                        string closeTag = series.MarkerSymbol == Enums.MarkerSymbol.Square ? "rect" : "polygon";
                        sb.AppendLine($"  </{closeTag}>");
                    }
                    else
                    {
                        sb.AppendLine("  " + MarkerShape(series.MarkerSymbol, px, py, scR, Escape(color), scStroke, scStrokeWv, $" clip-path=\"url(#{clipId})\""));
                    }
                }
                // Tooltip in overlay layer
                if (options.RenderMode != SvgMode.Static)
                {
                    tooltipLayer.AppendLine($"  <g class=\"data-point\">");
                    tooltipLayer.AppendLine($"    <circle cx=\"{F(px)}\" cy=\"{F(py)}\" r=\"8\" class=\"hit-area\" stroke=\"none\"/>");
                    AppendTooltip(tooltipLayer, px, py - 14, series.Name, v, svgWidth, svgHeight, options.Tooltip,
                        color, PaddingTop, PaddingTop + plotHeight);
                    tooltipLayer.AppendLine($"  </g>");
                }
            }
        }

        // ------------------------------------------------------------------ helpers

        private static void AppendDataPoint(StringBuilder sb, StringBuilder tooltipLayer,
            SvgMode mode, double cx, double cy,
            string color, string seriesName, double value, int svgWidth, int svgHeight,
            TooltipOptions tooltip,
            bool markerEnabled = true, int markerSize = 4,
            string markerBorderColor = ChartColor.White,
            double crosshairTop = -1, double crosshairBottom = -1,
            int dataIndex = -1, string xLabel = "",
            Enums.MarkerSymbol markerSymbol = Enums.MarkerSymbol.Circle,
            bool suppressVisibleMarker = false, string? rsGroupId = null)
        {
            if (!markerEnabled && !suppressVisibleMarker) return;
            string rHit = (markerSize + 4).ToString(CultureInfo.InvariantCulture);
            // Shape in sb
            if (!suppressVisibleMarker)
                sb.AppendLine("  " + MarkerShape(markerSymbol, cx, cy, markerSize, Escape(color), Escape(markerBorderColor), 1.5));
            // Tooltip hit-area in overlay layer
            if (mode != SvgMode.Static)
            {
                string rsAttr   = rsGroupId != null ? $" data-rspid=\"{Escape(rsGroupId)}\"" : "";
                string dataAttrs = dataIndex >= 0
                    ? $" data-di=\"{dataIndex}\" data-val=\"{Escape(FormatTick(value))}\" data-name=\"{Escape(seriesName)}\" data-color=\"{Escape(color)}\" data-ax=\"{F(cx)}\" data-ay=\"{F(cy - 20)}\" data-xlabel=\"{Escape(xLabel)}\""
                    : string.Empty;
                tooltipLayer.AppendLine($"  <g class=\"data-point\"{rsAttr}{dataAttrs}>");
                tooltipLayer.AppendLine($"    <circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{rHit}\" class=\"hit-area\" stroke=\"none\"/>");
                AppendTooltip(tooltipLayer, cx, cy - 20, seriesName, value, svgWidth, svgHeight, tooltip,
                    color, crosshairTop, crosshairBottom);
                tooltipLayer.AppendLine($"  </g>");
            }
        }

        /// <summary>
        /// Builds the SVG element for a data-point marker of the given <paramref name="symbol"/>,
        /// centred at (<paramref name="cx"/>, <paramref name="cy"/>) with half-extent
        /// <paramref name="size"/>. Circle → <c>&lt;circle&gt;</c>; all others → <c>&lt;polygon&gt;</c>.
        /// </summary>
        private static string MarkerShape(Enums.MarkerSymbol symbol, double cx, double cy, double size,
            string fill, string stroke, double strokeW, string extraAttr = "")
        {
            string common = $"fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\"{extraAttr}";
            switch (symbol)
            {
                case Enums.MarkerSymbol.Square:
                    return $"<rect x=\"{F(cx - size)}\" y=\"{F(cy - size)}\" width=\"{F(size * 2)}\" height=\"{F(size * 2)}\" {common}/>";
                case Enums.MarkerSymbol.Diamond:
                    return $"<polygon points=\"{F(cx)},{F(cy - size)} {F(cx + size)},{F(cy)} {F(cx)},{F(cy + size)} {F(cx - size)},{F(cy)}\" {common}/>";
                case Enums.MarkerSymbol.Triangle:
                    return $"<polygon points=\"{F(cx)},{F(cy - size)} {F(cx + size)},{F(cy + size)} {F(cx - size)},{F(cy + size)}\" {common}/>";
                case Enums.MarkerSymbol.TriangleDown:
                    return $"<polygon points=\"{F(cx)},{F(cy + size)} {F(cx + size)},{F(cy - size)} {F(cx - size)},{F(cy - size)}\" {common}/>";
                case Enums.MarkerSymbol.Circle:
                default:
                    return $"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(size)}\" {common}/>";
            }
        }

        // ------------------------------------------------------------------ zones / thresholds

        /// <summary>
        /// Returns the zone colour for <paramref name="value"/>, or <paramref name="fallback"/> when
        /// no zone matches. Zones are matched in ascending bound order; a null bound matches anything.
        /// </summary>
        private static string ZoneColorFor(List<Models.SeriesZone> zones, double value, string fallback)
        {
            for (int i = 0; i < zones.Count; i++)
            {
                var z = zones[i];
                if (z.Value == null || value <= z.Value.Value)
                    return z.Color;
            }
            return fallback;
        }

        /// <summary>
        /// Splits a contiguous line segment into colour runs at zone-threshold crossings, inserting
        /// interpolated crossing points so each run is drawn in a single zone colour.
        /// </summary>
        private static List<(List<(double x, double y)> pts, string color)> BuildZonedRuns(
            List<(double x, double y, double value)> seg, List<Models.SeriesZone> zones, string fallback)
        {
            // Finite zone bounds act as crossing thresholds.
            var bounds = new List<double>();
            foreach (var z in zones) if (z.Value.HasValue) bounds.Add(z.Value.Value);
            bounds.Sort();

            // Augment the segment with interpolated crossing points.
            var aug = new List<(double x, double y, double value)> { seg[0] };
            for (int i = 1; i < seg.Count; i++)
            {
                var a = seg[i - 1];
                var b = seg[i];
                double lo = Math.Min(a.value, b.value), hi = Math.Max(a.value, b.value);
                var cross = new List<double>();
                foreach (var t in bounds) if (t > lo && t < hi) cross.Add(t);
                cross.Sort((p, q) => a.value <= b.value ? p.CompareTo(q) : q.CompareTo(p));
                foreach (var t in cross)
                {
                    double frac = (t - a.value) / (b.value - a.value);
                    aug.Add((a.x + (b.x - a.x) * frac, a.y + (b.y - a.y) * frac, t));
                }
                aug.Add(b);
            }

            var runs = new List<(List<(double x, double y)> pts, string color)>();
            if (aug.Count < 2) return runs;

            string ColorForEdge(int j) => ZoneColorFor(zones, (aug[j].value + aug[j + 1].value) / 2.0, fallback);

            var curPts = new List<(double x, double y)> { (aug[0].x, aug[0].y), (aug[1].x, aug[1].y) };
            string curColor = ColorForEdge(0);
            for (int k = 2; k < aug.Count; k++)
            {
                string c = ColorForEdge(k - 1);
                if (c == curColor)
                {
                    curPts.Add((aug[k].x, aug[k].y));
                }
                else
                {
                    runs.Add((curPts, curColor));
                    curPts = new List<(double x, double y)> { (aug[k - 1].x, aug[k - 1].y), (aug[k].x, aug[k].y) };
                    curColor = c;
                }
            }
            runs.Add((curPts, curColor));
            return runs;
        }

        private static void AppendTooltip(StringBuilder sb, double x, double y, string label, double value,
            int svgWidth, int svgHeight, TooltipOptions tooltip,
            string seriesColor = "",
            double crosshairTop = -1, double crosshairBottom = -1)
        {
            if (!tooltip.Enabled) return;

            // ── Format value (ValueDecimals / ValuePrefix / ValueSuffix) ──────────────────
            string formattedValue = tooltip.ValueDecimals.HasValue
                ? value.ToString("F" + tooltip.ValueDecimals.Value.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)
                : FormatTick(value);
            string fullValue = (tooltip.ValuePrefix ?? string.Empty)
                             + formattedValue
                             + (tooltip.ValueSuffix ?? string.Empty);

            // ── Build display lines ───────────────────────────────────────────────────────
            bool isMultiLine = !string.IsNullOrEmpty(tooltip.HeaderFormat);
            string headerText = string.Empty;
            string pointText;

            if (isMultiLine)
            {
                headerText = tooltip.HeaderFormat!
                    .Replace("{label}", label)
                    .Replace("{value}", fullValue);
                string pointTemplate = tooltip.PointFormat ?? tooltip.Format ?? "{label}: {value}";
                pointText = pointTemplate
                    .Replace("{label}", label)
                    .Replace("{value}", fullValue);
            }
            else
            {
                string raw = tooltip.Format ?? "{label}: {value}";
                pointText = raw
                    .Replace("{label}", label)
                    .Replace("{value}", fullValue);
            }

            // ── Layout constants ─────────────────────────────────────────────────────────
            bool hasBullet  = !string.IsNullOrEmpty(seriesColor);
            double pad      = tooltip.Padding;
            const double BulletSize = 8.0;
            const double BulletGap  = 6.0;
            const double LineH      = 16.0;
            const double NotchH     = 7.0;
            const double NotchW     = 12.0;
            double boxH = isMultiLine ? 44.0 : 28.0;

            // Box width: driven by the longer of header / point line plus bullet indent
            string longestLine = isMultiLine && headerText.Length > pointText.Length
                ? headerText : pointText;
            double bulletIndent = hasBullet ? BulletSize + BulletGap : 0.0;
            double boxW = Math.Max(longestLine.Length * 7.2 + bulletIndent + pad * 2, 80.0);

            // ── Position ────────────────────────────────────────────────────────────────
            double boxX = Math.Max(2.0, Math.Min(x - boxW / 2.0, svgWidth - boxW - 2.0));
            double boxY = Math.Max(2.0, y - boxH - (tooltip.ShowArrow ? NotchH : 0.0));
            if (boxY + boxH + NotchH > svgHeight - 2.0)
                boxY = svgHeight - boxH - NotchH - 2.0;

            // Notch tip: near anchor x, clamped inside the box
            double ntx = Math.Max(boxX + NotchW, Math.Min(x, boxX + boxW - NotchW));
            double nty = boxY + boxH;

            // ── Crosshair vertical guide line (behind the tooltip box) ───────────────────
            if (tooltip.Crosshair && crosshairTop >= 0 && crosshairBottom >= 0)
            {
                sb.AppendLine($"    <line class=\"crosshair-x\"" +
                    $" x1=\"{F(x)}\" y1=\"{F(crosshairTop)}\" x2=\"{F(x)}\" y2=\"{F(crosshairBottom)}\"" +
                    $" stroke=\"{Escape(tooltip.CrosshairColor)}\" stroke-width=\"{tooltip.CrosshairWidth}\"" +
                    $" pointer-events=\"none\"/>");
            }

            // ── Border attribute ─────────────────────────────────────────────────────────
            string borderAttr = tooltip.BorderWidth > 0 && !string.IsNullOrEmpty(tooltip.BorderColor)
                ? $" stroke=\"{Escape(tooltip.BorderColor!)}\" stroke-width=\"{tooltip.BorderWidth}\""
                : string.Empty;

            // ── Box ──────────────────────────────────────────────────────────────────────
            sb.AppendLine($"    <rect class=\"tooltip-bg\" x=\"{F(boxX)}\" y=\"{F(boxY)}\" width=\"{F(boxW)}\" height=\"{F(boxH)}\" rx=\"{tooltip.BorderRadius}\"{borderAttr}/>");

            // ── Arrow / notch ────────────────────────────────────────────────────────────
            if (tooltip.ShowArrow)
                sb.AppendLine($"    <polygon class=\"tooltip-bg\" points=\"{F(ntx - NotchW / 2)},{F(nty)} {F(ntx + NotchW / 2)},{F(nty)} {F(ntx)},{F(nty + NotchH)}\"/>");

            // ── Color bullet (Highcharts-style series swatch) ────────────────────────────
            if (hasBullet)
            {
                // Bullet sits on the point text line (single) or second line (multi)
                double bulletCY = isMultiLine
                    ? boxY + LineH + (boxH - LineH) / 2.0   // vertically centred on the point row
                    : boxY + boxH / 2.0;
                double bulletTY = bulletCY - BulletSize / 2.0;
                sb.AppendLine($"    <rect class=\"tooltip-bullet\" x=\"{F(boxX + pad)}\" y=\"{F(bulletTY)}\" width=\"{BulletSize.ToString("F0", CultureInfo.InvariantCulture)}\" height=\"{BulletSize.ToString("F0", CultureInfo.InvariantCulture)}\" rx=\"2\" fill=\"{Escape(seriesColor)}\"/>");
            }

            // ── Text lines ───────────────────────────────────────────────────────────────
            double textStartX = hasBullet ? boxX + pad + BulletSize + BulletGap : boxX + boxW / 2.0;
            string textAnchor = hasBullet ? "start" : "middle";

            if (isMultiLine)
            {
                // Header line: bold, no bullet indent (spans full box)
                double headerX = hasBullet ? boxX + pad : boxX + boxW / 2.0;
                sb.AppendLine($"    <text class=\"tooltip-text\" x=\"{F(headerX)}\" y=\"{F(boxY + 15.0)}\" text-anchor=\"{textAnchor}\" font-weight=\"bold\">{Escape(headerText)}</text>");
                // Point line: indented when bullet is shown
                sb.AppendLine($"    <text class=\"tooltip-text\" x=\"{F(textStartX)}\" y=\"{F(boxY + 15.0 + LineH)}\" text-anchor=\"{textAnchor}\">{Escape(pointText)}</text>");
            }
            else
            {
                sb.AppendLine($"    <text class=\"tooltip-text\" x=\"{F(textStartX)}\" y=\"{F(boxY + 18.0)}\" text-anchor=\"{textAnchor}\">{Escape(pointText)}</text>");
            }
        }

        // ================================================================== New chart-type renderers

        // ------------------------------------------------------------------ Bubble

    }
}
