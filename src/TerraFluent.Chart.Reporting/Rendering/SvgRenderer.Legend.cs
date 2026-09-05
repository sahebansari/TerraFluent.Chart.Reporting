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
        private static (double W, double H) ComputeLegendSize(ChartOptions options, double maxRowW)
        {
            var leg     = options.Legend;
            int padding = leg.Padding;
            int symW    = leg.SymbolWidth;
            int symH    = leg.SymbolHeight;
            double itemGap = 6.0, colGap = 12.0;
            double rowH    = Math.Max(symH + 4.0, leg.ItemFontSize + 4.0);
            // Per-char width tracks the rendered (font-scaled) legend text so columns never overlap.
            double charW   = 7.2 * FontScaleOf(options);

            // Collect estimated pixel width for each legend item
            var widthList = new System.Collections.Generic.List<double>();
            for (int i = 0; i < options.Series.Count; i++)
            {
                var series = options.Series[i];
                if (!series.ShowInLegend) continue;
                if (series.Type == ChartType.Heatmap) continue;
                if (series.Type == ChartType.Pie || series.Type == ChartType.Funnel || series.Type == ChartType.Treemap)
                {
                    for (int j = 0; j < series.Data.Count; j++)
                    {
                        if (!series.Data[j].HasValue || series.Data[j]!.Value <= 0) continue;
                        string label = options.XAxis.Categories?.Count > j
                            ? options.XAxis.Categories[j] : $"Slice {j + 1}";
                        widthList.Add(label.Length * charW + symW + itemGap + colGap);
                    }
                }
                else if (series.Type == ChartType.Parliament)
                {
                    for (int j = 0; j < series.ParliamentData.Count; j++)
                    {
                        string label = $"{series.ParliamentData[j].Name} ({series.ParliamentData[j].Seats})";
                        widthList.Add(label.Length * charW + symW + itemGap + colGap);
                    }
                }
                else
                    widthList.Add(series.Name.Length * charW + symW + itemGap + colGap);
            }
            if (widthList.Count == 0) return (0, 0);

            // Tabular grid: uniform column width = widest item
            double maxItemW = 0;
            foreach (double w in widthList) if (w > maxItemW) maxItemW = w;

            bool   isVertical = string.Equals(leg.Layout, "vertical", StringComparison.OrdinalIgnoreCase);
            int    numCols;
            double legendW;
            if (isVertical)
            {
                numCols = 1;
                legendW = maxItemW + padding * 2;
            }
            else
            {
                double avail = maxRowW - padding * 2;
                numCols = Math.Max(1, (int)Math.Floor(avail / maxItemW));
                if (numCols > widthList.Count) numCols = widthList.Count;
                legendW = maxItemW * numCols + padding * 2;
            }

            int    totalRows   = (int)Math.Ceiling((double)widthList.Count / numCols);
            bool   paginate    = leg.MaxLegendRows > 0 && totalRows > leg.MaxLegendRows
                                 && options.RenderMode == SvgMode.Interactive;
            int    rowsVisible = paginate ? leg.MaxLegendRows : totalRows;
            double legendH     = rowsVisible * rowH + padding * 2 + (paginate ? 20.0 : 0.0);

            return (legendW, legendH);
        }

        private static void AppendLegend(StringBuilder sb, ChartOptions options,
            int svgWidth, int svgHeight, int plotHeight, string clipId, int bottomPad = PaddingBottom, int labelVertH = 0, int topLegendOffset = 0, int titleExtraH = 0)
        {
            var leg = options.Legend;
            int    padding  = leg.Padding;
            int    symW     = leg.SymbolWidth;
            int    symH     = leg.SymbolHeight;
            int    symR     = leg.SymbolRadius;
            double itemGap  = 6.0;   // gap between swatch and text
            double colGap   = 12.0;  // gap after each item in horizontal layout
            double rowH     = Math.Max(symH + 4.0, leg.ItemFontSize + 4.0);
            double maxRowW  = svgWidth - PaddingLeft - PaddingRight;
            // Per-char width tracks the rendered (font-scaled) legend text so columns never overlap.
            double charW    = 7.2 * FontScaleOf(options);

            // ----- collect items -----
            // Si >= 0 for series-level items (used for legend toggle); Si = -1 for slice-level items.
            var items = new System.Collections.Generic.List<(string Label, string Color, int Si)>();
            int legendColour = 0;
            for (int i = 0; i < options.Series.Count; i++)
            {
                var series = options.Series[i];
                if (!series.ShowInLegend) continue;

                // Heatmap uses a dedicated colour-axis legend (gradient scale), not a swatch.
                if (series.Type == ChartType.Heatmap) continue;

                if (series.Type == ChartType.Pie
                    || series.Type == ChartType.Funnel
                    || series.Type == ChartType.Treemap)
                {
                    for (int j = 0; j < series.Data.Count; j++)
                    {
                        if (!series.Data[j].HasValue || series.Data[j]!.Value <= 0) continue;
                        // Match the renderer's colour resolution for each slice/cell.
                        // Pie: always palette by index.
                        // Funnel/Treemap: explicit series.Color overrides palette.
                        string sliceColor = series.Type == ChartType.Pie
                            ? options.Theme.Colors[j % options.Theme.Colors.Length]
                            : (series.Color ?? options.Theme.Colors[j % options.Theme.Colors.Length]);
                        string sliceLabel = options.XAxis.Categories?.Count > j
                            ? options.XAxis.Categories[j]
                            : $"Slice {j + 1}";
                        items.Add((sliceLabel, sliceColor, -1));  // slice-level — no series toggle
                    }
                }
                else if (series.Type == ChartType.Parliament)
                {
                    // Each party becomes an individually-togglable legend item.
                    // Synthetic data-si = 1_000_000 + seriesIndex*10_000 + partyIndex keeps it
                    // well above real series indices (0..N) and matches the party group's data-si.
                    for (int j = 0; j < series.ParliamentData.Count; j++)
                    {
                        var grp      = series.ParliamentData[j];
                        string grpColor = grp.Color ?? options.Theme.Colors[j % options.Theme.Colors.Length];
                        items.Add(($"{grp.Name} ({grp.Seats})", grpColor, 1_000_000 + i * 10_000 + j));
                    }
                }
                else
                {
                    string color;
                    if (series.Fill != null && series.Visible)
                        // Reference the same gradient/pattern paint the series shapes use so the
                        // swatch matches the rendered fill instead of a flat palette colour.
                        color = $"url(#{clipId}-fill-{i})";
                    else if (series.Fill != null && series.Fill.Kind == Models.SeriesFillKind.Pattern)
                        color = series.Fill.PatternForeground;
                    else
                        color = series.Color ?? options.Theme.Colors[legendColour % options.Theme.Colors.Length];
                    legendColour++;
                    items.Add((series.Name, color, i));  // series-level — togglable
                }
            }

            if (items.Count == 0) return;

            // ----- tabular grid: uniform column width = widest item -----
            double maxItemW = 0;
            for (int i = 0; i < items.Count; i++)
            {
                double cw = items[i].Label.Length * charW + symW + itemGap + colGap;
                if (cw > maxItemW) maxItemW = cw;
            }

            // ----- compute legend bounding box -----
            bool   isVertical  = string.Equals(leg.Layout, "vertical", StringComparison.OrdinalIgnoreCase);
            int    numCols;
            double colW;
            if (isVertical)
            {
                numCols = 1;
                colW    = maxItemW;
            }
            else
            {
                double avail = maxRowW - padding * 2;
                numCols = Math.Max(1, (int)Math.Floor(avail / maxItemW));
                if (numCols > items.Count) numCols = items.Count;
                colW    = maxItemW;  // uniform: each column = widest item
            }

            int    totalRows   = (int)Math.Ceiling((double)items.Count / numCols);
            bool   canPaginate = leg.MaxLegendRows > 0 && totalRows > leg.MaxLegendRows;
            bool   hasPaging   = canPaginate && options.RenderMode == SvgMode.Interactive;
            int    rowsPerPage = hasPaging ? leg.MaxLegendRows : totalRows;
            int    totalPages  = hasPaging ? (int)Math.Ceiling((double)totalRows / rowsPerPage) : 1;
            const double NavRowH = 20.0;

            double legendW = colW * numCols + padding * 2;
            double legendH = (hasPaging ? rowsPerPage : totalRows) * rowH + padding * 2
                           + (hasPaging ? NavRowH : 0.0);

            // ----- compute block origin from Align / VerticalAlign + offsets -----
            string align  = leg.Align  ?? "center";
            string vAlign = leg.VerticalAlign ?? "bottom";

            int margin = leg.Margin;

            double blockX;
            if (string.Equals(align, "left", StringComparison.OrdinalIgnoreCase))
                blockX = PaddingLeft + margin + leg.X;
            else if (string.Equals(align, "right", StringComparison.OrdinalIgnoreCase))
                blockX = svgWidth - PaddingRight - legendW - margin + leg.X;
            else
                blockX = PaddingLeft + (maxRowW - legendW) / 2.0 + leg.X;

            double blockY;
            if (string.Equals(vAlign, "top", StringComparison.OrdinalIgnoreCase))
            {
                // Legend sits in the gap between title/subtitle and the (shifted) plot area.
                bool hasTitle    = !string.IsNullOrEmpty(options.Title?.Text);
                bool hasSubtitle = !string.IsNullOrEmpty(options.Subtitle?.Text);
                // A wrapped title occupies extra lines, so drop the legend below them.
                double headerBottom = (hasSubtitle ? 44.0 : (hasTitle ? 28.0 : 4.0)) + titleExtraH;
                blockY = headerBottom + margin + leg.Y;
            }
            else if (string.Equals(vAlign, "middle", StringComparison.OrdinalIgnoreCase))
            {
                // Centre vertically in the (possibly shifted) plot area.
                blockY = PaddingTop + topLegendOffset + plotHeight / 2.0 - legendH / 2.0 + leg.Y;
            }
            else
            {
                // Bottom: place below tick labels AND below x-axis title (when present).
                // Bar charts show the value-axis (YAxis) title along the bottom, so treat it as
                // the bottom axis title for spacing; other charts use the XAxis title.
                double tickLabelH = labelVertH > 0 ? labelVertH + 14.0 : 20.0;
                bool isBarChart_ = options.Series.Exists(s => s.Type == ChartType.Bar && s.Visible);
                bool hasBottomXTitle = isBarChart_
                    ? options.Stacking == Stacking.None && !string.IsNullOrEmpty(options.YAxis?.Title)
                    : !string.IsNullOrEmpty(options.XAxis?.Title);
                double xTitleBlock = hasBottomXTitle ? 32.0 : 0.0; // 24 title + 8 gap after
                blockY = PaddingTop + topLegendOffset + plotHeight + tickLabelH + 8.0 + xTitleBlock + leg.Y;
            }

            // ----- render background / border box -----
            bool hasBg     = !string.IsNullOrEmpty(leg.BackgroundColor);
            bool hasBorder = leg.BorderWidth > 0 && !string.IsNullOrEmpty(leg.BorderColor);
            if (hasBg || hasBorder)
            {
                string fillAttr   = hasBg ? $"fill=\"{Escape(leg.BackgroundColor!)}\"" : "fill=\"none\"";
                string strokeAttr = hasBorder
                    ? $"stroke=\"{Escape(leg.BorderColor!)}\" stroke-width=\"{leg.BorderWidth}\""
                    : "stroke=\"none\"";
                sb.AppendLine($"  <rect x=\"{F(blockX)}\" y=\"{F(blockY)}\" width=\"{F(legendW)}\" height=\"{F(legendH)}\" rx=\"{leg.BorderRadius}\" {fillAttr} {strokeAttr}/>");
            }

            // ----- emit items -----
            // si >= 0: series-level item → wrap in a clickable <g> in Interactive mode.
            // si < 0:  slice-level item  → plain emit, no toggle wrapper.

            // Emits one legend swatch: a marker glyph (with a connector for line/spline) for
            // marker-based series, otherwise the classic rounded rectangle.
            void EmitSwatch(double x, double sy, string swColor, int seriesIdx)
            {
                Models.Series? sr = (seriesIdx >= 0 && seriesIdx < options.Series.Count)
                    ? options.Series[seriesIdx] : null;
                bool markerBased = sr != null &&
                    (sr.Type == ChartType.Line || sr.Type == ChartType.Spline || sr.Type == ChartType.Scatter);

                if (!markerBased)
                {
                    sb.AppendLine($"  <rect x=\"{F(x)}\" y=\"{F(sy)}\" width=\"{symW}\" height=\"{symH}\" rx=\"{symR}\" fill=\"{Escape(swColor)}\"/>");
                    return;
                }

                double mcx  = x + symW / 2.0;
                double mcy  = sy + symH / 2.0;
                double mSz  = Math.Min(4.0, Math.Min(symW, symH) / 2.0);
                // Line/spline get a connector so the swatch reads as a line series; scatter is marker-only.
                if (sr!.Type != ChartType.Scatter)
                    sb.AppendLine($"  <line x1=\"{F(x)}\" y1=\"{F(mcy)}\" x2=\"{F(x + symW)}\" y2=\"{F(mcy)}\" stroke=\"{Escape(swColor)}\" stroke-width=\"2\" stroke-linecap=\"round\"/>");
                // Match the on-chart marker treatment (hollow ring in modern style, filled otherwise).
                bool hollow    = options.Theme.ModernStyle;
                string mFill   = hollow ? Escape(options.ResolvedBackgroundColor) : Escape(swColor);
                string mStroke = hollow ? Escape(swColor) : Escape(options.ResolvedBackgroundColor);
                double mStrokeW = hollow ? 2.0 : 1.2;
                sb.AppendLine("  " + MarkerShape(sr.MarkerSymbol, mcx, mcy, mSz, mFill, mStroke, mStrokeW));
            }

            void EmitItem(double ix, double iy, string label, string color, int si)
            {
                bool isTogglable = si >= 0 && options.RenderMode == SvgMode.Interactive;
                if (isTogglable)
                    sb.AppendLine($"  <g class=\"tf-li\" data-si=\"{si}\" data-sname=\"{Escape(label)}\" style=\"cursor:pointer;-webkit-user-select:none;user-select:none\" role=\"button\" aria-pressed=\"false\" aria-label=\"{Escape(label)}\" tabindex=\"0\">");

                double symY = iy + (rowH - symH) / 2.0;
                string effectiveFill = !string.IsNullOrEmpty(leg.ItemFontColor) ? leg.ItemFontColor! : options.Theme.TextColor;
                string itemSt = !string.IsNullOrEmpty(leg.ItemStyle) ? $" style=\"{Escape(leg.ItemStyle)}\"" : "";
                if (options.RightToLeft)
                {
                    // RTL: icon on the right, text on the left with anchor at its right edge
                    double cellContent = colW - colGap;
                    double iconX = ix + cellContent - symW;
                    double textX = ix + cellContent - symW - itemGap;
                    EmitSwatch(iconX, symY, color, si);
                    sb.AppendLine($"  <text class=\"legend-label\" x=\"{F(textX)}\" y=\"{F(iy + leg.ItemFontSize)}\" text-anchor=\"end\" fill=\"{Escape(effectiveFill)}\"{itemSt}>{Escape(label)}</text>");
                }
                else
                {
                    EmitSwatch(ix, symY, color, si);
                    sb.AppendLine($"  <text class=\"legend-label\" x=\"{F(ix + symW + itemGap)}\" y=\"{F(iy + leg.ItemFontSize)}\" fill=\"{Escape(effectiveFill)}\"{itemSt}>{Escape(label)}</text>");
                }

                if (isTogglable)
                    sb.AppendLine("  </g>");
            }

            // ----- emit pages (each page is a visibility-toggled <g> in Interactive paging mode) -----
            double startX = blockX + padding;
            double startY = blockY + padding;

            for (int pg = 0; pg < totalPages; pg++)
            {
                int firstItem = pg * rowsPerPage * numCols;
                int lastItem  = Math.Min(firstItem + rowsPerPage * numCols, items.Count);

                if (hasPaging)
                {
                    string vis = pg > 0 ? " visibility=\"hidden\"" : string.Empty;
                    sb.AppendLine($"  <g class=\"tf-leg-page\" id=\"tf-legpg-{pg}\"{vis}>");
                }

                for (int i = firstItem; i < lastItem; i++)
                {
                    int    local = i - firstItem;
                    int    row   = local / numCols;
                    int    col   = local % numCols;
                    double ix    = startX + col * colW;
                    double iy    = startY + row * rowH;
                    EmitItem(ix, iy, items[i].Label, items[i].Color, items[i].Si);
                }

                if (hasPaging)
                    sb.AppendLine("  </g>");
            }

            // ----- navigation arrows (Interactive + paging only) -----
            if (hasPaging)
            {
                double navY  = blockY + padding + rowsPerPage * rowH + 4.0;
                double navCX = blockX + legendW / 2.0;
                string navCol = Escape(!string.IsNullOrEmpty(leg.ItemFontColor)
                    ? leg.ItemFontColor : options.Theme.TextColor);

                // Prev
                double px = navCX - 58.0;
                sb.AppendLine($"  <g class=\"tf-leg-prev\" role=\"button\" tabindex=\"0\" aria-label=\"Previous page\" style=\"cursor:pointer\">");
                sb.AppendLine($"    <rect x=\"{F(px)}\" y=\"{F(navY)}\" width=\"22\" height=\"16\" fill=\"transparent\" rx=\"3\"/>");
                sb.AppendLine($"    <path d=\"M {F(px+14)},{F(navY+3)} L {F(px+8)},{F(navY+8)} L {F(px+14)},{F(navY+13)} Z\" fill=\"{navCol}\" opacity=\"0.4\"/>");
                sb.AppendLine("  </g>");

                // Page indicator "1 / N"
                sb.AppendLine($"  <text class=\"tf-leg-page-ind\" x=\"{F(navCX)}\" y=\"{F(navY+11)}\" text-anchor=\"middle\" font-size=\"11\" fill=\"{navCol}\">1 / {totalPages}</text>");

                // Next
                double nx = navCX + 36.0;
                sb.AppendLine($"  <g class=\"tf-leg-next\" role=\"button\" tabindex=\"0\" aria-label=\"Next page\" style=\"cursor:pointer\">");
                sb.AppendLine($"    <rect x=\"{F(nx)}\" y=\"{F(navY)}\" width=\"22\" height=\"16\" fill=\"transparent\" rx=\"3\"/>");
                sb.AppendLine($"    <path d=\"M {F(nx+8)},{F(navY+3)} L {F(nx+14)},{F(navY+8)} L {F(nx+8)},{F(navY+13)} Z\" fill=\"{navCol}\" opacity=\"1\"/>");
                sb.AppendLine("  </g>");
            }
        }

    }
}
