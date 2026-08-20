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
        private static void AppendAnnotations(StringBuilder sb, ChartOptions options,
            int plotWidth, int plotHeight, string clipId)
        {
            bool yLog = IsLog(options.YAxis);
            var (yMin, yMax) = ResolveYBounds(options.YAxis, options.Series);

            var cats = options.XAxis.Categories;
            bool hasCats = cats != null && cats.Count > 0;
            double catStep = hasCats ? (double)plotWidth / cats!.Count : 0;
            double xMin = options.XAxis.Min ?? 0;
            double xMax = options.XAxis.Max ?? 1;
            if (Math.Abs(xMax - xMin) < double.Epsilon) xMax = xMin + 1;

            double MapX(double x) => hasCats
                ? PaddingLeft + (x + 0.5) * catStep
                : PaddingLeft + (x - xMin) / (xMax - xMin) * plotWidth;
            double MapY(double y) => PaddingTop + plotHeight - Frac(y, yMin, yMax, yLog) * plotHeight;

            bool   _anim  = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string _aDur  = _anim ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string _aEase = _anim ? SmilEasing(options.Animation.Easing) : string.Empty;
            if (_anim)
            {
                sb.AppendLine("  <g opacity=\"0\">");
                sb.AppendLine($"    <animate attributeName=\"opacity\" from=\"0\" to=\"1\" dur=\"{_aDur}\" fill=\"freeze\"{_aEase}/>");
            }

            // Resolve label collisions when SmartLayout is active
            var resolved = options.AnnotationLayout?.Enabled == true
                ? ResolveAnnotationCollisions(options.Annotations, MapX, MapY, options.AnnotationLayout)
                : null;

            // Connectors rendered first so they sit beneath the label text
            if (resolved != null && options.AnnotationLayout!.DrawConnectors)
            {
                foreach (var kvp in resolved)
                {
                    if (!kvp.Value.connector) continue;
                    var src = options.Annotations[kvp.Key];
                    string connColor = ApplyAlpha(
                        src.Color ?? options.Theme.TextColor,
                        options.AnnotationLayout.ConnectorOpacity);
                    sb.AppendLine($"  <line x1=\"{F(kvp.Value.ax)}\" y1=\"{F(kvp.Value.ay)}\" x2=\"{F(kvp.Value.lx)}\" y2=\"{F(kvp.Value.ly)}\" stroke=\"{Escape(connColor)}\" stroke-width=\"1\" stroke-dasharray=\"3,2\"/>");
                }
            }

            for (int _ai = 0; _ai < options.Annotations.Count; _ai++)
            {
                var a = options.Annotations[_ai];
                string stroke = Escape(a.Color ?? options.Theme.TextColor);
                string dash   = BuildDashAttr(a.DashStyle);
                double sw     = a.StrokeWidth > 0 ? a.StrokeWidth : 1;

                switch (a.Kind)
                {
                    case AnnotationKind.Line:
                    {
                        double x1 = MapX(a.X), y1 = MapY(a.Y), x2 = MapX(a.X2), y2 = MapY(a.Y2);
                        sb.AppendLine($"  <line clip-path=\"url(#{clipId})\" x1=\"{F(x1)}\" y1=\"{F(y1)}\" x2=\"{F(x2)}\" y2=\"{F(y2)}\" stroke=\"{stroke}\" stroke-width=\"{F(sw)}\"{dash}/>");
                        break;
                    }
                    case AnnotationKind.Rect:
                    {
                        double rx1 = MapX(a.X), ry1 = MapY(a.Y), rx2 = MapX(a.X2), ry2 = MapY(a.Y2);
                        double rx = Math.Min(rx1, rx2), ry = Math.Min(ry1, ry2);
                        double rw = Math.Abs(rx2 - rx1), rh = Math.Abs(ry2 - ry1);
                        // Null background defaults to a subtle theme-tinted wash so rect regions stand out
                        string fill = a.BackgroundColor != null
                            ? Escape(a.BackgroundColor)
                            : ApplyAlpha(options.Theme.AccentColor, 0.10);
                        sb.AppendLine($"  <rect clip-path=\"url(#{clipId})\" x=\"{F(rx)}\" y=\"{F(ry)}\" width=\"{F(rw)}\" height=\"{F(rh)}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{F(sw)}\"{dash}/>");
                        break;
                    }
                    case AnnotationKind.Circle:
                    {
                        double cx = MapX(a.X), cy = MapY(a.Y);
                        string fill = a.BackgroundColor != null ? Escape(a.BackgroundColor) : "none";
                        sb.AppendLine($"  <circle clip-path=\"url(#{clipId})\" cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(a.Radius)}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{F(sw)}\"{dash}/>");
                        break;
                    }
                    case AnnotationKind.Label:
                    default:
                    {
                        double lx, ly;
                        if (resolved != null && resolved.TryGetValue(_ai, out var res))
                        {
                            lx = res.lx;
                            ly = res.ly;
                        }
                        else
                        {
                            lx = MapX(a.X) + a.OffsetX;
                            ly = MapY(a.Y) + a.OffsetY;
                        }

                        string text   = Escape(a.Text ?? string.Empty);
                        string anchor = a.TextAnchor == "start" || a.TextAnchor == "end" ? a.TextAnchor : "middle";

                        // Optional background box sized to the estimated text extent.
                        if (a.BackgroundColor != null && !string.IsNullOrEmpty(a.Text))
                        {
                            double tw = a.Text!.Length * a.FontSize * 0.6 + 10;
                            double th = a.FontSize + 8;
                            double bx = anchor == "start" ? lx - 5
                                      : anchor == "end"   ? lx - tw + 5
                                      :                     lx - tw / 2;
                            double by = ly - a.FontSize + 1;
                            sb.AppendLine($"  <rect x=\"{F(bx)}\" y=\"{F(by)}\" width=\"{F(tw)}\" height=\"{F(th)}\" rx=\"3\" fill=\"{Escape(a.BackgroundColor)}\" stroke=\"{stroke}\" stroke-width=\"{F(sw)}\"/>");
                        }

                        sb.AppendLine($"  <text x=\"{F(lx)}\" y=\"{F(ly)}\" text-anchor=\"{anchor}\" style=\"font: {a.FontSize}px {CssFontFamily(options.Theme.FontFamily)};\" fill=\"{stroke}\">{text}</text>");
                        break;
                    }
                }
            }
            if (_anim) sb.AppendLine("  </g>");
        }

        // ------------------------------------------------------------------ annotation collision detection

        private static bool AnnotationBoxOverlaps(
            double ax, double ay, double aw, double ah,
            double bx, double by, double bw, double bh,
            int hPad, int vPad)
            => ax < bx + bw + hPad && ax + aw > bx - hPad
            && ay < by + bh + vPad && ay + ah > by - vPad;

        private static System.Collections.Generic.Dictionary<int, (double lx, double ly, bool connector, double ax, double ay)>
            ResolveAnnotationCollisions(
                System.Collections.Generic.List<Annotation> annotations,
                System.Func<double, double> mapX,
                System.Func<double, double> mapY,
                Models.AnnotationLayoutOptions lo)
        {
            var result    = new System.Collections.Generic.Dictionary<int, (double, double, bool, double, double)>();
            var obstacles = new System.Collections.Generic.List<(double x, double y, double w, double h)>();
            var placed    = new System.Collections.Generic.List<(double x, double y, double w, double h)>();
            int hPad = lo.HorizontalPadding, vPad = lo.VerticalPadding;

            // Seed fixed obstacles: Circle bounding boxes only.
            // Rect annotations are background shading — labels are intentionally placed on top of them.
            foreach (var a in annotations)
            {
                if (a.Kind == AnnotationKind.Circle)
                {
                    double cx = mapX(a.X), cy = mapY(a.Y);
                    obstacles.Add((cx - a.Radius, cy - a.Radius, a.Radius * 2, a.Radius * 2));
                }
            }

            for (int i = 0; i < annotations.Count; i++)
            {
                var a = annotations[i];
                if (a.Kind != AnnotationKind.Label) continue;

                double origLx  = mapX(a.X) + a.OffsetX;
                double origLy  = mapY(a.Y) + a.OffsetY;
                double tw      = (a.Text?.Length ?? 0) * a.FontSize * 0.6 + 10;
                double th      = a.FontSize + 4;
                double anchorX = mapX(a.X);
                double anchorY = mapY(a.Y);

                // Bounding box top-left given the SVG text-anchor
                double BoxLeft(double lx) => a.TextAnchor == "start" ? lx : a.TextAnchor == "end" ? lx - tw : lx - tw / 2;
                double initBx = BoxLeft(origLx), initBy = origLy - a.FontSize;

                bool AnyCollision(double testBx, double testBy)
                {
                    foreach (var obs in obstacles)
                        if (AnnotationBoxOverlaps(testBx, testBy, tw, th, obs.x, obs.y, obs.w, obs.h, hPad, vPad))
                            return true;
                    foreach (var (px, py, pw, ph) in placed)
                        if (AnnotationBoxOverlaps(testBx, testBy, tw, th, px, py, pw, ph, hPad, vPad))
                            return true;
                    return false;
                }

                double finalBx = initBx, finalBy = initBy;
                bool nudged = false;
                if (AnyCollision(initBx, initBy))
                {
                    bool resolved2 = false;
                    for (int step = 1; step <= 4 && !resolved2; step++)
                    {
                        // Priority: up → right → left → down
                        var candidates = new (double dx, double dy)[]
                        {
                            (0,                     -(step * (th + vPad))),
                            ( step * (tw + hPad),    0),
                            (-(step * (tw + hPad)),  0),
                            (0,                      step * (th + vPad)),
                        };
                        foreach (var (dx, dy) in candidates)
                        {
                            if (!AnyCollision(initBx + dx, initBy + dy))
                            {
                                finalBx = initBx + dx;
                                finalBy = initBy + dy;
                                nudged = resolved2 = true;
                                break;
                            }
                        }
                    }
                }

                // Convert box back to SVG text anchor point
                double finalLx = a.TextAnchor == "start" ? finalBx
                               : a.TextAnchor == "end"   ? finalBx + tw
                               :                           finalBx + tw / 2;
                double finalLy = finalBy + a.FontSize;

                placed.Add((finalBx, finalBy, tw, th));
                result[i] = (finalLx, finalLy, nudged && lo.DrawConnectors, anchorX, anchorY);
            }
            return result;
        }

        // ------------------------------------------------------------------ Candlestick

        private static void AppendCandlestickSeries(StringBuilder sb, Series series, string color,            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            double yMin, double yMax, string clipId, StringBuilder tooltipLayer)
        {
            var pts = series.OhlcData;
            if (pts.Count == 0) return;
            int n = pts.Count;

            bool yLog = IsLog(options.YAxis);
            bool hasCats = options.XAxis.Categories?.Count > 0;
            int catCount = hasCats ? options.XAxis.Categories!.Count : n;
            double groupW = (double)plotWidth / catCount;
            double bodyW  = groupW * 0.5;
            double bodyPad = (groupW - bodyW) / 2.0;

            double Y(double v) => PaddingTop + plotHeight - Frac(v, yMin, yMax, yLog) * plotHeight;

            // Up (close ≥ open) vs down colours. BorderColor overrides the wick/outline colour;
            // Color overrides the up-body fill.
            string upFill   = series.Color ?? options.Theme.PositiveColor;
            string downFill = options.Theme.NegativeColor;
            string wick     = !string.IsNullOrEmpty(series.BorderColor) ? Escape(series.BorderColor!) : null!;
            double strokeW  = series.BorderWidth > 0 ? series.BorderWidth : 1.0;

            bool   anim  = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string aDur  = anim ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string aEase = anim ? SmilEasing(options.Animation.Easing) : string.Empty;

            for (int i = 0; i < n; i++)
            {
                var p   = pts[i];
                bool up = p.Close >= p.Open;
                string fill = up ? Escape(upFill) : Escape(downFill);
                string line = wick ?? fill;
                double cx  = PaddingLeft + groupW * i + groupW / 2.0;
                double xL  = PaddingLeft + groupW * i + bodyPad;
                double yHi = Y(p.High), yLo = Y(p.Low);
                double yOpen = Y(p.Open), yClose = Y(p.Close);
                double bodyTop = Math.Min(yOpen, yClose);
                double bodyH   = Math.Max(1.0, Math.Abs(yOpen - yClose));

                double bodyMid = bodyTop + bodyH / 2.0;
                if (anim)
                {
                    // Wick grows from body centre outward; body grows from its centre
                    sb.AppendLine($"  <line clip-path=\"url(#{clipId})\" x1=\"{F(cx)}\" y1=\"{F(bodyMid)}\" x2=\"{F(cx)}\" y2=\"{F(bodyMid)}\" stroke=\"{line}\" stroke-width=\"{F(strokeW)}\">");
                    sb.AppendLine($"    <animate attributeName=\"y1\" from=\"{F(bodyMid)}\" to=\"{F(yHi)}\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"    <animate attributeName=\"y2\" from=\"{F(bodyMid)}\" to=\"{F(yLo)}\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"  </line>");
                    sb.AppendLine($"  <rect clip-path=\"url(#{clipId})\" x=\"{F(xL)}\" y=\"{F(bodyMid)}\" width=\"{F(bodyW)}\" height=\"0\" fill=\"{fill}\" stroke=\"{line}\" stroke-width=\"{F(strokeW)}\">");
                    sb.AppendLine($"    <animate attributeName=\"height\" from=\"0\" to=\"{F(bodyH)}\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"    <animate attributeName=\"y\" from=\"{F(bodyMid)}\" to=\"{F(bodyTop)}\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"  </rect>");
                }
                else
                {
                    // High-low wick behind the body
                    sb.AppendLine($"  <line clip-path=\"url(#{clipId})\" x1=\"{F(cx)}\" y1=\"{F(yHi)}\" x2=\"{F(cx)}\" y2=\"{F(yLo)}\" stroke=\"{line}\" stroke-width=\"{F(strokeW)}\"/>");
                    // Open-close body
                    sb.AppendLine($"  <rect clip-path=\"url(#{clipId})\" x=\"{F(xL)}\" y=\"{F(bodyTop)}\" width=\"{F(bodyW)}\" height=\"{F(bodyH)}\" fill=\"{fill}\" stroke=\"{line}\" stroke-width=\"{F(strokeW)}\"/>");
                }

                if (options.RenderMode != SvgMode.Static && options.Tooltip.Enabled)
                {
                    tooltipLayer.AppendLine("  <g class=\"data-point\">");
                    tooltipLayer.AppendLine($"    <rect x=\"{F(xL)}\" y=\"{F(yHi)}\" width=\"{F(bodyW)}\" height=\"{F(Math.Max(1.0, yLo - yHi))}\" class=\"hit-area\" stroke=\"none\"/>");
                    AppendTooltip(tooltipLayer, cx, bodyTop, series.Name, p.Close, svgWidth, svgHeight, options.Tooltip, up ? upFill : downFill);
                    tooltipLayer.AppendLine("  </g>");
                }
            }
        }

        // ------------------------------------------------------------------ OHLC

        private static void AppendOhlcSeries(StringBuilder sb, Series series, string color,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            double yMin, double yMax, string clipId, StringBuilder tooltipLayer)
        {
            var pts = series.OhlcData;
            if (pts.Count == 0) return;
            int n = pts.Count;

            bool yLog = IsLog(options.YAxis);
            bool hasCats = options.XAxis.Categories?.Count > 0;
            int catCount = hasCats ? options.XAxis.Categories!.Count : n;
            double groupW = (double)plotWidth / catCount;
            double tickW  = groupW * 0.25;

            double Y(double v) => PaddingTop + plotHeight - Frac(v, yMin, yMax, yLog) * plotHeight;

            string upColor   = series.Color ?? options.Theme.PositiveColor;
            string downColor = options.Theme.NegativeColor;
            double strokeW   = series.BorderWidth > 0 ? series.BorderWidth : 1.5;

            bool   anim  = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string aDur  = anim ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string aEase = anim ? SmilEasing(options.Animation.Easing) : string.Empty;

            for (int i = 0; i < n; i++)
            {
                var p   = pts[i];
                bool up = p.Close >= p.Open;
                string stroke = Escape(up ? upColor : downColor);
                double cx  = PaddingLeft + groupW * i + groupW / 2.0;
                double yHi = Y(p.High), yLo = Y(p.Low);
                double yOpen = Y(p.Open), yClose = Y(p.Close);

                // Vertical high-low bar, left open tick, right close tick
                double barMid = (yHi + yLo) / 2.0;
                if (anim)
                {
                    // Vertical bar grows from midpoint outward; ticks slide out from centre
                    sb.AppendLine($"  <line clip-path=\"url(#{clipId})\" x1=\"{F(cx)}\" y1=\"{F(barMid)}\" x2=\"{F(cx)}\" y2=\"{F(barMid)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\">");
                    sb.AppendLine($"    <animate attributeName=\"y1\" from=\"{F(barMid)}\" to=\"{F(yHi)}\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"    <animate attributeName=\"y2\" from=\"{F(barMid)}\" to=\"{F(yLo)}\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"  </line>");
                    sb.AppendLine($"  <line clip-path=\"url(#{clipId})\" x1=\"{F(cx)}\" y1=\"{F(yOpen)}\" x2=\"{F(cx)}\" y2=\"{F(yOpen)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\">");
                    sb.AppendLine($"    <animate attributeName=\"x1\" from=\"{F(cx)}\" to=\"{F(cx - tickW)}\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"  </line>");
                    sb.AppendLine($"  <line clip-path=\"url(#{clipId})\" x1=\"{F(cx)}\" y1=\"{F(yClose)}\" x2=\"{F(cx)}\" y2=\"{F(yClose)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\">");
                    sb.AppendLine($"    <animate attributeName=\"x2\" from=\"{F(cx)}\" to=\"{F(cx + tickW)}\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"  </line>");
                }
                else
                {
                    sb.AppendLine($"  <line clip-path=\"url(#{clipId})\" x1=\"{F(cx)}\" y1=\"{F(yHi)}\" x2=\"{F(cx)}\" y2=\"{F(yLo)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\"/>");
                    sb.AppendLine($"  <line clip-path=\"url(#{clipId})\" x1=\"{F(cx - tickW)}\" y1=\"{F(yOpen)}\" x2=\"{F(cx)}\" y2=\"{F(yOpen)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\"/>");
                    sb.AppendLine($"  <line clip-path=\"url(#{clipId})\" x1=\"{F(cx)}\" y1=\"{F(yClose)}\" x2=\"{F(cx + tickW)}\" y2=\"{F(yClose)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\"/>");
                }

                if (options.RenderMode != SvgMode.Static && options.Tooltip.Enabled)
                {
                    tooltipLayer.AppendLine("  <g class=\"data-point\">");
                    tooltipLayer.AppendLine($"    <rect x=\"{F(cx - tickW)}\" y=\"{F(yHi)}\" width=\"{F(tickW * 2)}\" height=\"{F(Math.Max(1.0, yLo - yHi))}\" class=\"hit-area\" stroke=\"none\"/>");
                    AppendTooltip(tooltipLayer, cx, yHi, series.Name, p.Close, svgWidth, svgHeight, options.Tooltip, up ? upColor : downColor);
                    tooltipLayer.AppendLine("  </g>");
                }
            }
        }

    }
}
