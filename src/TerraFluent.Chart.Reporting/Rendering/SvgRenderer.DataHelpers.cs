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
        /// <summary>Series plotted against the primary Y axis (all series when there is no YAxis2).</summary>
        private static List<Series> PrimaryAxisSeries(ChartOptions options)
            => options.YAxis2 != null
                ? options.Series.FindAll(s => s.Visible && s.YAxisIndex == 0)
                : options.Series;

        /// <summary>
        /// Raw (ascending) value range of the primary Y axis. Every consumer — the axis ticks, the
        /// series renderers, plot bands/lines, annotations — must use this so they share one scale.
        /// It excludes secondary-axis series and, when stacking is on, spans the stacked totals of
        /// Column/Area series (0–100 for percent stacking) instead of the individual values.
        /// </summary>
        private static (double Min, double Max) ResolvePrimaryYBounds(ChartOptions options)
        {
            var list = PrimaryAxisSeries(options);
            if (options.Stacking == Stacking.None || IsLog(options.YAxis))
                return ResolveYBounds(options.YAxis, list);

            bool hasStackedCol  = list.Exists(s => s.Visible && s.Type == ChartType.Column);
            bool hasStackedArea = list.Exists(s => s.Visible && s.Type == ChartType.Area);
            if (!hasStackedCol && !hasStackedArea)
                return ResolveYBounds(options.YAxis, list);

            if (options.Stacking == Stacking.Percent)
                return (options.YAxis.Min ?? 0, options.YAxis.Max ?? 100);

            var (min, max) = ResolveYBounds(options.YAxis, list);
            if (!options.YAxis.Max.HasValue)
            {
                if (hasStackedCol)  max = Math.Max(max, ComputeStackedYMax(list));
                if (hasStackedArea) max = Math.Max(max, ComputeStackedAreaYMax(list));
            }
            if (!options.YAxis.Min.HasValue && hasStackedArea)
                min = Math.Min(min, ComputeStackedAreaYMin(list));
            if (Math.Abs(max - min) < double.Epsilon) max = min + 1;
            return (min, max);
        }

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
        /// and <see cref="Series.BorderRadius"/>. When the theme opts into
        /// <see cref="ChartTheme.ModernStyle"/> and no explicit radius is set, a small default
        /// corner radius is applied for a softer look.
        /// </summary>
        private static string BuildRectBorderAttr(Series series, ChartOptions options)
        {
            int radius = ResolveBarRadius(series, options);
            string rx = radius > 0 ? $" rx=\"{radius}\"" : string.Empty;
            return rx + BuildRectStrokeAttr(series);
        }

        // Corner radius for bar-like rects: explicit Series.BorderRadius, else 3px under ModernStyle.
        private static int ResolveBarRadius(Series series, ChartOptions options)
            => series.BorderRadius > 0 ? series.BorderRadius : (options.Theme.ModernStyle ? 3 : 0);

        private static string BuildRectStrokeAttr(Series series)
            => series.BorderWidth > 0 && !string.IsNullOrEmpty(series.BorderColor)
                ? $" stroke=\"{Escape(series.BorderColor!)}\" stroke-width=\"{series.BorderWidth}\""
                : string.Empty;

        // ---- outer-end rounded bar shape ----

        /// <summary>The free (outer) end of a bar that receives rounded corners.</summary>
        private enum BarEnd { None, Top, Bottom, Left, Right }

        /// <summary>
        /// Builds a closed path for a bar rectangle whose two corners at <paramref name="end"/> are
        /// rounded by <paramref name="r"/> (clamped to the bar's size); the base end stays square.
        /// All variants share the same command sequence per end, so SMIL can interpolate <c>d</c>.
        /// </summary>
        private static string RoundedBarPath(double x, double y, double w, double h, double r, BarEnd end)
        {
            w = Math.Max(0, w);
            h = Math.Max(0, h);
            double x2 = x + w, y2 = y + h;
            switch (end)
            {
                case BarEnd.Top:
                    r = Math.Max(0, Math.Min(r, Math.Min(w / 2, h)));
                    return $"M{F(x)},{F(y2)} L{F(x)},{F(y + r)} A{F(r)},{F(r)} 0 0 1 {F(x + r)},{F(y)} L{F(x2 - r)},{F(y)} A{F(r)},{F(r)} 0 0 1 {F(x2)},{F(y + r)} L{F(x2)},{F(y2)} Z";
                case BarEnd.Bottom:
                    r = Math.Max(0, Math.Min(r, Math.Min(w / 2, h)));
                    return $"M{F(x)},{F(y)} L{F(x2)},{F(y)} L{F(x2)},{F(y2 - r)} A{F(r)},{F(r)} 0 0 1 {F(x2 - r)},{F(y2)} L{F(x + r)},{F(y2)} A{F(r)},{F(r)} 0 0 1 {F(x)},{F(y2 - r)} Z";
                case BarEnd.Right:
                    r = Math.Max(0, Math.Min(r, Math.Min(h / 2, w)));
                    return $"M{F(x)},{F(y)} L{F(x2 - r)},{F(y)} A{F(r)},{F(r)} 0 0 1 {F(x2)},{F(y + r)} L{F(x2)},{F(y2 - r)} A{F(r)},{F(r)} 0 0 1 {F(x2 - r)},{F(y2)} L{F(x)},{F(y2)} Z";
                case BarEnd.Left:
                    r = Math.Max(0, Math.Min(r, Math.Min(h / 2, w)));
                    return $"M{F(x2)},{F(y)} L{F(x2)},{F(y2)} L{F(x + r)},{F(y2)} A{F(r)},{F(r)} 0 0 1 {F(x)},{F(y2 - r)} L{F(x)},{F(y + r)} A{F(r)},{F(r)} 0 0 1 {F(x + r)},{F(y)} Z";
                default:
                    return $"M{F(x)},{F(y)} L{F(x2)},{F(y)} L{F(x2)},{F(y2)} L{F(x)},{F(y2)} Z";
            }
        }

        /// <summary>
        /// Emits one Column/Bar shape. With a corner radius and an outer <paramref name="end"/> it is a
        /// path rounded only at that end; otherwise a plain square rect. When <paramref name="animate"/>
        /// is set the shape grows from the (fromX, fromY, fromW, fromH) geometry to its final geometry.
        /// </summary>
        private static void AppendBarShape(StringBuilder sb, string indent, string clipId,
            double x, double y, double w, double h, BarEnd end,
            string fill, string fillOpacity, string extraAttrs, Series series, ChartOptions options,
            bool animate, double fromX, double fromY, double fromW, double fromH,
            string dur, string easing)
        {
            int radius = ResolveBarRadius(series, options);
            string stroke = BuildRectStrokeAttr(series);
            string paint = $"fill=\"{fill}\" fill-opacity=\"{fillOpacity}\"{extraAttrs}{stroke}";

            if (radius > 0 && end != BarEnd.None)
            {
                string dTo = RoundedBarPath(x, y, w, h, radius, end);
                if (animate)
                {
                    string dFrom = RoundedBarPath(fromX, fromY, fromW, fromH, radius, end);
                    sb.AppendLine($"{indent}<path clip-path=\"url(#{clipId})\" d=\"{dFrom}\" {paint}>");
                    sb.AppendLine($"{indent}  <animate attributeName=\"d\" from=\"{dFrom}\" to=\"{dTo}\" dur=\"{dur}\" fill=\"freeze\"{easing}/>");
                    sb.AppendLine($"{indent}</path>");
                }
                else
                {
                    sb.AppendLine($"{indent}<path clip-path=\"url(#{clipId})\" d=\"{dTo}\" {paint}/>");
                }
                return;
            }

            if (animate)
            {
                sb.AppendLine($"{indent}<rect clip-path=\"url(#{clipId})\" x=\"{F(fromX)}\" y=\"{F(fromY)}\" width=\"{F(fromW)}\" height=\"{F(fromH)}\" {paint}>");
                AppendGrowAnimate(sb, indent, "width",  fromW, w, dur, easing);
                AppendGrowAnimate(sb, indent, "height", fromH, h, dur, easing);
                AppendGrowAnimate(sb, indent, "x",      fromX, x, dur, easing);
                AppendGrowAnimate(sb, indent, "y",      fromY, y, dur, easing);
                sb.AppendLine($"{indent}</rect>");
            }
            else
            {
                sb.AppendLine($"{indent}<rect clip-path=\"url(#{clipId})\" x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(w)}\" height=\"{F(h)}\" {paint}/>");
            }
        }

        private static void AppendGrowAnimate(StringBuilder sb, string indent, string attr,
            double from, double to, string dur, string easing)
        {
            string f = F(from), t = F(to);
            if (f != t)
                sb.AppendLine($"{indent}  <animate attributeName=\"{attr}\" from=\"{f}\" to=\"{t}\" dur=\"{dur}\" fill=\"freeze\"{easing}/>");
        }

        /// <summary>
        /// True when no later visible series of the same stacked <paramref name="type"/> adds a segment
        /// in the same direction at <paramref name="index"/> — i.e. this segment is the stack's outer end.
        /// </summary>
        private static bool IsOuterStackSegment(ChartOptions options, Series series, ChartType type,
            int index, bool positive)
        {
            bool after = false;
            foreach (var s in options.Series)
            {
                if (s.Type != type || !s.Visible) continue;
                if (s == series) { after = true; continue; }
                if (!after || index >= s.Data.Count || !s.Data[index].HasValue) continue;
                double v = s.Data[index]!.Value;
                if (positive ? v > 0 : v < 0) return false;
            }
            return true;
        }

        // True when the colour is a plain 6-digit hex string (e.g. "#7CB5EC"); gradient helpers
        // that call ChartColor.Lighten require this form and fall back to a flat fill otherwise.
        private static bool IsHex6(string? c) => !string.IsNullOrEmpty(c) && c!.Length == 7 && c[0] == '#';

        // Per-bar colour for a ColorByPoint series: the custom palette when given, else the theme
        // palette, cycling by data index. null when per-point colouring does not apply.
        private static string? PointColorFor(Series series, ChartOptions options, int index)
        {
            if (!series.ColorByPoint) return null;
            if (series.PointColors.Count > 0) return series.PointColors[index % series.PointColors.Count];
            var palette = options.Theme.Colors;
            return palette.Length > 0 ? palette[index % palette.Length] : null;
        }

        // Modern-style bar/column fill: when the theme opts into ModernStyle and the colour is a
        // plain hex, emits a subtle sheen gradient (a lightened tint fading to the base colour) and
        // returns its url(); otherwise returns <paramref name="plainFill"/> unchanged. The gradient
        // is geometry-only so it renders identically in Static, Animated and Interactive modes.
        private static string ModernBarFill(StringBuilder sb, ChartOptions options, string color,
            string plainFill, string gradId, bool horizontal)
        {
            if (!options.Theme.ModernStyle || !IsHex6(color))
                return plainFill;

            string light = ChartColor.Lighten(color, 0.22);
            string x2 = horizontal ? "1" : "0";
            string y2 = horizontal ? "0" : "1";
            sb.AppendLine($"  <linearGradient id=\"{Escape(gradId)}\" x1=\"0\" y1=\"0\" x2=\"{x2}\" y2=\"{y2}\">");
            sb.AppendLine($"    <stop offset=\"0\" stop-color=\"{Escape(light)}\"/>");
            sb.AppendLine($"    <stop offset=\"1\" stop-color=\"{Escape(color)}\"/>");
            sb.AppendLine("  </linearGradient>");
            return $"url(#{gradId})";
        }

        // Emits a subtle soft drop-shadow filter used for modern-style bar/column elevation.
        // feDropShadow is a single geometry-only primitive so it renders in every mode.
        private static void AppendSoftShadowFilter(StringBuilder sb, string filterId)
        {
            sb.AppendLine($"  <filter id=\"{Escape(filterId)}\" x=\"-20%\" y=\"-20%\" width=\"140%\" height=\"140%\">");
            sb.AppendLine("    <feDropShadow dx=\"0\" dy=\"1\" stdDeviation=\"1.5\" flood-color=\"#000000\" flood-opacity=\"0.22\"/>");
            sb.AppendLine("  </filter>");
        }

        // Emits a modern vertical hover-highlight band centred on a category slot (full plot height).
        // No-op unless ModernStyle + interactive mode + tooltips are enabled.
        private static void AppendHoverBandV(StringBuilder layer, ChartOptions options,
            double xCenter, double slotWidth, int plotHeight)
        {
            if (slotWidth <= 0 || !options.Theme.ModernStyle || !options.Tooltip.Enabled
                || options.RenderMode == SvgMode.Static) return;
            layer.AppendLine($"    <rect class=\"hover-band\" x=\"{F(xCenter - slotWidth / 2.0)}\" y=\"{PaddingTop}\" width=\"{F(slotWidth)}\" height=\"{plotHeight}\" rx=\"6\" fill=\"{ApplyAlpha(options.Theme.TextColor, 0.10)}\"/>");
        }

        // Modern horizontal hover-highlight band centred on a category row (full plot width) — for
        // horizontal bar / Gantt charts. No-op unless ModernStyle + interactive mode + tooltips are on.
        private static void AppendHoverBandH(StringBuilder layer, ChartOptions options,
            double yCenter, double slotHeight, int plotWidth)
        {
            if (slotHeight <= 0 || !options.Theme.ModernStyle || !options.Tooltip.Enabled
                || options.RenderMode == SvgMode.Static) return;
            layer.AppendLine($"    <rect class=\"hover-band\" x=\"{PaddingLeft}\" y=\"{F(yCenter - slotHeight / 2.0)}\" width=\"{plotWidth}\" height=\"{F(slotHeight)}\" rx=\"6\" fill=\"{ApplyAlpha(options.Theme.TextColor, 0.10)}\"/>");
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
            double barH   = Math.Min(groupH * 0.8, MaxBarThickness);
            double barPad = (groupH - barH) / 2.0;

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

                // Round only the outermost segment of the stack, and only at its free end.
                BarEnd barEnd = IsOuterStackSegment(options, series, ChartType.Bar, i, v >= 0)
                    ? (v >= 0 ? BarEnd.Right : BarEnd.Left)
                    : BarEnd.None;
                AppendBarShape(sb, "  ", clipId, barX, barY, barW, barH, barEnd,
                    Escape(color), "0.85", string.Empty, series, options,
                    animated, baseXPx, barY, 0, barH, dur, easing);

                if (series.DataLabel.Enabled && barW > 18)
                    AppendSegmentDataLabel(sb, barX + barW / 2, barY + barH / 2 + 4 + series.DataLabel.VerticalOffset.GetValueOrDefault(),
                        vertical: false, barX, barX + barW, FormatDataLabel(v, series.DataLabel.FormatString),
                        series.DataLabel.TextColor ?? options.Theme.TextColor, series.DataLabel.BackgroundColor, series.DataLabel.TextFontSize);

                if (options.RenderMode != SvgMode.Static)
                {
                    tooltipLayer.AppendLine($"  <g class=\"data-point\">");
                    AppendHoverBandH(tooltipLayer, options, PaddingTop + groupH * i + groupH / 2.0, groupH, plotWidth);
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

            // Same range the axis ticks use (stack totals, primary-axis series only).
            var (yMin, yMax) = ResolvePrimaryYBounds(options);

            bool hasCats = options.XAxis.Categories?.Count > 0;
            int catCount = hasCats ? options.XAxis.Categories!.Count : n;
            double groupWidth = (double)plotWidth / catCount;
            double barWidth   = Math.Min(groupWidth * 0.8, MaxBarThickness);
            double barPadding = (groupWidth - barWidth) / 2.0;

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
                // Round only the outermost segment of the stack, and only at its free end.
                BarEnd colEnd = IsOuterStackSegment(options, series, ChartType.Column, i, v >= 0)
                    ? (v >= 0 ? BarEnd.Top : BarEnd.Bottom)
                    : BarEnd.None;
                AppendBarShape(sb, "  ", clipId, x, y, barWidth, barH, colEnd,
                    Escape(color), "0.85", string.Empty, series, options,
                    animated, x, baseYPx, barWidth, 0, dur, easing);

                if (series.DataLabel.Enabled && barH > 12)
                    AppendSegmentDataLabel(sb, x + barWidth / 2, y + barH / 2 + 4 + series.DataLabel.VerticalOffset.GetValueOrDefault(),
                        vertical: true, y, y + barH, FormatDataLabel(v, series.DataLabel.FormatString),
                        series.DataLabel.TextColor ?? options.Theme.TextColor, series.DataLabel.BackgroundColor, series.DataLabel.TextFontSize);

                // Tooltip in overlay (always on top of all stacked bars)
                if (options.RenderMode != SvgMode.Static)
                {
                    tooltipLayer.AppendLine($"  <g class=\"data-point\">");
                    AppendHoverBandV(tooltipLayer, options, PaddingLeft + groupWidth * i + groupWidth / 2.0, groupWidth, plotHeight);
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
            // An explicit size is floored at MinDataLabelFontPx for readability.
            if (fontSize.HasValue) fontSize = Math.Max(MinDataLabelFontPx, fontSize.Value);
            double fs = fontSize ?? 11;   // effective font size (matches the .data-label CSS default)

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
            // font-size is appended only when overriding the default from the CSS.
            string style = fontSize.HasValue
                ? $"fill:{Escape(fill)};font-size:{fontSize}px"
                : $"fill:{Escape(fill)}";
            sb.AppendLine($"  <text x=\"{F(x)}\" y=\"{F(y)}\" text-anchor=\"middle\" class=\"data-label\" style=\"{style}\">{Escape(text)}</text>");

            // Record the footprint so movable labels resolved later can steer clear of it.
            _labelIndex?.AddBox(DataLabelBox(x, y, text, fs, !string.IsNullOrEmpty(bgColor)));
        }

        // ------------------------------------------------------------------ data-label collision avoidance
        //
        // Inside AppendSeries every cartesian data label is queued instead of written: the renderer
        // emits a placeholder comment in the label's own series group together with an ordered list
        // of candidate positions. Line series also register their segments and markers as obstacles.
        // Once all series have rendered, ResolvePendingDataLabels places bar/column labels first
        // (they can only slide within their own segment), then point labels, each taking the first
        // candidate that hits no placed label, marker or line, and swaps the placeholder for the real
        // label. The placeholder keeps the label inside its series <g>, so interactive legend
        // toggling still hides it together with its series. Labels emitted directly through
        // AppendDataLabel (pie, heatmap, …) are recorded as fixed obstacles.

        private struct LabelBox
        {
            public double X1, Y1, X2, Y2;
            public LabelBox(double x1, double y1, double x2, double y2) { X1 = x1; Y1 = y1; X2 = x2; Y2 = y2; }
            public double OverlapArea(LabelBox o)
            {
                double w = Math.Min(X2, o.X2) - Math.Max(X1, o.X1);
                double h = Math.Min(Y2, o.Y2) - Math.Max(Y1, o.Y1);
                return w > 0 && h > 0 ? w * h : 0;
            }
        }

        private struct LineObstacle
        {
            public double X1, Y1, X2, Y2, HalfWidth;
        }

        private sealed class PendingDataLabel
        {
            public int Priority, Order;
            public List<(double x, double y)> Candidates = new List<(double x, double y)>();
            public string Text = string.Empty, Fill = string.Empty;
            public string? BgColor;
            public int? FontSize;
        }

        /// <summary>
        /// Uniform-grid spatial index over placed label/marker boxes and line segments, so scoring a
        /// candidate position only inspects obstacles in the grid cells it touches instead of every
        /// obstacle in the chart (which made dense labelled charts quadratic).
        /// </summary>
        private sealed class LabelSpatialIndex
        {
            private const double CellPx = 32.0;
            private readonly List<LabelBox>     _boxes = new List<LabelBox>();
            private readonly List<LineObstacle> _lines = new List<LineObstacle>();
            private readonly Dictionary<long, List<int>> _boxCells  = new Dictionary<long, List<int>>();
            private readonly Dictionary<long, List<int>> _lineCells = new Dictionary<long, List<int>>();
            // Per-item "last query that counted me" stamps — de-duplicates items spanning several cells.
            private readonly List<int> _boxStamp  = new List<int>();
            private readonly List<int> _lineStamp = new List<int>();
            private int _query;

            private static int Cell(double v) => (int)Math.Floor(v / CellPx);
            private static long Key(int cx, int cy) => ((long)cx << 32) ^ (uint)cy;

            private static void Insert(Dictionary<long, List<int>> cells, int item,
                double x1, double y1, double x2, double y2)
            {
                int cx1 = Cell(x1), cx2 = Cell(x2), cy1 = Cell(y1), cy2 = Cell(y2);
                for (int cx = cx1; cx <= cx2; cx++)
                    for (int cy = cy1; cy <= cy2; cy++)
                    {
                        long k = Key(cx, cy);
                        if (!cells.TryGetValue(k, out var list)) cells[k] = list = new List<int>(4);
                        list.Add(item);
                    }
            }

            public void AddBox(LabelBox b)
            {
                int id = _boxes.Count;
                _boxes.Add(b);
                _boxStamp.Add(0);
                Insert(_boxCells, id, b.X1, b.Y1, b.X2, b.Y2);
            }

            public void AddLine(LineObstacle l)
            {
                int id = _lines.Count;
                _lines.Add(l);
                _lineStamp.Add(0);
                Insert(_lineCells, id,
                    Math.Min(l.X1, l.X2) - l.HalfWidth, Math.Min(l.Y1, l.Y2) - l.HalfWidth,
                    Math.Max(l.X1, l.X2) + l.HalfWidth, Math.Max(l.Y1, l.Y2) + l.HalfWidth);
            }

            /// <summary>Overlap area with placed boxes plus <paramref name="lineWeight"/> × length of line inside.</summary>
            public double Cost(LabelBox box, double lineWeight)
            {
                int q = ++_query;
                double cost = 0;
                int cx1 = Cell(box.X1), cx2 = Cell(box.X2), cy1 = Cell(box.Y1), cy2 = Cell(box.Y2);
                for (int cx = cx1; cx <= cx2; cx++)
                    for (int cy = cy1; cy <= cy2; cy++)
                    {
                        long k = Key(cx, cy);
                        if (_boxCells.TryGetValue(k, out var bl))
                            foreach (int id in bl)
                                if (_boxStamp[id] != q) { _boxStamp[id] = q; cost += box.OverlapArea(_boxes[id]); }
                        if (_lineCells.TryGetValue(k, out var ll))
                            foreach (int id in ll)
                                if (_lineStamp[id] != q) { _lineStamp[id] = q; cost += SegmentLengthInBox(_lines[id], box) * lineWeight; }
                    }
                return cost;
            }
        }

        // Per-render label state; [ThreadStatic] keeps concurrent renders isolated (see PaddingLeft).
        // Both stay null when no visible series shows data labels, so no obstacle bookkeeping is done.
        [ThreadStatic] private static LabelSpatialIndex? _labelIndex;
        [ThreadStatic] private static List<PendingDataLabel>? _pendingLabels;
        [ThreadStatic] private static int _labelRegionStart;   // sb offset where queued placeholders may appear

        private const string LabelTokenPrefix = "<!--tf-dl-";
        private const string LabelTokenSuffix = "-->";

        private const int LabelPriorityBar   = 0;   // constrained to its own bar segment — placed first
        private const int LabelPriorityPoint = 1;   // free to move around its marker — placed after

        private static double DataLabelFontPx(int? fontSize)
            => fontSize.HasValue ? Math.Max(MinDataLabelFontPx, fontSize.Value) : 11;

        // Estimated bounding box of a middle-anchored label whose baseline sits at y.
        private static LabelBox DataLabelBox(double x, double y, string text, double fs, bool hasBg)
        {
            const double gap = 2.0;   // keep a hair of space between neighbouring labels / lines
            double halfW = (text.Length * fs * (hasBg ? 0.62 : 0.6) + (hasBg ? 10 : 0)) / 2 + gap;
            double top   = y - fs * (hasBg ? 1.05 : 0.9) - gap;
            double bot   = y + fs * (hasBg ? 0.45 : 0.25) + gap;
            return new LabelBox(x - halfW, top, x + halfW, bot);
        }

        /// <summary>
        /// Starts collision-aware label layout for one render. Does nothing (zero overhead) when no
        /// visible series shows data labels.
        /// </summary>
        private static void BeginDataLabelLayout(ChartOptions options, StringBuilder sb)
        {
            _labelIndex    = null;
            _pendingLabels = null;
            if (!options.Series.Exists(s => s.Visible && s.DataLabel.Enabled)) return;
            _labelIndex       = new LabelSpatialIndex();
            _pendingLabels    = new List<PendingDataLabel>();
            _labelRegionStart = sb.Length;
        }

        /// <summary>Registers a rendered polyline (line/spline series) that data labels should avoid.</summary>
        private static void RegisterLineObstacle(List<(double x, double y, double value)> pts, double lineWidth)
        {
            var index = _labelIndex;
            if (index == null) return;
            double hw = lineWidth / 2.0 + 1.0;
            for (int i = 1; i < pts.Count; i++)
                index.AddLine(new LineObstacle { X1 = pts[i - 1].x, Y1 = pts[i - 1].y, X2 = pts[i].x, Y2 = pts[i].y, HalfWidth = hw });
        }

        /// <summary>Registers a point marker (circle of radius r) that data labels should avoid.</summary>
        private static void RegisterMarkerObstacle(double x, double y, double r)
            => _labelIndex?.AddBox(new LabelBox(x - r - 1, y - r - 1, x + r + 1, y + r + 1));

        private static void QueueDataLabel(StringBuilder sb, List<(double x, double y)> candidates, int priority,
            string text, string fill, string? bgColor, int? fontSize)
        {
            if (_pendingLabels == null)
            {
                AppendDataLabel(sb, candidates[0].x, candidates[0].y, text, fill, bgColor, fontSize);
                return;
            }
            int order = _pendingLabels.Count;
            _pendingLabels.Add(new PendingDataLabel
            {
                Priority = priority, Order = order, Candidates = candidates,
                Text = text, Fill = fill, BgColor = bgColor, FontSize = fontSize,
            });
            sb.Append(LabelTokenPrefix).Append(order).AppendLine(LabelTokenSuffix);
        }

        /// <summary>
        /// Queues a label for a point marker (line/spline). Preferred spot is <paramref name="preferredY"/>
        /// (above the marker); alternatives are below, a second tier above/below, then right/left.
        /// </summary>
        private static void AppendMovableDataLabel(StringBuilder sb, double anchorX, double anchorY,
            double preferredY, double markerRadius, string text, string fill, string? bgColor, int? fontSize)
        {
            double fs    = DataLabelFontPx(fontSize);
            double halfW = DataLabelBox(0, 0, text, fs, !string.IsNullOrEmpty(bgColor)).X2;
            double r     = Math.Max(markerRadius, 3);
            double below = anchorY + r + fs + 3;
            var candidates = new List<(double x, double y)>
            {
                (anchorX, preferredY),
                (anchorX, below),
                (anchorX, preferredY - fs - 4),
                (anchorX, below + fs + 4),
                (anchorX + r + 4 + halfW, anchorY + fs * 0.35),
                (anchorX - r - 4 - halfW, anchorY + fs * 0.35),
            };
            QueueDataLabel(sb, candidates, LabelPriorityPoint, text, fill, bgColor, fontSize);
        }

        /// <summary>
        /// Queues a label that lives inside a bar/column segment. It prefers (x, y) and may slide along
        /// the segment's long axis — between <paramref name="segStart"/> and <paramref name="segEnd"/>
        /// (Y for vertical columns, X for horizontal bars) — as long as it stays fully inside.
        /// </summary>
        private static void AppendSegmentDataLabel(StringBuilder sb, double x, double y, bool vertical,
            double segStart, double segEnd, string text, string fill, string? bgColor, int? fontSize)
        {
            double fs  = DataLabelFontPx(fontSize);
            var    box = DataLabelBox(0, 0, text, fs, !string.IsNullOrEmpty(bgColor));
            double lo  = Math.Min(segStart, segEnd), hi = Math.Max(segStart, segEnd);
            // Allowed range of the label's reference coordinate (baseline Y, or centre X).
            double min = vertical ? lo - box.Y1 : lo - box.X1;
            double max = vertical ? hi - box.Y2 : hi - box.X2;
            double pref = vertical ? y : x;

            var candidates = new List<(double x, double y)> { (x, y) };
            const double stepPx = 3.0;
            for (double d = stepPx; d <= hi - lo; d += stepPx)
            {
                foreach (double c in new[] { pref - d, pref + d })
                    if (c >= min && c <= max)
                        candidates.Add(vertical ? (x, c) : (c, y));
            }
            QueueDataLabel(sb, candidates, LabelPriorityBar, text, fill, bgColor, fontSize);
        }

        /// <summary>
        /// Queues a label that sits just outside the free end of a column (above it). Alternatives are
        /// a second tier further out, then just inside the column top when it is tall enough.
        /// </summary>
        private static void AppendColumnTopDataLabel(StringBuilder sb, double x, double y, double barTop,
            double barHeight, string text, string fill, string? bgColor, int? fontSize)
        {
            double fs = DataLabelFontPx(fontSize);
            var candidates = new List<(double x, double y)> { (x, y), (x, y - fs - 4) };
            if (barHeight > fs + 8) candidates.Add((x, barTop + fs + 3));
            QueueDataLabel(sb, candidates, LabelPriorityBar, text, fill, bgColor, fontSize);
        }

        // Length of segment s that falls inside box b (expanded by the stroke half-width), via
        // Liang–Barsky clipping; 0 when the segment misses the box. Allocation-free: it runs for
        // every candidate × nearby segment.
        private static double SegmentLengthInBox(LineObstacle s, LabelBox b)
        {
            double bx1 = b.X1 - s.HalfWidth, bx2 = b.X2 + s.HalfWidth;
            double by1 = b.Y1 - s.HalfWidth, by2 = b.Y2 + s.HalfWidth;
            double dx = s.X2 - s.X1, dy = s.Y2 - s.Y1;
            double t0 = 0, t1 = 1;
            if (!ClipEdge(-dx, s.X1 - bx1, ref t0, ref t1)) return 0;
            if (!ClipEdge( dx, bx2 - s.X1, ref t0, ref t1)) return 0;
            if (!ClipEdge(-dy, s.Y1 - by1, ref t0, ref t1)) return 0;
            if (!ClipEdge( dy, by2 - s.Y1, ref t0, ref t1)) return 0;
            return (t1 - t0) * Math.Sqrt(dx * dx + dy * dy);
        }

        // One Liang–Barsky edge test; false when the segment lies entirely outside this edge.
        private static bool ClipEdge(double p, double q, ref double t0, ref double t1)
        {
            if (p == 0) return q >= 0;
            double r = q / p;
            if (p < 0) { if (r > t0) t0 = r; }
            else       { if (r < t1) t1 = r; }
            return t0 <= t1;
        }

        private static void ResolvePendingDataLabels(StringBuilder sb, int plotWidth, int plotHeight)
        {
            var pending = _pendingLabels;
            var index   = _labelIndex;
            _pendingLabels = null;   // any later label is placed immediately
            _labelIndex    = null;
            if (pending == null || index == null || pending.Count == 0) return;

            double left = PaddingLeft, right = PaddingLeft + plotWidth;
            double top  = PaddingTop,  bottom = PaddingTop + plotHeight;

            // Choose positions: bar labels first (least freedom), then point labels, in render order.
            var markup = new string[pending.Count];
            var lbl    = new StringBuilder(160);
            _labelIndex = index;   // let AppendDataLabel record each placed label in the index
            pending.Sort((a, b) => a.Priority != b.Priority ? a.Priority.CompareTo(b.Priority) : a.Order.CompareTo(b.Order));
            foreach (var p in pending)
            {
                double fs   = DataLabelFontPx(p.FontSize);
                bool  hasBg = !string.IsNullOrEmpty(p.BgColor);

                (double x, double y) best = p.Candidates[0];
                double bestCost = double.MaxValue;
                foreach (var c in p.Candidates)
                {
                    var box = DataLabelBox(c.x, c.y, p.Text, fs, hasBg);
                    // A line through the text hurts legibility about as much as a label overlap.
                    double cost = index.Cost(box, fs);
                    // Strongly discourage leaving the plot area.
                    if (box.X1 < left || box.X2 > right || box.Y1 < top || box.Y2 > bottom) cost += 1e6;
                    if (cost < bestCost) { bestCost = cost; best = c; }
                    if (cost == 0) break;   // candidates are ordered by preference
                }

                lbl.Clear();
                AppendDataLabel(lbl, best.x, best.y, p.Text, p.Fill, p.BgColor, p.FontSize);   // also records its box
                markup[p.Order] = lbl.ToString();
            }
            _labelIndex = null;

            // Swap every placeholder for its label in a single pass over the series region
            // (one Replace per label rescanned the whole document — quadratic on big charts).
            int start = _labelRegionStart;
            string region = sb.ToString(start, sb.Length - start);
            sb.Length = start;
            int pos = 0;
            while (true)
            {
                int at = region.IndexOf(LabelTokenPrefix, pos, StringComparison.Ordinal);
                if (at < 0) break;
                int numStart = at + LabelTokenPrefix.Length;
                int numEnd   = region.IndexOf(LabelTokenSuffix, numStart, StringComparison.Ordinal);
                if (numEnd < 0) break;
                int id = 0;
                for (int i = numStart; i < numEnd; i++) id = id * 10 + (region[i] - '0');

                sb.Append(region, pos, at - pos);
                sb.Append(markup[id]);
                pos = numEnd + LabelTokenSuffix.Length;
                // The placeholder line's own newline is replaced by the label's AppendLine newline.
                if (string.CompareOrdinal(region, pos, Environment.NewLine, 0, Environment.NewLine.Length) == 0)
                    pos += Environment.NewLine.Length;
            }
            sb.Append(region, pos, region.Length - pos);
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
