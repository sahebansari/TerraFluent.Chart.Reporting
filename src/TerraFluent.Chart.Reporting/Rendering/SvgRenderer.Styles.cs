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
        private static void AppendStyles(StringBuilder sb, ChartOptions options, string svgId)
        {
            var t    = options.Theme;
            var mode = options.RenderMode;
            string p = $"#{svgId} ";   // CSS scope prefix, e.g. "#pc-3 "

            sb.AppendLine("  <style>");
            sb.AppendLine($"    {p}.chart-title   {{ font: bold 16px {Escape(t.FontFamily)}; fill: {Escape(t.TextColor)}; }}");
            sb.AppendLine($"    {p}.chart-subtitle{{ font: 12px {Escape(t.FontFamily)}; fill: {Escape(t.TextColor)}; }}");
            sb.AppendLine($"    {p}.axis-label    {{ font: 11px {Escape(t.FontFamily)}; fill: {Escape(t.TextColor)}; }}");
            sb.AppendLine($"    {p}.axis-title    {{ font: 12px {Escape(t.FontFamily)}; fill: {Escape(t.TextColor)}; }}");
            sb.AppendLine($"    {p}.legend-label  {{ font: {options.Legend.ItemFontSize}px {Escape(t.FontFamily)}; fill: {Escape(options.Legend.ItemFontColor ?? t.TextColor)}; }}");
            sb.AppendLine($"    {p}.data-label    {{ font: bold 10px {Escape(t.FontFamily)}; fill: {Escape(t.TextColor)}; pointer-events: none; }}");
            sb.AppendLine($"    {p}.axis-line     {{ stroke: {Escape(t.AxisLineColor)}; stroke-width: 1; }}");
            sb.AppendLine($"    {p}.grid-line     {{ stroke-width: 1; fill: none; }}");

            if (mode == SvgMode.Animated || mode == SvgMode.Interactive)
            {
                var tt = options.Tooltip;
                if (tt.Enabled)
                {
                    // Resolve tooltip colours: explicit override wins; null falls back to theme.
                    string effectiveBg  = tt.BackgroundColor ?? t.TooltipBackground;
                    string effectiveTxt = tt.TextColor       ?? t.TooltipTextColor;

                    string ttDur   = tt.TransitionDuration.ToString("F2", CultureInfo.InvariantCulture);
                    string ttFont  = tt.FontFamily != null
                        ? $"{tt.FontSize}px {Escape(tt.FontFamily)}"
                        : $"{tt.FontSize}px {Escape(t.FontFamily)}";

                    // Shadow via CSS drop-shadow filter on the tooltip box
                    string shadowRule = tt.Shadow
                        ? " filter: drop-shadow(0 2px 8px rgba(0,0,0,0.32));"
                        : string.Empty;

                    sb.AppendLine($"    {p}.data-point {{ cursor: pointer; -webkit-user-select: none; user-select: none; }}");
                    sb.AppendLine($"    {p}.tooltip-bg     {{ opacity: 0; transition: opacity {ttDur}s; fill: {Escape(effectiveBg)};{shadowRule} }}");
                    sb.AppendLine($"    {p}.tooltip-text   {{ opacity: 0; transition: opacity {ttDur}s; fill: {Escape(effectiveTxt)}; font: {ttFont}; pointer-events: none; }}");
                    sb.AppendLine($"    {p}.tooltip-bullet {{ opacity: 0; transition: opacity {ttDur}s; pointer-events: none; }}");

                    // In shared mode (Interactive only) individual CSS tooltips are suppressed;
                    // the JS-driven shared tooltip takes over.
                    if (tt.Shared && mode == SvgMode.Interactive)
                    {
                        // Keep CSS as-is; JS will override by hiding individual tooltips and
                        // showing the shared tooltip element instead.
                    }
                    else
                    {
                        sb.AppendLine($"    {p}.data-point:hover .tooltip-bg,");
                        sb.AppendLine($"    {p}.data-point:hover .tooltip-text,");
                        sb.AppendLine($"    {p}.data-point:hover .tooltip-bullet {{ opacity: 1; }}");
                    }
                }

                // Crosshair vertical guide line
                if (options.Tooltip.Enabled && options.Tooltip.Crosshair)
                {
                    string ttDurCh = options.Tooltip.TransitionDuration.ToString("F2", CultureInfo.InvariantCulture);
                    sb.AppendLine($"    {p}.crosshair-x {{ opacity: 0; transition: opacity {ttDurCh}s; pointer-events: none; }}");
                    sb.AppendLine($"    {p}.data-point:hover .crosshair-x {{ opacity: 1; }}");
                }

                // Hit-area overlay — provides noticeable hover feedback on the underlying visible shape.
                // fill  : light violet tint overlays the shape on hover.
                // stroke: vivid violet border outlines the shape edge clearly on any chart colour.
                // Circle hit-areas (scatter / line dots) also scale up for a clear "pop" ring effect.
                sb.AppendLine($"    {p}.hit-area {{");
                sb.AppendLine($"        fill: transparent;");
                sb.AppendLine($"        stroke: {ApplyAlpha(t.AccentColor, 0)};");
                sb.AppendLine($"        stroke-width: 1.5;");
                sb.AppendLine($"        pointer-events: all;");
                sb.AppendLine($"        transition: fill 0.15s ease, stroke 0.15s ease; }}");
                sb.AppendLine($"    {p}.data-point:hover .hit-area {{");
                sb.AppendLine($"        fill: {ApplyAlpha(t.AccentColor, 0.15)};");
                sb.AppendLine($"        stroke: {ApplyAlpha(t.AccentColor, 0.50)}; }}");
                // Circle hit-areas: animate the SVG 'r' geometry property — scales reliably
                // about the circle's own cx/cy without needing transform-origin hacks.
                sb.AppendLine($"    {p}.data-point circle.hit-area {{");
                sb.AppendLine($"        r: 8;");
                sb.AppendLine($"        transition: fill 0.15s ease, stroke 0.15s ease, r 0.15s ease; }}");
                sb.AppendLine($"    {p}.data-point:hover circle.hit-area {{ r: 13; }}");
                // Gauge arc glow on hover
                sb.AppendLine($"    {p}.gauge-highlight {{ opacity: 0; transition: opacity 0.15s ease; }}");
                sb.AppendLine($"    {p}.data-point:hover .gauge-highlight {{ opacity: 1; }}");
                // Suppress the browser default focus ring on mouse click (tabindex side-effect),
                // then restore a custom ring only for keyboard navigation via :focus-visible.
                sb.AppendLine($"    {p}.data-point:focus {{ outline: none; }}");
                sb.AppendLine($"    {p}.data-point:focus-visible {{ outline: 2px solid {ApplyAlpha(t.AccentColor, 0.7)}; outline-offset: 2px; }}");
                sb.AppendLine($"    {p}.tf-li:focus {{ outline: none; }}");
                sb.AppendLine($"    {p}.tf-li:focus-visible {{ outline: 2px solid {ApplyAlpha(t.AccentColor, 0.7)}; outline-offset: 3px; }}");
                // Export button / menu focus styles
                sb.AppendLine($"    {p}.tf-export-btn:focus {{ outline: none; }}");
                sb.AppendLine($"    {p}.tf-export-btn:focus-visible {{ outline: 2px solid {ApplyAlpha(t.AccentColor, 0.7)}; outline-offset: 2px; }}");
                // Export menu dropdown item hover effect
                sb.AppendLine($"    {p}.tf-export-item {{ cursor: pointer; }}");
                sb.AppendLine($"    {p}.tf-export-item:hover .tf-export-item-bg {{ fill: {ApplyAlpha(t.AccentColor, 0.7)}; }}");
                sb.AppendLine($"    {p}.tf-export-item:hover .tf-export-item-lbl {{ fill: #fff; }}");
            }

            sb.AppendLine("  </style>");

            if (options.RightToLeft)
            {
                string p2 = $"#{svgId} ";
                // Reopen a second <style> block just for RTL overrides
                sb.AppendLine("  <style>");
                AppendRtlStyles(sb, p2);
                sb.AppendLine("  </style>");
            }
        }

        // Injects RTL text-direction rules into an already-open <style> block (called before </style>).
        private static void AppendRtlStyles(StringBuilder sb, string p)
        {
            // direction:rtl sets the paragraph base direction; Unicode Bidi handles mixed Latin/Arabic runs.
            // bidi-override is intentionally omitted — it would reverse LTR characters inside Arabic strings.
            sb.AppendLine($"    {p}.chart-title, {p}.chart-subtitle,");
            sb.AppendLine($"    {p}.axis-title,");
            sb.AppendLine($"    {p}.data-label {{ direction: rtl; }}");
        }

        // ------------------------------------------------------------------ axes

    }
}
