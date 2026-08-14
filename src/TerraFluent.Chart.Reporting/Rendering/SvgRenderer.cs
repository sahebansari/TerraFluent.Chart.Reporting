using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TerraFluent.Chart.Reporting.Analysis;
using TerraFluent.Chart.Reporting.Enums;
using TerraFluent.Chart.Reporting.Models;

namespace TerraFluent.Chart.Reporting.Rendering
{
    /// <summary>
    /// Default SVG renderer. Produces a self-contained SVG string from <see cref="ChartOptions"/>.
    /// </summary>
    public partial class SvgRenderer : ISvgRenderer
    {
        // Minimum breathing space kept between the SVG border and any drawn content
        // (axis titles/labels), so nothing touches the canvas edge.
        private const int CanvasPadding = 32;

        // Default left padding. Horizontal bar charts widen it dynamically (see ComputeLeftPadding)
        // so long category labels on the value/category axis are not clipped at the canvas edge.
        private const int PaddingLeftDefault = 76;

        // Effective left padding for the current render. [ThreadStatic] keeps concurrent renders
        // isolated; it is assigned at the top of every Render() call before any layout runs.
        [ThreadStatic] private static int PaddingLeft;

        private const int PaddingRight  = 32;
        private const int PaddingTop    = 92;
        private const int PaddingBottom = 96;

        // Thread-safe counter — ensures every Render() call gets a unique ID,
        // preventing clipPath conflicts when multiple SVGs share one HTML document.
        private static int _renderCounter;

        /// <summary>Renders <paramref name="options"/> to an SVG string.</summary>
        /// <param name="options">The fully configured chart options. Must not be <c>null</c>.</param>
        /// <returns>A UTF-8 SVG string ready to embed or write to a file.</returns>
        public string Render(ChartOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));

            // Normalise invalid numeric inputs so geometry generation never emits
            // non-SVG tokens like NaN/Infinity.
            SanitizeNonFiniteInputs(options);

            int svgWidth  = options.Width  ?? 600;
            int svgHeight = options.Height;

            // Widen the left gutter for horizontal bar charts so long category labels fit.
            PaddingLeft = ComputeLeftPadding(options, svgWidth);

            // Give secondary Y-axis labels room on the right (they sit at plotWidth + 8px)
            bool hasSecondaryAxis = options.YAxis2 != null
                && options.Series.Exists(s => s.Visible && s.YAxisIndex == 1);
            int rightPad = hasSecondaryAxis ? 70 : PaddingRight;

            // Dynamic bottom padding: rotated X-axis labels extend DOWNWARD from each tick.
            // Vertical drop ≈ maxLabelLength × charWidth × sin(|angle|).
            // Compute this so the full label text stays within the SVG viewport.
            int labelVertH = 0;
            bool isBarChart = options.Series.Exists(s => s.Visible && s.Type == ChartType.Bar);
            if (!isBarChart && options.XAxis.Categories != null && options.XAxis.Categories.Count > 0)
            {
                var cats     = options.XAxis.Categories;
                int maxLen   = 0;
                foreach (var c in cats) if (c.Length > maxLen) maxLen = c.Length;
                double cStep = (double)(svgWidth - PaddingLeft - rightPad) / cats.Count;
                var llPre    = ComputeLabelLayout(options, cats, cStep);
                if (llPre.Rotation != 0)
                    labelVertH = (int)(maxLen * llPre.FontSize * 0.6 * Math.Abs(Math.Sin(llPre.Rotation * Math.PI / 180.0)));
                // Stagger creates an extra shifted label row — reserve its vertical offset
                if (llPre.Stagger)
                    labelVertH += llPre.StaggerOffset;
                // Word-wrap adds extra lines below the tick — reserve their height
                if (llPre.WordWrap && maxLen > llPre.MaxCharsPerLine)
                {
                    int extraLines = (int)Math.Ceiling((double)maxLen / llPre.MaxCharsPerLine) - 1;
                    labelVertH += extraLines * (llPre.FontSize + 2);
                }
            }
            // effectiveBottomPad: base space for x-axis labels + title + breathing room.
            // Extra legend space is added in the pre-pass below.
            int effectiveBottomPad = labelVertH > 0
                ? Math.Max(PaddingBottom, labelVertH + 70)
                : PaddingBottom;

            int plotWidth = svgWidth - PaddingLeft - rightPad;

