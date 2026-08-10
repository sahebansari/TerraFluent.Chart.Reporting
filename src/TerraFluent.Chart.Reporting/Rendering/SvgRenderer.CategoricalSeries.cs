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
        private static void AppendFunnelSeries(StringBuilder sb, Series series, string color,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            StringBuilder tooltipLayer)
        {
            int n = series.Data.Count;
            if (n == 0) return;

            // Collect non-null values
            var values = new List<(int idx, double value)>();
            for (int i = 0; i < n; i++)
                if (series.Data[i].HasValue) values.Add((i, series.Data[i]!.Value));
            if (values.Count == 0) return;

            double maxVal = 0;
            foreach (var (_, v) in values) if (v > maxVal) maxVal = v;
            if (maxVal <= 0) maxVal = 1;

            double maxFunnelW = plotWidth * 0.80;
            double stageH     = (double)plotHeight / values.Count;
            double cx         = PaddingLeft + plotWidth / 2.0;

            bool   anim  = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string dur   = anim ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string ease  = anim ? SmilEasing(options.Animation.Easing) : string.Empty;
            // Stagger: each stage starts 40% of total duration / n after the previous
            double stagger = anim && values.Count > 1 ? options.Animation.Duration.TotalSeconds * 0.4 / values.Count : 0;

            for (int i = 0; i < values.Count; i++)
            {
                var (idx, val) = values[i];
                double topW    = maxFunnelW * (val / maxVal);
                double botW    = i + 1 < values.Count ? maxFunnelW * (values[i + 1].value / maxVal) : topW * 0.85;
                double yTop    = PaddingTop + i * stageH;
                double yBot    = yTop + stageH - 2; // 2px gap between stages

                string stageColor = series.Color ?? options.Theme.Colors[i % options.Theme.Colors.Length];
                string label = idx < options.XAxis.Categories.Count ? options.XAxis.Categories[idx] : string.Empty;

                string trapD = $"M{F(cx - topW / 2)},{F(yTop)} L{F(cx + topW / 2)},{F(yTop)} L{F(cx + botW / 2)},{F(yBot)} L{F(cx - botW / 2)},{F(yBot)} Z";

                if (anim)
                {
                    string begin = stagger > 0 ? $" begin=\"{F(i * stagger)}s\"" : string.Empty;
                    sb.AppendLine($"  <path d=\"{trapD}\" fill=\"{Escape(stageColor)}\" fill-opacity=\"0\" stroke=\"{Escape(options.ResolvedBackgroundColor)}\" stroke-width=\"1\">");
                    sb.AppendLine($"    <animate attributeName=\"fill-opacity\" from=\"0\" to=\"0.9\"{begin} dur=\"{dur}\" fill=\"freeze\"{ease}/>");
                    sb.AppendLine($"  </path>");
                }
                else
                {
                    sb.AppendLine($"  <path d=\"{trapD}\" fill=\"{Escape(stageColor)}\" fill-opacity=\"0.9\" stroke=\"{Escape(options.ResolvedBackgroundColor)}\" stroke-width=\"1\"/>");
                }

                // Stage label (left) and value (right)
                double labelY = yTop + stageH / 2 + 4;
                if (!string.IsNullOrEmpty(label))
                    sb.AppendLine($"  <text class=\"axis-label\" x=\"{F(cx - topW / 2 - 6)}\" y=\"{F(labelY)}\" text-anchor=\"end\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(label)}</text>");

                if (series.DataLabel.Enabled)
                    AppendDataLabel(sb, cx, labelY, FormatDataLabel(val, series.DataLabel.FormatString),
                        series.DataLabel.TextColor ?? ChartColor.White, series.DataLabel.BackgroundColor, series.DataLabel.TextFontSize);

                if (options.RenderMode != SvgMode.Static)
                {
                    tooltipLayer.AppendLine($"  <g class=\"data-point\">");
                    tooltipLayer.AppendLine($"    <path d=\"{trapD}\" class=\"hit-area\" stroke=\"none\"/>");
                    AppendTooltip(tooltipLayer, cx, yTop + stageH / 2, string.IsNullOrEmpty(label) ? series.Name : label, val, svgWidth, svgHeight, options.Tooltip, color);
                    tooltipLayer.AppendLine($"  </g>");
                }
            }
        }

        // ------------------------------------------------------------------ Treemap

        private static void AppendTreemapSeries(StringBuilder sb, Series series,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            StringBuilder tooltipLayer)
        {
            int n = series.Data.Count;
            if (n == 0) return;

            var items = new List<(string Label, double Value, string Color)>();
            for (int i = 0; i < n; i++)
            {
                if (!series.Data[i].HasValue || series.Data[i]!.Value <= 0) continue;
                string label = i < options.XAxis.Categories.Count ? options.XAxis.Categories[i] : $"Item {i + 1}";
                string c     = series.Color ?? options.Theme.Colors[i % options.Theme.Colors.Length];
                items.Add((label, series.Data[i]!.Value, c));
            }
            if (items.Count == 0) return;

            // Sort descending by value for better aspect ratios
            items.Sort((a, b) => b.Value.CompareTo(a.Value));

            var cells = new List<(double X, double Y, double W, double H, string Label, double Value, string Color)>();
            TreemapSplit(items, 0, items.Count - 1,
                PaddingLeft, PaddingTop, plotWidth, plotHeight, cells);

            bool   anim = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string dur  = anim ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string ease = anim ? SmilEasing(options.Animation.Easing) : string.Empty;

            foreach (var cell in cells)
            {
                if (cell.W < 1 || cell.H < 1) continue;

                if (anim)
                {
                    sb.AppendLine($"  <rect x=\"{F(cell.X)}\" y=\"{F(cell.Y)}\" width=\"{F(cell.W)}\" height=\"{F(cell.H)}\" fill=\"{Escape(cell.Color)}\" fill-opacity=\"0\" stroke=\"{Escape(options.ResolvedBackgroundColor)}\" stroke-width=\"2\" rx=\"2\">");
                    sb.AppendLine($"    <animate attributeName=\"fill-opacity\" from=\"0\" to=\"0.85\" dur=\"{dur}\" fill=\"freeze\"{ease}/>");
                    sb.AppendLine($"  </rect>");
                }
                else
                {
                    sb.AppendLine($"  <rect x=\"{F(cell.X)}\" y=\"{F(cell.Y)}\" width=\"{F(cell.W)}\" height=\"{F(cell.H)}\" fill=\"{Escape(cell.Color)}\" fill-opacity=\"0.85\" stroke=\"{Escape(options.ResolvedBackgroundColor)}\" stroke-width=\"2\" rx=\"2\"/>");
                }

                // Label inside the cell when large enough
                if (cell.W > 30 && cell.H > 18)
                {
                    double lx = cell.X + cell.W / 2;
                    double ly = cell.Y + cell.H / 2;
                    bool showValue = cell.H > 36;
                    if (showValue)
                    {
                        sb.AppendLine($"  <text x=\"{F(lx)}\" y=\"{F(ly - 6)}\" text-anchor=\"middle\" dominant-baseline=\"central\" font-size=\"11\" font-weight=\"bold\" fill=\"{ChartColor.White}\" style=\"pointer-events:none\">{Escape(cell.Label)}</text>");
                        sb.AppendLine($"  <text x=\"{F(lx)}\" y=\"{F(ly + 8)}\" text-anchor=\"middle\" dominant-baseline=\"central\" font-size=\"10\" fill=\"{ChartColor.WithOpacity(ChartColor.White, 0.75)}\" style=\"pointer-events:none\">{Escape(FormatTick(cell.Value))}</text>");
                    }
                    else
                    {
                        sb.AppendLine($"  <text x=\"{F(lx)}\" y=\"{F(ly)}\" text-anchor=\"middle\" dominant-baseline=\"central\" font-size=\"11\" font-weight=\"bold\" fill=\"{ChartColor.White}\" style=\"pointer-events:none\">{Escape(cell.Label)}</text>");
                    }
                }

                if (options.RenderMode != SvgMode.Static)
                {
                    tooltipLayer.AppendLine($"  <g class=\"data-point\">");
                    tooltipLayer.AppendLine($"    <rect x=\"{F(cell.X)}\" y=\"{F(cell.Y)}\" width=\"{F(cell.W)}\" height=\"{F(cell.H)}\" class=\"hit-area\" stroke=\"none\" rx=\"2\"/>");
                    AppendTooltip(tooltipLayer, cell.X + cell.W / 2, cell.Y + cell.H / 2, cell.Label, cell.Value, svgWidth, svgHeight, options.Tooltip, cell.Color);
                    tooltipLayer.AppendLine($"  </g>");
                }
            }
        }

        // ================================================================== Parliament (hemicycle)

        /// <summary>
        /// Renders a Parliament (hemicycle) chart.
        /// Each party occupies a pizza-slice wedge: within every ring the dots are
        /// distributed proportionally (Largest Remainder Method) so party boundaries
        /// stay radially aligned across all rings.  Dot spacing is auto-computed so
        /// the total count matches <c>totalSeats</c> exactly.
        /// In Interactive mode the legend items are clickable (toggle party) and
        /// hovering a party group shows a tooltip with name and seat count.
        /// </summary>
        // ------------------------------------------------------------------ Radar / polar

        private static void AppendRadarSeries(StringBuilder sb, Series series, string color,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight, StringBuilder tooltipLayer)
        {
            int axisCount = options.XAxis.Categories != null && options.XAxis.Categories.Count > 0
                ? options.XAxis.Categories.Count
                : series.Data.Count;
            if (axisCount < 1) return;

            double cx = PaddingLeft + plotWidth / 2.0;
            double cy = PaddingTop + plotHeight / 2.0;
            double radius = Math.Min(plotWidth, plotHeight) / 2.0 * 0.72;

            var radarList = options.Series.FindAll(s => s.Visible && s.Type == ChartType.Radar);
            double minVal = options.YAxis.Min ?? 0;
            double maxVal = options.YAxis.Max ?? ComputeYMax(radarList);
            if (maxVal <= minVal) maxVal = minVal + 1;

            double Ang(int k) => -Math.PI / 2 + 2 * Math.PI * k / axisCount;
            double Rad(double v)
            {
                double f = (v - minVal) / (maxVal - minVal);
                if (f < 0) f = 0; else if (f > 1) f = 1;
                return radius * f;
            }

            // Grid, spokes and labels are drawn once — for the first visible radar series only.
            bool drawGrid = ReferenceEquals(
                options.Series.Find(s => s.Visible && s.Type == ChartType.Radar), series);
            if (drawGrid)
            {
                string gc = options.YAxis.GridLineColor ?? options.Theme.GridLineColor;
                const int ringCount = 4;

                for (int r = 1; r <= ringCount; r++)
                {
                    double rr = radius * r / ringCount;
                    var ring = new StringBuilder();
                    for (int k = 0; k < axisCount; k++)
                    {
                        double a = Ang(k);
                        ring.Append(k == 0 ? "M" : "L")
                            .Append(F(cx + rr * Math.Cos(a))).Append(' ')
                            .Append(F(cy + rr * Math.Sin(a))).Append(' ');
                    }
                    ring.Append('Z');
                    sb.AppendLine($"  <path class=\"grid-line\" d=\"{ring}\" fill=\"none\" stroke=\"{Escape(gc)}\"/>");

                    double val = minVal + (maxVal - minVal) * r / ringCount;
                    sb.AppendLine($"  <text class=\"axis-label\" x=\"{F(cx - 4)}\" y=\"{F(cy - rr + 4)}\" text-anchor=\"end\" fill=\"{Escape(options.Theme.TextColor)}\">{FormatTick(val)}</text>");
                }

                for (int k = 0; k < axisCount; k++)
                {
                    double a = Ang(k);
                    double ox = cx + radius * Math.Cos(a);
                    double oy = cy + radius * Math.Sin(a);
                    sb.AppendLine($"  <line class=\"grid-line\" stroke=\"{Escape(gc)}\" x1=\"{F(cx)}\" y1=\"{F(cy)}\" x2=\"{F(ox)}\" y2=\"{F(oy)}\"/>");

                    string label = (options.XAxis.Categories != null && k < options.XAxis.Categories.Count)
                        ? options.XAxis.Categories[k]
                        : (k + 1).ToString(CultureInfo.InvariantCulture);
                    double ca = Math.Cos(a), sa = Math.Sin(a);
                    double lx = cx + (radius + 14) * ca;
                    double ly = cy + (radius + 14) * sa;
                    string anchor = ca > 0.3 ? "start" : (ca < -0.3 ? "end" : "middle");
                    double dy = sa > 0.3 ? 10 : (sa < -0.3 ? -2 : 4);
                    sb.AppendLine($"  <text class=\"axis-label\" x=\"{F(lx)}\" y=\"{F(ly + dy)}\" text-anchor=\"{anchor}\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(label)}</text>");
                }
            }

            // Series polygon
            var path  = new StringBuilder();
            var verts = new List<(double x, double y, double v)>();
            for (int k = 0; k < axisCount; k++)
            {
                double v = (k < series.Data.Count && series.Data[k].HasValue) ? series.Data[k]!.Value : minVal;
                double a = Ang(k);
                double rr = Rad(v);
                double x = cx + rr * Math.Cos(a);
                double y = cy + rr * Math.Sin(a);
                path.Append(k == 0 ? "M" : "L").Append(F(x)).Append(' ').Append(F(y)).Append(' ');
                verts.Add((x, y, v));
            }
            path.Append('Z');

            double fillOpacity = series.FillOpacity ?? 0.25;
            int lineWidth = series.LineWidth > 0 ? series.LineWidth : 2;
            bool   anim  = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string aDur  = anim ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string aEase = anim ? SmilEasing(options.Animation.Easing) : string.Empty;

            // Polygon and markers fade in together as a group
            if (anim)
            {
                sb.AppendLine($"  <g opacity=\"0\">");
                sb.AppendLine($"    <animate attributeName=\"opacity\" from=\"0\" to=\"1\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
            }
            sb.AppendLine($"  <path d=\"{path}\" fill=\"{Escape(color)}\" fill-opacity=\"{F(fillOpacity)}\" stroke=\"{Escape(color)}\" stroke-width=\"{lineWidth}\" stroke-linejoin=\"round\"/>");

            if (series.MarkerEnabled != false)
            {
                int mr = series.MarkerSize ?? 3;
                foreach (var (x, y, _) in verts)
                    sb.AppendLine($"  <circle cx=\"{F(x)}\" cy=\"{F(y)}\" r=\"{mr}\" fill=\"{Escape(color)}\" stroke=\"{Escape(options.ResolvedBackgroundColor)}\" stroke-width=\"1\"/>");
            }
            if (anim) sb.AppendLine("  </g>");

            if (options.Tooltip.Enabled)
            {
                foreach (var (x, y, v) in verts)
                {
                    tooltipLayer.AppendLine("  <g class=\"data-point\">");
                    tooltipLayer.AppendLine($"    <circle cx=\"{F(x)}\" cy=\"{F(y)}\" r=\"10\" class=\"hit-area\" stroke=\"none\"/>");
                    AppendTooltip(tooltipLayer, x, y - 10, series.Name, v, svgWidth, svgHeight, options.Tooltip, color);
                    tooltipLayer.AppendLine("  </g>");
                }
            }
        }

        // ------------------------------------------------------------------ BoxPlot

        private static void AppendBoxPlotSeries(StringBuilder sb, Series series, string color,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            double yMin, double yMax, string clipId, StringBuilder tooltipLayer)
        {
            var pts = series.BoxPlotData;
            if (pts.Count == 0) return;
            int n = pts.Count;

            bool yLog = IsLog(options.YAxis);
            bool hasCats = options.XAxis.Categories?.Count > 0;
            int catCount = hasCats ? options.XAxis.Categories!.Count : n;
            double groupW = (double)plotWidth / catCount;
            double boxW   = groupW * 0.5;
            double boxPad = (groupW - boxW) / 2.0;

            double Y(double v) => PaddingTop + plotHeight - Frac(v, yMin, yMax, yLog) * plotHeight;

            string stroke = !string.IsNullOrEmpty(series.BorderColor) ? Escape(series.BorderColor!) : Escape(color);
            double strokeW = series.BorderWidth > 0 ? series.BorderWidth : 1.5;
            double fillOpacity = series.FillOpacity ?? 0.35;

            bool   anim  = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string aDur  = anim ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string aEase = anim ? SmilEasing(options.Animation.Easing) : string.Empty;

            for (int i = 0; i < n; i++)
            {
                var p  = pts[i];
                double cx  = PaddingLeft + groupW * i + groupW / 2.0;
                double xL  = PaddingLeft + groupW * i + boxPad;
                double yLo = Y(p.Low), yQ1 = Y(p.Q1), yMed = Y(p.Median), yQ3 = Y(p.Q3), yHi = Y(p.High);
                double capW = boxW * 0.4;

                // Interquartile box coordinates (needed by both animated and static paths)
                double boxTop = Math.Min(yQ1, yQ3);
                double boxH   = Math.Max(1.0, Math.Abs(yQ1 - yQ3));
                double boxMid = boxTop + boxH / 2.0;

                if (anim)
                {
                    // Whisker grows from box centre outward; caps slide out; box grows from its centre
                    sb.AppendLine($"  <line x1=\"{F(cx)}\" y1=\"{F(boxMid)}\" x2=\"{F(cx)}\" y2=\"{F(boxMid)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\">");
                    sb.AppendLine($"    <animate attributeName=\"y1\" from=\"{F(boxMid)}\" to=\"{F(yHi)}\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"    <animate attributeName=\"y2\" from=\"{F(boxMid)}\" to=\"{F(yLo)}\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"  </line>");
                    sb.AppendLine($"  <line x1=\"{F(cx)}\" y1=\"{F(yHi)}\" x2=\"{F(cx)}\" y2=\"{F(yHi)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\">");
                    sb.AppendLine($"    <animate attributeName=\"x1\" from=\"{F(cx)}\" to=\"{F(cx - capW)}\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"    <animate attributeName=\"x2\" from=\"{F(cx)}\" to=\"{F(cx + capW)}\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"  </line>");
                    sb.AppendLine($"  <line x1=\"{F(cx)}\" y1=\"{F(yLo)}\" x2=\"{F(cx)}\" y2=\"{F(yLo)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\">");
                    sb.AppendLine($"    <animate attributeName=\"x1\" from=\"{F(cx)}\" to=\"{F(cx - capW)}\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"    <animate attributeName=\"x2\" from=\"{F(cx)}\" to=\"{F(cx + capW)}\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"  </line>");
                    sb.AppendLine($"  <rect x=\"{F(xL)}\" y=\"{F(boxMid)}\" width=\"{F(boxW)}\" height=\"0\" fill=\"{Escape(color)}\" fill-opacity=\"{F(fillOpacity)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\">");
                    sb.AppendLine($"    <animate attributeName=\"height\" from=\"0\" to=\"{F(boxH)}\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"    <animate attributeName=\"y\" from=\"{F(boxMid)}\" to=\"{F(boxTop)}\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"  </rect>");
                    sb.AppendLine($"  <line x1=\"{F(xL)}\" y1=\"{F(yMed)}\" x2=\"{F(xL + boxW)}\" y2=\"{F(yMed)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW + 0.75)}\" opacity=\"0\">");
                    sb.AppendLine($"    <animate attributeName=\"opacity\" from=\"0\" to=\"1\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                    sb.AppendLine($"  </line>");
                }
                else
                {
                    // Whisker (Low → High) with end caps
                    sb.AppendLine($"  <line x1=\"{F(cx)}\" y1=\"{F(yHi)}\" x2=\"{F(cx)}\" y2=\"{F(yLo)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\"/>");
                    sb.AppendLine($"  <line x1=\"{F(cx - capW)}\" y1=\"{F(yHi)}\" x2=\"{F(cx + capW)}\" y2=\"{F(yHi)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\"/>");
                    sb.AppendLine($"  <line x1=\"{F(cx - capW)}\" y1=\"{F(yLo)}\" x2=\"{F(cx + capW)}\" y2=\"{F(yLo)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\"/>");
                    sb.AppendLine($"  <rect x=\"{F(xL)}\" y=\"{F(boxTop)}\" width=\"{F(boxW)}\" height=\"{F(boxH)}\" fill=\"{Escape(color)}\" fill-opacity=\"{F(fillOpacity)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\"/>");
                    sb.AppendLine($"  <line x1=\"{F(xL)}\" y1=\"{F(yMed)}\" x2=\"{F(xL + boxW)}\" y2=\"{F(yMed)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW + 0.75)}\"/>");
                }

                if (options.RenderMode != SvgMode.Static && options.Tooltip.Enabled)
                {
                    tooltipLayer.AppendLine("  <g class=\"data-point\">");
                    tooltipLayer.AppendLine($"    <rect x=\"{F(xL)}\" y=\"{F(yHi)}\" width=\"{F(boxW)}\" height=\"{F(Math.Max(1.0, yLo - yHi))}\" class=\"hit-area\" stroke=\"none\"/>");
                    AppendTooltip(tooltipLayer, cx, yMed, series.Name, p.Median, svgWidth, svgHeight, options.Tooltip, color);
                    tooltipLayer.AppendLine("  </g>");
                }
            }
        }

        // ------------------------------------------------------------------ ErrorBar

        private static void AppendErrorBarSeries(StringBuilder sb, Series series, string color,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            double yMin, double yMax, string clipId, StringBuilder tooltipLayer)
        {
            var pts = series.RangeData;
            if (pts.Count == 0) return;
            int n = pts.Count;

            bool yLog = IsLog(options.YAxis);
            bool hasCats = options.XAxis.Categories?.Count > 0;
            int catCount = hasCats ? options.XAxis.Categories!.Count : n;
            double groupW = (double)plotWidth / catCount;
            double capW   = groupW * 0.18;

            double Y(double v) => PaddingTop + plotHeight - Frac(v, yMin, yMax, yLog) * plotHeight;

            string stroke = !string.IsNullOrEmpty(series.BorderColor) ? Escape(series.BorderColor!) : Escape(color);
            double strokeW = series.BorderWidth > 0 ? series.BorderWidth : 2;

            for (int i = 0; i < n; i++)
            {
                var p  = pts[i];
                double cx  = PaddingLeft + groupW * i + groupW / 2.0;
                double yHi = Y(Math.Max(p.Low, p.High));
                double yLo = Y(Math.Min(p.Low, p.High));

                sb.AppendLine($"  <line x1=\"{F(cx)}\" y1=\"{F(yHi)}\" x2=\"{F(cx)}\" y2=\"{F(yLo)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\"/>");
                sb.AppendLine($"  <line x1=\"{F(cx - capW)}\" y1=\"{F(yHi)}\" x2=\"{F(cx + capW)}\" y2=\"{F(yHi)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\"/>");
                sb.AppendLine($"  <line x1=\"{F(cx - capW)}\" y1=\"{F(yLo)}\" x2=\"{F(cx + capW)}\" y2=\"{F(yLo)}\" stroke=\"{stroke}\" stroke-width=\"{F(strokeW)}\"/>");

                if (options.RenderMode != SvgMode.Static && options.Tooltip.Enabled)
                {
                    tooltipLayer.AppendLine("  <g class=\"data-point\">");
                    tooltipLayer.AppendLine($"    <rect x=\"{F(cx - capW)}\" y=\"{F(yHi)}\" width=\"{F(capW * 2)}\" height=\"{F(Math.Max(1.0, yLo - yHi))}\" class=\"hit-area\" stroke=\"none\"/>");
                    AppendTooltip(tooltipLayer, cx, yHi, series.Name, Math.Max(p.Low, p.High), svgWidth, svgHeight, options.Tooltip, color);
                    tooltipLayer.AppendLine("  </g>");
                }
            }
        }

    }
}
