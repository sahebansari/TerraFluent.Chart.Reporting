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
        private static void AppendDataTable(StringBuilder sb, ChartOptions options, int svgWidth,
            int tableY, int rowCount)
        {
            var dt      = options.DataTable;
            int rh      = dt.RowHeight;
            int fs      = dt.FontSize;
            string tc   = Escape(options.Theme.TextColor);
            string bg   = Escape(options.ResolvedBackgroundColor);

            // Visible numeric series only (skip special types that have no simple Data list)
            var numericTypes = new System.Collections.Generic.HashSet<ChartType>
            {
                ChartType.Line, ChartType.Spline, ChartType.Column, ChartType.Bar,
                ChartType.Area, ChartType.Scatter
            };
            var cols = new System.Collections.Generic.List<Series>();
            foreach (var s in options.Series)
                if (s.Visible && numericTypes.Contains(s.Type) && s.Data.Count > 0) cols.Add(s);

            if (cols.Count == 0) return;

            var cats = options.XAxis?.Categories;
            int nCols = cols.Count;
            // Category label column + one value column per series
            double valColW  = (double)(svgWidth - PaddingLeft - PaddingRight) / nCols;

            // Header row background
            sb.AppendLine($"  <rect x=\"0\" y=\"{tableY}\" width=\"{svgWidth}\" height=\"{rh}\" fill=\"rgba(128,128,128,0.12)\"/>");
            // Separator line above table
            sb.AppendLine($"  <line x1=\"{PaddingLeft}\" y1=\"{tableY}\" x2=\"{svgWidth - PaddingRight}\" y2=\"{tableY}\" stroke=\"rgba(128,128,128,0.30)\" stroke-width=\"1\"/>");

            // Header cells
            double hx = PaddingLeft;
            for (int c = 0; c < nCols; c++)
            {
                double cx = hx + c * valColW + valColW / 2.0;
                sb.AppendLine($"  <text x=\"{F(cx)}\" y=\"{tableY + rh - 5}\" text-anchor=\"middle\" font-size=\"{fs}\" font-weight=\"600\" fill=\"{tc}\">{Escape(cols[c].Name)}</text>");
            }

            // Data rows
            for (int row = 0; row < rowCount; row++)
            {
                int ry = tableY + (row + 1) * rh;

                // Alternating row background
                if (row % 2 == 1)
                    sb.AppendLine($"  <rect x=\"0\" y=\"{ry}\" width=\"{svgWidth}\" height=\"{rh}\" fill=\"rgba(128,128,128,0.05)\"/>");

                // Row divider
                sb.AppendLine($"  <line x1=\"{PaddingLeft}\" y1=\"{ry}\" x2=\"{svgWidth - PaddingRight}\" y2=\"{ry}\" stroke=\"rgba(128,128,128,0.12)\" stroke-width=\"1\"/>");

                // Category label
                string catLabel = (cats != null && row < cats.Count)
                    ? cats[row]
                    : row.ToString(CultureInfo.InvariantCulture);
                sb.AppendLine($"  <text x=\"{PaddingLeft - 4}\" y=\"{ry + rh - 5}\" text-anchor=\"end\" font-size=\"{fs}\" fill=\"{tc}\">{Escape(catLabel)}</text>");

                // Value cells
                for (int c = 0; c < nCols; c++)
                {
                    double cx = hx + c * valColW + valColW / 2.0;
                    string val = (row < cols[c].Data.Count && cols[c].Data[row].HasValue)
                        ? F(cols[c].Data[row]!.Value)
                        : "\u2014"; // em-dash for null
                    string color = string.IsNullOrEmpty(cols[c].Color)
                        ? tc
                        : Escape(cols[c].Color!);
                    sb.AppendLine($"  <text x=\"{F(cx)}\" y=\"{ry + rh - 5}\" text-anchor=\"middle\" font-size=\"{fs}\" fill=\"{color}\">{val}</text>");
                }
            }
        }

        // ------------------------------------------------------------------ ARIA helpers
        /// <summary>
        /// Builds a plain-text description of the chart suitable for the SVG &lt;desc&gt; element.
        /// Screen readers expose this when they cannot render or interpret the image visually.
        /// </summary>
        private static string BuildAriaDesc(ChartOptions options)
        {
            var sb = new System.Text.StringBuilder();

            if (!string.IsNullOrEmpty(options.Title?.Text))
                sb.Append(options.Title.Text).Append(" — ");
            if (!string.IsNullOrEmpty(options.Subtitle?.Text))
                sb.Append(options.Subtitle.Text).Append(" — ");

            // Visible series names
            var names = new System.Collections.Generic.List<string>();
            foreach (var s in options.Series)
                if (s.Visible && !string.IsNullOrEmpty(s.Name)) names.Add(s.Name);
            if (names.Count == 1)
                sb.Append("Series: ").Append(names[0]).Append(". ");
            else if (names.Count > 1)
                sb.Append("Series: ").Append(string.Join(", ", names)).Append(". ");

            // X-axis category range
            var cats = options.XAxis?.Categories;
            if (cats != null && cats.Count > 0)
            {
                if (cats.Count <= 4)
                    sb.Append("Categories: ").Append(string.Join(", ", cats)).Append(". ");
                else
                    sb.Append("Categories: ").Append(cats[0]).Append(" to ").Append(cats[cats.Count - 1]).Append(". ");
            }

            // Overall value range
            double vMin = double.MaxValue, vMax = double.MinValue;
            foreach (var s in options.Series)
            {
                if (!s.Visible) continue;
                foreach (var v in s.Data)
                    if (v.HasValue) { if (v.Value < vMin) vMin = v.Value; if (v.Value > vMax) vMax = v.Value; }
            }
            if (vMin != double.MaxValue)
                sb.Append("Values range from ").Append(FormatTick(vMin)).Append(" to ").Append(FormatTick(vMax)).Append(".");

            string result = sb.ToString().Trim().TrimEnd('-').Trim();
            return result.Length > 0 ? result : "Chart";
        }

        // ------------------------------------------------------------------ math helpers

        /// <summary>
        /// Converts a chart title into a safe filename for the SVG export download
        /// (ASCII letters/digits/hyphens only, max 50 chars, always ends in .svg).
        /// </summary>
        private static string SanitizeFilename(string? title)
        {
            if (string.IsNullOrWhiteSpace(title)) return "chart.svg";
            var sb = new System.Text.StringBuilder();
            foreach (char c in title)
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_') sb.Append(c);
                else if (c == ' ' && sb.Length > 0 && sb[sb.Length - 1] != '-') sb.Append('-');
            string name = sb.ToString().Trim('-');
            if (name.Length > 50) name = name.Substring(0, 50);
            return (name.Length > 0 ? name : "chart") + ".svg";
        }

        private static double ComputeYMin(List<Series> seriesList)
        {
            double min = double.MaxValue;
            foreach (var s in seriesList)
            {
                foreach (var v in s.Data) if (v.HasValue && v.Value < min) min = v.Value;
                foreach (var bp in s.BubbleData) if (bp.Y < min) min = bp.Y;
                foreach (var rp in s.RangeData) { if (rp.Low < min) min = rp.Low; if (rp.High < min) min = rp.High; }
                foreach (var box in s.BoxPlotData) if (box.Low < min) min = box.Low;
                foreach (var o in s.OhlcData) if (o.Low < min) min = o.Low;
            }
            return min == double.MaxValue ? 0 : Math.Min(min, 0);
        }

        private static double ComputeYMax(List<Series> seriesList)
        {
            double max = double.MinValue;
            foreach (var s in seriesList)
            {
                foreach (var v in s.Data) if (v.HasValue && v.Value > max) max = v.Value;
                foreach (var bp in s.BubbleData) if (bp.Y > max) max = bp.Y;
                foreach (var rp in s.RangeData) { if (rp.Low > max) max = rp.Low; if (rp.High > max) max = rp.High; }
                foreach (var box in s.BoxPlotData) if (box.High > max) max = box.High;
                foreach (var o in s.OhlcData) if (o.High > max) max = o.High;
            }
            return max == double.MinValue ? 1 : Math.Max(max, 0);
        }

        // ------------------------------------------------------------------ axis scale (linear / log)

        /// <summary>Smallest positive value an axis may represent on a logarithmic scale.</summary>
        private const double LogFloor = 1e-9;

        /// <summary><c>true</c> when the axis uses a base-10 logarithmic scale.</summary>
        private static bool IsLog(Axis? axis) => axis != null && axis.Type == Enums.AxisType.Logarithmic;

        /// <summary>
        /// Maps <paramref name="value"/> to its 0..1 fraction along an axis running from
        /// <paramref name="min"/> to <paramref name="max"/>. For linear axes this is the ordinary
        /// <c>(value - min) / (max - min)</c>; for logarithmic axes it interpolates in log-10 space.
        /// </summary>
        private static double Frac(double value, double min, double max, bool log)
        {
            if (log)
            {
                double lMin = Math.Log10(min <= 0 ? LogFloor : min);
                double lMax = Math.Log10(max <= 0 ? LogFloor : max);
                double d    = lMax - lMin;
                if (Math.Abs(d) < 1e-12) return 0;
                double lV = Math.Log10(value <= 0 ? LogFloor : value);
                return (lV - lMin) / d;
            }
            double dd = max - min;
            return Math.Abs(dd) < 1e-12 ? 0 : (value - min) / dd;
        }

        /// <summary>Smallest strictly-positive Y value across the series (for log-axis auto-floor).</summary>
        private static double ComputeYMinPositive(List<Series> seriesList)
        {
            double min = double.MaxValue;
            foreach (var s in seriesList)
            {
                foreach (var v in s.Data) if (v.HasValue && v.Value > 0 && v.Value < min) min = v.Value;
                foreach (var bp in s.BubbleData) if (bp.Y > 0 && bp.Y < min) min = bp.Y;
                foreach (var rp in s.RangeData)
                {
                    if (rp.Low  > 0 && rp.Low  < min) min = rp.Low;
                    if (rp.High > 0 && rp.High < min) min = rp.High;
                }
            }
            return min == double.MaxValue ? 1 : min;
        }

        /// <summary>
        /// Resolves the effective [min, max] bounds for a value axis, honouring explicit
        /// <see cref="Axis.Min"/>/<see cref="Axis.Max"/> and applying decade snapping and a
        /// positive floor for logarithmic axes.
        /// </summary>
        private static (double Min, double Max) ResolveYBounds(Axis axis, List<Series> seriesList)
        {
            if (IsLog(axis))
            {
                double lo = axis.Min ?? ComputeYMinPositive(seriesList);
                double hi = axis.Max ?? ComputeYMax(seriesList);
                if (lo <= 0) lo = LogFloor;
                if (hi <= 0) hi = lo * 10;
                // Snap auto bounds outward to whole decades so ticks land on 1/10/100.
                if (!axis.Min.HasValue) lo = Math.Pow(10, Math.Floor(Math.Log10(lo)));
                if (!axis.Max.HasValue) hi = Math.Pow(10, Math.Ceiling(Math.Log10(hi)));
                if (hi <= lo) hi = lo * 10;
                return (lo, hi);
            }

            double min = axis.Min ?? ComputeYMin(seriesList);
            double max = axis.Max ?? ComputeYMax(seriesList);
            if (Math.Abs(max - min) < double.Epsilon) max = min + 1;
            return (min, max);
        }

        /// <summary>
        /// Generates tick values for a logarithmic axis: one per decade (…, 1, 10, 100, …), with
        /// intermediate 2·/5· ticks added when the axis spans only one or two decades so the scale
        /// isn't sparse.
        /// </summary>
        private static List<double> GenerateLogTicks(double min, double max)
        {
            var ticks = new List<double>();
            if (min <= 0) min = LogFloor;
            if (max <= min) max = min * 10;

            int startExp = (int)Math.Floor(Math.Log10(min));
            int endExp   = (int)Math.Ceiling(Math.Log10(max));
            int decades  = endExp - startExp;
            // Add 1-2-5 subdivisions for narrow ranges; decade-only for wide ranges.
            double[] mantissas = decades <= 2 ? new[] { 1.0, 2.0, 5.0 } : new[] { 1.0 };

            for (int exp = startExp; exp <= endExp; exp++)
            {
                double decade = Math.Pow(10, exp);
                foreach (double m in mantissas)
                {
                    double t = m * decade;
                    if (t < min * 0.999 || t > max * 1.001) continue;
                    ticks.Add(t);
                }
            }
            if (ticks.Count == 0) { ticks.Add(min); ticks.Add(max); }
            return ticks;
        }

        /// <summary>Enumerates linear tick values from <paramref name="first"/> up to <paramref name="max"/> in <paramref name="step"/> increments.</summary>
        private static List<double> EnumerateLinearTicks(double first, double max, double step)
        {
            var ticks = new List<double>();
            if (step <= 0) { ticks.Add(first); return ticks; }
            for (double tick = first; tick <= max + step * 0.5; tick += step)
                ticks.Add(tick);
            return ticks;
        }

        private static double NiceStep(double roughStep)
        {
            if (roughStep <= 0) return 1;
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(roughStep)));
            double residual = roughStep / magnitude;
            if (residual < 1.5) return 1 * magnitude;
            if (residual < 3.5) return 2 * magnitude;
            if (residual < 7.5) return 5 * magnitude;
            return 10 * magnitude;
        }

        private static string FormatTick(double v, System.Globalization.CultureInfo? culture = null)
        {
            var ci = culture ?? CultureInfo.InvariantCulture;
            if (Math.Abs(v) >= 1_000_000) return (v / 1_000_000).ToString("0.#", ci) + "M";
            if (Math.Abs(v) >= 1_000)     return (v / 1_000).ToString("0.#", ci) + "k";
            return v.ToString("0.##", ci);
        }

        // Matches "{value}" or "{value:<format>}" tokens inside a label template.
        private static readonly System.Text.RegularExpressions.Regex ValueTokenRegex =
            new System.Text.RegularExpressions.Regex(
                @"\{value(?::([^{}]*))?\}",
                System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// Applies a label template containing <c>{value}</c> tokens to <paramref name="v"/>.
        /// A bare <c>{value}</c> uses the compact abbreviated form (e.g. <c>1.5k</c>).
        /// A <c>{value:fmt}</c> token applies a standard .NET numeric format string
        /// (e.g. <c>N0</c>, <c>F2</c>, <c>C</c>, <c>0.00</c>) using
        /// <see cref="CultureInfo.InvariantCulture"/>, producing an exact, non-abbreviated number.
        /// </summary>
        private static string ApplyValueTemplate(string template, double v, System.Globalization.CultureInfo? culture = null)
        {
            var ci = culture ?? CultureInfo.InvariantCulture;
            return ValueTokenRegex.Replace(template, m =>
            {
                if (m.Groups[1].Success)
                {
                    string fmt = m.Groups[1].Value;
                    try { return v.ToString(fmt, ci); }
                    catch (FormatException) { return v.ToString(ci); }
                }
                return FormatTick(v, culture);
            });
        }

        private static string FormatAxisTick(double v, string? labelFormat, System.Globalization.CultureInfo? culture = null)
        {
            if (string.IsNullOrEmpty(labelFormat)) return FormatTick(v, culture);
            return ApplyValueTemplate(labelFormat!, v, culture);
        }

        /// <summary>
        /// Maps a DashStyle name to an SVG <c>stroke-dasharray</c> attribute snippet.
        /// Returns an empty string for "Solid" or unrecognised values.
        /// </summary>
        private static string BuildDashAttr(string? dashStyle)
        {
            if (string.IsNullOrEmpty(dashStyle)
                || string.Equals(dashStyle, "Solid", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            string upper = dashStyle.ToUpperInvariant();
            string array;
            if      (upper == "SHORTDASH")       array = "4,3";
            else if (upper == "SHORTDOT")        array = "1,3";
            else if (upper == "SHORTDASHDOT")    array = "4,3,1,3";
            else if (upper == "SHORTDASHDOTDOT") array = "4,3,1,3,1,3";
            else if (upper == "DOT")             array = "2,6";
            else if (upper == "DASH")            array = "8,6";
            else if (upper == "LONGDASH")        array = "16,6";
            else if (upper == "DASHDOT")         array = "8,6,2,6";
            else if (upper == "LONGDASHDOT")     array = "16,6,2,6";
            else if (upper == "LONGDASHDOTDOT")  array = "16,6,2,6,2,6";
            else return string.Empty;

            return $" stroke-dasharray=\"{array}\"";
        }

        // ------------------------------------------------------------------ Sprint 3 new renderers

        // ---- border / radius helper ----
        /// <summary>
        /// Returns SVG stroke and rx attributes for bar, column, and waterfall rectangles,
        /// driven by <see cref="Series.BorderColor"/>, <see cref="Series.BorderWidth"/>
        /// and <see cref="Series.BorderRadius"/>.
        /// </summary>
        private static string BuildRectBorderAttr(Series series)
        {
            var attr = new System.Text.StringBuilder();
            if (series.BorderRadius > 0)
                attr.Append($" rx=\"{series.BorderRadius}\"");
            if (series.BorderWidth > 0 && !string.IsNullOrEmpty(series.BorderColor))
            {
                attr.Append($" stroke=\"{Escape(series.BorderColor!)}\"" );
                attr.Append($" stroke-width=\"{series.BorderWidth}\"");
            }
            return attr.ToString();
        }

        // ------------------------------------------------------------------ gradient / pattern defs

        /// <summary>
        /// Emits a <c>&lt;defs&gt;</c> block containing the gradient or pattern definition for
        /// <paramref name="fill"/> under the id <paramref name="id"/>, so shapes can reference it
        /// via <c>fill="url(#id)"</c>.
        /// </summary>
        private static void AppendFillDef(StringBuilder sb, Models.SeriesFill fill, string id)
        {
            sb.AppendLine("  <defs>");
            switch (fill.Kind)
            {
                case Models.SeriesFillKind.LinearGradient:
                {
                    // Convert angle (degrees) into x1/y1→x2/y2 on the unit square. 90° = top→bottom.
                    double rad = fill.Angle * Math.PI / 180.0;
                    double dx = Math.Cos(rad), dy = Math.Sin(rad);
                    double x1 = 0.5 - dx / 2.0, y1 = 0.5 - dy / 2.0;
                    double x2 = 0.5 + dx / 2.0, y2 = 0.5 + dy / 2.0;
                    sb.AppendLine($"    <linearGradient id=\"{Escape(id)}\" x1=\"{F(x1)}\" y1=\"{F(y1)}\" x2=\"{F(x2)}\" y2=\"{F(y2)}\">");
                    AppendGradientStops(sb, fill);
                    sb.AppendLine("    </linearGradient>");
                    break;
                }
                case Models.SeriesFillKind.RadialGradient:
                {
                    sb.AppendLine($"    <radialGradient id=\"{Escape(id)}\" cx=\"0.5\" cy=\"0.5\" r=\"0.5\">");
                    AppendGradientStops(sb, fill);
                    sb.AppendLine("    </radialGradient>");
                    break;
                }
                case Models.SeriesFillKind.Pattern:
                {
                    double s  = fill.PatternSize > 0 ? fill.PatternSize : 8;
                    string fg = Escape(fill.PatternForeground);
                    sb.AppendLine($"    <pattern id=\"{Escape(id)}\" patternUnits=\"userSpaceOnUse\" width=\"{F(s)}\" height=\"{F(s)}\">");
                    if (!string.IsNullOrEmpty(fill.PatternBackground))
                        sb.AppendLine($"      <rect width=\"{F(s)}\" height=\"{F(s)}\" fill=\"{Escape(fill.PatternBackground!)}\"/>");
                    double sw = Math.Max(1.0, s / 8.0);
                    switch (fill.Pattern)
                    {
                        case Models.PatternKind.Dots:
                            sb.AppendLine($"      <circle cx=\"{F(s / 2)}\" cy=\"{F(s / 2)}\" r=\"{F(s / 5)}\" fill=\"{fg}\"/>");
                            break;
                        case Models.PatternKind.HorizontalLines:
                            sb.AppendLine($"      <line x1=\"0\" y1=\"{F(s / 2)}\" x2=\"{F(s)}\" y2=\"{F(s / 2)}\" stroke=\"{fg}\" stroke-width=\"{F(sw)}\"/>");
                            break;
                        case Models.PatternKind.VerticalLines:
                            sb.AppendLine($"      <line x1=\"{F(s / 2)}\" y1=\"0\" x2=\"{F(s / 2)}\" y2=\"{F(s)}\" stroke=\"{fg}\" stroke-width=\"{F(sw)}\"/>");
                            break;
                        case Models.PatternKind.Grid:
                            sb.AppendLine($"      <line x1=\"0\" y1=\"{F(s / 2)}\" x2=\"{F(s)}\" y2=\"{F(s / 2)}\" stroke=\"{fg}\" stroke-width=\"{F(sw)}\"/>");
                            sb.AppendLine($"      <line x1=\"{F(s / 2)}\" y1=\"0\" x2=\"{F(s / 2)}\" y2=\"{F(s)}\" stroke=\"{fg}\" stroke-width=\"{F(sw)}\"/>");
                            break;
                        case Models.PatternKind.CrossHatch:
                            sb.AppendLine($"      <path d=\"M0,0 L{F(s)},{F(s)}\" stroke=\"{fg}\" stroke-width=\"{F(sw)}\"/>");
                            sb.AppendLine($"      <path d=\"M{F(s)},0 L0,{F(s)}\" stroke=\"{fg}\" stroke-width=\"{F(sw)}\"/>");
                            break;
                        case Models.PatternKind.DiagonalLines:
                        default:
                            sb.AppendLine($"      <path d=\"M0,{F(s)} L{F(s)},0\" stroke=\"{fg}\" stroke-width=\"{F(sw)}\"/>");
                            break;
                    }
                    sb.AppendLine("    </pattern>");
                    break;
                }
            }
            sb.AppendLine("  </defs>");
        }

        private static void AppendGradientStops(StringBuilder sb, Models.SeriesFill fill)
        {
            if (fill.Stops.Count == 0)
            {
                sb.AppendLine("      <stop offset=\"0\" stop-color=\"#000000\"/>");
                return;
            }
            foreach (var stop in fill.Stops)
            {
                string op = stop.Opacity < 1.0
                    ? $" stop-opacity=\"{F(stop.Opacity)}\""
                    : string.Empty;
                sb.AppendLine($"      <stop offset=\"{F(stop.Offset)}\" stop-color=\"{Escape(stop.Color)}\"{op}/>");
            }
        }

        private static void AppendStackedBarSeries(StringBuilder sb, Series series, int si,
            string color, ChartOptions options, int svgWidth, int svgHeight,
            int plotWidth, int plotHeight, string clipId, StringBuilder tooltipLayer)
        {
            int n = series.Data.Count;
            if (n == 0) return;

            bool isPercent = options.Stacking == Stacking.Percent;

            // Per-category bar totals (for Percent mode — sum of all Bar series at each row)
            var totals = new double[n];
            if (isPercent)
                foreach (var s in options.Series)
                    if (s.Type == ChartType.Bar && s.Visible)
                        for (int i = 0; i < Math.Min(n, s.Data.Count); i++)
                            if (s.Data[i].HasValue) totals[i] += Math.Abs(s.Data[i]!.Value);

            // Cumulative base for each category row (positive and negative stacks)
            var posBase = new double[n];
            var negBase = new double[n];
            foreach (var s in options.Series)
            {
                if (s.Type != ChartType.Bar || !s.Visible) continue;
                if (s == series) break;
                for (int i = 0; i < Math.Min(n, s.Data.Count); i++)
                {
                    if (!s.Data[i].HasValue) continue;
                    double effective = s.Data[i]!.Value;
                    if (isPercent && totals[i] > 0) effective = effective / totals[i] * 100;
                    if (effective >= 0) posBase[i] += effective;
                    else negBase[i] += effective;
                }
            }

            double xMin = isPercent ? 0 : 0;
            double xMax = isPercent ? 100 : ComputeStackedBarXMax(options.Series);
            if (Math.Abs(xMax - xMin) < double.Epsilon) xMax = xMin + 1;

            bool hasCats = options.XAxis.Categories?.Count > 0;
            int catCount = hasCats ? options.XAxis.Categories!.Count : n;
            double groupH = (double)plotHeight / catCount;
            double barPad = groupH * 0.1;
            double barH   = groupH - barPad * 2;

            bool   animated = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string dur      = animated ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string easing   = animated ? SmilEasing(options.Animation.Easing) : string.Empty;

            // Draw axes on the first Bar series only (same as AppendBarSeries)
            bool isFirst = true;
            foreach (var s in options.Series)
            {
                if (s.Type == ChartType.Bar && s.Visible) { isFirst = (s == series); break; }
            }
            if (isFirst)
            {
                string barGc = options.YAxis.GridLineColor ?? options.Theme.GridLineColor;
                double range = xMax - xMin;
                double barTickStep = NiceStep(range / 5);
                for (double tick = Math.Floor(xMin / barTickStep) * barTickStep;
                     tick <= xMax + barTickStep * 0.5; tick += barTickStep)
                {
                    double xTick = PaddingLeft + (tick - xMin) / range * plotWidth;
                    if (xTick < PaddingLeft - 1 || xTick > PaddingLeft + plotWidth + 1) continue;
                    if (options.YAxis.GridLineVisible)
                        sb.AppendLine($"  <line class=\"grid-line\" stroke=\"{Escape(barGc)}\" x1=\"{F(xTick)}\" y1=\"{PaddingTop}\" x2=\"{F(xTick)}\" y2=\"{PaddingTop + plotHeight}\"/>");
                    sb.AppendLine($"  <text class=\"axis-label\" x=\"{F(xTick)}\" y=\"{PaddingTop + plotHeight + 16}\" text-anchor=\"middle\" fill=\"{Escape(options.Theme.TextColor)}\">{FormatAxisTick(tick, options.YAxis.LabelFormat)}</text>");
                }
                sb.AppendLine($"  <line class=\"axis-line\" x1=\"{PaddingLeft}\" y1=\"{PaddingTop}\" x2=\"{PaddingLeft}\" y2=\"{PaddingTop + plotHeight}\"/>");
                sb.AppendLine($"  <line class=\"axis-line\" x1=\"{PaddingLeft}\" y1=\"{PaddingTop + plotHeight}\" x2=\"{PaddingLeft + plotWidth}\" y2=\"{PaddingTop + plotHeight}\"/>");
                if (hasCats)
                    for (int i = 0; i < catCount && i < options.XAxis.Categories!.Count; i++)
                    {
                        double ly = PaddingTop + groupH * i + groupH / 2.0 + 4;
                        sb.AppendLine($"  <text class=\"axis-label\" x=\"{PaddingLeft - 8}\" y=\"{F(ly)}\" text-anchor=\"end\" fill=\"{Escape(options.Theme.TextColor)}\">{Escape(options.XAxis.Categories[i])}</text>");
                    }
            }

            double range2 = xMax - xMin;
            for (int i = 0; i < n; i++)
            {
                if (series.Data[i] is null) continue;
                double v = series.Data[i]!.Value;
                if (isPercent && totals[i] > 0) v = v / totals[i] * 100;

                double baseV  = v >= 0 ? posBase[i] : negBase[i];
                double barW   = Math.Abs(v / range2 * plotWidth);
                double barY   = PaddingTop + groupH * i + barPad;
                double baseXPx = PaddingLeft + (baseV - xMin) / range2 * plotWidth;
                double barX   = v >= 0 ? baseXPx : baseXPx - barW;

                if (animated)
                {
                    sb.AppendLine($"  <rect clip-path=\"url(#{clipId})\" x=\"{F(baseXPx)}\" y=\"{F(barY)}\" width=\"0\" height=\"{F(barH)}\" fill=\"{Escape(color)}\" fill-opacity=\"0.85\"{BuildRectBorderAttr(series)}>");
                    sb.AppendLine($"    <animate attributeName=\"width\" from=\"0\" to=\"{F(barW)}\" dur=\"{dur}\" fill=\"freeze\"{easing}/>");
                    if (v < 0) sb.AppendLine($"    <animate attributeName=\"x\" from=\"{F(baseXPx)}\" to=\"{F(barX)}\" dur=\"{dur}\" fill=\"freeze\"{easing}/>");
                    sb.AppendLine($"  </rect>");
                }
                else
                {
                    sb.AppendLine($"  <rect clip-path=\"url(#{clipId})\" x=\"{F(barX)}\" y=\"{F(barY)}\" width=\"{F(barW)}\" height=\"{F(barH)}\" fill=\"{Escape(color)}\" fill-opacity=\"0.85\"{BuildRectBorderAttr(series)}/>");
                }

                if (series.DataLabel.Enabled && barW > 18)
                    AppendDataLabel(sb, barX + barW / 2, barY + barH / 2 + 4 + series.DataLabel.VerticalOffset.GetValueOrDefault(),
                        FormatDataLabel(v, series.DataLabel.FormatString),
                        series.DataLabel.TextColor ?? options.Theme.TextColor, series.DataLabel.BackgroundColor, series.DataLabel.TextFontSize);

                if (options.RenderMode != SvgMode.Static)
                {
                    tooltipLayer.AppendLine($"  <g class=\"data-point\">");
                    tooltipLayer.AppendLine($"    <rect x=\"{F(barX)}\" y=\"{F(barY)}\" width=\"{F(barW)}\" height=\"{F(barH)}\" class=\"hit-area\" stroke=\"none\"/>");
                    AppendTooltip(tooltipLayer, barX + barW / 2, barY + barH / 2, series.Name, v, svgWidth, svgHeight, options.Tooltip, color);
                    tooltipLayer.AppendLine($"  </g>");
                }
            }
        }

        /// <summary>Returns the maximum stacked sum across all Bar series at any single category.</summary>
        private static double ComputeStackedBarXMax(List<Series> seriesList)
        {
            int maxPoints = 0;
            foreach (var s in seriesList)
                if (s.Type == ChartType.Bar && s.Visible && s.Data.Count > maxPoints)
                    maxPoints = s.Data.Count;

            double max = 0;
            for (int i = 0; i < maxPoints; i++)
            {
                double sum = 0;
                foreach (var s in seriesList)
                    if (s.Type == ChartType.Bar && s.Visible && i < s.Data.Count && s.Data[i].HasValue && s.Data[i]!.Value > 0)
                        sum += s.Data[i]!.Value;
                if (sum > max) max = sum;
            }
            return max > 0 ? max : 1;
        }

        private static void AppendStackedColumnSeries(StringBuilder sb, Series series, int si,
            string color, ChartOptions options, int svgWidth, int svgHeight,
            int plotWidth, int plotHeight, string clipId, StringBuilder tooltipLayer)
        {
            int n = series.Data.Count;
            if (n == 0) return;

            bool isPercent = options.Stacking == Stacking.Percent;

            // Per-point column totals (for Percent mode)
            var totals = new double[n];
            if (isPercent)
                foreach (var s in options.Series)
                    if (s.Type == ChartType.Column && s.Visible)
                        for (int i = 0; i < Math.Min(n, s.Data.Count); i++)
                            if (s.Data[i].HasValue) totals[i] += Math.Abs(s.Data[i]!.Value);

            // Cumulative base (sum of all preceding Column series at each point)
            var posBase = new double[n];
            var negBase = new double[n];
            foreach (var s in options.Series)
            {
                if (s.Type != ChartType.Column || !s.Visible) continue;
                if (s == series) break;
                for (int i = 0; i < Math.Min(n, s.Data.Count); i++)
                {
                    if (!s.Data[i].HasValue) continue;
                    double effective = s.Data[i]!.Value;
                    if (isPercent && totals[i] > 0) effective = effective / totals[i] * 100;
                    if (effective >= 0) posBase[i] += effective;
                    else negBase[i] += effective;
                }
            }

            double yMin = isPercent ? 0 : (options.YAxis.Min ?? ComputeYMin(options.Series));
            double yMax = isPercent ? 100 : (options.YAxis.Max ?? ComputeStackedYMax(options.Series));
            if (Math.Abs(yMax - yMin) < double.Epsilon) yMax = yMin + 1;

            bool hasCats = options.XAxis.Categories?.Count > 0;
            int catCount = hasCats ? options.XAxis.Categories!.Count : n;
            double groupWidth = (double)plotWidth / catCount;
            double barPadding = groupWidth * 0.1;
            double barWidth   = groupWidth - barPadding * 2;

            bool   animated = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string dur      = animated ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string easing   = animated ? SmilEasing(options.Animation.Easing) : string.Empty;

            for (int i = 0; i < n; i++)
            {
                if (series.Data[i] is null) continue;
                double v = series.Data[i]!.Value;
                if (isPercent && totals[i] > 0) v = v / totals[i] * 100;

                double baseV   = v >= 0 ? posBase[i] : negBase[i];
                double barH    = Math.Abs(v / (yMax - yMin) * plotHeight);
                double x       = PaddingLeft + groupWidth * i + barPadding;
                double baseYPx = PaddingTop + plotHeight - (baseV - yMin) / (yMax - yMin) * plotHeight;
                double y       = baseYPx - (v >= 0 ? barH : 0);

                // Shape in sb (no data-point wrapper)
                if (animated)
                {
                    sb.AppendLine($"  <rect clip-path=\"url(#{clipId})\" x=\"{F(x)}\" y=\"{F(baseYPx)}\" width=\"{F(barWidth)}\" height=\"0\" fill=\"{Escape(color)}\" fill-opacity=\"0.85\"{BuildRectBorderAttr(series)}>");
                    sb.AppendLine($"    <animate attributeName=\"height\" from=\"0\" to=\"{F(barH)}\" dur=\"{dur}\" fill=\"freeze\"{easing}/>");
                    sb.AppendLine($"    <animate attributeName=\"y\" from=\"{F(baseYPx)}\" to=\"{F(y)}\" dur=\"{dur}\" fill=\"freeze\"{easing}/>");
                    sb.AppendLine($"  </rect>");
                }
                else
                {
                    sb.AppendLine($"  <rect clip-path=\"url(#{clipId})\" x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(barWidth)}\" height=\"{F(barH)}\" fill=\"{Escape(color)}\" fill-opacity=\"0.85\"{BuildRectBorderAttr(series)}/>");
                }

                if (series.DataLabel.Enabled && barH > 12)
                    AppendDataLabel(sb, x + barWidth / 2, y + barH / 2 + 4 + series.DataLabel.VerticalOffset.GetValueOrDefault(), FormatDataLabel(v, series.DataLabel.FormatString),
                        series.DataLabel.TextColor ?? options.Theme.TextColor, series.DataLabel.BackgroundColor, series.DataLabel.TextFontSize);

                // Tooltip in overlay (always on top of all stacked bars)
                if (options.RenderMode != SvgMode.Static)
                {
                    tooltipLayer.AppendLine($"  <g class=\"data-point\">");
                    tooltipLayer.AppendLine($"  <rect x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(barWidth)}\" height=\"{F(barH)}\" class=\"hit-area\" stroke=\"none\"/>");
                    AppendTooltip(tooltipLayer, x + barWidth / 2, y - 8, series.Name, v, svgWidth, svgHeight, options.Tooltip,
                        color, PaddingTop, PaddingTop + plotHeight);
                    tooltipLayer.AppendLine($"  </g>");
                }
            }
        }

        // ------------------------------------------------------------------ data-label helpers

        private static void AppendDataLabel(StringBuilder sb, double x, double y, string text,
            string fill = ChartColor.GraphiteGray, string? bgColor = null, int? fontSize = null)
        {
            double fs = fontSize ?? 10;   // effective font size

            if (!string.IsNullOrEmpty(bgColor))
            {
                // Background pill dimensions scale with the font size
                double bw = text.Length * fs * 0.62 + 10;  // estimated text width + 5 px padding each side
                double bh = fs * 1.5;                       // ~cap-height + descenders + padding
                double bx = x - bw / 2;
                double by = y - fs * 1.05;                  // anchor above baseline
                sb.AppendLine($"  <rect x=\"{F(bx)}\" y=\"{F(by)}\" width=\"{F(bw)}\" height=\"{F(bh)}\" rx=\"3\" fill=\"{Escape(bgColor)}\"/>");
            }

            // Always put fill in the inline style so it wins over the CSS class rule
            // (CSS specificity beats SVG presentation attributes, but loses to inline styles).
            // font-size is appended only when overriding the 10 px default from the CSS.
            string style = fontSize.HasValue
                ? $"fill:{Escape(fill)};font-size:{fontSize}px"
                : $"fill:{Escape(fill)}";
            sb.AppendLine($"  <text x=\"{F(x)}\" y=\"{F(y)}\" text-anchor=\"middle\" class=\"data-label\" style=\"{style}\">{Escape(text)}</text>");
        }

        private static string FormatDataLabel(double v, string? format)
        {
            if (string.IsNullOrEmpty(format)) return FormatTick(v);
            return ApplyValueTemplate(format!, v);
        }

        // ------------------------------------------------------------------ stacking helpers

        private static double ComputeStackedYMax(List<Series> seriesList)
        {
            // Find the highest stack sum across all column data points
            int maxPoints = 0;
            foreach (var s in seriesList)
                if (s.Type == ChartType.Column && s.Visible && s.Data.Count > maxPoints)
                    maxPoints = s.Data.Count;

            double max = 0;
            for (int i = 0; i < maxPoints; i++)
            {
                double sum = 0;
                foreach (var s in seriesList)
                    if (s.Type == ChartType.Column && s.Visible && i < s.Data.Count && s.Data[i].HasValue && s.Data[i]!.Value > 0)
                        sum += s.Data[i]!.Value;
                if (sum > max) max = sum;
            }
            return max > 0 ? max : 1;
        }

        private static double ComputeStackedAreaYMin(List<Series> seriesList)
        {
            int maxPoints = 0;
            foreach (var s in seriesList)
                if (s.Type == ChartType.Area && s.Visible && s.Data.Count > maxPoints)
                    maxPoints = s.Data.Count;

            double min = 0;
            for (int i = 0; i < maxPoints; i++)
            {
                double sum = 0;
                foreach (var s in seriesList)
                    if (s.Type == ChartType.Area && s.Visible && i < s.Data.Count && s.Data[i].HasValue && s.Data[i]!.Value < 0)
                        sum += s.Data[i]!.Value;
                if (sum < min) min = sum;
            }
            return min;
        }

        private static double ComputeStackedAreaYMax(List<Series> seriesList)
        {
            int maxPoints = 0;
            foreach (var s in seriesList)
                if (s.Type == ChartType.Area && s.Visible && s.Data.Count > maxPoints)
                    maxPoints = s.Data.Count;

            double max = 0;
            for (int i = 0; i < maxPoints; i++)
            {
                double sum = 0;
                foreach (var s in seriesList)
                    if (s.Type == ChartType.Area && s.Visible && i < s.Data.Count && s.Data[i].HasValue && s.Data[i]!.Value > 0)
                        sum += s.Data[i]!.Value;
                if (sum > max) max = sum;
            }
            return max > 0 ? max : 1;
        }

    }
}
