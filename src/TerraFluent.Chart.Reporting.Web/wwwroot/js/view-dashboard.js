/* Dashboard view — auto-generated KPIs, trend/comparison/distribution charts. */
import {
  $, content, state, esc, toast, loading, errorBox,
  api, analyzeRequest, requireDataset, ensureColumns, chartCard, insightBlock, chartStyleQuery,
} from "./core.js";
import { icon } from "./icons.js";

export async function renderDashboardView() {
  if (!requireDataset()) return;
  content.innerHTML = loading("Preparing dashboard…");

  // The dataset caps how many KPIs are possible (one per numeric measure). Bind the
  // KPI selector to that count and default to 3 (or fewer when the dataset has fewer).
  await ensureColumns();
  const kpiMax = Math.max(1, Math.min(12, state.measureCount || 1));
  const defaultKpi = Math.min(3, kpiMax);
  const kpiOptions = Array.from({ length: kpiMax }, (_, i) => i + 1)
    .map(n => `<option value="${n}"${n === defaultKpi ? " selected" : ""}>${n}</option>`).join("");

  content.innerHTML = `
    <div class="card">
      <div class="section-head">
        <div class="titles"><h2>Smart Dashboard</h2><p>KPIs, trend, comparison and distribution charts generated automatically from <strong>${esc(state.dataset.name)}</strong>.</p></div>
        <div class="row">
          <label class="field">KPIs<select id="maxKpis" style="width:80px">${kpiOptions}</select></label>
          <button class="btn" id="genDash" style="align-self:flex-end">${icon("grid")} Generate dashboard</button>
          <button class="btn blue" id="exportDash" style="align-self:flex-end">${icon("download")} Export dashboard</button>
        </div>
      </div>
    </div>
    <div id="dashOut"></div>`;

  $("#genDash").onclick = async () => {
    const out = $("#dashOut");
    out.innerHTML = loading("Building dashboard…");
    try {
      const d = await api.json("/api/analytics/dashboard", {
        method: "POST", body: analyzeRequest({ maxRecommendations: 24 }), query: { maxKpis: $("#maxKpis").value, ...chartStyleQuery() },
      });
      out.innerHTML = renderDashboard(d);
    } catch (e) { out.innerHTML = errorBox(e); }
  };
  $("#exportDash").onclick = async () => {
    const btn = $("#exportDash");
    btn.disabled = true;
    try {
      const html = await api.text("/api/analytics/dashboard/html", {
        method: "POST", body: analyzeRequest({ maxRecommendations: 24 }), query: { maxKpis: $("#maxKpis").value, ...chartStyleQuery() },
      });
      const blob = new Blob([html], { type: "text/html" });
      const a = document.createElement("a");
      a.href = URL.createObjectURL(blob);
      a.download = (state.dataset.name || "dashboard").replace(/\s+/g, "-").toLowerCase() + "-dashboard.html";
      a.click();
      URL.revokeObjectURL(a.href);
      toast("Dashboard exported.");
    } catch (e) { toast(e.message, true); }
    finally { btn.disabled = false; }
  };
  $("#genDash").click();
}

function renderDashboard(d) {
  const s = d.summary || {};
  const kpis = (d.kpis || []).map(k => `
    <div class="kpi">
      <div class="kpi-label">${esc(k.label)}</div>
      <div class="kpi-value">${esc(k.displayValue)}</div>
      <div class="kpi-caption">${esc(k.caption || "")}</div>
    </div>`).join("");

  const chartSection = (title, arr) => (arr && arr.length) ? `
    <div class="card"><h3>${title}</h3><div class="grid cols-2">${
      arr.map(c => chartCard(c.svg, c.title, c.reason, c.suitabilityScore)).join("")
    }</div></div>` : "";

  return `
    <div class="card">
      <h2>${esc(d.title || "Dashboard")}</h2>
      <p class="sub">${esc(s.headline || "")}</p>
      <div class="stat-row">
        <span class="stat-pill"><strong>${s.rowCount}</strong> rows</span>
        <span class="stat-pill"><strong>${s.columnCount}</strong> columns</span>
        <span class="stat-pill"><strong>${s.measureCount}</strong> measures</span>
        <span class="stat-pill"><strong>${s.dimensionCount}</strong> dimensions</span>
        <span class="stat-pill">Quality: <strong>${esc(s.dataQuality)}</strong></span>
      </div>
    </div>
    ${kpis ? `<h3 style="margin:0 0 12px">Executive KPIs</h3><div class="grid cols-4" style="margin-bottom:20px">${kpis}</div>` : ""}
    ${chartSection("Trends", d.trendCharts)}
    ${chartSection("Comparisons", d.comparisonCharts)}
    ${chartSection("Distributions", d.distributionCharts)}
    ${insightBlock("Anomalies", d.anomalies)}
    ${insightBlock("Key insights", d.keyInsights)}`;
}
