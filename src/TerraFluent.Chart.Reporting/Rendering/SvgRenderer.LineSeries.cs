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
        private static void AppendLineSeries(StringBuilder sb, Series series, string color,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            double yMin, double yMax, double step, string clipId, StringBuilder tooltipLayer,
            string? rsPathId = null)
        {
            bool hasCats = options.XAxis.Categories?.Count > 0;
            double xOffset = hasCats ? step / 2.0 : (series.Data.Count == 1 ? plotWidth / 2.0 : 0);

            // Collect points, splitting into contiguous segments at null values so a gap in
            // the data breaks the line rather than being bridged by a misleading straight
            // segment. `points` keeps every drawn point (with its original category index)
            // for markers and labels.
            bool yLog = IsLog(series.YAxisIndex == 1 ? options.YAxis2 : options.YAxis);
            var points   = new List<(double x, double y, double value, int idx)>();
            var segments = new List<List<(double x, double y, double value)>>();
            var current  = new List<(double x, double y, double value)>();
            for (int i = 0; i < series.Data.Count; i++)
            {
                double? rawVal = series.Data[i];
                if (rawVal is null)
                {
                    switch (series.NullGapPolicy)
                    {
                        case Enums.GapPolicy.Break:
                            if (current.Count > 0) { segments.Add(current); current = new List<(double x, double y, double value)>(); }
                            continue;
                        case Enums.GapPolicy.Connect:
                            continue; // skip, keep current segment open
                        default: // Zero
                            rawVal = 0.0;
                            break;
                    }
                }
                double v = rawVal!.Value;
                double px = PaddingLeft + xOffset + i * step;
                double py = PaddingTop + plotHeight - Frac(v, yMin, yMax, yLog) * plotHeight;
                current.Add((px, py, v));
                points.Add((px, py, v, i));
            }
            if (current.Count > 0) segments.Add(current);

            if (points.Count == 0) return;

            bool smooth = series.Type == ChartType.Spline;
            string dashAttr = BuildDashAttr(series.DashStyle);
            bool   animated = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string dur      = animated ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string aEasing  = animated ? SmilEasing(options.Animation.Easing) : string.Empty;
            bool   zoned    = series.Zones.Count > 0;
            // Prevent stroke widths from scaling when the range-selector zoom transform is active.
            string veAttr   = options.RangeSelector.Enabled ? " vector-effect=\"non-scaling-stroke\"" : "";
            // In RS mode the line path gets a stable ID for JS redraw; markers are suppressed.
            string? rsOrigId = rsPathId; // save before the segments loop nulls it
            bool   isRsMode  = rsPathId != null;

            // One path per contiguous segment (a lone point yields no visible line, only a marker).
            foreach (var seg in segments)
            {
                if (zoned)
                {
                    // Threshold colouring: split the segment into straight colour runs at zone
                    // boundaries (spline smoothing is bypassed while zones are active).
                    foreach (var (pts, runColor) in BuildZonedRuns(seg, series.Zones, color))
                    {
                        if (pts.Count < 2) continue;
                        var rp = new StringBuilder();
                        rp.Append($"M{F(pts[0].x)},{F(pts[0].y)}");
                        for (int p = 1; p < pts.Count; p++)
                            rp.Append($" L{F(pts[p].x)},{F(pts[p].y)}");
                        if (animated && string.IsNullOrEmpty(series.DashStyle))
                        {
                            // Draw-on animation — matches non-zoned line behaviour
                            sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{rp}\" fill=\"none\" stroke=\"{Escape(runColor)}\" stroke-width=\"{series.LineWidth}\" stroke-linejoin=\"round\" stroke-linecap=\"round\"{veAttr} pathLength=\"1\" stroke-dasharray=\"1\" stroke-dashoffset=\"1\">");
                            sb.AppendLine($"    <animate attributeName=\"stroke-dashoffset\" from=\"1\" to=\"0\" dur=\"{dur}\" fill=\"freeze\"{aEasing}/>");
                            sb.AppendLine($"  </path>");
                        }
                        else if (animated)
                        {
                            // Dashed style: fall back to opacity fade (dasharray would conflict)
                            sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{rp}\" fill=\"none\" stroke=\"{Escape(runColor)}\" stroke-width=\"{series.LineWidth}\" stroke-linejoin=\"round\" stroke-linecap=\"round\"{dashAttr}{veAttr} opacity=\"0\">");
                            sb.AppendLine($"    <animate attributeName=\"opacity\" from=\"0\" to=\"1\" dur=\"{dur}\" fill=\"freeze\"{aEasing}/>");
                            sb.AppendLine($"  </path>");
                        }
                        else
                        {
                            sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{rp}\" fill=\"none\" stroke=\"{Escape(runColor)}\" stroke-width=\"{series.LineWidth}\" stroke-linejoin=\"round\" stroke-linecap=\"round\"{dashAttr}{veAttr}/>");
                        }
                    }
                    continue;
                }

                string pathD = BuildPath(seg, smooth);   // BuildPath falls back to straight lines for <3 points
                // First non-zoned segment in RS mode gets a stable ID for JS path redraw.
                string rsIdAttr = rsPathId != null ? $" id=\"{rsPathId}\"" : "";
                rsPathId = null; // only the first path gets the ID
                if (animated && string.IsNullOrEmpty(series.DashStyle))
                {
                    sb.AppendLine($"  <path{rsIdAttr} clip-path=\"url(#{clipId})\" d=\"{pathD}\" fill=\"none\" stroke=\"{Escape(color)}\" stroke-width=\"{series.LineWidth}\" stroke-linejoin=\"round\" stroke-linecap=\"round\"{veAttr} pathLength=\"1\" stroke-dasharray=\"1\" stroke-dashoffset=\"1\">");
                    sb.AppendLine($"    <animate attributeName=\"stroke-dashoffset\" from=\"1\" to=\"0\" dur=\"{dur}\" fill=\"freeze\"{aEasing}/>");
                    sb.AppendLine($"  </path>");
                }
                else if (animated)
                {
                    sb.AppendLine($"  <path{rsIdAttr} clip-path=\"url(#{clipId})\" d=\"{pathD}\" fill=\"none\" stroke=\"{Escape(color)}\" stroke-width=\"{series.LineWidth}\" stroke-linejoin=\"round\" stroke-linecap=\"round\"{dashAttr}{veAttr} opacity=\"0\">");
                    sb.AppendLine($"    <animate attributeName=\"opacity\" from=\"0\" to=\"1\" dur=\"{dur}\" fill=\"freeze\"{aEasing}/>");
                    sb.AppendLine($"  </path>");
                }
                else
                {
                    sb.AppendLine($"  <path{rsIdAttr} clip-path=\"url(#{clipId})\" d=\"{pathD}\" fill=\"none\" stroke=\"{Escape(color)}\" stroke-width=\"{series.LineWidth}\" stroke-linejoin=\"round\" stroke-linecap=\"round\"{dashAttr}{veAttr}/>");
                }
            }

            int lineMarkerR = series.MarkerSize ?? 4;
            double linePlotBottom = PaddingTop + plotHeight;
            foreach (var (px, py, value, idx) in points)
            {
                string xLbl = options.XAxis.Categories?.Count > idx ? options.XAxis.Categories[idx] : idx.ToString(CultureInfo.InvariantCulture);
                string markerColor = zoned ? ZoneColorFor(series.Zones, value, color) : color;
                AppendDataPoint(sb, tooltipLayer, options.RenderMode, px, py, markerColor, series.Name, value, svgWidth, svgHeight, options.Tooltip, series.MarkerEnabled, lineMarkerR, options.ResolvedBackgroundColor, PaddingTop, linePlotBottom, idx, xLbl, series.MarkerSymbol, suppressVisibleMarker: isRsMode, rsGroupId: rsOrigId);
                if (series.DataLabel.Enabled)
                    AppendDataLabel(sb, px, py - 10 + series.DataLabel.VerticalOffset.GetValueOrDefault(), FormatDataLabel(value, series.DataLabel.FormatString),
                        series.DataLabel.TextColor ?? options.Theme.TextColor, series.DataLabel.BackgroundColor, series.DataLabel.TextFontSize);
            }

            if (series.AutoInsight != null)
                AppendAutoInsightOverlays(sb, series, color, options, plotWidth, plotHeight, yMin, yMax, step, clipId);
        }

        private static void AppendAreaSeries(StringBuilder sb, Series series, string color,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            double yMin, double yMax, double step, string clipId, StringBuilder tooltipLayer,
            string? fillPaint = null)
        {
            bool hasCats = options.XAxis.Categories?.Count > 0;
            double xOffset = hasCats ? step / 2.0 : (series.Data.Count == 1 ? plotWidth / 2.0 : 0);
            double baseY = PaddingTop + plotHeight;

            // A gradient/pattern fill overrides the flat colour and uses its own opacity.
            bool customFill = fillPaint != null;
            string areaFill  = fillPaint ?? Escape(color);
            double areaOp    = customFill ? (series.Fill?.Opacity ?? 1.0) : (series.FillOpacity ?? 0.25);

            // Split into contiguous segments at nulls (or apply GapPolicy.Connect/Zero).
            bool yLog = IsLog(series.YAxisIndex == 1 ? options.YAxis2 : options.YAxis);
            var points   = new List<(double x, double y, double value, int idx)>();
            var segments = new List<List<(double x, double y, double value)>>();
            var current  = new List<(double x, double y, double value)>();
            for (int i = 0; i < series.Data.Count; i++)
            {
                double? rawVal = series.Data[i];
                if (rawVal is null)
                {
                    switch (series.NullGapPolicy)
                    {
                        case Enums.GapPolicy.Break:
                            if (current.Count > 0) { segments.Add(current); current = new List<(double x, double y, double value)>(); }
                            continue;
                        case Enums.GapPolicy.Connect:
                            continue;
                        default: // Zero
                            rawVal = 0.0;
                            break;
                    }
                }
                double v = rawVal!.Value;
                double px = PaddingLeft + xOffset + i * step;
                double py = PaddingTop + plotHeight - Frac(v, yMin, yMax, yLog) * plotHeight;
                current.Add((px, py, v));
                points.Add((px, py, v, i));
            }
            if (current.Count > 0) segments.Add(current);
            if (points.Count == 0) return;

            bool anim   = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string aDur    = anim ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string aEase   = anim ? SmilEasing(options.Animation.Easing) : string.Empty;
            string dashAttr  = BuildDashAttr(series.DashStyle);

            foreach (var seg in segments)
            {
                // Fill area for this segment (down to the baseline at both ends)
                var fill = new StringBuilder();
                fill.Append($"M{seg[0].x.ToString("F1", CultureInfo.InvariantCulture)},{baseY.ToString("F1", CultureInfo.InvariantCulture)}");
                foreach (var (px, py, _) in seg)
                    fill.Append($" L{px.ToString("F1", CultureInfo.InvariantCulture)},{py.ToString("F1", CultureInfo.InvariantCulture)}");
                fill.Append($" L{seg[seg.Count - 1].x.ToString("F1", CultureInfo.InvariantCulture)},{baseY.ToString("F1", CultureInfo.InvariantCulture)} Z");

                if (anim)
                {
                    double targetOp = areaOp;
                    sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{fill}\" fill=\"{areaFill}\" fill-opacity=\"0\" stroke=\"none\">");
                    sb.AppendLine($"    <animate attributeName=\"fill-opacity\" from=\"0\" to=\"{F(targetOp)}\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"  </path>");
                }
                else
                {
                    sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{fill}\" fill=\"{areaFill}\" fill-opacity=\"{F(areaOp)}\" stroke=\"none\"/>");
                }

                // Line on top for this segment
                string linePathD = BuildPath(seg, false);
                if (anim && string.IsNullOrEmpty(series.DashStyle))
                {
                    sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{linePathD}\" fill=\"none\" stroke=\"{Escape(color)}\" stroke-width=\"{series.LineWidth}\" pathLength=\"1\" stroke-dasharray=\"1\" stroke-dashoffset=\"1\">");
                    sb.AppendLine($"    <animate attributeName=\"stroke-dashoffset\" from=\"1\" to=\"0\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"  </path>");
                }
                else
                {
                    sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{linePathD}\" fill=\"none\" stroke=\"{Escape(color)}\" stroke-width=\"{series.LineWidth}\"{dashAttr}/>");
                }
            }

            int nonStackedAreaMarkerR = series.MarkerSize ?? 4;
            double areaPB = PaddingTop + plotHeight;
            foreach (var (px, py, value, idx) in points)
            {
                string xLbl = options.XAxis.Categories?.Count > idx ? options.XAxis.Categories[idx] : idx.ToString(CultureInfo.InvariantCulture);
                AppendDataPoint(sb, tooltipLayer, options.RenderMode, px, py, color, series.Name, value, svgWidth, svgHeight, options.Tooltip, series.MarkerEnabled, nonStackedAreaMarkerR, options.ResolvedBackgroundColor, PaddingTop, areaPB, idx, xLbl, series.MarkerSymbol);
            }

            if (series.AutoInsight != null)
                AppendAutoInsightOverlays(sb, series, color, options, plotWidth, plotHeight, yMin, yMax, step, clipId);
        }

    }
}
