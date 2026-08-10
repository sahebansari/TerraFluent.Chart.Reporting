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
        private static void AppendAutoInsightOverlays(
            StringBuilder sb, Series series, string seriesColor,
            ChartOptions options, int plotWidth, int plotHeight,
            double yMin, double yMax, double step, string clipId)
        {
            var ai   = series.AutoInsight!;
            var data = series.Data;
            if (data.Count < 2) return;

            bool   hasCats = options.XAxis.Categories?.Count > 0;
            double xOffset = hasCats ? step / 2.0 : 0.0;
            bool   yLog    = IsLog(series.YAxisIndex == 1 ? options.YAxis2 : options.YAxis);

            // Coordinate helpers — mirror the same transform used by AppendLineSeries.
            double Px(int i) => PaddingLeft + xOffset + i * step;
            double Py(double v) =>
                PaddingTop + plotHeight - Frac(v, yMin, yMax, yLog) * plotHeight;

            // ── 1. Anomaly band ───────────────────────────────────────────────────────
            if (ai.ShowAnomalyBands)
            {
                var (_, _, lower, upper) = SeriesAnalyzer.StdDevBands(data, ai.AnomalyBandsSigma);
                double yUpper  = Math.Max(yMin, Math.Min(yMax, upper));
                double yLower  = Math.Max(yMin, Math.Min(yMax, lower));
                double rectTop = Math.Min(Py(yUpper), Py(yLower));
                double rectBot = Math.Max(Py(yUpper), Py(yLower));
                double rectH   = rectBot - rectTop;
                if (rectH > 0.5)
                {
                    string bandFill = ai.AnomalyBandColor ?? seriesColor;
                    // Band spans only the first..last non-null indices so null-padded slots are excluded.
                    int bandFirst = 0, bandLast = data.Count - 1;
                    while (bandFirst < data.Count && !data[bandFirst].HasValue) bandFirst++;
                    while (bandLast > bandFirst  && !data[bandLast].HasValue)  bandLast--;
                    double x1 = Px(bandFirst);
                    double x2 = Px(bandLast);
                    sb.AppendLine($"  <rect clip-path=\"url(#{clipId})\" x=\"{F(x1)}\" y=\"{F(rectTop)}\" width=\"{F(x2 - x1)}\" height=\"{F(rectH)}\" fill=\"{Escape(bandFill)}\" fill-opacity=\"0.12\" stroke=\"{Escape(bandFill)}\" stroke-opacity=\"0.25\" stroke-width=\"1\"/>");
                }
            }

            // ── 2. Trend line ──────────────────────────────────────────────────────────
            if (ai.ShowTrendLine)
            {
                var (slope, intercept) = SeriesAnalyzer.LinearRegression(data);
                double v0 = intercept;
                double v1 = slope * (data.Count - 1) + intercept;
                double tx0 = Px(0);
                double ty0 = Py(v0);
                double tx1 = Px(data.Count - 1);
                double ty1 = Py(v1);
                sb.AppendLine($"  <line clip-path=\"url(#{clipId})\" x1=\"{F(tx0)}\" y1=\"{F(ty0)}\" x2=\"{F(tx1)}\" y2=\"{F(ty1)}\" stroke=\"{Escape(ai.TrendLineColor ?? options.Theme.TextColor)}\" stroke-width=\"1.5\" stroke-dasharray=\"6,3\" stroke-linecap=\"round\"/>");
            }

            // ── 3. Moving average ─────────────────────────────────────────────────────
            if (ai.ShowMovingAverage)
            {
                double?[] ma        = SeriesAnalyzer.MovingAverage(data, ai.MovingAveragePeriod);
                string    maColor   = ai.MovingAverageColor ?? seriesColor;
                var       maPts     = new StringBuilder();
                bool      maFirst   = true;
                for (int i = 0; i < ma.Length; i++)
                {
                    if (!ma[i].HasValue) continue;
                    double mpx = Px(i);
                    double mpy = Py(ma[i]!.Value);
                    if (maFirst) { maPts.Append($"M{F(mpx)},{F(mpy)}");  maFirst = false; }
                    else          maPts.Append($" L{F(mpx)},{F(mpy)}");
                }
                if (!maFirst)
                    sb.AppendLine($"  <path clip-path=\"url(#{clipId})\" d=\"{maPts}\" fill=\"none\" stroke=\"{Escape(maColor)}\" stroke-width=\"2\" stroke-dasharray=\"4,2\" stroke-opacity=\"0.85\" stroke-linecap=\"round\"/>");
            }

            // ── 4. Peak highlights ────────────────────────────────────────────────────
            // Redesigned as badge callouts (filled pill + leader line + dot) so the label
            // never overlaps the title/subtitle and the indicator never gets half-clipped
            // at the plot edge regardless of how close the extreme value is to yMax/yMin.
            if (ai.HighlightPeaks)
            {
                var (maxIdx, maxVal, minIdx, minVal) = SeriesAnalyzer.FindPeaks(data);
                double plotTop    = PaddingTop;
                double plotBottom = PaddingTop + plotHeight;
                const double BadgeH    = 16.0;   // badge height (px)
                const double BadgePadX = 7.0;    // horizontal padding inside badge
                const double DotR      = 4.0;    // highlight dot radius
                const double SafeInset = 4.0;    // minimum gap from plot edges

                if (maxIdx >= 0)
                {
                    double mpx      = Px(maxIdx);
                    double mpy      = Py(maxVal);
                    string shortVal = SeriesAnalyzer.FormatShort(maxVal);
                    double badgeW   = shortVal.Length * 6.5 + BadgePadX * 2;
                    string posColor = options.Theme.PositiveColor;

                    // Prefer badge ABOVE the point; flip BELOW when too close to the top edge
                    double badgeCy = mpy - 22;
                    if (badgeCy - BadgeH / 2 < plotTop + SafeInset)
                        badgeCy = Math.Min(mpy + 22, plotBottom - BadgeH / 2 - SafeInset);
                    badgeCy = Math.Max(badgeCy, plotTop  + BadgeH / 2 + SafeInset);
                    badgeCy = Math.Min(badgeCy, plotBottom - BadgeH / 2 - SafeInset);

                    // Dashed leader line from data point to badge centre
                    sb.AppendLine($"  <line clip-path=\"url(#{clipId})\" x1=\"{F(mpx)}\" y1=\"{F(mpy)}\" x2=\"{F(mpx)}\" y2=\"{F(badgeCy)}\" stroke=\"{Escape(posColor)}\" stroke-width=\"1\" stroke-dasharray=\"3,2\" opacity=\"0.7\"/>");
                    // Highlight dot at data point
                    sb.AppendLine($"  <circle clip-path=\"url(#{clipId})\" cx=\"{F(mpx)}\" cy=\"{F(mpy)}\" r=\"{F(DotR)}\" fill=\"{Escape(posColor)}\"/>");
                    // Badge background — tinted positive-colour pill
                    sb.AppendLine($"  <rect clip-path=\"url(#{clipId})\" x=\"{F(mpx - badgeW / 2)}\" y=\"{F(badgeCy - BadgeH / 2)}\" width=\"{F(badgeW)}\" height=\"{F(BadgeH)}\" rx=\"8\" fill=\"{Escape(posColor)}\" fill-opacity=\"0.18\" stroke=\"{Escape(posColor)}\" stroke-width=\"1.2\"/>");
                    // Badge value label
                    sb.AppendLine($"  <text clip-path=\"url(#{clipId})\" x=\"{F(mpx)}\" y=\"{F(badgeCy + 4.5)}\" text-anchor=\"middle\" font-size=\"10\" fill=\"{Escape(posColor)}\" font-weight=\"700\">{Escape(shortVal)}</text>");
                }

                if (minIdx >= 0 && minIdx != maxIdx)
                {
                    double mpx      = Px(minIdx);
                    double mpy      = Py(minVal);
                    string shortVal = SeriesAnalyzer.FormatShort(minVal);
                    double badgeW   = shortVal.Length * 6.5 + BadgePadX * 2;
                    string negColor = options.Theme.NegativeColor;

                    // Prefer badge BELOW the point; flip ABOVE when too close to the bottom edge
                    double badgeCy = mpy + 22;
                    if (badgeCy + BadgeH / 2 > plotBottom - SafeInset)
                        badgeCy = Math.Max(mpy - 22, plotTop + BadgeH / 2 + SafeInset);
                    badgeCy = Math.Max(badgeCy, plotTop  + BadgeH / 2 + SafeInset);
                    badgeCy = Math.Min(badgeCy, plotBottom - BadgeH / 2 - SafeInset);

                    sb.AppendLine($"  <line clip-path=\"url(#{clipId})\" x1=\"{F(mpx)}\" y1=\"{F(mpy)}\" x2=\"{F(mpx)}\" y2=\"{F(badgeCy)}\" stroke=\"{Escape(negColor)}\" stroke-width=\"1\" stroke-dasharray=\"3,2\" opacity=\"0.7\"/>");
                    sb.AppendLine($"  <circle clip-path=\"url(#{clipId})\" cx=\"{F(mpx)}\" cy=\"{F(mpy)}\" r=\"{F(DotR)}\" fill=\"{Escape(negColor)}\"/>");
                    sb.AppendLine($"  <rect clip-path=\"url(#{clipId})\" x=\"{F(mpx - badgeW / 2)}\" y=\"{F(badgeCy - BadgeH / 2)}\" width=\"{F(badgeW)}\" height=\"{F(BadgeH)}\" rx=\"8\" fill=\"{Escape(negColor)}\" fill-opacity=\"0.18\" stroke=\"{Escape(negColor)}\" stroke-width=\"1.2\"/>");
                    sb.AppendLine($"  <text clip-path=\"url(#{clipId})\" x=\"{F(mpx)}\" y=\"{F(badgeCy + 4.5)}\" text-anchor=\"middle\" font-size=\"10\" fill=\"{Escape(negColor)}\" font-weight=\"700\">{Escape(shortVal)}</text>");
                }
            }

            // ── 5. Narrative summary ──────────────────────────────────────────────────
            if (ai.ShowNarrativeSummary)
            {
                var (slope2, _)        = SeriesAnalyzer.LinearRegression(data);
                var (mean2, sd2, _, _) = SeriesAnalyzer.StdDevBands(data, ai.AnomalyBandsSigma);
                int anomalies = ai.ShowAnomalyBands
                    ? SeriesAnalyzer.CountAnomalies(data, mean2, sd2, ai.AnomalyBandsSigma)
                    : 0;
                string narrative = SeriesAnalyzer.BuildNarrative(data, slope2, mean2, anomalies);

                // Derive text colour from trend direction:
                //   positive slope → theme PositiveColor, negative → NegativeColor, flat → TextColor.
                double trendThreshold = mean2 > 0 ? 0.005 * mean2 : 0.001;
                string narrativeColor = slope2 > trendThreshold  ? options.Theme.PositiveColor
                                      : slope2 < -trendThreshold ? options.Theme.NegativeColor
                                      :                             options.Theme.TextColor;

                // ~7 px per character at font-size 12; add fixed padding for the pill.
                const double NarrCharW = 7.0;
                const double NarrPadX  = 10.0;
                const double NarrH     = 20.0;
                double textW   = narrative.Length * NarrCharW;
                double pillW   = textW + NarrPadX * 2;
                // Bottom-right corner, inset from the plot edges so it never clips
                double pillX   = PaddingLeft + plotWidth - pillW - 6;
                double pillY   = PaddingTop  + plotHeight - NarrH - 8;

                // Outer pill — chart background + series-coloured border
                sb.AppendLine($"  <rect clip-path=\"url(#{clipId})\" x=\"{F(pillX)}\" y=\"{F(pillY)}\" width=\"{F(pillW)}\" height=\"{F(NarrH)}\" rx=\"10\" fill=\"{Escape(options.ResolvedBackgroundColor)}\" fill-opacity=\"0.97\" stroke=\"{Escape(seriesColor)}\" stroke-width=\"1.2\" stroke-opacity=\"0.70\"/>");
                // Narrative text — bold, colour-coded by trend direction
                sb.AppendLine($"  <text clip-path=\"url(#{clipId})\" x=\"{F(pillX + pillW / 2)}\" y=\"{F(pillY + NarrH * 0.68)}\" text-anchor=\"middle\" font-size=\"12\" font-weight=\"700\" fill=\"{Escape(narrativeColor)}\">{Escape(narrative)}</text>");
            }
        }

        /// <summary>Short-hand: format a coordinate to 1 decimal place, invariant culture.</summary>
        private static string F(double v) => v.ToString("F1", CultureInfo.InvariantCulture);

        // ------------------------------------------------------------------ label layout

        private struct LabelLayoutResult
        {
            public int    Rotation;
            public int    Stride;
            public int    FontSize;
            public bool   Stagger;
            public int    StaggerOffset;
            public bool   WordWrap;
            public int    MaxCharsPerLine;
        }

        /// <summary>
        /// Resolves rotation, stride, font size, stagger, and word-wrap settings for X-axis
        /// category labels.  When <see cref="Models.LabelLayoutOptions"/> is configured on the
        /// chart options the full collision-detection algorithm runs; otherwise legacy behaviour
        /// (auto-rotate at -45° when labels are too wide) is preserved exactly.
        /// </summary>
        private static LabelLayoutResult ComputeLabelLayout(
            Models.ChartOptions options,
            System.Collections.Generic.List<string> cats,
            double step)
        {
            const double CharWidthFactor = 0.6; // estimated char width relative to font size

            var ll = options.LabelLayout;

            // ---- Legacy path (no LabelLayout configured) --------------------------------
            if (ll == null)
            {
                int maxLen = 0;
                foreach (var c in cats) if (c.Length > maxLen) maxLen = c.Length;
                int rot = options.XAxis.LabelRotation
                    ?? (maxLen * 7.0 > step * 0.8 ? -45 : 0);
                bool xIsDateLeg = options.XAxis.Type == Enums.AxisType.DateTime;
                int strideLeg = (xIsDateLeg && cats.Count > 12)
                    ? (int)Math.Ceiling(cats.Count / 12.0)
                    : 1;
                return new LabelLayoutResult
                {
                    Rotation       = rot,
                    Stride         = strideLeg,
                    FontSize       = 11,
                    Stagger        = false,
                    StaggerOffset  = 10,
                    WordWrap       = false,
                    MaxCharsPerLine = 12,
                };
            }

            // ---- Full collision-detection path ------------------------------------------
            int hPad = ll.HorizontalPadding;

            // Estimate whether labels at given (fontSize, rotation, stride) fit without overlap
            bool FitsAt(int fs, int rot, int stride_)
            {
                double charW = fs * CharWidthFactor;
                double maxLabelW = 0;
                foreach (var c in cats)
                {
                    double w = c.Length * charW;
                    if (w > maxLabelW) maxLabelW = w;
                }
                double radAbs = Math.Abs(rot) * Math.PI / 180.0;
                double lineH  = fs + 2;
                // Bounding-box footprint of a rotated text block projected onto the X axis
                double projW = maxLabelW * Math.Cos(radAbs) + lineH * Math.Sin(radAbs);
                return (projW + hPad) <= step * stride_;
            }

            int resolvedFontSize = ll.MaxFontSize;
            int resolvedRotation = ll.Rotation ?? 0;
            int resolvedStride   = ll.Stride   ?? 1;

            if (ll.Rotation.HasValue)
            {
                // Explicit rotation: only apply font scaling if needed
                if (ll.AutoScale && !FitsAt(resolvedFontSize, resolvedRotation, resolvedStride))
                {
                    for (int fs = resolvedFontSize - 1; fs >= ll.MinFontSize; fs--)
                    {
                        if (FitsAt(fs, resolvedRotation, resolvedStride)) { resolvedFontSize = fs; break; }
                        if (fs == ll.MinFontSize) resolvedFontSize = fs;
                    }
                }
            }
            else if (ll.AutoRotate)
            {
                bool solved = FitsAt(resolvedFontSize, 0, resolvedStride);
                if (!solved && ll.AutoScale)
                {
                    // Try shrinking the font at 0° first
                    for (int fs = resolvedFontSize - 1; fs >= ll.MinFontSize; fs--)
                    {
                        if (FitsAt(fs, 0, resolvedStride)) { resolvedFontSize = fs; solved = true; break; }
                    }
                }
                if (!solved)
                {
                    // Try progressively steeper rotations
                    int[] angles = new[] { -30, -45, -60, -90 };
                    foreach (int angle in angles)
                    {
                        if (Math.Abs(angle) > ll.MaxRotation) break;
                        // Try full font size first, then scaled
                        if (FitsAt(ll.MaxFontSize, angle, resolvedStride))
                        {
                            resolvedRotation = angle;
                            resolvedFontSize = ll.MaxFontSize;
                            solved = true;
                            break;
                        }
                        if (ll.AutoScale)
                        {
                            for (int fs = ll.MaxFontSize - 1; fs >= ll.MinFontSize; fs--)
                            {
                                if (FitsAt(fs, angle, resolvedStride))
                                {
                                    resolvedRotation = angle;
                                    resolvedFontSize = fs;
                                    solved = true;
                                    break;
                                }
                            }
                            if (solved) break;
                        }
                    }
                    if (!solved)
                    {
                        resolvedRotation = -ll.MaxRotation;
                        resolvedFontSize = ll.MinFontSize;
                    }
                }
            }

            // Auto-skip: compute minimum stride that eliminates any remaining overlap
            if (ll.AutoSkip && !FitsAt(resolvedFontSize, resolvedRotation, resolvedStride))
            {
                double charW = resolvedFontSize * CharWidthFactor;
                double maxLabelW = 0;
                foreach (var c in cats)
                {
                    double w = c.Length * charW;
                    if (w > maxLabelW) maxLabelW = w;
                }
                double radAbs = Math.Abs(resolvedRotation) * Math.PI / 180.0;
                double lineH  = resolvedFontSize + 2;
                double projW  = maxLabelW * Math.Cos(radAbs) + lineH * Math.Sin(radAbs) + hPad;
                if (step > 0)
                    resolvedStride = Math.Max(resolvedStride, (int)Math.Ceiling(projW / step));
            }

            // Explicit stride always wins if larger than what collision detection chose
            if (ll.Stride.HasValue && ll.Stride.Value > resolvedStride)
                resolvedStride = ll.Stride.Value;

            return new LabelLayoutResult
            {
                Rotation        = resolvedRotation,
                Stride          = resolvedStride,
                FontSize        = resolvedFontSize,
                Stagger         = ll.Stagger,
                StaggerOffset   = ll.StaggerOffset,
                WordWrap        = ll.WordWrap,
                MaxCharsPerLine = ll.MaxCharsPerLine,
            };
        }

        /// <summary>
        /// Splits <paramref name="text"/> into lines of at most <paramref name="maxChars"/> characters,
        /// breaking at whitespace boundaries. Returns at least one line.
        /// </summary>
        private static System.Collections.Generic.List<string> WrapLabel(string text, int maxChars)
        {
            var lines  = new System.Collections.Generic.List<string>();
            var words  = text.Split(new[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            var current = new System.Text.StringBuilder();
            foreach (var word in words)
            {
                if (current.Length == 0)
                {
                    current.Append(word);
                }
                else if (current.Length + 1 + word.Length <= maxChars)
                {
                    current.Append(' ');
                    current.Append(word);
                }
                else
                {
                    lines.Add(current.ToString());
                    current.Clear();
                    current.Append(word);
                }
            }
            if (current.Length > 0) lines.Add(current.ToString());
            if (lines.Count == 0)   lines.Add(text);
            return lines;
        }

        /// <summary>
        /// Applies a CSS <c>rgba()</c> alpha to an arbitrary hex or rgba colour string.
        /// Accepts <c>#rgb</c>, <c>#rrggbb</c>, and <c>rgba(...)</c> inputs.
        /// Returns an <c>rgba(r,g,b,alpha)</c> string suitable for inline SVG/CSS.
        /// </summary>
        private static string ApplyAlpha(string color, double alpha)
        {
            alpha = Math.Max(0.0, Math.Min(1.0, alpha));
            string a = alpha.ToString("F2", CultureInfo.InvariantCulture);
            color = color?.Trim() ?? string.Empty;

            if (color.StartsWith("#", System.StringComparison.Ordinal))
            {
                string hex = color.Substring(1);
                if (hex.Length == 3)
                    hex = new string(new char[] { hex[0], hex[0], hex[1], hex[1], hex[2], hex[2] });
                if (hex.Length == 6)
                {
#if NET6_0_OR_GREATER
                    int r = System.Convert.ToInt32(hex[0..2], 16);
                    int g = System.Convert.ToInt32(hex[2..4], 16);
                    int b = System.Convert.ToInt32(hex[4..6], 16);
#else
                    int r = System.Convert.ToInt32(hex.Substring(0, 2), 16);
                    int g = System.Convert.ToInt32(hex.Substring(2, 2), 16);
                    int b = System.Convert.ToInt32(hex.Substring(4, 2), 16);
#endif
                    return $"rgba({r},{g},{b},{a})";
                }
            }

            // Already rgba/rgb — replace existing alpha.
            var m = System.Text.RegularExpressions.Regex.Match(
                color, @"rgba?\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success)
                return $"rgba({m.Groups[1].Value},{m.Groups[2].Value},{m.Groups[3].Value},{a})";

            return color; // fallback: return unchanged
        }

        /// <summary>
        /// Builds an SVG path <c>d</c> string from a list of plot points.
        /// When <paramref name="smooth"/> is <c>true</c> and there are ≥3 points,
        /// Catmull-Rom spline control points (cubic Bézier) are used for smooth curves.
        /// </summary>
        private static string BuildPath(List<(double x, double y, double value)> pts, bool smooth)
        {
            if (pts.Count == 0) return string.Empty;
            var sb = new StringBuilder();
            sb.Append($"M{F(pts[0].x)},{F(pts[0].y)}");
            if (!smooth || pts.Count < 3)
            {
                for (int i = 1; i < pts.Count; i++)
                    sb.Append($" L{F(pts[i].x)},{F(pts[i].y)}");
            }
            else
            {
                // Catmull-Rom → Cubic Bézier conversion
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    var p0 = i > 0 ? pts[i - 1] : pts[0];
                    var p1 = pts[i];
                    var p2 = pts[i + 1];
                    var p3 = i + 2 < pts.Count ? pts[i + 2] : pts[i + 1];
                    double cp1x = p1.x + (p2.x - p0.x) / 6.0;
                    double cp1y = p1.y + (p2.y - p0.y) / 6.0;
                    double cp2x = p2.x - (p3.x - p1.x) / 6.0;
                    double cp2y = p2.y - (p3.y - p1.y) / 6.0;
                    sb.Append($" C{F(cp1x)},{F(cp1y)} {F(cp2x)},{F(cp2y)} {F(p2.x)},{F(p2.y)}");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Maps an <see cref="Easing"/> value to SMIL <c>calcMode</c>/<c>keySplines</c> attributes.
        /// Returns an empty string for <see cref="Easing.Linear"/> (SMIL default).
        /// </summary>
        private static string SmilEasing(Easing easing)
        {
            switch (easing)
            {
                case Easing.EaseIn:    return " calcMode=\"spline\" keyTimes=\"0;1\" keySplines=\"0.42,0,1,1\"";
                case Easing.EaseOut:   return " calcMode=\"spline\" keyTimes=\"0;1\" keySplines=\"0,0,0.58,1\"";
                case Easing.EaseInOut: return " calcMode=\"spline\" keyTimes=\"0;1\" keySplines=\"0.42,0,0.58,1\"";
                // Bounce/Elastic cannot be expressed natively in SMIL; approximate with EaseOut
                case Easing.Bounce:
                case Easing.Elastic:   return " calcMode=\"spline\" keyTimes=\"0;1\" keySplines=\"0,0,0.58,1\"";
                default:               return string.Empty;
            }
        }

        /// <summary>
        /// Converts non-finite numeric inputs (NaN/Infinity) into safe render-time values.
        /// Nullable scalar data points are turned into gaps; structured points with any
        /// non-finite numeric component are dropped.
        /// </summary>
        private static void SanitizeNonFiniteInputs(ChartOptions options)
        {
            static bool Finite(double v) => !(double.IsNaN(v) || double.IsInfinity(v));

            static void SanitizeAxis(Axis axis)
            {
                if (axis.Min.HasValue && !Finite(axis.Min.Value)) axis.Min = null;
                if (axis.Max.HasValue && !Finite(axis.Max.Value)) axis.Max = null;
                if (axis.TickInterval.HasValue && !Finite(axis.TickInterval.Value)) axis.TickInterval = null;
            }

            SanitizeAxis(options.XAxis);
            SanitizeAxis(options.YAxis);
            if (options.YAxis2 != null) SanitizeAxis(options.YAxis2);

            foreach (var series in options.Series)
            {
                for (int i = 0; i < series.Data.Count; i++)
                {
                    if (series.Data[i].HasValue && !Finite(series.Data[i]!.Value))
                        series.Data[i] = null;
                }

                series.BubbleData.RemoveAll(p => !Finite(p.X) || !Finite(p.Y) || !Finite(p.Z));
                series.RangeData.RemoveAll(p => !Finite(p.Low) || !Finite(p.High));
                series.BoxPlotData.RemoveAll(p => !Finite(p.Low) || !Finite(p.Q1) || !Finite(p.Median) || !Finite(p.Q3) || !Finite(p.High));
                series.OhlcData.RemoveAll(p => !Finite(p.Open) || !Finite(p.High) || !Finite(p.Low) || !Finite(p.Close));
                series.HeatmapData.RemoveAll(p => !Finite(p.Value));
            }
        }

        /// <summary>
        /// XML-escapes a user-provided string for safe emission into SVG/XML markup.
        /// Escapes the five XML entity characters (<c>&amp;</c>, <c>&lt;</c>, <c>&gt;</c>,
        /// <c>"</c>, <c>'</c>) and removes control characters that are illegal in XML 1.0
        /// (all C0 controls except tab, LF and CR, plus <c>U+FFFE</c>/<c>U+FFFF</c>).
        /// Returns an empty string for <c>null</c> input.
        /// </summary>
        public static string Escape(string? text)
        {
            if (text is null) return string.Empty;
            string escaped = text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
            return StripInvalidXmlChars(escaped);
        }

        /// <summary>Returns <c>true</c> if <paramref name="c"/> is a legal XML 1.0 character.</summary>
        private static bool IsLegalXmlChar(char c)
        {
            return c == '\t' || c == '\n' || c == '\r'
                || (c >= 0x20 && c <= 0xFFFD);
        }

        /// <summary>
        /// Removes characters that are illegal in XML 1.0. Allocates only when an illegal
        /// character is actually present, so the common case is a zero-copy fast path.
        /// </summary>
        private static string StripInvalidXmlChars(string text)
        {
            int i = 0;
            while (i < text.Length && IsLegalXmlChar(text[i])) i++;
            if (i == text.Length) return text;

            var sb = new StringBuilder(text.Length);
            sb.Append(text, 0, i);
            for (; i < text.Length; i++)
                if (IsLegalXmlChar(text[i])) sb.Append(text[i]);
            return sb.ToString();
        }
    }
}