            // ── Legend layout pre-pass ──────────────────────────────────────────────────────
            // Pre-compute exact legend dimensions so we can reserve the right amount of space
            // and guarantee the legend never overlaps the plot area.
            //   Top    legend → chart content shifts down (topLegendOffset); SVG grows.
            //   Bottom legend → effectiveBottomPad grows to fit legend; SVG grows to compensate.
            //   Middle / right / left → floating; no extra height needed.
            int topLegendOffset = 0;
            if (options.Legend?.Enabled == true)
            {
                double maxRowW = (double)(svgWidth - PaddingLeft - PaddingRight);
                var (_, legendH) = ComputeLegendSize(options, maxRowW);
                if (legendH > 0)
                {
                    string vAlignPre = options.Legend.VerticalAlign ?? "bottom";
                    if (string.Equals(vAlignPre, "top", StringComparison.OrdinalIgnoreCase))
                    {
                        // Push chart content down so legend sits cleanly below title/subtitle.
                        bool hasTitle_    = !string.IsNullOrEmpty(options.Title?.Text);
                        bool hasSubtitle_ = !string.IsNullOrEmpty(options.Subtitle?.Text);
                        double headerBot  = hasSubtitle_ ? 44.0 : (hasTitle_ ? 28.0 : 4.0);
                        double legendEndY = headerBot + (double)options.Legend.Margin + legendH;
                        // Legend must end at least 12 px above the plot area (y = PaddingTop).
                        topLegendOffset = (int)Math.Max(0, Math.Ceiling(legendEndY + 12.0 - PaddingTop));
                        svgHeight      += topLegendOffset;
                    }
                    else if (string.Equals(vAlignPre, "bottom", StringComparison.OrdinalIgnoreCase))
                    {
                        // Layout below plot: tick-labels | gap | [x-title | gap] | legend | bottom-pad
                        // The x-axis title (if any) sits ABOVE the legend. Bar charts render the
                        // value-axis (YAxis) title along the bottom like an x-axis title, so reserve
                        // room for it too — otherwise the title and the legend overlap.
                        int xTickH      = labelVertH > 0 ? labelVertH + 14 : 20;
                        bool hasXTitle_ = isBarChart
                            ? options.Stacking == Stacking.None && !string.IsNullOrEmpty(options.YAxis?.Title)
                            : !string.IsNullOrEmpty(options.XAxis?.Title);
                        int xTitleBlock = hasXTitle_ ? 32 : 0; // 24 title height + 8 gap after
                        int needed      = xTickH + 8 + xTitleBlock + (int)Math.Ceiling(legendH) + 8;
                        if (needed > effectiveBottomPad)
                        {
                            int extra      = needed - effectiveBottomPad;
                            svgHeight     += extra;
                            effectiveBottomPad = needed;
                        }
                    }
                }
            }

            int plotHeight = svgHeight - topLegendOffset - PaddingTop - effectiveBottomPad;

            // Data table: extend the SVG canvas downward to fit the table rows.
            int tableRowCount = 0;
            int tableHeight   = 0;
            if (options.DataTable.Visible)
            {
                var tblCats = options.XAxis?.Categories;
                int nRows = (tblCats != null && tblCats.Count > 0)
                    ? tblCats.Count
                    : (options.Series.Count > 0 ? options.Series[0].Data.Count : 0);
                tableRowCount = System.Math.Min(nRows, 30);
                tableHeight   = (tableRowCount + 1) * options.DataTable.RowHeight + 16; // +1 header, 16 padding
                svgHeight    += tableHeight;
            }

            // Range selector: reserve strip height below everything else (Interactive only).
            int rsStripH = 0;
            int rsStripY = svgHeight;
            if (options.RangeSelector.Enabled && options.RenderMode == SvgMode.Interactive)
            {
                rsStripH = options.RangeSelector.Height + 24; // 24 = gap above + x-labels room
                svgHeight += rsStripH;
            }

            // When a bottom legend is present AND the chart has a bottom x-axis title,
            // compute the explicit title y so it sits between tick labels and the legend.
            // (Default -1 lets AppendAxes fall back to its own svgHeight - 8 formula.)
            int xAxisTitleY = -1;
            {
                bool hasBottomLeg_  = options.Legend?.Enabled == true
                    && !string.Equals(options.Legend.VerticalAlign ?? "bottom", "top",
                                      StringComparison.OrdinalIgnoreCase);
                bool hasXTitleBot_  = isBarChart
                    ? options.Stacking == Stacking.None && !string.IsNullOrEmpty(options.YAxis?.Title)
                    : !string.IsNullOrEmpty(options.XAxis?.Title);
                if (hasBottomLeg_ && hasXTitleBot_)
                {
                    int xTickH_ = labelVertH > 0 ? labelVertH + 14 : 20;
                    // baseline sits 20 px below the tick-label area (8 gap + 12 for ascent).
                    xAxisTitleY = PaddingTop + topLegendOffset + plotHeight + xTickH_ + 20;
                }
            }

