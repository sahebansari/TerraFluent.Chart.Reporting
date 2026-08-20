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
        private static void AppendParliamentSeries(
            StringBuilder sb, Series series, ChartOptions options,
            int svgWidth, int svgHeight, int plotWidth, string clipId, StringBuilder tooltipLayer)
        {
            var groups = series.ParliamentData;
            if (groups.Count == 0) return;

            int totalSeats = 0;
            foreach (var g in groups) totalSeats += g.Seats;
            if (totalSeats == 0) return;

            // Derive SVG id from clipId ("pc-12-clip" → "pc-12") for unique party group ids
            string svgId  = clipId.Length > 5 && clipId.EndsWith("-clip", StringComparison.Ordinal)
                ? clipId.Substring(0, clipId.Length - 5)
                : clipId;
            // Series index used to build synthetic data-si for party groups (1_000_000 + si*10_000 + pi)
            int si_idx = Math.Max(0, options.Series.IndexOf(series));

            // ── Layout: keep the hemicycle clear of the Y-axis, X-axis and both edges ──
            // LegendReserve is computed from expected legend height so the hemicycle
            // never overlaps the party legend regardless of party count.
            const double TopMargin   = 12.0;  // gap below the chart subtitle / PaddingTop
            double LegendReserve;
            {
                var legL = options.Legend;
                if (legL.Enabled)
                {
                    double lfsz = legL.ItemFontSize > 0 ? (double)legL.ItemFontSize : 11.0;
                    double sR_e = Math.Max(3.0, Math.Min(legL.SymbolWidth, legL.SymbolHeight) / 2.0);
                    double rH_e = Math.Max(sR_e * 2 + 6.0, lfsz + 8.0);
                    double cW_e = lfsz * 0.56;
                    double miW  = 0;
                    foreach (var g in groups)
                    {
                        double w = sR_e * 2 + 8.0 + $"{g.Name} ({g.Seats})".Length * cW_e + 20.0;
                        if (w > miW) miW = w;
                    }
                    double av_e = svgWidth - 24.0 - legL.Padding * 2;
                    int    nc_e = miW > 0 ? Math.Max(1, Math.Min(groups.Count, (int)Math.Floor(av_e / miW))) : 1;
                    int    tr_e = (int)Math.Ceiling((double)groups.Count / nc_e);
                    bool   hp_e = legL.MaxLegendRows > 0 && tr_e > legL.MaxLegendRows
                                  && options.RenderMode == SvgMode.Interactive;
                    int    vr_e = hp_e ? legL.MaxLegendRows : tr_e;
                    double lgH  = vr_e * rH_e + legL.Padding * 2 + (hp_e ? 20.0 : 0.0);
                    LegendReserve = lgH + 38.0 + legL.Margin;  // 38 ≈ dotR(~5) + gap(30) + pad(3)
                }
                else
                {
                    LegendReserve = 8.0;
                }
            }

            // Align plot edges to legend margin so both start from the same left/right coordinate.
            // Parliament has no Y-axis, so use symmetric horizontal padding (PaddingRight on
            // both sides) instead of the axis-sized PaddingLeft. This centres the hemicycle in
            // the full width and removes the extra left-side gap.
            double horizPad    = options.Legend.Enabled ? options.Legend.Margin : 18.0;
            double usableLeft  = PaddingRight + horizPad;
            double usableRight = svgWidth - PaddingRight - horizPad;
            double cx          = (usableLeft + usableRight) / 2.0;
            double cy          = svgHeight - LegendReserve;   // svgHeight here is svgHC

            double rMaxH = (usableRight - usableLeft) / 2.0;
            // Reserve a vertical band above the arc so the top party's outside data label
            // (which is placed radially beyond the boundary arc) is not clipped by the title.
            double labelBand = 0.0;
            if (series.DataLabel.Enabled)
            {
                double lfs = series.DataLabel.TextFontSize ?? 9;
                labelBand = 18.0 /*radial gap*/ + lfs + 12.0 /*pill + dot slack*/;
            }
            double rMaxV = cy - PaddingTop - TopMargin - labelBand;
            double rMax  = Math.Min(rMaxH, rMaxV);
            if (rMax < 40) rMax = 40;
            double rInner = Math.Max(50.0, rMax * 0.40);  // 40 % of rMax gives a wider open centre

            // ── Binary-search for dot spacing d so ∑ dots across all rings = totalSeats ──
            double d    = FindParliamentDotSpacing(rInner, rMax, totalSeats);
            double dotR = Math.Max(2.5, d * 0.35);

            // ── Enumerate ring radii (geometry only; seats are allocated exactly below) ─
            var ringRadii = new System.Collections.Generic.List<double>();
            for (double r = rInner; r <= rMax + 1e-9; r += d)
                ringRadii.Add(r);
            if (ringRadii.Count == 0) ringRadii.Add(rInner);
            double rOuter = ringRadii[ringRadii.Count - 1];

            // ── CSS for hover effects (Animated + Interactive modes only) ─────────
            bool isInteractive = options.RenderMode == SvgMode.Interactive;
            bool hasHover      = options.RenderMode != SvgMode.Static;
            if (hasHover)
            {
                string cssScope = $"#{svgId} ";
                sb.AppendLine("  <style>");
                sb.AppendLine($"    {cssScope}.tf-party-group {{ cursor: pointer; transition: filter 0.12s; }}");
                sb.AppendLine($"    {cssScope}.tf-party-group:hover {{ filter: brightness(1.35); }}");
                sb.AppendLine($"    {cssScope}.tf-party-group:hover circle {{ stroke: {ChartColor.WithOpacity(ChartColor.White, 0.85)}; stroke-width: 1.5px; }}");
                sb.AppendLine($"    {cssScope}.tf-parl-leg {{ cursor: pointer; transition: opacity 0.15s; -webkit-user-select:none; user-select:none; }}");
                sb.AppendLine($"    {cssScope}.tf-parl-leg:focus {{ outline: none; }}");
                sb.AppendLine($"    {cssScope}.tf-parl-leg:focus-visible {{ outline: 1px solid #555; outline-offset: 2px; }}");
                sb.AppendLine($"    {cssScope}.tf-parl-leg:hover {{ opacity: 0.75; }}");
                sb.AppendLine("  </style>");
            }

            // ── Boundary arc & baseline ───────────────────────────────────────────
            double arcR    = rOuter + dotR + 4.0;
            string gridCol = Escape(options.Theme.GridLineColor);
            sb.AppendLine(
                $"  <path d=\"M {F(cx - arcR)},{F(cy)} A {F(arcR)},{F(arcR)} 0 0,1 {F(cx + arcR)},{F(cy)}\"" +
                $" fill=\"none\" stroke=\"{gridCol}\" stroke-width=\"1\" opacity=\"0.35\"/>");
            sb.AppendLine(
                $"  <line x1=\"{F(cx - arcR - 5)}\" y1=\"{F(cy)}\"" +
                $" x2=\"{F(cx + arcR + 5)}\" y2=\"{F(cy)}\"" +
                $" stroke=\"{gridCol}\" stroke-width=\"1\" opacity=\"0.35\"/>");

            // ── Allocate an EXACT number of seats to each ring, then to each party ──
            // A ring's geometric capacity (round(πr/d)) only approximates the target,
            // so we distribute exactly totalSeats with the Largest Remainder Method —
            // first across rings (weighted by capacity), then assign parties to the
            // angle-ordered seats in contiguous blocks. This guarantees both
            //   Σ dots == totalSeats   AND   dots(party) == party.Seats   exactly.
            int nParties = groups.Count;
            var partyDots = new System.Collections.Generic.List<(double X, double Y)>[nParties];
            for (int pi = 0; pi < nParties; pi++)
                partyDots[pi] = new System.Collections.Generic.List<(double, double)>();

            int ringCount = ringRadii.Count;
            var ringCap = new int[ringCount];        // geometric capacity per ring
            double capSum = 0;
            for (int ri = 0; ri < ringCount; ri++)
            {
                ringCap[ri] = Math.Max(1, (int)Math.Round(Math.PI * ringRadii[ri] / d));
                capSum += ringCap[ri];
            }

            // Largest-remainder seat allocation across rings (weighted by capacity).
            var ringSeats = new int[ringCount];
            var ringRem   = new double[ringCount];
            int placed = 0;
            for (int ri = 0; ri < ringCount; ri++)
            {
                double q = ringCap[ri] / capSum * totalSeats;
                ringSeats[ri] = (int)Math.Floor(q);
                ringRem[ri]   = q - ringSeats[ri];
                placed       += ringSeats[ri];
            }
            for (int k = 0, extra = totalSeats - placed; k < extra; k++)
            {
                int best = 0;
                for (int ri = 1; ri < ringCount; ri++)
                    if (ringRem[ri] > ringRem[best]) best = ri;
                ringSeats[best]++;
                ringRem[best] = -1.0;
            }

            // Generate every seat position, tagged with its angle (π = far left … 0 = far right).
            var seats = new System.Collections.Generic.List<(double Theta, double X, double Y)>(totalSeats);
            for (int ri = 0; ri < ringCount; ri++)
            {
                int n = ringSeats[ri];
                if (n <= 0) continue;
                double r = ringRadii[ri];
                for (int k = 0; k < n; k++)
                {
                    double theta = n > 1 ? Math.PI - k * Math.PI / (n - 1) : Math.PI / 2.0;
                    seats.Add((theta, cx + r * Math.Cos(theta), cy - r * Math.Sin(theta)));
                }
            }

            // Seat order = left → right (θ descending), rings interleaved so each party
            // forms one contiguous angular wedge spanning all rings.
            seats.Sort((a, b) => b.Theta.CompareTo(a.Theta));

            // Assign parties to the ordered seats in blocks sized by each party's seat count.
            int seatIdx = 0;
            for (int pi = 0; pi < nParties; pi++)
            {
                int want = groups[pi].Seats;
                for (int k = 0; k < want && seatIdx < seats.Count; k++, seatIdx++)
                    partyDots[pi].Add((seats[seatIdx].X, seats[seatIdx].Y));
            }

            bool   anim = options.Animation.Enabled && options.RenderMode != SvgMode.Static;
            string dur  = anim ? F(options.Animation.Duration.TotalSeconds) + "s" : string.Empty;
            string ease = anim ? SmilEasing(options.Animation.Easing) : string.Empty;

            // ── Emit one <g> per party so each has a stable id for toggle/hover ────
            for (int pi = 0; pi < nParties; pi++)
            {
                string col   = Escape(groups[pi].Color ?? ChartColor.AshGray);
                string pname = Escape(groups[pi].Name);
                int    psi   = 1_000_000 + si_idx * 10_000 + pi;  // synthetic data-si for legend toggle
                sb.AppendLine(
                    $"  <g class=\"tf-party-group tf-sg\"" +
                    $" id=\"tf-pty-{svgId}-{pi}\"" +
                    $" data-si=\"{psi}\"" +
                    $" data-party-name=\"{pname}\"" +
                    $" data-party-seats=\"{groups[pi].Seats}\"" +
                    $" data-party-color=\"{col}\">");

                foreach (var (x, y) in partyDots[pi])
                {
                    if (anim)
                        sb.AppendLine(
                            $"    <circle cx=\"{F(x)}\" cy=\"{F(y)}\" r=\"0\" fill=\"{col}\">" +
                            $"<animate attributeName=\"r\" from=\"0\" to=\"{F(dotR)}\" dur=\"{dur}\" fill=\"freeze\"{ease}/>" +
                            $"</circle>");
                    else
                        sb.AppendLine($"    <circle cx=\"{F(x)}\" cy=\"{F(y)}\" r=\"{F(dotR)}\" fill=\"{col}\"/>");
                }

                // ── Party data label — placed outside the hemicycle boundary arc ──
                if (series.DataLabel.Enabled && partyDots[pi].Count > 0)
                {
                    // ── Mean angle of this party's wedge ────────────────────────────────────
                    // A party's dots span a contiguous angular range across the concentric
                    // rings, so the circular mean angle points at the middle of that wedge.
                    // (Averaging sin/cos avoids the wrap-around bias of a cartesian centroid,
                    // which would sit at mid-radius *inside* the hemicycle.)
                    double sumSin = 0, sumCos = 0;
                    foreach (var (px2, py2) in partyDots[pi])
                    {
                        double a = Math.Atan2(cy - py2, px2 - cx);   // +y points "up" (above baseline)
                        sumSin += Math.Sin(a);
                        sumCos += Math.Cos(a);
                    }
                    double theta = Math.Atan2(sumSin, sumCos);
                    double ux = Math.Cos(theta), uy = Math.Sin(theta);

                    // ── Radial gap beyond the boundary arc (clamped to stay on-canvas) ──────
                    // RadiusFraction > 1 is read as a fraction of the arc radius beyond the
                    // arc; smaller values fall back to a pixel offset. Default → 18 px.
                    double labelGap = series.DataLabel.RadiusFraction.HasValue
                        ? Math.Max(10.0, Math.Min(40.0,
                            series.DataLabel.RadiusFraction.Value > 1.0
                                ? (series.DataLabel.RadiusFraction.Value - 1.0) * arcR
                                : series.DataLabel.RadiusFraction.Value * 20.0))
                        : 18.0;
                    double labelR = arcR + labelGap;

                    // ── Label anchor OUTSIDE the arc, clamped inside the canvas ─────────────
                    double olx = cx + ux * labelR;
                    double oly = cy - uy * labelR;
                    olx = Math.Max(PaddingRight + 8.0, Math.Min(svgWidth - PaddingRight - 8.0, olx));
                    oly = Math.Max(PaddingTop + 6.0, oly);

                    // ── Connector: starts ON the boundary arc and runs out toward the label ─
                    double csx = cx + ux * (arcR + 1.0);
                    double csy = cy - uy * (arcR + 1.0);
                    double cdx = olx - csx, cdy = oly - csy;
                    double cd  = Math.Sqrt(cdx * cdx + cdy * cdy);
                    double cex = cd > 8.0 ? olx - cdx / cd * 8.0 : csx;   // stop 8 px short of the label
                    double cey = cd > 8.0 ? oly - cdy / cd * 8.0 : csy;
                    string connCol = Escape(series.DataLabel.TextColor ?? options.Theme.TextColor);
                    sb.AppendLine($"  <line x1=\"{F(csx)}\" y1=\"{F(csy)}\" x2=\"{F(cex)}\" y2=\"{F(cey)}\" stroke=\"{connCol}\" stroke-width=\"0.8\" opacity=\"0.5\"/>");

                    // ── Label text ──────────────────────────────────────────────────────────
                    string labelText = string.IsNullOrEmpty(series.DataLabel.FormatString)
                        ? groups[pi].Seats.ToString(CultureInfo.InvariantCulture)
                        : FormatDataLabel(groups[pi].Seats, series.DataLabel.FormatString);
                    AppendDataLabel(sb, olx, oly, labelText,
                        series.DataLabel.TextColor ?? options.Theme.TextColor,
                        series.DataLabel.BackgroundColor,
                        series.DataLabel.TextFontSize ?? 9);
                }

                sb.AppendLine("  </g>");
            }

            // ── Centre label ─────────────────────────────────────────────────────
            var pc = series.ParliamentCenter;
            if (pc.Enabled)
            {
                string textCol  = Escape(pc.TextColor ?? options.Theme.TextColor);
                string mainText = Escape(pc.Line1 ?? totalSeats.ToString(CultureInfo.InvariantCulture));
                string subText  = pc.Line2 ?? "seats";
                bool   showSub  = subText.Length > 0;
                int    mainFsz  = pc.Line1FontSize  > 0 ? pc.Line1FontSize  : 14;
                int    subFsz   = pc.Line2FontSize > 0 ? pc.Line2FontSize : 11;

                double mainY = showSub ? cy - subFsz - 5 : cy - 5;
                sb.AppendLine(
                    $"  <text x=\"{F(cx)}\" y=\"{F(mainY)}\" text-anchor=\"middle\"" +
                    $" font-size=\"{mainFsz}\" font-weight=\"700\" fill=\"{textCol}\">{mainText}</text>");
                if (showSub)
                    sb.AppendLine(
                        $"  <text x=\"{F(cx)}\" y=\"{F(cy - 2)}\" text-anchor=\"middle\"" +
                        $" font-size=\"{subFsz}\" fill=\"{textCol}\" opacity=\"0.65\">{Escape(subText)}</text>");
            }

            // ── Interactive JS: party tooltip only — legend+toggle wired by AppendLegend/AppendInteractiveScript ──
            if (isInteractive)
            {
                var tt         = options.Tooltip;
                bool tipEnabled = tt.Enabled;

                if (tipEnabled)
                {
                    // ── Resolve tooltip rendering params from options ──────────
                    int    tipFsz1   = tt.FontSize > 0 ? tt.FontSize : 12;
                    int    tipFsz2   = Math.Max(9, tipFsz1 - 1);
                    int    tipLH1    = tipFsz1 + 4;
                    int    tipGap    = 4;
                    int    tipVPad   = 9;
                    int    tipHPad   = tt.Padding > 0 ? tt.Padding : 10;
                    int    tipBoxH   = tipVPad + tipLH1 + tipGap + tipFsz2 + tipVPad;
                    int    tipRx     = tt.BorderRadius;
                    double tipDotR   = Math.Max(3.0, tipFsz1 * 0.33);
                    double tipTextX  = tipHPad + tipDotR * 2 + 5;
                    double tipY1     = tipVPad + tipFsz1 * 0.82;
                    double tipY2     = tipVPad + tipLH1 + tipGap + tipFsz2 * 0.82;
                    string tipBg     = Escape(!string.IsNullOrEmpty(tt.BackgroundColor) ? tt.BackgroundColor : options.Theme.TooltipBackground);
                    string tipFg     = Escape(!string.IsNullOrEmpty(tt.TextColor)       ? tt.TextColor       : options.Theme.TooltipTextColor);
                    string tipShadow = tt.Shadow ? "drop-shadow(0 2px 6px rgba(0,0,0,0.28))" : "none";
                    string tipTrans  = $"opacity {F(tt.TransitionDuration)}s";
                    string tipFontFamilyLine = !string.IsNullOrEmpty(tt.FontFamily)
                        ? $"tipLine1.style.fontFamily='{tt.FontFamily.Replace("\\", "\\\\").Replace("'", "\\'")}'; tipLine2.style.fontFamily='{tt.FontFamily.Replace("\\", "\\\\").Replace("'", "\\'")}';"
                        : string.Empty;
                    string tipBorderLine = tt.BorderWidth > 0 && !string.IsNullOrEmpty(tt.BorderColor)
                        ? $"tipBgEl.setAttribute('stroke','{Escape(tt.BorderColor)}'); tipBgEl.setAttribute('stroke-width','{tt.BorderWidth}');"
                        : string.Empty;
                    // Format templates: {label} = party name, {value} = seat count
                    string jsLine1Fmt = (!string.IsNullOrEmpty(tt.Format)      ? tt.Format      : "{label}"     ).Replace("\\", "\\\\").Replace("'", "\\'");
                    string jsLine2Fmt = (!string.IsNullOrEmpty(tt.PointFormat) ? tt.PointFormat : "Representatives: {value}").Replace("\\", "\\\\").Replace("'", "\\'");

                    sb.AppendLine("  <script type=\"text/javascript\">");
                    sb.AppendLine("  <![CDATA[");
                    sb.AppendLine("  (function() {");
                    sb.AppendLine("    'use strict';");
                    sb.AppendLine("    var svg = document.currentScript ? document.currentScript.closest('svg') : null;");
                    sb.AppendLine("    if (!svg) { var sc = document.querySelectorAll('svg script'); svg = sc.length ? sc[sc.length-1].closest('svg') : null; }");
                    sb.AppendLine("    if (!svg) return;");
                    sb.AppendLine("    var svgIdLocal = svg.id;");

                    if (tipEnabled)
                    {
                        sb.AppendLine("    var vb = svg.viewBox ? svg.viewBox.baseVal : null;");
                        sb.AppendLine("    var svgW = (vb && vb.width)  ? vb.width  : (svg.clientWidth  || 760);");
                        sb.AppendLine("");
                        sb.AppendLine("    // ── Tooltip DOM ──────────────────────────────────────");
                        sb.AppendLine("    var tip = document.createElementNS('http://www.w3.org/2000/svg', 'g');");
                        sb.AppendLine("    tip.style.pointerEvents = 'none';");
                        sb.AppendLine("    tip.style.opacity = '0';");
                        sb.AppendLine($"   tip.style.transition = '{tipTrans}';");
                        sb.AppendLine("    svg.appendChild(tip);");
                        sb.AppendLine("    var tipBgEl  = document.createElementNS('http://www.w3.org/2000/svg', 'rect');");
                        sb.AppendLine("    var tipDotEl = document.createElementNS('http://www.w3.org/2000/svg', 'circle');");
                        sb.AppendLine("    var tipLine1 = document.createElementNS('http://www.w3.org/2000/svg', 'text');");
                        sb.AppendLine("    var tipLine2 = document.createElementNS('http://www.w3.org/2000/svg', 'text');");
                        sb.AppendLine("    tipLine1.style.pointerEvents = 'none';");
                        sb.AppendLine($"   tipLine1.setAttribute('font-size', '{tipFsz1}');");
                        sb.AppendLine("    tipLine1.setAttribute('font-weight', '600');");
                        sb.AppendLine($"   tipLine1.setAttribute('fill', '{tipFg}');");
                        sb.AppendLine("    tipLine2.style.pointerEvents = 'none';");
                        sb.AppendLine($"   tipLine2.setAttribute('font-size', '{tipFsz2}');");
                        sb.AppendLine($"   tipLine2.setAttribute('fill', '{tipFg}');");
                        sb.AppendLine("    tipLine2.setAttribute('fill-opacity', '0.78');");
                        if (!string.IsNullOrEmpty(tipFontFamilyLine))
                            sb.AppendLine($"   {tipFontFamilyLine}");
                        sb.AppendLine("    tip.appendChild(tipBgEl); tip.appendChild(tipDotEl);");
                        sb.AppendLine("    tip.appendChild(tipLine1); tip.appendChild(tipLine2);");
                        sb.AppendLine("");
                        sb.AppendLine($"   var line1Fmt = '{jsLine1Fmt}';");
                        sb.AppendLine($"   var line2Fmt = '{jsLine2Fmt}';");
                        sb.AppendLine("    function applyFmt(fmt, lbl, val) {");
                        sb.AppendLine("      return fmt.replace('{label}', lbl).replace('{value}', val);");
                        sb.AppendLine("    }");
                        sb.AppendLine("");
                        sb.AppendLine("    // moveTip: auto-fits width via getComputedTextLength");
                        sb.AppendLine("    function moveTip(x, y, partyName, seats, color) {");
                        sb.AppendLine($"     var hPad = {tipHPad}, dotR = {F(tipDotR)}, textX = {F(tipTextX)}, boxH = {tipBoxH};");
                        sb.AppendLine("      tipLine1.textContent = applyFmt(line1Fmt, partyName, seats);");
                        sb.AppendLine("      tipLine2.textContent = applyFmt(line2Fmt, partyName, seats);");
                        sb.AppendLine("      var w1 = (tipLine1.getComputedTextLength && tipLine1.getComputedTextLength()) || tipLine1.textContent.length * 7.2;");
                        sb.AppendLine("      var w2 = (tipLine2.getComputedTextLength && tipLine2.getComputedTextLength()) || tipLine2.textContent.length * 6.5;");
                        sb.AppendLine("      var boxW = Math.ceil(Math.max(w1, w2)) + textX + hPad;");
                        sb.AppendLine("      var bx = Math.max(2, Math.min(x - boxW / 2, svgW - boxW - 2));");
                        sb.AppendLine("      var by = Math.max(2, y - boxH - 12);");
                        sb.AppendLine("      tipBgEl.setAttribute('x', bx); tipBgEl.setAttribute('y', by);");
                        sb.AppendLine("      tipBgEl.setAttribute('width', boxW); tipBgEl.setAttribute('height', boxH);");
                        sb.AppendLine($"     tipBgEl.setAttribute('rx', '{tipRx}');");
                        sb.AppendLine($"     tipBgEl.setAttribute('fill', '{tipBg}');");
                        sb.AppendLine($"     tipBgEl.style.filter = '{tipShadow}';");
                        if (!string.IsNullOrEmpty(tipBorderLine))
                            sb.AppendLine($"     {tipBorderLine}");
                        sb.AppendLine($"     tipDotEl.setAttribute('cx', bx + hPad + dotR);");
                        sb.AppendLine($"     tipDotEl.setAttribute('cy', by + {F(tipVPad + tipLH1 * 0.5)});");
                        sb.AppendLine($"     tipDotEl.setAttribute('r', dotR);");
                        sb.AppendLine("      tipDotEl.setAttribute('fill', color);");
                        sb.AppendLine("      tipLine1.setAttribute('x', bx + textX);");
                        sb.AppendLine($"     tipLine1.setAttribute('y', by + {F(tipY1)});");
                        sb.AppendLine("      tipLine2.setAttribute('x', bx + textX);");
                        sb.AppendLine($"     tipLine2.setAttribute('y', by + {F(tipY2)});");
                        sb.AppendLine("      tip.style.opacity = '1';");
                        sb.AppendLine("    }");
                        sb.AppendLine("");
                        sb.AppendLine("    function svgXY(e) {");
                        sb.AppendLine("      var pt = svg.createSVGPoint();");
                        sb.AppendLine("      pt.x = e.clientX; pt.y = e.clientY;");
                        sb.AppendLine("      try { return pt.matrixTransform(svg.getScreenCTM().inverse()); }");
                        sb.AppendLine("      catch(_) { return { x: e.offsetX || 0, y: e.offsetY || 0 }; }");
                        sb.AppendLine("    }");
                        sb.AppendLine("");
                        sb.AppendLine("    svg.querySelectorAll('.tf-party-group').forEach(function(grp) {");
                        sb.AppendLine("      var name  = grp.getAttribute('data-party-name')  || '';");
                        sb.AppendLine("      var seats = grp.getAttribute('data-party-seats') || '0';");
                        sb.AppendLine("      var color = grp.getAttribute('data-party-color') || '#888';");
                        sb.AppendLine("      grp.addEventListener('mousemove', function(e) {");
                        sb.AppendLine("        var pt = svgXY(e); moveTip(pt.x, pt.y, name, seats, color);");
                        sb.AppendLine("      });");
                        sb.AppendLine("      grp.addEventListener('mouseleave', function() { tip.style.opacity = '0'; });");
                        sb.AppendLine("    });");
                    }

                    sb.AppendLine("  })();");
                    sb.AppendLine("  ]]>");
                    sb.AppendLine("  </script>");
                }
            }
        }

        /// <summary>
        /// Finds the centre-to-centre dot spacing such that the total number of dots
        /// placed on concentric semicircular arcs from <paramref name="rInner"/> to
        /// <paramref name="rMax"/> equals <paramref name="targetDots"/>.
        /// Uses binary search — converges in ≤ 64 iterations.
        /// </summary>
        private static double FindParliamentDotSpacing(double rInner, double rMax, int targetDots)
        {
            double lo = 1.0, hi = rMax;
            for (int iter = 0; iter < 64; iter++)
            {
                double mid = (lo + hi) * 0.5;
                if (CountParliamentDots(rInner, rMax, mid) > targetDots) lo = mid;
                else                                                       hi = mid;
            }
            return (lo + hi) * 0.5;
        }

        /// <summary>Returns the total dot count for the given ring geometry and spacing.</summary>
        private static int CountParliamentDots(double rInner, double rMax, double d)
        {
            int total = 0;
            for (double r = rInner; r <= rMax + 1e-9; r += d)
            {
                int n = (int)Math.Round(Math.PI * r / d);
                if (n >= 1) total += n;
            }
            return total;
        }

        // ================================================================== Helpers for new types

        /// <summary>
        /// Linearly interpolates between two hex colour strings based on <paramref name="t"/> ∈ [0, 1].
        /// Falls back to the hot colour when either colour cannot be parsed.
        /// </summary>
        private static string HeatmapColor(double t, string coldHex, string hotHex)
        {
            t = Math.Max(0, Math.Min(1, t));
            if (!ParseHexColor(coldHex, out int r1, out int g1, out int b1)) { r1 = 0xDC; g1 = 0xE8; b1 = 0xF5; }
            if (!ParseHexColor(hotHex,  out int r2, out int g2, out int b2)) { r2 = 0x7C; g2 = 0xB5; b2 = 0xEC; }
            return string.Format("#{0:X2}{1:X2}{2:X2}",
                Math.Max(0, Math.Min(255, (int)(r1 + (r2 - r1) * t))),
                Math.Max(0, Math.Min(255, (int)(g1 + (g2 - g1) * t))),
                Math.Max(0, Math.Min(255, (int)(b1 + (b2 - b1) * t))));
        }

        private static bool ParseHexColor(string hex, out int r, out int g, out int b)
        {
            r = g = b = 0;
            if (string.IsNullOrEmpty(hex) || hex[0] != '#' || hex.Length < 7) return false;
            try
            {
                r = Convert.ToInt32(hex.Substring(1, 2), 16);
                g = Convert.ToInt32(hex.Substring(3, 2), 16);
                b = Convert.ToInt32(hex.Substring(5, 2), 16);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Balanced binary-split treemap layout (recursive).
        /// <paramref name="items"/> must be sorted descending by value before the first call.
        /// </summary>
        private static void TreemapSplit(
            List<(string Label, double Value, string Color)> items,
            int start, int end,
            double x, double y, double w, double h,
            List<(double X, double Y, double W, double H, string Label, double Value, string Color)> cells)
        {
            if (start > end) return;
            if (start == end)
            {
                cells.Add((x, y, w, h, items[start].Label, items[start].Value, items[start].Color));
                return;
            }

            double total = 0;
            for (int i = start; i <= end; i++) total += items[i].Value;
            if (total <= 0) return;

            // Find the split index that best balances left and right halves
            double half = total / 2.0;
            double acc  = 0;
            int splitAt = start;
            for (int i = start; i < end; i++)
            {
                double prev = acc;
                acc += items[i].Value;
                if (acc >= half)
                {
                    // Choose whichever side is closer to half
                    splitAt = (half - prev) < (acc - half) && i > start ? i - 1 : i;
                    break;
                }
                splitAt = i;
            }

            double leftSum = 0;
            for (int i = start; i <= splitAt; i++) leftSum += items[i].Value;
            double frac = leftSum / total;

            if (w >= h)
            {
                double lw = w * frac;
                TreemapSplit(items, start, splitAt, x, y, lw, h, cells);
                if (splitAt + 1 <= end) TreemapSplit(items, splitAt + 1, end, x + lw, y, w - lw, h, cells);
            }
            else
            {
                double th = h * frac;
                TreemapSplit(items, start, splitAt, x, y, w, th, cells);
                if (splitAt + 1 <= end) TreemapSplit(items, splitAt + 1, end, x, y + th, w, h - th, cells);
            }
        }

    }
}
