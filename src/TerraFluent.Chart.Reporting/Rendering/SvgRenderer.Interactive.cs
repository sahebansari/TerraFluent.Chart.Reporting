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
        private static void AppendInteractiveScript(StringBuilder sb, ChartOptions options, string svgId,
            int plotWidth = 0, int plotHeight = 0)
        {
            var tt = options.Tooltip;
            // Resolve tooltip colours: explicit override wins; null falls back to theme.
            string effectiveJsBg  = tt.BackgroundColor ?? options.Theme.TooltipBackground;
            string effectiveJsTxt = tt.TextColor       ?? options.Theme.TooltipTextColor;
            sb.AppendLine("  <script type=\"text/javascript\">");
            sb.AppendLine("  <![CDATA[");

            // ── Click handler on individual data points ───────────────────────────────────
            // Only emitted when the caller has registered a handler via OnPointClick().
            // Wrapped in an IIFE that first resolves the *owning* SVG element so that,
            // when multiple SVGs are inlined in the same HTML page, the querySelectorAll
            // is scoped to this chart's SVG only and does not bleed into other charts.
            if (!string.IsNullOrWhiteSpace(options.PointClickHandler))
            {
                sb.AppendLine("    (function() {");
                sb.AppendLine($"      var _tfPointClickFn = {options.PointClickHandler};");
                sb.AppendLine("      var _tfSvg = document.currentScript ? document.currentScript.closest('svg') : null;");
                sb.AppendLine("      if (!_tfSvg) { var _s = document.querySelectorAll('svg script'); _tfSvg = _s.length ? _s[_s.length-1].closest('svg') : null; }");
                sb.AppendLine("      if (!_tfSvg) return;");
                sb.AppendLine("      _tfSvg.querySelectorAll('.data-point').forEach(function(el) {");
                sb.AppendLine("        el.addEventListener('click', function(e) {");
                sb.AppendLine("          var ha = el.querySelector('.hit-area');");
                sb.AppendLine("          if (ha) {");
                sb.AppendLine($"            ha.style.fill = '{ChartColor.WithOpacity(ChartColor.White, 0.35)}';");
                sb.AppendLine("            setTimeout(function() { ha.style.fill = ''; }, 250);");
                sb.AppendLine("          }");
                sb.AppendLine("          if (typeof _tfPointClickFn === 'function') {");
                sb.AppendLine("            var pt = {");
                sb.AppendLine("              index:    parseInt(el.getAttribute('data-di')    || '-1', 10),");
                sb.AppendLine("              value:    parseFloat(el.getAttribute('data-val') || '0'),");
                sb.AppendLine("              name:     el.getAttribute('data-name')  || '',");
                sb.AppendLine("              color:    el.getAttribute('data-color') || '',");
                sb.AppendLine("              x:        parseFloat(el.getAttribute('data-ax')  || '0'),");
                sb.AppendLine("              y:        parseFloat(el.getAttribute('data-ay')  || '0'),");
                sb.AppendLine("              category: el.getAttribute('data-xlabel') || ''");
                sb.AppendLine("            };");
                sb.AppendLine("            _tfPointClickFn(pt, e);");
                sb.AppendLine("          }");
                sb.AppendLine("        });");
                sb.AppendLine("      });");
                sb.AppendLine("    })();");
            }

            // ── Shared tooltip: show all series values at the same data-index ─────────────
            if (tt.Enabled && tt.Shared)
            {
                sb.AppendLine("");
                sb.AppendLine("    (function() {");
                sb.AppendLine("      // Shared tooltip: on hover of any .data-point[data-di],");
                sb.AppendLine("      // highlight all points with the same index and show a combined tooltip.");
                sb.AppendLine("      var svg = document.currentScript ? document.currentScript.closest('svg') : null;");
                sb.AppendLine("      if (!svg) { var scripts = document.querySelectorAll('svg script'); svg = scripts.length ? scripts[scripts.length-1].closest('svg') : null; }");
                sb.AppendLine("      if (!svg) return;");
                sb.AppendLine("");
                sb.AppendLine("      // Suppress individual CSS hover tooltips when shared mode is active.");
                sb.AppendLine("      // Scope the rule to this SVG's ID so it does not bleed into other charts.");
                sb.AppendLine("      var sharedStyle = document.createElementNS('http://www.w3.org/2000/svg', 'style');");
                sb.AppendLine("      var svgScope = svg.id ? '#' + svg.id + ' ' : 'svg ';");
                sb.AppendLine("      sharedStyle.textContent = svgScope + '.data-point:hover .tooltip-bg, ' + svgScope + '.data-point:hover .tooltip-text, ' + svgScope + '.data-point:hover .tooltip-bullet { opacity: 0 !important; }';");
                sb.AppendLine("      svg.insertBefore(sharedStyle, svg.firstChild);");
                sb.AppendLine("");
                sb.AppendLine("      // Build shared tooltip SVG group");
                sb.AppendLine("      var sharedTip = document.createElementNS('http://www.w3.org/2000/svg', 'g');");
                sb.AppendLine("      sharedTip.setAttribute('id', 'shared-tooltip');");
                sb.AppendLine("      sharedTip.style.pointerEvents = 'none';");
                sb.AppendLine("      sharedTip.style.opacity = '0';");
                sb.AppendLine("      sharedTip.style.transition = 'opacity 0.15s';");
                sb.AppendLine("      svg.appendChild(sharedTip);");
                sb.AppendLine("");
                sb.AppendLine("      var svgW = svg.viewBox.baseVal.width  || svg.clientWidth  || 600;");
                sb.AppendLine("      var svgH = svg.viewBox.baseVal.height || svg.clientHeight || 400;");
                sb.AppendLine($"      var bgColor  = '{Escape(effectiveJsBg)}';");
                sb.AppendLine($"      var txtColor = '{Escape(effectiveJsTxt)}';");
                sb.AppendLine($"      var fontSize = {tt.FontSize};");
                sb.AppendLine($"      var pad = {tt.Padding};");
                sb.AppendLine($"      var radius = {tt.BorderRadius};");
                sb.AppendLine($"      var shadow = {(tt.Shadow ? "true" : "false")};");
                sb.AppendLine("");
                sb.AppendLine("      function showShared(pts, anchorX, anchorY) {");
                sb.AppendLine("        while (sharedTip.firstChild) sharedTip.removeChild(sharedTip.firstChild);");
                sb.AppendLine("        if (!pts.length) return;");
                sb.AppendLine("        var lineH = fontSize + 5;");
                sb.AppendLine("        var bulletSize = 8;");
                sb.AppendLine("        var bulletGap = 6;");
                sb.AppendLine("        var headerH = 20;");
                sb.AppendLine("        var boxH = headerH + pts.length * lineH + pad;");
                sb.AppendLine("        var maxLen = 0;");
                sb.AppendLine("        pts.forEach(function(p) { var l = (p.name + ': ' + p.val).length; if (l > maxLen) maxLen = l; });");
                sb.AppendLine("        var boxW = Math.max(maxLen * 7.2 + bulletSize + bulletGap + pad * 2, 120);");
                sb.AppendLine("        var notchH = 7, notchW = 12;");
                sb.AppendLine("        var boxX = Math.max(2, Math.min(anchorX - boxW / 2, svgW - boxW - 2));");
                sb.AppendLine("        var boxY = Math.max(2, anchorY - boxH - notchH);");
                sb.AppendLine("        if (boxY + boxH + notchH > svgH - 2) boxY = svgH - boxH - notchH - 2;");
                sb.AppendLine("        // Box");
                sb.AppendLine("        var box = document.createElementNS('http://www.w3.org/2000/svg', 'rect');");
                sb.AppendLine("        box.setAttribute('x', boxX); box.setAttribute('y', boxY);");
                sb.AppendLine("        box.setAttribute('width', boxW); box.setAttribute('height', boxH);");
                sb.AppendLine("        box.setAttribute('rx', radius); box.setAttribute('fill', bgColor);");
                sb.AppendLine("        if (shadow) box.style.filter = 'drop-shadow(0 2px 8px rgba(0,0,0,0.32))';");
                sb.AppendLine("        sharedTip.appendChild(box);");
                sb.AppendLine("        // Notch");
                sb.AppendLine("        var ntx = Math.max(boxX + notchW, Math.min(anchorX, boxX + boxW - notchW));");
                sb.AppendLine("        var poly = document.createElementNS('http://www.w3.org/2000/svg', 'polygon');");
                sb.AppendLine("        poly.setAttribute('points', (ntx-notchW/2)+','+(boxY+boxH)+' '+(ntx+notchW/2)+','+(boxY+boxH)+' '+ntx+','+(boxY+boxH+notchH));");
                sb.AppendLine("        poly.setAttribute('fill', bgColor);");
                sb.AppendLine("        sharedTip.appendChild(poly);");
                sb.AppendLine("        // Header: x-label from first point's xLabel");
                sb.AppendLine("        var hdr = document.createElementNS('http://www.w3.org/2000/svg', 'text');");
                sb.AppendLine("        hdr.setAttribute('x', boxX + pad); hdr.setAttribute('y', boxY + 14);");
                sb.AppendLine("        hdr.setAttribute('fill', txtColor); hdr.setAttribute('font-size', fontSize);");
                sb.AppendLine("        hdr.setAttribute('font-weight', 'bold'); hdr.style.pointerEvents = 'none';");
                sb.AppendLine("        hdr.textContent = pts[0].xLabel || '';");
                sb.AppendLine("        sharedTip.appendChild(hdr);");
                sb.AppendLine("        // Separator line");
                sb.AppendLine("        var sep = document.createElementNS('http://www.w3.org/2000/svg', 'line');");
                sb.AppendLine("        sep.setAttribute('x1', boxX + pad); sep.setAttribute('x2', boxX + boxW - pad);");
                sb.AppendLine("        sep.setAttribute('y1', boxY + headerH); sep.setAttribute('y2', boxY + headerH);");
                sb.AppendLine("        sep.setAttribute('stroke', txtColor); sep.setAttribute('stroke-opacity', '0.2'); sep.setAttribute('stroke-width', '1');");
                sb.AppendLine("        sharedTip.appendChild(sep);");
                sb.AppendLine("        // Per-series rows");
                sb.AppendLine("        pts.forEach(function(p, idx) {");
                sb.AppendLine("          var rowY = boxY + headerH + pad / 2 + idx * lineH + lineH * 0.7;");
                sb.AppendLine("          // Bullet");
                sb.AppendLine("          var bul = document.createElementNS('http://www.w3.org/2000/svg', 'rect');");
                sb.AppendLine("          bul.setAttribute('x', boxX + pad); bul.setAttribute('y', rowY - bulletSize / 2);");
                sb.AppendLine("          bul.setAttribute('width', bulletSize); bul.setAttribute('height', bulletSize);");
                sb.AppendLine("          bul.setAttribute('rx', 2); bul.setAttribute('fill', p.color);");
                sb.AppendLine("          sharedTip.appendChild(bul);");
                sb.AppendLine("          // Text");
                sb.AppendLine("          var txt = document.createElementNS('http://www.w3.org/2000/svg', 'text');");
                sb.AppendLine("          txt.setAttribute('x', boxX + pad + bulletSize + bulletGap);");
                sb.AppendLine("          txt.setAttribute('y', rowY); txt.setAttribute('fill', txtColor);");
                sb.AppendLine("          txt.setAttribute('font-size', fontSize);");
                sb.AppendLine("          txt.setAttribute('dominant-baseline', 'central');");
                sb.AppendLine("          txt.style.pointerEvents = 'none';");
                sb.AppendLine("          txt.textContent = p.name + ': ' + p.val;");
                sb.AppendLine("          sharedTip.appendChild(txt);");
                sb.AppendLine("        });");
                sb.AppendLine("        sharedTip.style.opacity = '1';");
                sb.AppendLine("      }");
                sb.AppendLine("");
                sb.AppendLine("      function hideShared() { sharedTip.style.opacity = '0'; }");
                sb.AppendLine("");
                sb.AppendLine("      svg.querySelectorAll('.data-point[data-di]').forEach(function(el) {");
                sb.AppendLine("        el.addEventListener('mouseenter', function() {");
                sb.AppendLine("          var di = el.getAttribute('data-di');");
                sb.AppendLine("          var peers = svg.querySelectorAll('.data-point[data-di=\"' + di + '\"]');");
                sb.AppendLine("          var pts = [];");
                sb.AppendLine("          var ax = 0, ay = 0, n = 0;");
                sb.AppendLine("          peers.forEach(function(p) {");
                sb.AppendLine("            if (!p.getAttribute('data-val')) return;");
                sb.AppendLine("            pts.push({ name: p.getAttribute('data-name') || '',");
                sb.AppendLine("                        val:  p.getAttribute('data-val')  || '',");
                sb.AppendLine("                        color: p.getAttribute('data-color') || '#888',");
                sb.AppendLine("                        xLabel: p.getAttribute('data-xlabel') || '' });");
                sb.AppendLine("            var bx = parseFloat(p.getAttribute('data-ax') || 0);");
                sb.AppendLine("            var by = parseFloat(p.getAttribute('data-ay') || 0);");
                sb.AppendLine("            if (bx) { ax += bx; n++; }");
                sb.AppendLine("            if (by) ay = Math.min(ay || by, by);");
                sb.AppendLine("          });");
                sb.AppendLine("          if (pts.length) showShared(pts, n ? ax / n : 0, ay ? ay : 50);");
                sb.AppendLine("        });");
                sb.AppendLine("        el.addEventListener('mouseleave', function() { hideShared(); });");
                sb.AppendLine("      });");
                sb.AppendLine("    })();");
            }

            // ── Follow-pointer: tooltip tracks the mouse cursor ───────────────────────────
            if (tt.Enabled && tt.FollowPointer)
            {
                sb.AppendLine("");
                sb.AppendLine("    (function() {");
                sb.AppendLine("      var svg = document.currentScript ? document.currentScript.closest('svg') : null;");
                sb.AppendLine("      if (!svg) { var sc = document.querySelectorAll('svg script'); svg = sc.length ? sc[sc.length-1].closest('svg') : null; }");
                sb.AppendLine("      if (!svg) return;");
                sb.AppendLine("      svg.querySelectorAll('.data-point').forEach(function(el) {");
                sb.AppendLine("        el.addEventListener('mousemove', function(e) {");
                sb.AppendLine("          var svgRect = svg.getBoundingClientRect();");
                sb.AppendLine("          var scaleX = (svg.viewBox.baseVal.width  || svgRect.width)  / svgRect.width;");
                sb.AppendLine("          var scaleY = (svg.viewBox.baseVal.height || svgRect.height) / svgRect.height;");
                sb.AppendLine("          var mx = (e.clientX - svgRect.left) * scaleX;");
                sb.AppendLine("          var my = (e.clientY - svgRect.top)  * scaleY;");
                sb.AppendLine("          // Move tooltip-bg, tooltip-text, tooltip-bullet, crosshair-x,");
                sb.AppendLine("          // polygon arrow to follow the cursor by applying a transform offset.");
                sb.AppendLine("          var tipBg = el.querySelector('.tooltip-bg');");
                sb.AppendLine("          if (!tipBg) return;");
                sb.AppendLine("          var bx = parseFloat(tipBg.getAttribute('x') || 0);");
                sb.AppendLine("          var by = parseFloat(tipBg.getAttribute('y') || 0);");
                sb.AppendLine("          var bw = parseFloat(tipBg.getAttribute('width')  || 0);");
                sb.AppendLine("          var bh = parseFloat(tipBg.getAttribute('height') || 0);");
                sb.AppendLine("          var dx = mx - (bx + bw / 2);");
                sb.AppendLine("          var dy = my - (by + bh + 8);");
                sb.AppendLine("          el.querySelectorAll('.tooltip-bg, .tooltip-text, .tooltip-bullet').forEach(function(n) {");
                sb.AppendLine("            n.setAttribute('transform', 'translate(' + dx + ',' + dy + ')');");
                sb.AppendLine("          });");
                sb.AppendLine("        });");
                sb.AppendLine("        el.addEventListener('mouseleave', function() {");
                sb.AppendLine("          el.querySelectorAll('.tooltip-bg, .tooltip-text, .tooltip-bullet').forEach(function(n) {");
                sb.AppendLine("            n.removeAttribute('transform');");
                sb.AppendLine("          });");
                sb.AppendLine("        });");
                sb.AppendLine("      });");
                sb.AppendLine("    })();");
            }

            // ── Data-point accessibility: tabindex, aria-label, keyboard activation ──────
            sb.AppendLine("");
            sb.AppendLine("    (function() {");
            sb.AppendLine("      var svg = document.currentScript ? document.currentScript.closest('svg') : null;");
            sb.AppendLine("      if (!svg) { var sc = document.querySelectorAll('svg script'); svg = sc.length ? sc[sc.length-1].closest('svg') : null; }");
            sb.AppendLine("      if (!svg) return;");
            sb.AppendLine("      svg.querySelectorAll('.data-point').forEach(function(el) {");
            sb.AppendLine("        el.setAttribute('tabindex', '0');");
            sb.AppendLine("        el.setAttribute('role', 'img');");
            sb.AppendLine("        var name = el.getAttribute('data-name')  || '';");
            sb.AppendLine("        var val  = el.getAttribute('data-val')   || '';");
            sb.AppendLine("        var cat  = el.getAttribute('data-xlabel') || '';");
            sb.AppendLine("        if (name || val || cat) {");
            sb.AppendLine("          var parts = [];");
            sb.AppendLine("          if (name) parts.push(name);");
            sb.AppendLine("          if (cat)  parts.push(cat);");
            sb.AppendLine("          if (val)  parts.push(val);");
            sb.AppendLine("          el.setAttribute('aria-label', parts.join(', '));");
            sb.AppendLine("        }");
            sb.AppendLine("        el.addEventListener('keydown', function(e) {");
            sb.AppendLine("          if (e.key === 'Enter' || e.key === ' ') {");
            sb.AppendLine("            e.preventDefault();");
            sb.AppendLine("            el.dispatchEvent(new MouseEvent('click', { bubbles: true }));");
            sb.AppendLine("          }");
            sb.AppendLine("        });");
            sb.AppendLine("      });");
            sb.AppendLine("    })();");

            // ── Legend pagination ─────────────────────────────────────────────────────────
            // Wire prev/next arrows when .tf-leg-page groups exist (Legend.MaxLegendRows > 0).
            if (options.Legend?.Enabled == true)
            {
                sb.AppendLine("");
                sb.AppendLine("    (function() {");
                sb.AppendLine("      var svg = document.currentScript ? document.currentScript.closest('svg') : null;");
                sb.AppendLine("      if (!svg) { var sc = document.querySelectorAll('svg script'); svg = sc.length ? sc[sc.length-1].closest('svg') : null; }");
                sb.AppendLine("      if (!svg) return;");
                sb.AppendLine("      var pages = Array.prototype.slice.call(svg.querySelectorAll('.tf-leg-page'));");
                sb.AppendLine("      if (pages.length > 1) {");
                sb.AppendLine("        var cur = 0;");
                sb.AppendLine("        function showPage(n) {");
                sb.AppendLine("          for (var i = 0; i < pages.length; i++)");
                sb.AppendLine("            pages[i].setAttribute('visibility', i === n ? 'visible' : 'hidden');");
                sb.AppendLine("          cur = n;");
                sb.AppendLine("          var ind = svg.querySelector('.tf-leg-page-ind');");
                sb.AppendLine("          if (ind) ind.textContent = (cur+1) + ' / ' + pages.length;");
                sb.AppendLine("          var prev = svg.querySelector('.tf-leg-prev');");
                sb.AppendLine("          var next = svg.querySelector('.tf-leg-next');");
                sb.AppendLine("          if (prev) prev.style.opacity = cur === 0 ? '0.3' : '1';");
                sb.AppendLine("          if (next) next.style.opacity = cur >= pages.length-1 ? '0.3' : '1';");
                sb.AppendLine("        }");
                sb.AppendLine("        function bindBtn(sel, delta) {");
                sb.AppendLine("          var btn = svg.querySelector(sel); if (!btn) return;");
                sb.AppendLine("          function act() { var n=cur+delta; if(n>=0&&n<pages.length) showPage(n); }");
                sb.AppendLine("          btn.addEventListener('click', act);");
                sb.AppendLine("          btn.addEventListener('keydown', function(e){if(e.key==='Enter'||e.key===' '){e.preventDefault();act();}});");
                sb.AppendLine("        }");
                sb.AppendLine("        bindBtn('.tf-leg-prev', -1);");
                sb.AppendLine("        bindBtn('.tf-leg-next', 1);");
                sb.AppendLine("        showPage(0);");
                sb.AppendLine("      }");
                sb.AppendLine("    })();");
            }

            // ── Legend series toggle ──────────────────────────────────────────────────────
            // Click a legend item → hide/show that series group and its tooltip hit-areas.
            // Only emitted when the chart has a legend (otherwise no .tf-li elements exist).
            if (options.Legend?.Enabled == true)
            {
                sb.AppendLine("");
                sb.AppendLine("    (function() {");
                sb.AppendLine("      var svg = document.currentScript ? document.currentScript.closest('svg') : null;");
                sb.AppendLine("      if (!svg) { var sc = document.querySelectorAll('svg script'); svg = sc.length ? sc[sc.length-1].closest('svg') : null; }");
                sb.AppendLine("      if (!svg) return;");
                sb.AppendLine("      svg.querySelectorAll('.tf-li[data-si]').forEach(function(leg) {");
                sb.AppendLine("        function toggleLeg(e) {");
                sb.AppendLine("          e.stopPropagation();");
                sb.AppendLine("          var si    = leg.getAttribute('data-si');");
                sb.AppendLine("          var sname = leg.getAttribute('data-sname');");
                sb.AppendLine("          var sg    = svg.querySelector('.tf-sg[data-si=\"' + si + '\"]');");
                sb.AppendLine("          var hidden = sg && sg.getAttribute('data-hidden') === '1';");
                sb.AppendLine("          var show   = !!hidden;   // show=true means we are un-hiding");
                sb.AppendLine("          if (sg) {");
                sb.AppendLine("            sg.style.transition = 'opacity 0.2s';");
                sb.AppendLine("            sg.style.opacity = show ? '' : '0.15';");
                sb.AppendLine("            sg.setAttribute('data-hidden', show ? '0' : '1');");
                sb.AppendLine("          }");
                sb.AppendLine("          // Also hide tooltip hit-areas belonging to this series.");
                sb.AppendLine("          if (sname !== null) {");
                sb.AppendLine("            svg.querySelectorAll('.data-point').forEach(function(dp) {");
                sb.AppendLine("              if (dp.getAttribute('data-name') === sname) dp.style.display = show ? '' : 'none';");
                sb.AppendLine("            });");
                sb.AppendLine("          }");
                sb.AppendLine("          // Dim legend item when hidden; update ARIA pressed state.");
                sb.AppendLine("          leg.style.opacity = show ? '' : '0.4';");
                sb.AppendLine("          leg.setAttribute('aria-pressed', show ? 'false' : 'true');");
                sb.AppendLine("        }");
                sb.AppendLine("        leg.addEventListener('click', toggleLeg);");
                sb.AppendLine("        leg.addEventListener('keydown', function(e) {");
                sb.AppendLine("          if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); toggleLeg(e); }");
                sb.AppendLine("        });");
                sb.AppendLine("      });");
                sb.AppendLine("    })();");
            }

            // ── Export button download handler ────────────────────────────────────────────────
            if (options.ExportMenuEnabled && options.ExportMenuFormats != null && options.ExportMenuFormats.Count > 0)
            {
                string menuFname = SanitizeFilename(options.Title?.Text);
                // Base name without extension
                string menuBase  = menuFname.EndsWith(".svg", System.StringComparison.OrdinalIgnoreCase)
                                   ? menuFname.Substring(0, menuFname.Length - 4) : menuFname;
                sb.AppendLine("");
                sb.AppendLine("    // ── Export menu ────────────────────────────────────────────────────────────");
                sb.AppendLine("    (function() {");
                sb.AppendLine("      var svg = document.currentScript ? document.currentScript.closest('svg') : null;");
                sb.AppendLine("      if (!svg) { var sc = document.querySelectorAll('svg script'); svg = sc.length ? sc[sc.length-1].closest('svg') : null; }");
                sb.AppendLine("      if (!svg) return;");
                sb.AppendLine("      var trigger  = svg.querySelector('.tf-export-trigger');");
                sb.AppendLine("      var dropdown = svg.querySelector('.tf-export-dropdown');");
                sb.AppendLine("      if (!trigger || !dropdown) return;");
                sb.AppendLine("");
                sb.AppendLine("      function closeMenu() {");
                sb.AppendLine("        dropdown.style.display = 'none';");
                sb.AppendLine("        trigger.setAttribute('aria-expanded', 'false');");
                sb.AppendLine("      }");
                sb.AppendLine("      function openMenu() {");
                sb.AppendLine("        dropdown.style.display = '';");
                sb.AppendLine("        trigger.setAttribute('aria-expanded', 'true');");
                sb.AppendLine("      }");
                sb.AppendLine("");
                sb.AppendLine("      // Toggle on trigger click");
                sb.AppendLine("      trigger.addEventListener('click', function(e) {");
                sb.AppendLine("        e.stopPropagation();");
                sb.AppendLine("        dropdown.style.display === 'none' ? openMenu() : closeMenu();");
                sb.AppendLine("      });");
                sb.AppendLine("      trigger.addEventListener('keydown', function(e) {");
                sb.AppendLine("        if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); dropdown.style.display === 'none' ? openMenu() : closeMenu(); }");
                sb.AppendLine("        if (e.key === 'Escape') closeMenu();");
                sb.AppendLine("      });");
                sb.AppendLine("");
                sb.AppendLine("      // Format item clicks");
                sb.AppendLine("      dropdown.addEventListener('click', function(e) {");
                sb.AppendLine("        e.stopPropagation();");
                sb.AppendLine("        var item = e.target.closest('.tf-export-item');");
                sb.AppendLine("        if (!item) return;");
                sb.AppendLine("        closeMenu();");
                sb.AppendLine("        exportAs(item.dataset.fmt);");
                sb.AppendLine("      });");
                sb.AppendLine("");
                sb.AppendLine("      // Close on outside click");
                sb.AppendLine("      document.addEventListener('click', closeMenu);");
                sb.AppendLine("");
                sb.AppendLine($"      var _base = '{menuBase}';");
                sb.AppendLine("");
                sb.AppendLine("      function exportAs(fmt) {");
                sb.AppendLine("        var f = (fmt || 'SVG').toUpperCase();");
                sb.AppendLine("        if (f === 'SVG')  exportSvg();");
                sb.AppendLine("        else if (f === 'PNG')  exportRaster('image/png',  _base + '.png');");
                sb.AppendLine("        else if (f === 'JPEG') exportRaster('image/jpeg', _base + '.jpg');");
                sb.AppendLine("        else if (f === 'PDF')  exportPdf();");
                sb.AppendLine("      }");
                sb.AppendLine("");
                sb.AppendLine("      function bakeSvgClone() {");
                sb.AppendLine("        var clone = svg.cloneNode(true);");
                sb.AppendLine("        clone.querySelectorAll('animate').forEach(function(anim) {");
                sb.AppendLine("          var parent = anim.parentElement;");
                sb.AppendLine("          var attr   = anim.getAttribute('attributeName');");
                sb.AppendLine("          var toVal  = anim.getAttribute('to');");
                sb.AppendLine("          if (parent && attr && toVal !== null) parent.setAttribute(attr, toVal);");
                sb.AppendLine("          anim.parentNode.removeChild(anim);");
                sb.AppendLine("        });");
                sb.AppendLine("        clone.querySelectorAll('[stroke-dashoffset]').forEach(function(el) {");
                sb.AppendLine("          el.removeAttribute('stroke-dashoffset');");
                sb.AppendLine("          el.removeAttribute('stroke-dasharray');");
                sb.AppendLine("          el.removeAttribute('pathLength');");
                sb.AppendLine("        });");
                sb.AppendLine("        clone.querySelectorAll('.tf-export-btn,.tf-export-dropdown').forEach(function(el) {");
                sb.AppendLine("          if (el.parentNode) el.parentNode.removeChild(el);");
                sb.AppendLine("        });");
                sb.AppendLine("        return clone;");
                sb.AppendLine("      }");
                sb.AppendLine("");
                sb.AppendLine("      function exportSvg() {");
                sb.AppendLine("        var xml = new XMLSerializer().serializeToString(bakeSvgClone());");
                sb.AppendLine($"        var blob = new Blob([xml], {{ type: 'image/svg+xml;charset=utf-8' }});");
                sb.AppendLine("        triggerDownload(URL.createObjectURL(blob), _base + '.svg');");
                sb.AppendLine("      }");
                sb.AppendLine("");
                sb.AppendLine("      function exportRaster(mime, fname) {");
                sb.AppendLine("        var size = getExportSize();");
                sb.AppendLine("        var w = size.w;");
                sb.AppendLine("        var h = size.h;");
                sb.AppendLine("        svgToCanvas(w, h, function(canvas) {");
                sb.AppendLine("          if (mime === 'image/jpeg') { var cx=canvas.getContext('2d'); cx.globalCompositeOperation='destination-over'; cx.fillStyle='#fff'; cx.fillRect(0,0,w,h); }");
                sb.AppendLine("          var dataUrl = canvas.toDataURL(mime, 0.95);");
                sb.AppendLine("          triggerDownload(dataUrl, fname);");
                sb.AppendLine("        });");
                sb.AppendLine("      }");
                sb.AppendLine("");
                sb.AppendLine("      function exportPdf() {");
                sb.AppendLine("        // PDF cannot natively embed SVG as vectors without an external library.");
                sb.AppendLine("        // Serialize the animation-baked SVG into a minimal HTML page, inject it");
                sb.AppendLine("        // into a hidden iframe (no popup-blocking), and call contentWindow.print().");
                sb.AppendLine("        // The browser print engine renders SVG as true vectors -> crisp at any zoom.");
                sb.AppendLine("        var xml = new XMLSerializer().serializeToString(bakeSvgClone());");
                sb.AppendLine("        var size = getExportSize();");
                sb.AppendLine("        var w = size.w;");
                sb.AppendLine("        var h = size.h;");
                sb.AppendLine("        var html = '<!DOCTYPE html><html><head><meta charset=\"utf-8\">'");
                sb.AppendLine("          + '<style>@page{size:' + w + 'px ' + h + 'px;margin:0}'");
                sb.AppendLine("          + 'html,body{margin:0;padding:0;width:' + w + 'px;height:' + h + 'px;background:#fff}'");
                sb.AppendLine("          + 'svg{display:block;width:' + w + 'px;height:' + h + 'px}'");
                sb.AppendLine("          + '.tf-export-btn,.tf-export-dropdown{display:none!important}'");
                sb.AppendLine("          + '</style><title>' + _base + '</title></head><body>' + xml + '</body></html>';");
                sb.AppendLine("        // Write directly into the iframe document instead of using a blob src.");
                sb.AppendLine("        // A blob: URL gets origin null, which triggers a cross-origin SecurityError.");
                sb.AppendLine("        // contentDocument.write() stays same-origin so contentWindow.print() works.");
                sb.AppendLine("        var iframe = document.createElement('iframe');");
                sb.AppendLine("        iframe.style.cssText = 'position:fixed;top:-9999px;left:-9999px;width:' + w + 'px;height:' + h + 'px;border:0;visibility:hidden';");
                sb.AppendLine("        document.body.appendChild(iframe);");
                sb.AppendLine("        iframe.contentDocument.open();");
                sb.AppendLine("        iframe.contentDocument.write(html);");
                sb.AppendLine("        iframe.contentDocument.close();");
                sb.AppendLine("        iframe.contentWindow.focus();");
                sb.AppendLine("        iframe.contentWindow.print();");
                sb.AppendLine("        setTimeout(function() {");
                sb.AppendLine("          document.body.removeChild(iframe);");
                sb.AppendLine("        }, 60000);");
                sb.AppendLine("      }");
                sb.AppendLine("");
                sb.AppendLine("      function getExportSize() {");
                sb.AppendLine("        var vb = svg.viewBox && svg.viewBox.baseVal ? svg.viewBox.baseVal : null;");
                sb.AppendLine("        var w = vb && vb.width  ? vb.width  : 0;");
                sb.AppendLine("        var h = vb && vb.height ? vb.height : 0;");
                sb.AppendLine("        if (!w || !h) {");
                sb.AppendLine("          var r = svg.getBoundingClientRect();");
                sb.AppendLine("          if (!w && r.width)  w = r.width;");
                sb.AppendLine("          if (!h && r.height) h = r.height;");
                sb.AppendLine("        }");
                sb.AppendLine("        if (!w || !h) {");
                sb.AppendLine("          w = 800;");
                sb.AppendLine("          h = 600;");
                sb.AppendLine("        }");
                sb.AppendLine("        return { w: Math.max(1, Math.round(w)), h: Math.max(1, Math.round(h)) };");
                sb.AppendLine("      }");
                sb.AppendLine("");
                sb.AppendLine("      function svgToCanvas(w, h, cb) {");
                sb.AppendLine($"        var xml = new XMLSerializer().serializeToString(bakeSvgClone());");
                sb.AppendLine($"        var blob = new Blob([xml], {{ type: 'image/svg+xml' }});");
                sb.AppendLine("        var url = URL.createObjectURL(blob);");
                sb.AppendLine("        var img = new Image(); img.width = w; img.height = h;");
                sb.AppendLine("        img.onload = function() {");
                sb.AppendLine("          var canvas = document.createElement('canvas');");
                sb.AppendLine("          canvas.width = w; canvas.height = h;");
                sb.AppendLine("          canvas.getContext('2d').drawImage(img, 0, 0, w, h);");
                sb.AppendLine("          URL.revokeObjectURL(url);");
                sb.AppendLine("          cb(canvas);");
                sb.AppendLine("        };");
                sb.AppendLine("        img.src = url;");
                sb.AppendLine("      }");
                sb.AppendLine("");
                sb.AppendLine("      function triggerDownload(url, fname) {");
                sb.AppendLine("        var a = document.createElement('a');");
                sb.AppendLine("        a.href = url; a.download = fname;");
                sb.AppendLine("        document.body.appendChild(a); a.click(); document.body.removeChild(a);");
                sb.AppendLine("        setTimeout(function(){ URL.revokeObjectURL(url); }, 1000);");
                sb.AppendLine("      }");
                sb.AppendLine("");
                sb.AppendLine("      // Hover effect on trigger button");
                sb.AppendLine("      var bg = trigger.querySelector('.tf-export-bg');");
                sb.AppendLine("      if (bg) {");
                sb.AppendLine("        trigger.addEventListener('mouseenter', function(){ bg.setAttribute('fill','rgba(128,128,128,0.25)'); });");
                sb.AppendLine("        trigger.addEventListener('mouseleave', function(){ bg.setAttribute('fill','rgba(128,128,128,0.12)'); });");
                sb.AppendLine("      }");
                sb.AppendLine("    })();");
            }

            if (options.ExportButtonEnabled)
            {
                string fname = SanitizeFilename(options.Title?.Text);
                sb.AppendLine("");
                sb.AppendLine("    (function() {");
                sb.AppendLine("      var svg = document.currentScript ? document.currentScript.closest('svg') : null;");
                sb.AppendLine("      if (!svg) { var sc = document.querySelectorAll('svg script'); svg = sc.length ? sc[sc.length-1].closest('svg') : null; }");
                sb.AppendLine("      if (!svg) return;");
                sb.AppendLine("      var btn = svg.querySelector('.tf-export-btn');");
                sb.AppendLine("      if (!btn) return;");
                sb.AppendLine("      var bg = btn.querySelector('.tf-export-bg');");
                sb.AppendLine("      function doExport() {");
                sb.AppendLine("        var clone = svg.cloneNode(true);");
                sb.AppendLine("        clone.querySelectorAll('animate').forEach(function(anim) {");
                sb.AppendLine("          var parent = anim.parentElement;");
                sb.AppendLine("          var attr   = anim.getAttribute('attributeName');");
                sb.AppendLine("          var toVal  = anim.getAttribute('to');");
                sb.AppendLine("          if (parent && attr && toVal !== null) parent.setAttribute(attr, toVal);");
                sb.AppendLine("          anim.parentNode.removeChild(anim);");
                sb.AppendLine("        });");
                sb.AppendLine("        clone.querySelectorAll('[stroke-dashoffset]').forEach(function(el) {");
                sb.AppendLine("          el.removeAttribute('stroke-dashoffset');");
                sb.AppendLine("          el.removeAttribute('stroke-dasharray');");
                sb.AppendLine("          el.removeAttribute('pathLength');");
                sb.AppendLine("        });");
                sb.AppendLine("        clone.querySelectorAll('.tf-export-btn,.tf-export-dropdown').forEach(function(el) {");
                sb.AppendLine("          if (el.parentNode) el.parentNode.removeChild(el);");
                sb.AppendLine("        });");
                sb.AppendLine("        var xml = new XMLSerializer().serializeToString(clone);");
                sb.AppendLine($"        var blob = new Blob([xml], {{ type: 'image/svg+xml;charset=utf-8' }});");
                sb.AppendLine($"        var url = URL.createObjectURL(blob);");
                sb.AppendLine($"        var a = document.createElement('a');");
                sb.AppendLine($"        a.href = url; a.download = '{fname}';");
                sb.AppendLine($"        document.body.appendChild(a); a.click(); document.body.removeChild(a);");
                sb.AppendLine($"        setTimeout(function() {{ URL.revokeObjectURL(url); }}, 1000);");
                sb.AppendLine("      }");
                sb.AppendLine("      btn.addEventListener('click', doExport);");
                sb.AppendLine("      btn.addEventListener('keydown', function(e) {");
                sb.AppendLine("        if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); doExport(); }");
                sb.AppendLine("      });");
                sb.AppendLine("      if (bg) {");
                sb.AppendLine("        btn.addEventListener('mouseenter', function() { bg.setAttribute('fill', 'rgba(128,128,128,0.25)'); });");
                sb.AppendLine("        btn.addEventListener('mouseleave', function() { bg.setAttribute('fill', 'rgba(128,128,128,0.12)'); });");
                sb.AppendLine("      }");
                sb.AppendLine("    })();");
            }

            // ── Range selector: brush drives zoom+pan on the main chart ───────────────────
            if (options.RangeSelector.Enabled)
            {
                var rs   = options.RangeSelector;
                var cats = options.XAxis.Categories;

                // Serialize x-axis categories to a compact JSON array embedded in the script.
                var catsSb = new System.Text.StringBuilder("[");
                if (cats != null)
                {
                    for (int ci = 0; ci < cats.Count; ci++)
                    {
                        if (ci > 0) catsSb.Append(',');
                        catsSb.Append('"');
                        foreach (char ch in cats[ci])
                        {
                            if      (ch == '"')  catsSb.Append("\\\"");
                            else if (ch == '\\') catsSb.Append("\\\\");
                            else if (ch < 0x20)  catsSb.Append("\\u").Append(((int)ch).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
                            else                 catsSb.Append(ch);
                        }
                        catsSb.Append('"');
                    }
                }
                catsSb.Append(']');

                // Compute y-axis bounds for path redraw (same as AppendSeries uses).
                var (yMinRs, yMaxRs) = ResolveYBounds(options.YAxis, options.Series);

                // Serialize Line/Spline series data for JS path redraw.
                // IDs must match the pattern used in AppendSeries: {svgId}-rs-lp-{si}
                var seriesSb = new System.Text.StringBuilder("[");
                bool firstSeries = true;
                for (int si = 0; si < options.Series.Count; si++)
                {
                    var s = options.Series[si];
                    if (s.Type != ChartType.Line && s.Type != ChartType.Spline) continue;
                    if (!firstSeries) seriesSb.Append(',');
                    firstSeries = false;
                    seriesSb.Append("{\"id\":\"");
                    seriesSb.Append(Escape(svgId));
                    seriesSb.Append("-rs-lp-");
                    seriesSb.Append(si.ToString(CultureInfo.InvariantCulture));
                    seriesSb.Append("\",\"data\":[");
                    for (int di = 0; di < s.Data.Count; di++)
                    {
                        if (di > 0) seriesSb.Append(',');
                        if (s.Data[di] == null) seriesSb.Append("null");
                        else seriesSb.Append(s.Data[di]!.Value.ToString(CultureInfo.InvariantCulture));
                    }
                    seriesSb.Append("]}");
                }
                seriesSb.Append(']');

                string textColorJs  = Escape(options.Theme.TextColor);
                string gridColorJs  = Escape(options.Theme.GridLineColor);
                int    tickYLit     = PaddingTop + plotHeight;  // y of the x-axis baseline
                double initialStart = rs.InitialStart;
                double initialEnd   = rs.InitialEnd;

                sb.AppendLine("");
                sb.AppendLine("    // ── Range selector ─────────────────────────────────────────────────────────");
                sb.AppendLine("    (function() {");
                sb.AppendLine("      var svg = document.currentScript ? document.currentScript.closest('svg') : null;");
                sb.AppendLine("      if (!svg) { var sc = document.querySelectorAll('svg script'); svg = sc.length ? sc[sc.length-1].closest('svg') : null; }");
                sb.AppendLine("      if (!svg) return;");
                sb.AppendLine($"      var overlay = svg.getElementById('{Escape(svgId)}-rs-overlay');");
                sb.AppendLine($"      var lh = svg.getElementById('{Escape(svgId)}-rs-lh');");
                sb.AppendLine($"      var rh = svg.getElementById('{Escape(svgId)}-rs-rh');");
                sb.AppendLine("      if (!overlay || !lh || !rh) return;");
                sb.AppendLine("      var ox = parseFloat(overlay.getAttribute('x'));");   // PaddingLeft
                sb.AppendLine("      var ow = parseFloat(overlay.getAttribute('width'));"); // plotWidth
                sb.AppendLine($"      var xAxisG = svg.getElementById('{Escape(svgId)}-rs-xg');");
                sb.AppendLine($"      var cats = {catsSb};");
                sb.AppendLine($"      var seriesData = {seriesSb};");
                sb.AppendLine($"      var yMin  = {F(yMinRs)};");
                sb.AppendLine($"      var yMax  = {F(yMaxRs)};");
                sb.AppendLine($"      var plotH = {plotHeight};");
                sb.AppendLine($"      var padT  = {PaddingTop};");
                sb.AppendLine($"      var tickY = {tickYLit};");
                sb.AppendLine($"      var textColor = '{textColorJs}';");
                sb.AppendLine($"      var gridColor = '{gridColorJs}';");
                // Initial brush position driven by InitialStart / InitialEnd
                sb.AppendLine($"      var selL = {F(initialStart)} * ow;");
                sb.AppendLine($"      var selW = {F(initialEnd - initialStart)} * ow;");
                sb.AppendLine("      function clamp(v, lo, hi) { return Math.max(lo, Math.min(hi, v)); }");
                sb.AppendLine("      // Format a category string as a readable date label.");
                sb.AppendLine("      function tfFmtCat(s, visCount) {");
                sb.AppendLine("        var d = new Date(s + 'T00:00:00');");
                sb.AppendLine("        if (isNaN(d.getTime())) return s;");
                sb.AppendLine("        var mo = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];");
                sb.AppendLine("        return visCount > 60 ? mo[d.getMonth()] + ' ' + d.getFullYear() : mo[d.getMonth()] + ' ' + d.getDate();");
                sb.AppendLine("      }");
                sb.AppendLine("      // Rebuild x-axis labels for the currently visible slice.");
                sb.AppendLine("      function tfRsUpdateXAxis() {");
                sb.AppendLine("        if (!xAxisG || !cats.length) return;");
                sb.AppendLine("        var n  = cats.length;");
                sb.AppendLine("        var si = Math.round(selL / ow * n);");
                sb.AppendLine("        var ei = Math.min(n, Math.round((selL + selW) / ow * n));");
                sb.AppendLine("        var cnt = Math.max(1, ei - si);");
                sb.AppendLine("        var maxTicks = 7, stride = Math.max(1, Math.ceil(cnt / maxTicks));");
                sb.AppendLine("        var step = ow / cnt;");
                sb.AppendLine("        var ns = 'http://www.w3.org/2000/svg';");
                sb.AppendLine("        while (xAxisG.firstChild) xAxisG.removeChild(xAxisG.firstChild);");
                sb.AppendLine("        for (var i = si; i < ei; i += stride) {");
                sb.AppendLine("          var x = ox + (i - si + 0.5) * step;");
                sb.AppendLine("          var gl = document.createElementNS(ns, 'line');");
                sb.AppendLine("          gl.setAttribute('class','grid-line'); gl.setAttribute('stroke', gridColor);");
                sb.AppendLine("          gl.setAttribute('x1',x.toFixed(1)); gl.setAttribute('y1',padT);");
                sb.AppendLine("          gl.setAttribute('x2',x.toFixed(1)); gl.setAttribute('y2',tickY);");
                sb.AppendLine("          xAxisG.appendChild(gl);");
                sb.AppendLine("          var t = document.createElementNS(ns, 'text');");
                sb.AppendLine("          t.setAttribute('class','axis-label'); t.setAttribute('x',x.toFixed(1));");
                sb.AppendLine("          t.setAttribute('y',(tickY+16).toFixed(1)); t.setAttribute('text-anchor','middle');");
                sb.AppendLine("          t.setAttribute('fill', textColor);");
                sb.AppendLine("          t.textContent = tfFmtCat(cats[i], cnt);");
                sb.AppendLine("          xAxisG.appendChild(t);");
                sb.AppendLine("        }");
                sb.AppendLine("      }");
                sb.AppendLine("      // Redraw each line series path for the current visible window.");
                sb.AppendLine("      // This avoids SVG transform + clip-path interaction (which is unreliable");
                sb.AppendLine("      // cross-browser) by regenerating the 'd' attribute directly.");
                sb.AppendLine("      function tfRsRedrawPaths() {");
                sb.AppendLine("        var n = cats.length || (seriesData.length ? seriesData[0].data.length : 0);");
                sb.AppendLine("        if (n === 0 || selW <= 0) return;");
                sb.AppendLine("        var startI   = Math.round(selL / ow * n);");
                sb.AppendLine("        var endI     = Math.min(n, Math.round((selL + selW) / ow * n));");
                sb.AppendLine("        if (endI <= startI) endI = startI + 1;");
                sb.AppendLine("        var cnt      = endI - startI;");
                sb.AppendLine("        var step     = ow / cnt;");
                sb.AppendLine("        var origStep = ow / n;");
                sb.AppendLine("        var yRange   = yMax - yMin;");
                sb.AppendLine("        for (var i = 0; i < seriesData.length; i++) {");
                sb.AppendLine("          var s  = seriesData[i];");
                sb.AppendLine("          var el = svg.getElementById(s.id);");
                sb.AppendLine("          if (!el) continue;");
                sb.AppendLine("          var d = '', prevNull = true;");
                sb.AppendLine("          for (var j = startI; j < endI; j++) {");
                sb.AppendLine("            if (s.data[j] == null) { prevNull = true; continue; }");
                sb.AppendLine("            var ptX = ox + (j - startI + 0.5) * step;");
                sb.AppendLine("            var cy  = yRange > 0 ? (s.data[j] - yMin) / yRange : 0.5;");
                sb.AppendLine("            var y   = padT + plotH - cy * plotH;");
                sb.AppendLine("            d += (prevNull ? 'M ' : ' L ') + ptX.toFixed(1) + ' ' + y.toFixed(1);");
                sb.AppendLine("            prevNull = false;");
                sb.AppendLine("          }");
                sb.AppendLine("          el.setAttribute('d', d || 'M 0 0');");
                sb.AppendLine("          // Reposition tooltip hit-areas to match the redrawn path.");
                sb.AppendLine("          var dps = svg.querySelectorAll('g.data-point[data-rspid=\"' + s.id + '\"]');");
                sb.AppendLine("          for (var k = 0; k < dps.length; k++) {");
                sb.AppendLine("            var dp  = dps[k];");
                sb.AppendLine("            var di  = parseInt(dp.getAttribute('data-di'), 10);");
                sb.AppendLine("            if (di >= startI && di < endI) {");
                sb.AppendLine("              var newX  = ox + (di - startI + 0.5) * step;");
                sb.AppendLine("              var origX = ox + (di + 0.5) * origStep;");
                sb.AppendLine("              dp.setAttribute('transform', 'translate(' + (newX - origX).toFixed(2) + ' 0)');");
                sb.AppendLine("              dp.style.display = '';");
                sb.AppendLine("            } else {");
                sb.AppendLine("              dp.style.display = 'none';");
                sb.AppendLine("            }");
                sb.AppendLine("          }");
                sb.AppendLine("        }");
                sb.AppendLine("      }");
                sb.AppendLine("      function updateBrush() {");
                sb.AppendLine("        selL = clamp(selL, 0, ow - 8);");
                sb.AppendLine("        selW = clamp(selW, 8, ow - selL);");
                sb.AppendLine("        overlay.setAttribute('x', (ox + selL).toFixed(2));");
                sb.AppendLine("        overlay.setAttribute('width', selW.toFixed(2));");
                sb.AppendLine("        lh.setAttribute('x', (ox + selL - 4).toFixed(2));");
                sb.AppendLine("        rh.setAttribute('x', (ox + selL + selW - 4).toFixed(2));");
                sb.AppendLine("        tfRsRedrawPaths();");
                sb.AppendLine("        tfRsUpdateXAxis();");
                sb.AppendLine("        svg.dispatchEvent(new CustomEvent('tf:rangechange', { bubbles: true, detail: {");
                sb.AppendLine("          start: selL / ow, end: (selL + selW) / ow, svgId: svg.id");
                sb.AppendLine("        }}));");
                sb.AppendLine("      }");
                sb.AppendLine("      var drag = null, dragX0 = 0, dragL0 = 0, dragW0 = 0;");
                sb.AppendLine("      function onDown(what, clientX) {");
                sb.AppendLine("        drag = what; dragX0 = clientX; dragL0 = selL; dragW0 = selW;");
                sb.AppendLine("      }");
                sb.AppendLine("      function onMove(clientX) {");
                sb.AppendLine("        if (!drag) return;");
                sb.AppendLine("        var svgRect = svg.getBoundingClientRect();");
                sb.AppendLine("        var scaleX = (svg.viewBox.baseVal.width || svgRect.width) / Math.max(1, svgRect.width);");
                sb.AppendLine("        var dx = (clientX - dragX0) * scaleX;");
                sb.AppendLine("        if (drag === 'move') { selL = dragL0 + dx; }");
                sb.AppendLine("        else if (drag === 'l') { selL = dragL0 + dx; selW = dragW0 - dx; }");
                sb.AppendLine("        else if (drag === 'r') { selW = dragW0 + dx; }");
                sb.AppendLine("        updateBrush();");
                sb.AppendLine("      }");
                sb.AppendLine("      overlay.addEventListener('mousedown', function(e) { onDown('move', e.clientX); e.preventDefault(); });");
                sb.AppendLine("      lh.addEventListener('mousedown',      function(e) { onDown('l', e.clientX);    e.preventDefault(); e.stopPropagation(); });");
                sb.AppendLine("      rh.addEventListener('mousedown',      function(e) { onDown('r', e.clientX);    e.preventDefault(); e.stopPropagation(); });");
                sb.AppendLine("      document.addEventListener('mousemove', function(e) { onMove(e.clientX); });");
                sb.AppendLine("      document.addEventListener('mouseup',   function()  { drag = null; });");
                sb.AppendLine("      overlay.addEventListener('touchstart', function(e) { onDown('move', e.touches[0].clientX); e.preventDefault(); }, {passive:false});");
                sb.AppendLine("      lh.addEventListener('touchstart',      function(e) { onDown('l', e.touches[0].clientX);    e.preventDefault(); e.stopPropagation(); }, {passive:false});");
                sb.AppendLine("      rh.addEventListener('touchstart',      function(e) { onDown('r', e.touches[0].clientX);    e.preventDefault(); e.stopPropagation(); }, {passive:false});");
                sb.AppendLine("      document.addEventListener('touchmove',  function(e) { if (drag) onMove(e.touches[0].clientX); }, {passive:false});");
                sb.AppendLine("      document.addEventListener('touchend',   function()  { drag = null; });");
                sb.AppendLine("      // Apply initial selection on load.");
                sb.AppendLine("      updateBrush();");
                sb.AppendLine("    })();");
            }

            // ── Synchronized tooltips ─────────────────────────────────────────────────────
            if (!string.IsNullOrEmpty(options.SyncGroup))
            {
                string grp = Escape(options.SyncGroup!);
                sb.AppendLine("");
                sb.AppendLine("    // ── Synchronised tooltips ──────────────────────────────────────────────────");
                sb.AppendLine("    (function() {");
                sb.AppendLine("      var svg = document.currentScript ? document.currentScript.closest('svg') : null;");
                sb.AppendLine("      if (!svg) { var sc = document.querySelectorAll('svg script'); svg = sc.length ? sc[sc.length-1].closest('svg') : null; }");
                sb.AppendLine("      if (!svg) return;");
                sb.AppendLine($"      var groupId = '{grp}';");
                sb.AppendLine("      window._tfSync = window._tfSync || {};");
                sb.AppendLine("      window._tfSync[groupId] = window._tfSync[groupId] || [];");
                sb.AppendLine("      var entry = { svg: svg, showFn: null, hideFn: null, syncing: false };");
                sb.AppendLine("      window._tfSync[groupId].push(entry);");
                // showFn sets inline opacity on tooltip children — CSS :hover can't be triggered synthetically.
                sb.AppendLine("      entry.showFn = function(di) {");
                sb.AppendLine("        var dp = svg.querySelector('.data-point[data-di=\"' + di + '\"]');");
                sb.AppendLine("        if (!dp) return;");
                sb.AppendLine("        dp.querySelectorAll('.tooltip-bg,.tooltip-text,.tooltip-bullet,.crosshair-x').forEach(function(el) {");
                sb.AppendLine("          el.style.opacity = '1';");
                sb.AppendLine("        });");
                sb.AppendLine("      };");
                sb.AppendLine("      entry.hideFn = function() {");
                sb.AppendLine("        svg.querySelectorAll('.tooltip-bg,.tooltip-text,.tooltip-bullet,.crosshair-x').forEach(function(el) {");
                sb.AppendLine("          el.style.opacity = '';");
                sb.AppendLine("        });");
                sb.AppendLine("      };");
                sb.AppendLine("      svg.querySelectorAll('.data-point[data-di]').forEach(function(el) {");
                sb.AppendLine("        el.addEventListener('mouseenter', function() {");
                sb.AppendLine("          if (entry.syncing) return;");
                sb.AppendLine("          var di = el.getAttribute('data-di');");
                sb.AppendLine("          (window._tfSync[groupId] || []).forEach(function(peer) {");
                sb.AppendLine("            if (peer.svg !== svg && peer.showFn) {");
                sb.AppendLine("              peer.syncing = true;");
                sb.AppendLine("              peer.showFn(di);");
                sb.AppendLine("              peer.syncing = false;");
                sb.AppendLine("            }");
                sb.AppendLine("          });");
                sb.AppendLine("        });");
                sb.AppendLine("        el.addEventListener('mouseleave', function() {");
                sb.AppendLine("          if (entry.syncing) return;");
                sb.AppendLine("          (window._tfSync[groupId] || []).forEach(function(peer) {");
                sb.AppendLine("            if (peer.svg !== svg && peer.hideFn) {");
                sb.AppendLine("              peer.syncing = true;");
                sb.AppendLine("              peer.hideFn();");
                sb.AppendLine("              peer.syncing = false;");
                sb.AppendLine("            }");
                sb.AppendLine("          });");
                sb.AppendLine("        });");
                sb.AppendLine("      });");
                sb.AppendLine("    })();");
            }

            // ── Drill-down: click data-points of drillable series to show child chart ────
            if (options.Series.Exists(s => s.DrilldownChart != null || s.DrilldownCharts.Count > 0))
            {
                sb.AppendLine("");
                sb.AppendLine("    // ── Drill-down ─────────────────────────────────────────────────────────────");
                sb.AppendLine("    (function() {");
                sb.AppendLine("      var svg = document.currentScript ? document.currentScript.closest('svg') : null;");
                sb.AppendLine("      if (!svg) { var sc = document.querySelectorAll('svg script'); svg = sc.length ? sc[sc.length-1].closest('svg') : null; }");
                sb.AppendLine("      if (!svg) return;");
                // ddMap keys:
                //   "SeriesName"     → whole-series group id (DrilldownChart fallback)
                //   "SeriesName:di"  → per-point group id   (DrilldownCharts[di])
                // Click handler checks the specific-point key first, then falls back to whole-series.
                sb.AppendLine("      var ddMap = {");
                for (int si = 0; si < options.Series.Count; si++)
                {
                    var series = options.Series[si];
                    if (series.DrilldownChart != null)
                        sb.AppendLine($"        '{Escape(series.Name)}': '{svgId}-dd-{si}',");
                    foreach (var kvp in series.DrilldownCharts)
                        sb.AppendLine($"        '{Escape(series.Name)}:{kvp.Key}': '{svgId}-dd-{si}-{kvp.Key}',");
                }
                sb.AppendLine("      };");
                sb.AppendLine("      svg.querySelectorAll('.data-point').forEach(function(dp) {");
                sb.AppendLine("        var name = dp.getAttribute('data-name') || '';");
                sb.AppendLine("        var di   = dp.getAttribute('data-di')   || '';");
                sb.AppendLine("        var gId  = ddMap[name + ':' + di] || ddMap[name];");
                sb.AppendLine("        if (!gId) return;");
                sb.AppendLine("        dp.style.cursor = 'pointer';");
                sb.AppendLine("        dp.addEventListener('click', function() {");
                sb.AppendLine("          var ddG = svg.getElementById(gId);");
                sb.AppendLine("          if (ddG) ddG.style.display = '';");
                sb.AppendLine("        });");
                sb.AppendLine("      });");
                sb.AppendLine("      svg.querySelectorAll('.tf-dd-back').forEach(function(btn) {");
                sb.AppendLine("        function goBack() {");
                sb.AppendLine("          var p = btn.parentElement;");
                sb.AppendLine("          while (p && !p.classList.contains('tf-dd')) p = p.parentElement;");
                sb.AppendLine("          if (p) p.style.display = 'none';");
                sb.AppendLine("        }");
                sb.AppendLine("        btn.addEventListener('click', goBack);");
                sb.AppendLine("        btn.addEventListener('keydown', function(e) {");
                sb.AppendLine("          if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); goBack(); }");
                sb.AppendLine("        });");
                sb.AppendLine("      });");
                sb.AppendLine("    })();");
            }

            sb.AppendLine("  ]]>");
            sb.AppendLine("  </script>");
        }

        // ------------------------------------------------------------------ range selector strip

        private static void AppendRangeSelectorStrip(StringBuilder sb, ChartOptions options, string svgId,
            int svgWidth, int plotWidth, int plotHeight, int stripY, int stripH)
        {
            var rs  = options.RangeSelector;
            string tc = Escape(options.Theme.TextColor);
            string gl = Escape(options.Theme.GridLineColor);

            // Background
            sb.AppendLine($"  <clipPath id=\"{svgId}-rs-cp\"><rect id=\"{svgId}-rs-cr\" x=\"{PaddingLeft}\" y=\"{PaddingTop}\" width=\"{plotWidth}\" height=\"{plotHeight}\"/></clipPath>");
            sb.AppendLine($"  <rect id=\"{svgId}-rs\" class=\"tf-rs-bg\" x=\"{PaddingLeft}\" y=\"{F(stripY)}\" width=\"{plotWidth}\" height=\"{stripH}\" fill=\"{gl}\" fill-opacity=\"0.10\" rx=\"2\" stroke=\"{tc}\" stroke-opacity=\"0.20\" stroke-width=\"1\"/>");

            // Mini overview paths for numeric series
            var numericTypes = new System.Collections.Generic.HashSet<ChartType>
                { ChartType.Line, ChartType.Spline, ChartType.Area, ChartType.Column };
            var visSeries = options.Series.FindAll(s => s.Visible && numericTypes.Contains(s.Type) && s.Data.Count > 0);
            int catCount  = options.XAxis.Categories?.Count > 0
                            ? options.XAxis.Categories!.Count
                            : (visSeries.Count > 0 ? visSeries[0].Data.Count : 0);

            if (visSeries.Count > 0 && catCount > 0)
            {
                double dataMin = double.MaxValue, dataMax = double.MinValue;
                foreach (var s in visSeries)
                    foreach (var v in s.Data)
                        if (v.HasValue) { if (v.Value < dataMin) dataMin = v.Value; if (v.Value > dataMax) dataMax = v.Value; }
                double rng = Math.Abs(dataMax - dataMin) > 1e-9 ? dataMax - dataMin : 1;
                double step = (double)plotWidth / catCount;
                foreach (var series in visSeries)
                {
                    int si = options.Series.IndexOf(series);
                    string c = series.Color ?? options.Theme.Colors[si % options.Theme.Colors.Length];
                    var pts = new System.Text.StringBuilder();
                    bool first = true;
                    for (int i = 0; i < series.Data.Count && i < catCount; i++)
                    {
                        if (!series.Data[i].HasValue) continue;
                        double cx = PaddingLeft + (i + 0.5) * step;
                        double cy = stripY + stripH - 3 - (series.Data[i]!.Value - dataMin) / rng * (stripH - 6);
                        pts.Append(first ? $"M {F(cx)} {F(cy)}" : $" L {F(cx)} {F(cy)}");
                        first = false;
                    }
                    if (!first)
                        sb.AppendLine($"  <path class=\"tf-rs-path\" d=\"{pts}\" fill=\"none\" stroke=\"{Escape(c)}\" stroke-width=\"1.5\" stroke-opacity=\"0.55\" pointer-events=\"none\"/>");
                }
            }

            // Brush overlay (initially full width)
            sb.AppendLine($"  <rect id=\"{svgId}-rs-overlay\" class=\"tf-rs-overlay\" x=\"{PaddingLeft}\" y=\"{F(stripY)}\" width=\"{plotWidth}\" height=\"{stripH}\" fill=\"{Escape(rs.FillColor)}\" rx=\"1\" style=\"cursor:grab\"/>");
            // Left handle
            sb.AppendLine($"  <rect id=\"{svgId}-rs-lh\" class=\"tf-rs-handle\" x=\"{PaddingLeft - 4}\" y=\"{F(stripY)}\" width=\"8\" height=\"{stripH}\" rx=\"3\" fill=\"{Escape(rs.HandleColor)}\" style=\"cursor:ew-resize\"/>");
            // Right handle
            sb.AppendLine($"  <rect id=\"{svgId}-rs-rh\" class=\"tf-rs-handle\" x=\"{PaddingLeft + plotWidth - 4}\" y=\"{F(stripY)}\" width=\"8\" height=\"{stripH}\" rx=\"3\" fill=\"{Escape(rs.HandleColor)}\" style=\"cursor:ew-resize\"/>");
        }

        // ------------------------------------------------------------------ drilldown embed

        private static void AppendDrilldownCharts(StringBuilder sb, ChartOptions options, string svgId,
            int svgWidth, int svgHeight)
        {
            for (int si = 0; si < options.Series.Count; si++)
            {
                var series = options.Series[si];

                // Whole-series fallback: one child for all points → group id = {svgId}-dd-{si}
                if (series.DrilldownChart != null)
                    EmbedDrilldownChild(sb, series.DrilldownChart, $"{svgId}-dd-{si}", svgWidth, svgHeight);

                // Per-point children → group id = {svgId}-dd-{si}-{di}
                foreach (var kvp in series.DrilldownCharts)
                    EmbedDrilldownChild(sb, kvp.Value, $"{svgId}-dd-{si}-{kvp.Key}", svgWidth, svgHeight);
            }
        }

        private static void EmbedDrilldownChild(StringBuilder sb, ChartOptions childOpts,
            string groupId, int svgWidth, int svgHeight)
        {
            if (!childOpts.Width.HasValue) childOpts.Width = svgWidth;
            if (childOpts.Height <= 0)     childOpts.Height = svgHeight;

            // The nested render reassigns the [ThreadStatic] PaddingLeft; restore the parent's
            // value afterwards so the back button and any later parent drawing stay aligned.
            int savedPaddingLeft = PaddingLeft;
            string childSvg = new SvgRenderer().Render(childOpts);
            PaddingLeft = savedPaddingLeft;

            int svgOpen  = childSvg.IndexOf("<svg", StringComparison.Ordinal);
            int svgGT    = svgOpen >= 0 ? childSvg.IndexOf('>', svgOpen) : -1;
            int svgClose = childSvg.LastIndexOf("</svg>", StringComparison.Ordinal);
            if (svgOpen < 0 || svgGT < 0 || svgClose < 0) return;
            string childInner = childSvg.Substring(svgGT + 1, svgClose - svgGT - 1);

            int btnW = 80, btnH = 26;
            sb.AppendLine($"  <g id=\"{groupId}\" class=\"tf-dd\" style=\"display:none\">");
            // Child content first — its own background rect covers the parent chart
            sb.Append(childInner);
            // Back button rendered last so it sits above all child chart content
            sb.AppendLine($"    <g class=\"tf-dd-back\" tabindex=\"0\" role=\"button\" style=\"cursor:pointer\" aria-label=\"Back to main chart\">");
            sb.AppendLine($"      <rect x=\"{PaddingLeft}\" y=\"8\" width=\"{btnW}\" height=\"{btnH}\" rx=\"4\" fill=\"rgba(128,128,128,0.25)\" stroke=\"rgba(128,128,128,0.55)\" stroke-width=\"1\"/>");
            sb.AppendLine($"      <text x=\"{F(PaddingLeft + btnW / 2.0)}\" y=\"25\" text-anchor=\"middle\" font-size=\"12\" font-weight=\"600\" fill=\"{Escape(childOpts.Theme.TextColor)}\">&#x25c4; Back</text>");
            sb.AppendLine($"    </g>");
            sb.AppendLine($"  </g>");
        }

        private static void AppendExportMenu(StringBuilder sb, ChartOptions options, int svgWidth)
        {
            var formats  = options.ExportMenuFormats;
            if (formats == null || formats.Count == 0) return;

            int btnW  = 96;
            int btnH  = 24;
            int itemH = 26;
            int btnX  = svgWidth - PaddingRight - btnW;
            int btnY  = 8;
            string textFill  = Escape(options.Theme.TextColor);
            string _themeBg   = options.Theme.BackgroundColor;
            bool   _bgIsLight = _themeBg == null || _themeBg == ChartColor.None
                                || _themeBg.StartsWith("#f", System.StringComparison.OrdinalIgnoreCase);
            string ddBg       = _bgIsLight
                                ? ChartColor.WithOpacity(ChartColor.White, 0.97)
                                : ChartColor.WithOpacity(_themeBg ?? ChartColor.White, 0.97);
            string itemTxt    = options.Theme.TextColor;

            // Trigger button
            sb.AppendLine($"  <g class=\"tf-export-btn tf-export-trigger\" tabindex=\"0\" role=\"button\" aria-haspopup=\"true\" aria-expanded=\"false\" style=\"cursor:pointer;-webkit-user-select:none;user-select:none\">");
            sb.AppendLine($"    <rect x=\"{btnX}\" y=\"{btnY}\" width=\"{btnW}\" height=\"{btnH}\" rx=\"4\" class=\"tf-export-bg\" fill=\"rgba(128,128,128,0.12)\" stroke=\"rgba(128,128,128,0.30)\" stroke-width=\"1\"/>");
            sb.AppendLine($"    <text x=\"{F(btnX + btnW / 2.0)}\" y=\"{btnY + 16}\" text-anchor=\"middle\" font-size=\"11\" fill=\"{textFill}\" style=\"pointer-events:none\">{Escape("\u2b07 Export \u25be")}</text>");
            sb.AppendLine($"  </g>");

            // Dropdown panel (hidden by default; JS toggles display)
            int ddY = btnY + btnH + 2;
            int ddH = formats.Count * itemH;
            sb.AppendLine($"  <g class=\"tf-export-dropdown\" style=\"display:none\">");
            sb.AppendLine($"    <rect x=\"{btnX - 1}\" y=\"{ddY - 1}\" width=\"{btnW + 2}\" height=\"{ddH + 2}\" rx=\"4\" fill=\"{Escape(ddBg)}\" stroke=\"rgba(128,128,128,0.35)\" stroke-width=\"1\"/>");
            for (int i = 0; i < formats.Count; i++)
            {
                string fmt   = formats[i];
                int    itemY = ddY + i * itemH;
                sb.AppendLine($"    <g class=\"tf-export-item\" data-fmt=\"{Escape(fmt)}\">");
                sb.AppendLine($"      <rect class=\"tf-export-item-bg\" x=\"{btnX}\" y=\"{itemY}\" width=\"{btnW}\" height=\"{itemH}\" fill=\"transparent\"/>");
                sb.AppendLine($"      <text class=\"tf-export-item-lbl\" x=\"{F(btnX + btnW / 2.0)}\" y=\"{itemY + 17}\" text-anchor=\"middle\" font-size=\"11\" fill=\"{Escape(itemTxt)}\" style=\"pointer-events:none\">{Escape(fmt)}</text>");
                sb.AppendLine($"    </g>");
            }
            sb.AppendLine($"  </g>");
        }

        // ------------------------------------------------------------------ export button

        private static void AppendExportButton(StringBuilder sb, ChartOptions options, int svgWidth)
        {
            string label  = options.ExportButtonLabel ?? "\u2b07 SVG";
            int btnW  = Math.Max(70, label.Length * 8 + 20);
            int btnH  = 22;
            int btnX  = svgWidth - PaddingRight - btnW;
            int btnY  = 8;

            string textFill = Escape(options.Theme.TextColor);

            sb.AppendLine($"  <g class=\"tf-export-btn\" tabindex=\"0\" role=\"button\" aria-label=\"Download chart as SVG\" style=\"cursor:pointer;-webkit-user-select:none;user-select:none\">");
            sb.AppendLine($"    <rect x=\"{btnX}\" y=\"{btnY}\" width=\"{btnW}\" height=\"{btnH}\" rx=\"4\" class=\"tf-export-bg\" fill=\"rgba(128,128,128,0.12)\" stroke=\"rgba(128,128,128,0.30)\" stroke-width=\"1\"/>");
            sb.AppendLine($"    <text x=\"{F(btnX + btnW / 2.0)}\" y=\"{btnY + 15}\" text-anchor=\"middle\" font-size=\"11\" fill=\"{textFill}\" style=\"pointer-events:none\">{Escape(label)}</text>");
            sb.AppendLine($"  </g>");
        }

        // ------------------------------------------------------------------ Dumbbell

    }
}