            string svgId  = "pc-" + System.Threading.Interlocked.Increment(ref _renderCounter)
                                        .ToString(CultureInfo.InvariantCulture);
            string clipId = svgId + "-clip";

            var sb = new StringBuilder();

            // Responsive width: null → 100% (fluid layout); fixed value → pixel size
            string widthAttr = options.Width.HasValue
                ? svgWidth.ToString(CultureInfo.InvariantCulture)
                : "100%";

            // Accessibility label (title + subtitle combined)
            string ariaAutoLabel = !string.IsNullOrEmpty(options.Title?.Text) ? options.Title.Text : "Chart";
            if (!string.IsNullOrEmpty(options.Subtitle?.Text))
                ariaAutoLabel += " \u2014 " + options.Subtitle.Text;
            string ariaLabel = options.AriaLabel ?? ariaAutoLabel;

            // Culture + RTL attributes for SVG root
            string langAttr = "";
            if (options.DisplayCulture != null)
            {
                string lc = Escape(options.DisplayCulture.TwoLetterISOLanguageName);
                langAttr = $" lang=\"{lc}\" xml:lang=\"{lc}\"";
            }
            string dirAttr = options.RightToLeft ? " dir=\"rtl\"" : "";

            // --- SVG root ---
            string titleId = svgId + "-title";
            string descId  = svgId + "-desc";
            sb.AppendLine($"<svg id=\"{svgId}\" xmlns=\"http://www.w3.org/2000/svg\" width=\"{widthAttr}\" height=\"{svgHeight}\" viewBox=\"0 0 {svgWidth} {svgHeight}\" role=\"img\"{langAttr}{dirAttr} aria-labelledby=\"{titleId}\" aria-describedby=\"{descId}\">");
            sb.AppendLine($"  <title id=\"{titleId}\">{Escape(ariaLabel)}</title>");
            string descText = !string.IsNullOrEmpty(options.AriaDescription) ? options.AriaDescription : BuildAriaDesc(options);
            sb.AppendLine($"  <desc id=\"{descId}\">{Escape(descText)}</desc>");

            // --- Embedded styles ---
            AppendStyles(sb, options, svgId);

            // --- Full SVG background ---
sb.AppendLine($"  <rect aria-hidden=\"true\" width=\"{svgWidth}\" height=\"{svgHeight}\" rx=\"12\" ry=\"12\" fill=\"{Escape(options.ResolvedBackgroundColor)}\"/>");

            // --- Chart header: title & subtitle (never shifted by topLegendOffset) ---
            if (!string.IsNullOrEmpty(options.Title?.Text))
            {
                string ts = !string.IsNullOrEmpty(options.Title.Style) ? $" style=\"{Escape(options.Title.Style)}\"" : "";
                sb.AppendLine($"  <text x=\"{svgWidth / 2}\" y=\"42\" text-anchor=\"middle\" class=\"chart-title\" fill=\"{Escape(options.Theme.TextColor)}\"{ts}>{Escape(options.Title.Text)}</text>");
            }
            if (!string.IsNullOrEmpty(options.Subtitle?.Text))
            {
                string ss = !string.IsNullOrEmpty(options.Subtitle.Style) ? $" style=\"{Escape(options.Subtitle.Style)}\"" : "";
                sb.AppendLine($"  <text x=\"{svgWidth / 2}\" y=\"60\" text-anchor=\"middle\" class=\"chart-subtitle\" fill=\"{Escape(options.Theme.TextColor)}\"{ss}>{Escape(options.Subtitle.Text)}</text>");
            }

            // Parliament charts use their own spatial encoding (hemicycle) — skip standard axes.
            // Legend is now handled by the shared AppendLegend like all other chart types.
            bool isParliamentOnly = options.Series.Count > 0
                && !options.Series.Exists(s => s.Visible && s.Type != ChartType.Parliament);

