/* Analyze view — full profiling, validation, insights and chart recommendations. */
import {
  $, $$, content, state, esc, fmt, loading, errorBox,
  api, analyzeRequest, requireDataset, insightRow, toast, chartStyleQuery,
} from "./core.js";
import { icon } from "./icons.js";

export function renderAnalyzeView() {
  if (!requireDataset()) return;
  content.innerHTML = `
    <div class="card">
      <div class="section-head">
        <div class="titles"><h2>Full Analysis</h2><p>Profiling, validation, ranked insights and chart recommendations.</p></div>
        <div style="display:flex;flex-direction:column;gap:10px;flex:1 1 420px">
          <div class="row" style="justify-content:flex-end">
            <button class="switch" id="incSvg" type="button" role="switch" aria-checked="true">
              <span class="switch-track"><span class="switch-knob"></span></span><span>Preview charts</span>
            </button>
          </div>
          <div class="row">
            <label class="field">Max insights<input type="number" id="maxIns" value="12" min="1" max="100" style="width:90px"></label>
            <div class="spacer"></div>
            <button class="btn" id="runAnalyze">${icon("play")} Run analysis</button>
            <button class="btn blue" id="exportAnalysis">${icon("download")} Export analysis</button>
          </div>
        </div>
      </div>
    </div>
    <div id="anOut"></div>`;

  // Switch buttons flip their checked state on click, then re-run so the change is reflected at once.
  $$(".switch", content).forEach(b =>
    b.onclick = () => {
      b.setAttribute("aria-checked", b.getAttribute("aria-checked") === "true" ? "false" : "true");
      $("#runAnalyze").click();
    });

  $("#runAnalyze").onclick = async () => {
    const out = $("#anOut");
    out.innerHTML = loading("Analyzing dataset…");
    // "Already summarized" is a dataset-level flag (set on the Data Source page).
    const pre = state.dataset?.preAggregated === true;
    try {
      const r = await api.json("/api/analytics/analyze", {
        method: "POST",
        body: analyzeRequest({ maxInsights: +$("#maxIns").value, maxRecommendations: pre ? 24 : 12 }),
        query: { includeSvg: $("#incSvg").getAttribute("aria-checked") === "true", ...chartStyleQuery() },
      });
      state.columns = r.columns || [];
      out.innerHTML = renderAnalysis(r);
    } catch (e) { out.innerHTML = errorBox(e); }
  };
  $("#exportAnalysis").onclick = async () => {
    const btn = $("#exportAnalysis");
    btn.disabled = true;
    const pre = state.dataset?.preAggregated === true;
    try {
      const html = await api.text("/api/analytics/analyze/html", {
        method: "POST", body: analyzeRequest({ maxInsights: +$("#maxIns").value, maxRecommendations: pre ? 24 : 12 }),
        query: { ...chartStyleQuery() },
      });
      const blob = new Blob([html], { type: "text/html" });
      const a = document.createElement("a");
      a.href = URL.createObjectURL(blob);
      a.download = (state.dataset.name || "analysis").replace(/\s+/g, "-").toLowerCase() + "-analysis.html";
      a.click();
      URL.revokeObjectURL(a.href);
      toast("Analysis exported.");
    } catch (e) { toast(e.message, true); }
    finally { btn.disabled = false; }
  };
  $("#runAnalyze").click();
}

function renderAnalysis(r) {
  const s = r.summary || {}, v = r.validation || {};
  const vBadge = v.hasErrors ? `<span class="badge err">Errors</span>`
    : v.isClean ? `<span class="badge ok">Clean</span>` : `<span class="badge warn">Warnings</span>`;

  const findings = (s.keyFindings || []).map(f => `<li>${esc(f)}</li>`).join("");
  const insights = (r.insights || []).map(insightRow).join("") || `<p class="sub">No notable insights.</p>`;

  const recs = (r.recommendations || []).map(rc => `
    <div class="chart-card">
      ${rc.svg ? `<figure>${rc.svg}</figure>` : ""}
      <div class="cap"><span class="tag">${esc(rc.title)}</span> · ${esc(rc.chartType)} · suitability ${rc.suitabilityScore}<br>${esc(rc.reason)}</div>
    </div>`).join("") || `<p class="sub">No recommendations.</p>`;

  const cols = (r.columns || []).map(c => `
    <tr>
      <td><strong>${esc(c.name)}</strong></td>
      <td><span class="badge muted">${esc(c.type)}</span></td>
      <td><span class="badge info">${esc(c.role)}</span></td>
      <td class="num">${c.distinctCount}</td>
      <td class="num">${(c.completeness * 100).toFixed(0)}%</td>
      <td class="num">${c.min != null ? fmt(c.min) : "—"}</td>
      <td class="num">${c.max != null ? fmt(c.max) : "—"}</td>
      <td class="num">${c.mean != null ? fmt(c.mean) : "—"}</td>
      <td class="num">${c.sum != null ? fmt(c.sum) : "—"}</td>
    </tr>`).join("");

  return `
    <div class="card">
      <div class="row" style="justify-content:space-between">
        <h2 style="margin:0">${esc(s.headline || "Summary")}</h2>${vBadge}
      </div>
      <div class="stat-row" style="margin-top:12px">
        <span class="stat-pill"><strong>${s.rowCount}</strong> rows</span>
        <span class="stat-pill"><strong>${s.columnCount}</strong> columns</span>
        <span class="stat-pill"><strong>${s.insightCount}</strong> insights</span>
        <span class="stat-pill"><strong>${s.recommendationCount}</strong> recommendations</span>
      </div>
      ${findings ? `<h3 style="margin-top:18px">Key findings</h3><ul>${findings}</ul>` : ""}
    </div>
    <div class="card"><h3>Insights</h3>${insights}</div>
    <div class="card"><h3>Chart recommendations</h3><div class="grid cols-2">${recs}</div></div>
    <div class="card"><h3>Column profiles</h3>
      <div class="table-wrap"><table class="data">
        <thead><tr><th>Column</th><th>Type</th><th>Role</th><th class="num">Distinct</th><th class="num">Complete</th>
        <th class="num">Min</th><th class="num">Max</th><th class="num">Mean</th><th class="num">Sum</th></tr></thead>
        <tbody>${cols}</tbody>
      </table></div>
    </div>`;
}
