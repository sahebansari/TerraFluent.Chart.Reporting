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
        private static void AppendDumbbellSeries(StringBuilder sb, Series series, string color,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            double yMin, double yMax, string clipId, StringBuilder tooltipLayer)
        {
            var pts = series.RangeData;
            if (pts.Count == 0) return;
            int n = pts.Count;
            bool yLog = IsLog(options.YAxis);

            bool hasCats = options.XAxis.Categories?.Count > 0;
            int catCount = hasCats ? options.XAxis.Categories!.Count : n;
            double step = (double)plotWidth / catCount;
            double dotR = series.MarkerSize ?? 7;

            for (int i = 0; i < n; i++)
            {
                double cx   = PaddingLeft + (i + 0.5) * step;
                double ptLo = Math.Min(pts[i].Low, pts[i].High);
                double ptHi = Math.Max(pts[i].Low, pts[i].High);

                double yHi = PaddingTop + plotHeight * (1.0 - Frac(ptHi, yMin, yMax, yLog));
                double yLo = PaddingTop + plotHeight * (1.0 - Frac(ptLo, yMin, yMax, yLog));

                // Connector
                sb.AppendLine($"  <line x1=\"{F(cx)}\" y1=\"{F(yHi)}\" x2=\"{F(cx)}\" y2=\"{F(yLo)}\" stroke=\"{Escape(color)}\" stroke-width=\"3\" stroke-opacity=\"0.4\" clip-path=\"url(#{clipId})\"/>");
                // Low dot (faded)
                sb.AppendLine($"  <circle cx=\"{F(cx)}\" cy=\"{F(yLo)}\" r=\"{F(dotR)}\" fill=\"{Escape(color)}\" fill-opacity=\"0.4\" clip-path=\"url(#{clipId})\"/>");
                // High dot (solid)
                sb.AppendLine($"  <circle cx=\"{F(cx)}\" cy=\"{F(yHi)}\" r=\"{F(dotR)}\" fill=\"{Escape(color)}\" clip-path=\"url(#{clipId})\"/>");

                if (series.DataLabel.Enabled)
                {
                    string txt = FormatDataLabel(ptHi, series.DataLabel.FormatString);
                    AppendDataLabel(sb, cx, yHi - dotR - 4 + series.DataLabel.VerticalOffset.GetValueOrDefault(),
                        txt, series.DataLabel.TextColor ?? options.Theme.TextColor,
                        series.DataLabel.BackgroundColor, series.DataLabel.TextFontSize);
                }

                if (options.RenderMode != SvgMode.Static)
                {
                    double hitH = Math.Max(Math.Abs(yLo - yHi) + dotR * 2, dotR * 4);
                    tooltipLayer.AppendLine("  <g class=\"data-point\">");
                    AppendHoverBandV(tooltipLayer, options, cx, step, plotHeight);
                    tooltipLayer.AppendLine($"  <rect x=\"{F(cx - step / 2)}\" y=\"{F(yHi - dotR)}\" width=\"{F(step)}\" height=\"{F(hitH)}\" class=\"hit-area\" stroke=\"none\"/>");
                    AppendTooltip(tooltipLayer, cx, yHi - dotR - 10,
                        $"{series.Name}: {FormatTick(ptLo)}\u2013{FormatTick(ptHi)}", ptHi, svgWidth, svgHeight,
                        options.Tooltip, color, PaddingTop, PaddingTop + plotHeight);
                    tooltipLayer.AppendLine("  </g>");
                }
            }
        }

        // ------------------------------------------------------------------ Stream

        private static void AppendStreamSeries(StringBuilder sb, Series thisSeries, string color,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            string clipId, StringBuilder tooltipLayer)
        {
            var allStream = options.Series.FindAll(s => s.Visible && s.Type == ChartType.Stream);
            int si = allStream.IndexOf(thisSeries);
            if (si < 0) return;

            int n = 0;
            foreach (var s in allStream) if (s.Data.Count > n) n = s.Data.Count;
            if (n == 0) return;

            // Wiggle baseline: center the total stack at y=0
            var totals = new double[n];
            foreach (var s in allStream)
                for (int i = 0; i < n; i++)
                    totals[i] += (i < s.Data.Count && s.Data[i].HasValue) ? s.Data[i]!.Value : 0;

            double maxTotal = 0;
            for (int i = 0; i < n; i++) if (totals[i] > maxTotal) maxTotal = totals[i];
            double streamYMin = -maxTotal / 2.0;
            double streamYMax =  maxTotal / 2.0;
            if (Math.Abs(streamYMax - streamYMin) < 1e-9) streamYMax = streamYMin + 1;

            // Cumulative bottom starts at baseline for each x
            var cumBottom = new double[n];
            for (int i = 0; i < n; i++) cumBottom[i] = -totals[i] / 2.0;
            for (int j = 0; j < si; j++)
                for (int i = 0; i < n; i++)
                    cumBottom[i] += (i < allStream[j].Data.Count && allStream[j].Data[i].HasValue) ? allStream[j].Data[i]!.Value : 0;

            var lo = new double[n];
            var hi = new double[n];
            for (int i = 0; i < n; i++)
            {
                double v = (i < thisSeries.Data.Count && thisSeries.Data[i].HasValue) ? thisSeries.Data[i]!.Value : 0;
                lo[i] = cumBottom[i];
                hi[i] = cumBottom[i] + v;
            }

            bool hasCats = options.XAxis.Categories?.Count > 0;
            double step = hasCats
                ? (double)plotWidth / options.XAxis.Categories!.Count
                : (n > 1 ? (double)plotWidth / (n - 1) : plotWidth);

            var path = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                double cx = hasCats ? PaddingLeft + (i + 0.5) * step : PaddingLeft + i * step;
                double yPx = PaddingTop + plotHeight * (1.0 - Frac(hi[i], streamYMin, streamYMax, false));
                path.Append(i == 0 ? $"M {F(cx)} {F(yPx)}" : $" L {F(cx)} {F(yPx)}");
            }
            for (int i = n - 1; i >= 0; i--)
            {
                double cx = hasCats ? PaddingLeft + (i + 0.5) * step : PaddingLeft + i * step;
                double yPx = PaddingTop + plotHeight * (1.0 - Frac(lo[i], streamYMin, streamYMax, false));
                path.Append($" L {F(cx)} {F(yPx)}");
            }
            path.Append(" Z");

            sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{path}\" fill=\"{Escape(color)}\" fill-opacity=\"0.80\"/>");
        }

        // ------------------------------------------------------------------ Gantt

        private static void AppendGanttSeries(StringBuilder sb, Series series, string color,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            string clipId, StringBuilder tooltipLayer)
        {
            var tasks = series.GanttData;
            if (tasks.Count == 0) return;
            int n = tasks.Count;

            // X (time) range
            double xMin = options.XAxis.Min ?? double.MaxValue;
            double xMax = options.XAxis.Max ?? double.MinValue;
            if (!options.XAxis.Min.HasValue || !options.XAxis.Max.HasValue)
                foreach (var t in tasks)
                {
                    if (t.Start < xMin) xMin = t.Start;
                    if (t.End   > xMax) xMax = t.End;
                }
            if (xMax <= xMin) xMax = xMin + 1;

            double rowH   = (double)plotHeight / Math.Max(n, 1);
            double barPad = rowH * 0.15;
            double barH   = rowH - barPad * 2;

            string tc = Escape(options.Theme.TextColor);
            string gl = Escape(options.Theme.GridLineColor);

            // Row labels + horizontal grid lines
            for (int i = 0; i < n; i++)
            {
                double cy = PaddingTop + i * rowH;
                sb.AppendLine($"  <line aria-hidden=\"true\" class=\"grid-line\" stroke=\"{gl}\" x1=\"{PaddingLeft}\" y1=\"{F(cy)}\" x2=\"{PaddingLeft + plotWidth}\" y2=\"{F(cy)}\"/>");
                sb.AppendLine($"  <text class=\"axis-label\" x=\"{PaddingLeft - 8}\" y=\"{F(cy + rowH / 2 + 4)}\" text-anchor=\"end\" fill=\"{tc}\">{Escape(tasks[i].Name)}</text>");
            }

            // X-axis ticks
            int xTicks = 5;
            for (int t = 0; t <= xTicks; t++)
            {
                double v  = xMin + (xMax - xMin) * t / xTicks;
                double xPx = PaddingLeft + (v - xMin) / (xMax - xMin) * plotWidth;
                sb.AppendLine($"  <line aria-hidden=\"true\" class=\"grid-line\" stroke=\"{gl}\" x1=\"{F(xPx)}\" y1=\"{F(PaddingTop)}\" x2=\"{F(xPx)}\" y2=\"{F(PaddingTop + plotHeight)}\"/>");
                sb.AppendLine($"  <text class=\"axis-label\" x=\"{F(xPx)}\" y=\"{F(PaddingTop + plotHeight + 16)}\" text-anchor=\"middle\" fill=\"{tc}\">{FormatTick(v)}</text>");
            }
            sb.AppendLine($"  <line class=\"axis-line\" x1=\"{PaddingLeft}\" y1=\"{F(PaddingTop + plotHeight)}\" x2=\"{PaddingLeft + plotWidth}\" y2=\"{F(PaddingTop + plotHeight)}\"/>");
            sb.AppendLine($"  <line class=\"axis-line\" x1=\"{PaddingLeft}\" y1=\"{F(PaddingTop)}\" x2=\"{PaddingLeft}\" y2=\"{F(PaddingTop + plotHeight)}\"/>");

            // Task bars
            for (int i = 0; i < n; i++)
            {
                var task       = tasks[i];
                string barClr  = task.Color ?? color;
                double barX    = PaddingLeft + (task.Start - xMin) / (xMax - xMin) * plotWidth;
                double barW    = Math.Max(2, (task.End - task.Start) / (xMax - xMin) * plotWidth);
                double barY    = PaddingTop + i * rowH + barPad;

                sb.AppendLine($"  <rect clip-path=\"url(#{clipId})\" x=\"{F(barX)}\" y=\"{F(barY)}\" width=\"{F(barW)}\" height=\"{F(barH)}\" rx=\"3\" fill=\"{Escape(barClr)}\" fill-opacity=\"0.85\"/>");

                if (barW > 40 && !string.IsNullOrEmpty(task.Label))
                    sb.AppendLine($"  <text class=\"data-label\" x=\"{F(barX + barW / 2)}\" y=\"{F(barY + barH / 2 + 4)}\" text-anchor=\"middle\" fill=\"#ffffff\">{Escape(task.Label)}</text>");

                if (options.RenderMode != SvgMode.Static)
                {
                    tooltipLayer.AppendLine("  <g class=\"data-point\">");
                    AppendHoverBandH(tooltipLayer, options, PaddingTop + i * rowH + rowH / 2.0, rowH, plotWidth);
                    tooltipLayer.AppendLine($"  <rect x=\"{F(barX)}\" y=\"{F(barY)}\" width=\"{F(barW)}\" height=\"{F(barH)}\" class=\"hit-area\" stroke=\"none\"/>");
                    AppendTooltip(tooltipLayer, barX + barW / 2, barY - 8,
                        $"{task.Name}: {FormatTick(task.Start)}\u2013{FormatTick(task.End)}",
                        task.End - task.Start, svgWidth, svgHeight,
                        options.Tooltip, barClr, PaddingTop, PaddingTop + plotHeight);
                    tooltipLayer.AppendLine("  </g>");
                }
            }
        }

        // ------------------------------------------------------------------ Sankey

        private static void AppendSankeySeries(StringBuilder sb, Series series, string color,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            StringBuilder tooltipLayer)
        {
            var nodes = series.SankeyNodes;
            var links = series.SankeyLinks;
            if (nodes.Count == 0 || links.Count == 0) return;

            int nNodes = nodes.Count;
            string[] palette = options.Theme.Colors;

            // 1. Column assignment via BFS from source nodes (zero in-degree)
            var inDeg = new int[nNodes];
            foreach (var lk in links)
                if (lk.To >= 0 && lk.To < nNodes) inDeg[lk.To]++;

            var col = new int[nNodes];
            for (int i = 0; i < nNodes; i++) col[i] = -1;
            var queue = new Queue<int>();
            for (int i = 0; i < nNodes; i++) if (inDeg[i] == 0) { col[i] = 0; queue.Enqueue(i); }
            if (queue.Count == 0) { col[0] = 0; queue.Enqueue(0); }

            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                foreach (var lk in links)
                {
                    if (lk.From != cur || lk.To < 0 || lk.To >= nNodes) continue;
                    int nc = col[cur] + 1;
                    if (col[lk.To] < nc) { col[lk.To] = nc; queue.Enqueue(lk.To); }
                }
            }
            for (int i = 0; i < nNodes; i++) if (col[i] < 0) col[i] = 0;

            int numCols = 0;
            for (int i = 0; i < nNodes; i++) if (col[i] + 1 > numCols) numCols = col[i] + 1;
            if (numCols < 2) numCols = 2;

            // 2. Flow totals per node
            var flowOut = new double[nNodes];
            var flowIn  = new double[nNodes];
            foreach (var lk in links)
            {
                if (lk.From >= 0 && lk.From < nNodes) flowOut[lk.From] += lk.Value;
                if (lk.To   >= 0 && lk.To   < nNodes) flowIn [lk.To  ] += lk.Value;
            }
            var nodeFlow = new double[nNodes];
            for (int i = 0; i < nNodes; i++) nodeFlow[i] = Math.Max(flowOut[i], flowIn[i]);

            // 3. Column node counts + total flow per column
            var colCount = new int[numCols];
            var colFlow  = new double[numCols];
            for (int i = 0; i < nNodes; i++) { colCount[col[i]]++; colFlow[col[i]] += nodeFlow[i]; }

            double maxColFlow = 1e-9;
            for (int c = 0; c < numCols; c++) if (colFlow[c] > maxColFlow) maxColFlow = colFlow[c];

            double nodePad   = 10.0;
            double nodeW     = 16.0;
            double colSpacX  = numCols > 1 ? (double)plotWidth / (numCols - 1) : plotWidth;
            double availH    = plotHeight - nodePad * (nNodes + 1);

            var nodeH_  = new double[nNodes];
            var nodeX_  = new double[nNodes];
            var nodeY_  = new double[nNodes];

            for (int i = 0; i < nNodes; i++)
            {
                nodeH_[i] = Math.Max(8, nodeFlow[i] / maxColFlow * availH);
                nodeX_[i] = PaddingLeft + col[i] * colSpacX - nodeW / 2.0;
            }
            // Clamp first/last column nodes to plot edges
            for (int i = 0; i < nNodes; i++)
                if (col[i] == 0) nodeX_[i] = PaddingLeft;
                else if (col[i] == numCols - 1) nodeX_[i] = PaddingLeft + plotWidth - nodeW;

            // Stack nodes per column top→bottom
            for (int c = 0; c < numCols; c++)
            {
                double yOff = PaddingTop + nodePad;
                for (int i = 0; i < nNodes; i++)
                {
                    if (col[i] != c) continue;
                    nodeY_[i] = yOff;
                    yOff += nodeH_[i] + nodePad;
                }
            }

            // 4. Links
            var cumOut = new double[nNodes];
            var cumIn  = new double[nNodes];

            foreach (var lk in links)
            {
                if (lk.From < 0 || lk.From >= nNodes || lk.To < 0 || lk.To >= nNodes) continue;
                double ratio  = Math.Max(flowOut[lk.From], 1e-9);
                double ratio2 = Math.Max(flowIn [lk.To],   1e-9);
                double lkH    = lk.Value / maxColFlow * availH;
                double halfH  = Math.Max(1.5, lkH / 2.0);

                double sx = nodeX_[lk.From] + nodeW;
                double sy = nodeY_[lk.From] + nodeH_[lk.From] * (cumOut[lk.From] / ratio) + halfH;
                double ex = nodeX_[lk.To];
                double ey = nodeY_[lk.To]   + nodeH_[lk.To]   * (cumIn [lk.To  ] / ratio2) + halfH;
                cumOut[lk.From] += lk.Value;
                cumIn [lk.To  ] += lk.Value;

                double midX = (sx + ex) / 2.0;
                string lkColor = lk.Color ?? nodes[lk.From].Color ?? palette[lk.From % palette.Length];
                sb.AppendLine($"  <path d=\"M {F(sx)} {F(sy)} C {F(midX)} {F(sy)}, {F(midX)} {F(ey)}, {F(ex)} {F(ey)}\" stroke=\"{Escape(lkColor)}\" stroke-width=\"{F(lkH)}\" fill=\"none\" stroke-opacity=\"0.45\"/>");
            }

            // 5. Nodes + labels
            for (int i = 0; i < nNodes; i++)
            {
                string nodeColor = nodes[i].Color ?? palette[i % palette.Length];
                sb.AppendLine($"  <rect x=\"{F(nodeX_[i])}\" y=\"{F(nodeY_[i])}\" width=\"{F(nodeW)}\" height=\"{F(nodeH_[i])}\" fill=\"{Escape(nodeColor)}\" rx=\"2\"/>");
                bool rightSide  = col[i] < numCols - 1;
                double labelX   = rightSide ? nodeX_[i] + nodeW + 5 : nodeX_[i] - 5;
                string anchor   = rightSide ? "start" : "end";
                double labelY   = nodeY_[i] + nodeH_[i] / 2.0 + 4;
                sb.AppendLine($"  <text class=\"axis-label\" x=\"{F(labelX)}\" y=\"{F(labelY)}\" text-anchor=\"{anchor}\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(nodes[i].Name)}</text>");
            }
        }

        // ------------------------------------------------------------------ data table

    }
}