            // --- Top legend: rendered before the translate group so it sits between
            //     the header and the (shifted) plot area ---
            bool isTopLegend = options.Legend?.Enabled == true
                && string.Equals(options.Legend.VerticalAlign ?? "bottom", "top", StringComparison.OrdinalIgnoreCase);
            if (isTopLegend)
                AppendLegend(sb, options, svgWidth, svgHeight, plotHeight, clipId, effectiveBottomPad, labelVertH, topLegendOffset);

            // --- Translate group: shifts plot, axes, series, and tooltip layer
            //     down by topLegendOffset so the top legend has unobstructed space above. ---
            if (topLegendOffset > 0)
                sb.AppendLine($"  <g transform=\"translate(0,{topLegendOffset})\">" );

            // --- Plot background (optional, from theme) ---
            if (!string.IsNullOrEmpty(options.Theme.PlotBackgroundColor)
                && options.Theme.PlotBackgroundColor != ChartColor.None)
                sb.AppendLine($"  <rect aria-hidden=\"true\" x=\"{PaddingLeft}\" y=\"{PaddingTop}\" width=\"{plotWidth}\" height=\"{plotHeight}\" fill=\"{Escape(options.Theme.PlotBackgroundColor)}\"/>");

            // --- Plot area clip ---
            sb.AppendLine($"  <clipPath id=\"{clipId}\"><rect x=\"{PaddingLeft}\" y=\"{PaddingTop}\" width=\"{plotWidth}\" height=\"{plotHeight}\"/></clipPath>");

            // --- Grid & axes ---
            // When inside a translate group, pass (svgHeight - topLegendOffset) so the
            // x-axis title (at y = svgHeight_arg - 8) ends up at the correct absolute position.
            int svgHC = topLegendOffset > 0 ? svgHeight - topLegendOffset : svgHeight;
            // Parliament charts carry their own spatial encoding; skip standard axes.
            if (!isParliamentOnly)
                AppendAxes(sb, options, svgWidth, svgHC, plotWidth, plotHeight, xAxisTitleY, options.DisplayCulture, svgId);

            // --- Series (shapes only; tooltips go into tooltip overlay) ---
            var tooltipLayer = new StringBuilder();
            AppendSeries(sb, options, svgWidth, svgHC, plotWidth, plotHeight, clipId, tooltipLayer, xAxisTitleY);

            // --- Annotations (labels / shapes drawn above the series) ---
            if (options.Annotations.Count > 0)
                AppendAnnotations(sb, options, plotWidth, plotHeight, clipId);

            // --- Tooltip overlay (inside translate group for correct y-coordinates) ---
            if (tooltipLayer.Length > 0)
            {
                sb.AppendLine($"  <g id=\"{clipId}-tips\">");
                sb.Append(tooltipLayer);
                sb.AppendLine("  </g>");
            }

            // --- Close translate group ---
            if (topLegendOffset > 0)
                sb.AppendLine("  </g>");

            // --- Bottom / middle / right / left legend ---
            if (options.Legend?.Enabled == true && !isTopLegend)
                AppendLegend(sb, options, svgWidth, svgHeight, plotHeight, clipId, effectiveBottomPad, labelVertH, topLegendOffset);

            // --- Export button / menu (Interactive mode only) ---
            if (options.RenderMode == SvgMode.Interactive && options.ExportButtonEnabled)
                AppendExportButton(sb, options, svgWidth);
            if (options.RenderMode == SvgMode.Interactive && options.ExportMenuEnabled)
                AppendExportMenu(sb, options, svgWidth);

            // --- Range selector strip (Interactive + Enabled) ---
            if (rsStripH > 0)
                AppendRangeSelectorStrip(sb, options, svgId, svgWidth, plotWidth, plotHeight, rsStripY + 8, options.RangeSelector.Height);

            // --- Drilldown child charts (pre-rendered, hidden until clicked) ---
            AppendDrilldownCharts(sb, options, svgId, svgWidth, svgHeight);

            // --- Embedded JS (interactive mode) ---
            if (options.RenderMode == SvgMode.Interactive)
                AppendInteractiveScript(sb, options, svgId, plotWidth, plotHeight);

