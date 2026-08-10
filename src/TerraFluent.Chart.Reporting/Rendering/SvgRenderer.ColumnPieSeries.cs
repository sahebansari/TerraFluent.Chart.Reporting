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
        private static void AppendColumnSeries(StringBuilder sb, Series series, string color,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            double yMin, double yMax, string clipId, StringBuilder tooltipLayer,
            string? fillPaint = null)
        {
            int n = series.Data.Count;
            if (n == 0) return;

            string barFill = fillPaint ?? Escape(color);
            string barOp   = fillPaint != null ? F(series.Fill?.Opacity ?? 1.0) : "0.85";

            // Count Column series only for grouping (Bar is horizontal — separate renderer)
            int colSeriesCount = 0;
            int colSeriesIndex = 0;
            for (int k = 0; k < options.Series.Count; k++)
            {
                var s = options.Series[k];
                if (s.Type == ChartType.Column)
                {
                    if (s == series) colSeriesIndex = colSeriesCount;
                    colSeriesCount++;
                }
            }

            bool hasCats = options.XAxis.Categories?.Count > 0;
            int catCount = hasCats ? options.XAxis.Categories!.Count : n;
            double groupWidth = (double)plotWidth / catCount;
            double barPadding = groupWidth * 0.1;
            double barWidth = (groupWidth - barPadding * 2) / Math.Max(colSeriesCount, 1);
            bool yLog = IsLog(series.YAxisIndex == 1 ? options.YAxis2 : options.YAxis);

            for (int i = 0; i < n; i++)
            {
                if (series.Data[i] is null) continue;
                double v = series.Data[i]!.Value;
                // zeroFrac: fraction of the zero line; 0 for log-axis or when yMin > 0.
                double vFrac    = Frac(v, yMin, yMax, yLog);
                double zeroFrac = (!yLog && yMin <= 0.0) ? Frac(0.0, yMin, yMax, false) : 0.0;
                double barH = Math.Abs((vFrac - zeroFrac) * plotHeight);
                double x = PaddingLeft + groupWidth * i + barPadding + barWidth * colSeriesIndex;
                double y = PaddingTop + plotHeight - Math.Max(vFrac, zeroFrac) * plotHeight;

                // Plain group — tooltip hit-area lives in tooltipLayer, not here
                sb.AppendLine($"  <g>");

                // Threshold colouring: when zones are set, each bar is painted by its value's zone.
                string thisBarFill = series.Zones.Count > 0
                    ? Escape(ZoneColorFor(series.Zones, v, color))
                    : barFill;

                if (options.Animation.Enabled && options.RenderMode != SvgMode.Static)
                {
                    string cdur    = F(options.Animation.Duration.TotalSeconds) + "s";
                    string ceasing = SmilEasing(options.Animation.Easing);
                    double baseY   = PaddingTop + plotHeight - zeroFrac * plotHeight;
                    sb.AppendLine($"    <rect clip-path=\"url(#{clipId})\" x=\"{F(x)}\" y=\"{F(baseY)}\" width=\"{F(barWidth)}\" height=\"0\" fill=\"{thisBarFill}\" fill-opacity=\"{barOp}\"{BuildRectBorderAttr(series)}>");
                    sb.AppendLine($"      <animate attributeName=\"height\" from=\"0\" to=\"{F(barH)}\" dur=\"{cdur}\" fill=\"freeze\"{ceasing}/>");
                    sb.AppendLine($"      <animate attributeName=\"y\" from=\"{F(baseY)}\" to=\"{F(y)}\" dur=\"{cdur}\" fill=\"freeze\"{ceasing}/>");
                    sb.AppendLine($"    </rect>");
                }
                else
                {
                    sb.AppendLine($"    <rect clip-path=\"url(#{clipId})\" x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(barWidth)}\" height=\"{F(barH)}\" fill=\"{thisBarFill}\" fill-opacity=\"{barOp}\"{BuildRectBorderAttr(series)}/>");
                }

                sb.AppendLine($"  </g>");

                if (series.DataLabel.Enabled)
                    AppendDataLabel(sb, x + barWidth / 2, y - 4 + series.DataLabel.VerticalOffset.GetValueOrDefault(), FormatDataLabel(v, series.DataLabel.FormatString),
                        series.DataLabel.TextColor ?? options.Theme.TextColor, series.DataLabel.BackgroundColor, series.DataLabel.TextFontSize);

                // Tooltip hit-area group goes to overlay so it's always on top
                if (options.RenderMode != SvgMode.Static)
                {
                    string colXLabel = options.XAxis.Categories?.Count > i ? Escape(options.XAxis.Categories[i]) : i.ToString(CultureInfo.InvariantCulture);
                    tooltipLayer.AppendLine($"  <g class=\"data-point\" data-di=\"{i}\" data-val=\"{Escape(FormatTick(v))}\" data-name=\"{Escape(series.Name)}\" data-color=\"{Escape(color)}\" data-ax=\"{F(x + barWidth / 2)}\" data-ay=\"{F(y - 10)}\" data-xlabel=\"{colXLabel}\">");
                    tooltipLayer.AppendLine($"    <rect x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(barWidth)}\" height=\"{F(barH)}\" class=\"hit-area\" stroke=\"none\"/>");
                    AppendTooltip(tooltipLayer, x + barWidth / 2, y - 10, series.Name, v, svgWidth, svgHeight, options.Tooltip,
                        color, PaddingTop, PaddingTop + plotHeight);
                    tooltipLayer.AppendLine($"  </g>");
                }
            }
        }

        private static void AppendPieSeries(StringBuilder sb, Series series,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            StringBuilder tooltipLayer)
        {
            double total = 0;
            foreach (var v in series.Data) if (v.HasValue && v.Value > 0) total += v.Value;
            if (total <= 0) return;

            double cx     = PaddingLeft + plotWidth / 2.0;
            double cy     = PaddingTop + plotHeight / 2.0;
            double radius = Math.Min(plotWidth, plotHeight) / 2.0 * 0.8;
            double angle  = -Math.PI / 2;

            bool   animated = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string pdur     = F(options.Animation.Duration.TotalSeconds) + "s";
            string pease    = SmilEasing(options.Animation.Easing);

            double holePercent = Math.Max(0.0, Math.Min(0.99, series.DonutHolePercent));
            double holeR       = radius * holePercent;

            // Deferred label data: collected in phase 1, rendered in phase 4 so labels sit above
            // the animated cover circle and are not hidden during the sweep.
            var labels = new List<(double lx, double ly, string text,
                                   bool hasConn,
                                   double ex, double ey, double cpx, double cpy, double enx, double eny)>();

            // ── Phase 1: slice paths (static — cover circle provides the animation) ──────
            for (int i = 0; i < series.Data.Count; i++)
            {
                if (!series.Data[i].HasValue || series.Data[i]!.Value <= 0) continue;
                double v        = series.Data[i]!.Value;
                double slice    = v / total * 2 * Math.PI;
                double endAngle = angle + slice;

                double x1 = cx + radius * Math.Cos(angle);
                double y1 = cy + radius * Math.Sin(angle);
                double x2 = cx + radius * Math.Cos(endAngle);
                double y2 = cy + radius * Math.Sin(endAngle);
                int largeArc = slice > Math.PI ? 1 : 0;

                string color = options.Theme.Colors[i % options.Theme.Colors.Length];
                string label = options.XAxis.Categories?.Count > i
                    ? options.XAxis.Categories[i] : $"Slice {i + 1}";

                string piePathD = $"M{cx.ToString("F2", CultureInfo.InvariantCulture)},{cy.ToString("F2", CultureInfo.InvariantCulture)}" +
                                  $" L{x1.ToString("F2", CultureInfo.InvariantCulture)},{y1.ToString("F2", CultureInfo.InvariantCulture)}" +
                                  $" A{radius.ToString("F2", CultureInfo.InvariantCulture)},{radius.ToString("F2", CultureInfo.InvariantCulture)}" +
                                  $" 0 {largeArc},1 {x2.ToString("F2", CultureInfo.InvariantCulture)},{y2.ToString("F2", CultureInfo.InvariantCulture)} Z";

                sb.AppendLine($"  <path d=\"{piePathD}\" fill=\"{Escape(color)}\" stroke=\"{Escape(options.ResolvedBackgroundColor)}\" stroke-width=\"2\"/>");

                // Tooltip hit-area goes to overlay layer so it's always on top of all slices
                if (options.RenderMode != SvgMode.Static)
                {
                    double midAngle = angle + slice / 2;
                    double tx = cx + radius * 0.65 * Math.Cos(midAngle);
                    double ty = cy + radius * 0.65 * Math.Sin(midAngle);
                    tooltipLayer.AppendLine($"  <g class=\"data-point\">");
                    tooltipLayer.AppendLine($"    <path d=\"{piePathD}\" class=\"hit-area\" stroke=\"none\"/>");
                    AppendTooltip(tooltipLayer, tx, ty, label, v, svgWidth, svgHeight, options.Tooltip, color);
                    tooltipLayer.AppendLine($"  </g>");
                }

                // Collect data-label info for deferred rendering (phase 4)
                if (series.DataLabel.Enabled)
                {
                    double labelFrac = series.DataLabel.RadiusFraction ?? 0.7;
                    double labelR    = radius * labelFrac;
                    double midA      = angle + slice / 2;
                    double lx = cx + labelR * Math.Cos(midA);
                    double ly = cy + labelR * Math.Sin(midA) + series.DataLabel.VerticalOffset.GetValueOrDefault();
                    string labelText = FormatDataLabel(v / total * 100, series.DataLabel.FormatString ?? "{value}%");

                    bool hasConn = false;
                    double ex = 0, ey = 0, cpx = 0, cpy = 0, enx = 0, eny = 0;
                    if (labelFrac > 1.0)
                    {
                        ex = cx + radius * Math.Cos(midA);
                        ey = cy + radius * Math.Sin(midA);
                        double cdx  = lx - ex, cdy = ly - ey;
                        double cLen = Math.Sqrt(cdx * cdx + cdy * cdy);
                        const double connPad = 7;
                        if (cLen > connPad + 2)
                        {
                            double nx = cdx / cLen, ny = cdy / cLen;
                            enx = lx - nx * connPad;
                            eny = ly - ny * connPad;
                            double bowSize = cLen * 0.45;
                            cpx = (ex + enx) / 2 + (-Math.Sin(midA)) * bowSize;
                            cpy = (ey + eny) / 2 + ( Math.Cos(midA)) * bowSize;
                            hasConn = true;
                        }
                    }
                    labels.Add((lx, ly, labelText, hasConn, ex, ey, cpx, cpy, enx, eny));
                }

                angle = endAngle;
            }

            // ── Phase 2: clockwise reveal cover ──────────────────────────────────────────
            // Technique: a thick-stroke circle in the background colour sits on top of all
            // slices. stroke-dashoffset animates 0 → circumference so the stroke "wipes away"
            // clockwise from 12 o'clock, revealing the underlying slices progressively.
            if (animated)
            {
                // The stroke covers exactly the ring between the hole edge and the outer radius.
                // For a solid pie holeR=0, so the stroke covers center-to-outer-edge.
                double strokeR = (holeR + radius) / 2.0;
                double strokeW = radius - holeR + 2.0;   // +2 px prevents 1-px aliasing seams
                double circ    = 2.0 * Math.PI * strokeR;
                string bgColor = Escape(options.ResolvedBackgroundColor);

                sb.AppendLine($"  <circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(strokeR)}\" fill=\"none\"");
                sb.AppendLine($"          stroke=\"{bgColor}\" stroke-width=\"{F(strokeW)}\"");
                sb.AppendLine($"          stroke-dasharray=\"{F(circ)} {F(circ)}\" stroke-dashoffset=\"0\"");
                sb.AppendLine($"          transform=\"rotate(-90 {F(cx)} {F(cy)})\">");
                sb.AppendLine($"    <animate attributeName=\"stroke-dashoffset\" from=\"{F(circ * 2.0)}\" to=\"{F(circ)}\"");
                sb.AppendLine($"             dur=\"{pdur}\" fill=\"freeze\"{pease}/>");
                sb.AppendLine($"  </circle>");
            }

            // ── Phase 3: donut hole (always on top, never hidden by cover) ───────────────
            if (holePercent > 0)
            {
                sb.AppendLine($"  <circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(holeR)}\" fill=\"{Escape(options.ResolvedBackgroundColor)}\" stroke=\"none\"/>");

                // ── Phase 3b: center label ────────────────────────────────────────────────
                var dc = series.DonutCenter;
                if (dc.Enabled)
                {
                    // Main value: custom text or auto-formatted sum of all slice values
                    string valueText = !string.IsNullOrEmpty(dc.CustomText)
                        ? dc.CustomText!
                        : FormatTick(total);

                    // Auto-scale font sizes based on hole radius so text always fits
                    int valueFontSize = dc.ValueFontSize > 0
                        ? dc.ValueFontSize
                        : Math.Max(12, (int)(holeR * 0.38));
                    int titleFontSize = dc.CaptionFontSize > 0
                        ? dc.CaptionFontSize
                        : Math.Max(10, (int)(holeR * 0.22));

                    string valueColor = Escape(dc.TextColor ?? options.Theme.TextColor);
                    string titleColor = Escape(dc.TitleTextColor ?? options.Theme.TextColor);

                    bool hasTitle = !string.IsNullOrEmpty(dc.CenterTitle);

                    if (hasTitle)
                    {
                        // Two-line layout — vertically centre the pair around cy
                        double gap   = 4.0;
                        double titleCy = cy - (gap + valueFontSize) / 2.0;
                        double valueCy = cy + (gap + titleFontSize) / 2.0;

                        sb.AppendLine($"  <text x=\"{F(cx)}\" y=\"{F(titleCy)}\" text-anchor=\"middle\" dominant-baseline=\"central\"");
                        sb.AppendLine($"        font-size=\"{titleFontSize}\" fill=\"{titleColor}\" opacity=\"0.65\">{Escape(dc.CenterTitle!)}</text>");
                        sb.AppendLine($"  <text x=\"{F(cx)}\" y=\"{F(valueCy)}\" text-anchor=\"middle\" dominant-baseline=\"central\"");
                        sb.AppendLine($"        font-size=\"{valueFontSize}\" font-weight=\"bold\" fill=\"{valueColor}\">{Escape(valueText)}</text>");
                    }
                    else
                    {
                        // Single-line — centred directly on cy
                        sb.AppendLine($"  <text x=\"{F(cx)}\" y=\"{F(cy)}\" text-anchor=\"middle\" dominant-baseline=\"central\"");
                        sb.AppendLine($"        font-size=\"{valueFontSize}\" font-weight=\"bold\" fill=\"{valueColor}\">{Escape(valueText)}</text>");
                    }
                }
            }

            // ── Phase 4: data labels — rendered last so they sit above the cover circle ──
            foreach (var (lx, ly, text, hasConn, ex, ey, cpx, cpy, enx, eny) in labels)
            {
                if (hasConn)
                    sb.AppendLine($"  <path d=\"M{F(ex)},{F(ey)} Q{F(cpx)},{F(cpy)} {F(enx)},{F(eny)}\" fill=\"none\" stroke=\"{Escape(options.Theme.TextColor)}\" stroke-width=\"0.9\" opacity=\"0.5\"/>");

                AppendDataLabel(sb, lx, ly, text,
                    series.DataLabel.TextColor ?? options.Theme.TextColor,
                    series.DataLabel.BackgroundColor,
                    series.DataLabel.TextFontSize);
            }
        }

        private static void AppendStackedAreaSeries(StringBuilder sb, Series series, int si,
            string color, ChartOptions options, int svgWidth, int svgHeight,
            int plotWidth, int plotHeight, double step, string clipId, StringBuilder tooltipLayer)
        {
            int n = series.Data.Count;
            if (n == 0) return;

            bool isPercent = options.Stacking == Stacking.Percent;

            var totals = new double[n];
            if (isPercent)
                foreach (var s in options.Series)
                    if (s.Type == ChartType.Area && s.Visible)
                        for (int i = 0; i < Math.Min(n, s.Data.Count); i++)
                            if (s.Data[i].HasValue) totals[i] += Math.Abs(s.Data[i]!.Value);

            var posBase = new double[n];
            var negBase = new double[n];
            foreach (var s in options.Series)
            {
                if (s.Type != ChartType.Area || !s.Visible) continue;
                if (s == series) break;
                for (int i = 0; i < Math.Min(n, s.Data.Count); i++)
                {
                    if (!s.Data[i].HasValue) continue;
                    double eff = s.Data[i]!.Value;
                    if (isPercent && totals[i] > 0) eff = eff / totals[i] * 100;
                    if (eff >= 0) posBase[i] += eff;
                    else          negBase[i] += eff;
                }
            }

            double yMin = isPercent ? 0 : (options.YAxis.Min ?? ComputeStackedAreaYMin(options.Series));
            double yMax = isPercent ? 100 : (options.YAxis.Max ?? ComputeStackedAreaYMax(options.Series));
            if (Math.Abs(yMax - yMin) < double.Epsilon) yMax = yMin + 1;

            bool hasCats = options.XAxis.Categories?.Count > 0;
            double xOff  = hasCats ? step / 2.0 : 0;

            var upper = new List<(double x, double y, double val)>();
            var lower = new List<(double x, double y)>();

            for (int i = 0; i < n; i++)
            {
                if (series.Data[i] is null) continue;
                double v = series.Data[i]!.Value;
                if (isPercent && totals[i] > 0) v = v / totals[i] * 100;
                double baseV = v >= 0 ? posBase[i] : negBase[i];
                double topV  = baseV + v;

                double px      = PaddingLeft + xOff + i * step;
                double upperY  = PaddingTop + plotHeight - (topV  - yMin) / (yMax - yMin) * plotHeight;
                double lowerY  = PaddingTop + plotHeight - (baseV - yMin) / (yMax - yMin) * plotHeight;
                upper.Add((px, upperY, v));
                lower.Add((px, lowerY));
            }
            if (upper.Count == 0) return;

            // Fill polygon: upper line → lower line reversed
            var fill = new StringBuilder();
            fill.Append($"M{F(upper[0].x)},{F(upper[0].y)}");
            for (int i = 1; i < upper.Count; i++) fill.Append($" L{F(upper[i].x)},{F(upper[i].y)}");
            for (int i = lower.Count - 1; i >= 0; i--) fill.Append($" L{F(lower[i].x)},{F(lower[i].y)}");
            fill.Append(" Z");

            // Upper stroke line
            var line = new StringBuilder();
            line.Append($"M{F(upper[0].x)},{F(upper[0].y)}");
            for (int i = 1; i < upper.Count; i++) line.Append($" L{F(upper[i].x)},{F(upper[i].y)}");

            bool anim  = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string aDur  = anim ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string aEase = anim ? SmilEasing(options.Animation.Easing) : string.Empty;

            if (anim)
            {
                sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{fill}\" fill=\"{Escape(color)}\" fill-opacity=\"0\" stroke=\"none\">");
                sb.AppendLine($"    <animate attributeName=\"fill-opacity\" from=\"0\" to=\"0.7\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                sb.AppendLine($"  </path>");
                sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{line}\" fill=\"none\" stroke=\"{Escape(color)}\" stroke-width=\"2\" pathLength=\"1\" stroke-dasharray=\"1\" stroke-dashoffset=\"1\">");
                sb.AppendLine($"    <animate attributeName=\"stroke-dashoffset\" from=\"1\" to=\"0\" dur=\"{aDur}\" fill=\"freeze\"{aEase}/>");
                sb.AppendLine($"  </path>");
            }
            else
            {
                sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{fill}\" fill=\"{Escape(color)}\" fill-opacity=\"{F(series.FillOpacity ?? 0.7)}\" stroke=\"none\"/>");
                sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{line}\" fill=\"none\" stroke=\"{Escape(color)}\" stroke-width=\"2\"/>");
            }

            // Tooltips at upper boundary points
            int areaMarkerR = series.MarkerSize ?? 4;
            double stackedAreaPB = PaddingTop + plotHeight;
            for (int i = 0; i < upper.Count; i++)
            {
                var (px, py, val) = upper[i];
                string xLblA = options.XAxis.Categories?.Count > i ? options.XAxis.Categories[i] : i.ToString(CultureInfo.InvariantCulture);
                AppendDataPoint(sb, tooltipLayer, options.RenderMode, px, py, color, series.Name, val, svgWidth, svgHeight, options.Tooltip, series.MarkerEnabled, areaMarkerR, options.ResolvedBackgroundColor, PaddingTop, stackedAreaPB, i, xLblA, series.MarkerSymbol);
            }
        }

        // ------------------------------------------------------------------ Sprint 5 new series renderers

        private static void AppendWaterfallSeries(StringBuilder sb, Series series, string color,
            ChartOptions options, int svgWidth, int svgHeight, int plotWidth, int plotHeight,
            double yMin, double yMax, string clipId, StringBuilder tooltipLayer)
        {
            int n = series.Data.Count;
            if (n == 0) return;

            // Determine total-bar flags.
            // Full spec: use exactly as provided. Empty: auto first/last. Partial: fill remainder with
            // false but always keep first and last as totals for a coherent running-total shape.
            bool[] totals = new bool[n];
            if (series.WaterfallTotals.Count >= n)
            {
                for (int i = 0; i < n; i++) totals[i] = series.WaterfallTotals[i];
            }
            else if (series.WaterfallTotals.Count == 0)
            {
                totals[0] = true;
                if (n > 1) totals[n - 1] = true;
            }
            else
            {
                // Partial spec: copy provided flags; first and last always totals
                for (int i = 0; i < series.WaterfallTotals.Count; i++) totals[i] = series.WaterfallTotals[i];
                totals[0] = true;
                totals[n - 1] = true;
            }

            // Compute running total per point to determine yMin/yMax if not set
            double runningMin = 0, runningMax = 0, acc = 0;
            for (int i = 0; i < n; i++)
            {
                if (series.Data[i] is null) continue;
                double v = series.Data[i]!.Value;
                if (totals[i]) { runningMax = Math.Max(runningMax, acc); runningMin = Math.Min(runningMin, acc); }
                else { acc += v; runningMax = Math.Max(runningMax, acc); runningMin = Math.Min(runningMin, acc); }
            }
            double effYMin = options.YAxis.Min ?? Math.Min(runningMin, 0);
            double effYMax = options.YAxis.Max ?? (runningMax > 0 ? runningMax * 1.1 : 1);
            if (Math.Abs(effYMax - effYMin) < double.Epsilon) effYMax = effYMin + 1;
            double range = effYMax - effYMin;

            bool hasCats  = options.XAxis.Categories?.Count > 0;
            int catCount  = hasCats ? options.XAxis.Categories!.Count : n;
            double groupW = (double)plotWidth / catCount;
            double barPad = groupW * 0.15;
            double barW   = groupW - barPad * 2;

            string colorIncr = options.Theme.PositiveColor;
            string colorDecr = options.Theme.NegativeColor;

            bool   animated = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string dur      = animated ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string aEasing  = animated ? SmilEasing(options.Animation.Easing) : string.Empty;

            double cumulative = 0;
            double prevTopPx  = PaddingTop + plotHeight; // connector tracking

            for (int i = 0; i < n; i++)
            {
                if (series.Data[i] is null) continue;
                double v      = series.Data[i]!.Value;
                double barBot, barTop;

                if (totals[i])
                {
                    barBot = Math.Min(0.0, cumulative);
                    barTop = Math.Max(0.0, cumulative);
                }
                else
                {
                    barBot = v >= 0 ? cumulative : cumulative + v;
                    barTop = v >= 0 ? cumulative + v : cumulative;
                }

                double x  = PaddingLeft + groupW * i + barPad;
                double yT = PaddingTop + plotHeight - (barTop - effYMin) / range * plotHeight;
                double yB = PaddingTop + plotHeight - (barBot - effYMin) / range * plotHeight;
                double bH = Math.Abs(yB - yT);
                double yRect = Math.Min(yT, yB);

                string barColor = totals[i] ? Escape(color) :
                                  (v >= 0 ? colorIncr : colorDecr);

                // Connector line from previous bar top to this bar bottom
                if (i > 0)
                {
                    sb.AppendLine($"  <line x1=\"{F(x - barPad)}\" y1=\"{F(prevTopPx)}\" x2=\"{F(x + barW + barPad)}\" y2=\"{F(prevTopPx)}\" stroke=\"{Escape(options.Theme.GridLineColor)}\" stroke-width=\"1\" stroke-dasharray=\"2,2\"/>");
                }

                sb.AppendLine($"  <g>");
                if (animated)
                {
                    double baseYPx = PaddingTop + plotHeight;
                    sb.AppendLine($"    <rect clip-path=\"url(#{clipId})\" x=\"{F(x)}\" y=\"{F(baseYPx)}\" width=\"{F(barW)}\" height=\"0\" fill=\"{barColor}\" fill-opacity=\"0.85\"{BuildRectBorderAttr(series)}>");
                    sb.AppendLine($"      <animate attributeName=\"height\" from=\"0\" to=\"{F(bH)}\" dur=\"{dur}\" fill=\"freeze\"{aEasing}/>");
                    sb.AppendLine($"      <animate attributeName=\"y\" from=\"{F(baseYPx)}\" to=\"{F(yRect)}\" dur=\"{dur}\" fill=\"freeze\"{aEasing}/>");
                    sb.AppendLine($"    </rect>");
                }
                else
                {
                    sb.AppendLine($"    <rect clip-path=\"url(#{clipId})\" x=\"{F(x)}\" y=\"{F(yRect)}\" width=\"{F(barW)}\" height=\"{F(bH)}\" fill=\"{barColor}\" fill-opacity=\"0.85\"{BuildRectBorderAttr(series)}/>");
                }
                sb.AppendLine($"  </g>");

                if (options.RenderMode != SvgMode.Static)
                {
                    tooltipLayer.AppendLine($"  <g class=\"data-point\">");
                    tooltipLayer.AppendLine($"    <rect x=\"{F(x)}\" y=\"{F(yRect)}\" width=\"{F(barW)}\" height=\"{F(bH)}\" class=\"hit-area\" stroke=\"none\"/>");
                    AppendTooltip(tooltipLayer, x + barW / 2, yRect - 8, series.Name, v, svgWidth, svgHeight, options.Tooltip, color);
                    tooltipLayer.AppendLine($"  </g>");
                }

                prevTopPx = yRect + (v >= 0 || totals[i] ? 0 : bH);
                if (!totals[i]) cumulative += v;
            }
        }

    }
}