            // --- Data table (below all other content) ---
            if (options.DataTable.Visible && tableRowCount > 0)
            {
                int tableY = svgHeight - tableHeight;
                AppendDataTable(sb, options, svgWidth, tableY, tableRowCount);
            }

            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        // Horizontal bar charts draw category labels in the left gutter (anchored at PaddingLeft - 8).
        // Estimate the widest label and widen the gutter so long text is not clipped at the edge,
        // capped so labels never consume more than ~45% of the canvas width.
        private static int ComputeLeftPadding(ChartOptions options, int svgWidth)
        {
            int cap = (int)(svgWidth * 0.45);
            double scale = FontScaleOf(options); // widen the gutter proportionally when text is enlarged

            bool isBar = options.Series.Exists(s => s.Visible && s.Type == ChartType.Bar);
            var cats   = options.XAxis?.Categories;
            if (isBar && cats != null && cats.Count > 0)
            {
                int maxLen = 0;
                foreach (var c in cats)
                {
                    int len = c?.Length ?? 0;
                    if (len > maxLen) maxLen = len;
                }
                if (maxLen == 0) return PaddingLeftDefault;

                // .axis-label is 11px (× scale); estimated glyph width ≈ fontSize × 0.6.
                int catLabelWidth = (int)Math.Ceiling(maxLen * 11 * scale * 0.6);

                // Category labels grow LEFTWARD from x = PaddingLeft − 8. When a category-axis
                // title is present it is drawn rotated at x = CanvasPadding, so reserve an extra
                // title band before the labels; otherwise just add the 8px offset + breathing room.
                int catNeeded = string.IsNullOrEmpty(options.XAxis?.Title)
                    ? catLabelWidth + 14
                    : CanvasPadding + (int)Math.Ceiling(22 * scale) + catLabelWidth + 8;
                return Math.Max(PaddingLeftDefault, Math.Min(catNeeded, cap));
            }

            // Vertical charts (column/line/area/…): the numeric Y-axis tick labels grow LEFTWARD
            // from x = PaddingLeft − 6. Reserve room for the widest label plus the rotated axis
            // title band so wide labels (e.g. "1,234,567") never overlap the Y-axis title.
            int labelWidth = EstimateMaxYTickLabelWidth(options);
            if (labelWidth <= 0) return PaddingLeftDefault;

            // Title band: rotated 12px (× scale) axis title occupies a ~14px horizontal strip near
            // the left edge (at x = CanvasPadding) plus an 8px gap; 0 when there is no title.
            int titleBand = string.IsNullOrEmpty(options.YAxis?.Title) ? 0 : (int)Math.Ceiling(22 * scale);
            // Label left edge = PaddingLeft − 6 − labelWidth; keep it clear of the title band /
            // canvas edge (CanvasPadding) with a small gap.
            int needed = CanvasPadding + titleBand + labelWidth + 6;
            return Math.Max(PaddingLeftDefault, Math.Min(needed, cap));
        }

        /// <summary>
        /// Estimates the pixel width of the widest primary Y-axis tick label for the current
        /// options, replicating the tick generation and formatting used by <c>AppendAxes</c>.
        /// Returns 0 when the axis is hidden or has no ticks.
        /// </summary>
        private static int EstimateMaxYTickLabelWidth(ChartOptions options)
        {
            if (options.YAxis == null || !options.YAxis.Visible) return 0;

            bool yLog = IsLog(options.YAxis);
            var (yMinRaw, yMaxRaw) = ResolveYBounds(options.YAxis, options.Series);
            string? yTickFmt = options.YAxis.LabelFormat
                ?? (options.Stacking == Stacking.Percent ? "{value}%" : null);

            List<double> yTicks;
            if (yLog)
            {
                yTicks = GenerateLogTicks(yMinRaw, yMaxRaw);
            }
            else
            {
                double tickStep = options.YAxis.TickInterval.HasValue && options.YAxis.TickInterval.Value > 0
                    ? options.YAxis.TickInterval.Value
                    : NiceStep((yMaxRaw - yMinRaw) / 5);
                double firstTick = Math.Floor(yMinRaw / tickStep) * tickStep;
                yTicks = EnumerateLinearTicks(firstTick, yMaxRaw, tickStep);
            }

            int maxLen = 0;
            foreach (double tick in yTicks)
            {
                int len = FormatAxisTick(tick, yTickFmt).Length;
                if (len > maxLen) maxLen = len;
            }
            if (maxLen == 0) return 0;

            // .axis-label is 11px (× scale); estimated glyph width ≈ fontSize × 0.6.
            return (int)Math.Ceiling(maxLen * 11 * FontScaleOf(options) * 0.6);
        }

    }
}
