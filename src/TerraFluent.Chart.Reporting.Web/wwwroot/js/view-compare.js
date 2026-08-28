/* Compare view — diffs the active dataset against a second one.

   Both datasets are posted together on each request rather than held in app state, so comparison
   stays as stateless as every other screen: nothing is stored, on the server or in the browser. */
import {
  $, content, state, esc, fmt, loading, errorBox,
  api, requireDataset, insightRow, toast, chartStyleQuery, datasetPayload,
} from "./core.js";
import { icon } from "./icons.js";
import { SAMPLES } from "./samples.js";

// The comparison dataset lives for the lifetime of the view only.
let other = null;

export function renderCompareView() {
  if (!requireDataset()) return;

  content.innerHTML = `
    <div class="card">
      <div class="section-head">
        <div class="titles">
          <h2>Compare Datasets</h2>
          <p>Diff the active dataset against another: what changed structurally, how each shared measure moved, and how the category mix shifted.</p>
        </div>
      </div>

      <div class="row" style="align-items:stretch;gap:16px;flex-wrap:wrap">
        <div class="card" style="flex:1 1 280px;margin:0;background:var(--surface-2,#f8fafc)">
          <h3 style="margin-top:0">${icon("database")} Baseline</h3>
          <p class="sub" style="margin:6px 0 0">The &ldquo;before&rdquo;. Provide it below.</p>
          <label class="field" style="margin-top:12px">Name
            <input type="text" id="cmpBaseName" placeholder="e.g. Q1" value="${esc(other?.name || "")}">
          </label>
          <label class="field">Raw data <span class="hint">CSV or JSON array</span>
            <textarea id="cmpBaseData" rows="7" placeholder="Month,Region,Revenue&#10;2024-01,EU,12000">${esc(other?.data || "")}</textarea>
          </label>
          <div class="row" style="gap:8px;flex-wrap:wrap">
            <input type="file" id="cmpFile" accept=".csv,.json,.txt,.xlsx" hidden>
            <button class="btn subtle sm" id="cmpBrowse" type="button">${icon("upload")} Upload file</button>
            <select id="cmpSample" class="sm" style="flex:1 1 140px">
              <option value="">Load a sample…</option>
              ${Object.entries(SAMPLES).map(([k, s]) => `<option value="${esc(k)}">${esc(s.name)}</option>`).join("")}
            </select>
          </div>
        </div>

        <div class="card" style="flex:1 1 280px;margin:0;background:var(--surface-2,#f8fafc)">
          <h3 style="margin-top:0">${icon("check-circle")} Current</h3>
          <p class="sub" style="margin:6px 0 0">The &ldquo;after&rdquo; — your active dataset.</p>
          <div class="stat-row" style="margin-top:12px">
            <span class="stat-pill"><strong>${esc(state.dataset.name)}</strong></span>
          </div>
          <p class="sub" style="margin-top:10px">
            To compare a different pair, load the &ldquo;after&rdquo; dataset on
            <a href="#" data-goto="data">Data Source</a> first.
          </p>
        </div>
      </div>

      <div class="row" style="margin-top:16px">
        <button class="btn" id="runCompare">${icon("compare")} Compare</button>
        <button class="btn blue" id="exportCompare">${icon("download")} Export comparison</button>
      </div>
    </div>
    <div id="cmpOut"></div>`;

  // ── Baseline input ──────────────────────────────────────────────────────
  $("#cmpBrowse").onclick = () => $("#cmpFile").click();
  $("#cmpFile").onchange = async (e) => {
    const file = e.target.files[0];
    if (!file) return;
    const stem = file.name.replace(/\.[^.]+$/, "");
    try {
      if (/\.xlsx$/i.test(file.name)) {
        const res = await api.binary("/api/analytics/convert/xlsx", await file.arrayBuffer(), {
          contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
          query: { datasetName: stem },
        });
        $("#cmpBaseData").value = res.data;
      } else {
        $("#cmpBaseData").value = await file.text();
      }
      $("#cmpBaseName").value = stem;
      toast(`Loaded ${file.name} as the baseline.`);
    } catch (err) { toast(err.message || "Could not read that file.", true); }
  };

  $("#cmpSample").onchange = (e) => {
    const s = SAMPLES[e.target.value];
    if (!s) return;
    $("#cmpBaseData").value = s.data;
    $("#cmpBaseName").value = s.name;
    toast(`Loaded sample: ${s.name}`);
  };

  // ── Run ─────────────────────────────────────────────────────────────────
  const buildRequest = () => {
    const data = $("#cmpBaseData").value.trim();
    if (!data) { toast("Provide a baseline dataset to compare against.", true); return null; }
    other = { name: $("#cmpBaseName").value.trim() || "Baseline", data };
    return {
      baseline: { data, datasetName: other.name, format: "Auto" },
      // The active dataset may be a live connection, in which case only its name travels.
      current: datasetPayload(),
    };
  };

  $("#runCompare").onclick = async () => {
    const body = buildRequest();
    if (!body) return;
    const out = $("#cmpOut");
    out.innerHTML = loading("Comparing datasets…");
    try {
      const r = await api.json("/api/analytics/compare", {
        method: "POST", body, query: { includeSvg: true, ...chartStyleQuery() },
      });
      out.innerHTML = renderComparison(r);
    } catch (e) { out.innerHTML = errorBox(e); }
  };

  $("#exportCompare").onclick = async () => {
    const body = buildRequest();
    if (!body) return;
    const btn = $("#exportCompare");
    btn.disabled = true;
    try {
      const html = await api.text("/api/analytics/compare/html", { method: "POST", body, query: { ...chartStyleQuery() } });
      const blob = new Blob([html], { type: "text/html" });
      const a = document.createElement("a");
      a.href = URL.createObjectURL(blob);
      a.download = `${other.name}-vs-${state.dataset.name || "current"}`.replace(/\s+/g, "-").toLowerCase() + "-comparison.html";
      a.click();
      URL.revokeObjectURL(a.href);
      toast("Comparison exported.");
    } catch (e) { toast(e.message, true); }
    finally { btn.disabled = false; }
  };
}

// ── Rendering ─────────────────────────────────────────────────────────────

export function renderComparison(r) {
  const badge = r.isComparable
    ? `<span class="badge ok">Comparable</span>`
    : `<span class="badge err">Not comparable</span>`;

  const insights = (r.insights || []).map(insightRow).join("")
    || `<p class="sub">No material differences between the two datasets.</p>`;

  const charts = (r.charts || []).map(c => `
    <div class="chart-card">
      ${c.svg ? `<figure>${c.svg}</figure>` : ""}
      <div class="cap"><span class="tag">${esc(c.title)}</span> · ${esc(c.chartType)}<br>${esc(c.reason)}</div>
    </div>`).join("");

  return `
    <div class="card">
      <div class="row" style="justify-content:space-between">
        <h2 style="margin:0">${esc(r.headline || "Comparison")}</h2>${badge}
      </div>
      <div class="stat-row" style="margin-top:12px">
        <span class="stat-pill"><strong>${r.baselineRowCount}</strong> → <strong>${r.currentRowCount}</strong> rows</span>
        <span class="stat-pill"><strong>${(r.sharedMeasures || []).length}</strong> shared measures</span>
        <span class="stat-pill"><strong>${(r.schemaChanges || []).length}</strong> structural changes</span>
      </div>
      <p class="sub" style="margin-top:12px">${esc(r.compatibilityNote)}</p>
    </div>

    <div class="card"><h3>What changed</h3>${insights}</div>
    ${measureTable(r)}
    ${mixTable(r)}
    ${schemaList(r)}
    ${charts ? `<div class="card"><h3>Charts</h3><div class="grid cols-2">${charts}</div></div>` : ""}`;
}

function measureTable(r) {
  const rows = (r.measureDeltas || []).map(d => {
    const pct = d.deltaPct != null ? signed(d.deltaPct * 100, 1) + "%" : "—";
    const trend = d.trendReversed
      ? `<span class="badge warn">${esc(d.baselineTrend)} → ${esc(d.currentTrend)}</span>`
      : `<span class="badge muted">${esc(d.currentTrend)}</span>`;
    return `<tr>
      <td><strong>${esc(d.measure)}</strong></td>
      <td><span class="badge info">${esc(d.statistic)}</span></td>
      <td class="num">${fmt(d.baselineValue)}</td>
      <td class="num">${fmt(d.currentValue)}</td>
      <td class="num ${dir(d.delta)}">${signed(d.delta, 2)}</td>
      <td class="num ${dir(d.delta)}">${esc(pct)}</td>
      <td>${trend}</td></tr>`;
  }).join("");

  if (!rows) return "";
  return `<div class="card"><h3>Measures</h3>
    <div class="table-wrap"><table class="data">
      <thead><tr><th>Measure</th><th>Compared on</th>
        <th class="num">${esc(r.baselineName)}</th><th class="num">${esc(r.currentName)}</th>
        <th class="num">Change</th><th class="num">Change %</th><th>Trend</th></tr></thead>
      <tbody>${rows}</tbody>
    </table></div></div>`;
}

function mixTable(r) {
  const rows = (r.categoryShifts || []).map(s => {
    const status = s.isNew ? `<span class="badge ok">new</span>`
      : s.isGone ? `<span class="badge err">gone</span>`
      : `<span class="badge muted">present</span>`;
    return `<tr>
      <td>${esc(s.dimension)}</td>
      <td><strong>${esc(s.category)}</strong></td>
      <td>${esc(s.measure)}</td>
      <td class="num ${dir(s.shareDelta)}">${(s.baselineShare * 100).toFixed(1)}%</td>
      <td class="num ${dir(s.shareDelta)}">${(s.currentShare * 100).toFixed(1)}%</td>
      <td class="num ${dir(s.shareDelta)}">${signed(s.shareDelta * 100, 1)} pts</td>
      <td>${status}</td></tr>`;
  }).join("");

  if (!rows) return "";
  return `<div class="card"><h3>Category mix</h3>
    <div class="table-wrap"><table class="data">
      <thead><tr><th>Dimension</th><th>Category</th><th>Measure</th>
        <th class="num">Share before</th><th class="num">Share after</th>
        <th class="num">Shift</th><th>Status</th></tr></thead>
      <tbody>${rows}</tbody>
    </table></div></div>`;
}

function schemaList(r) {
  const items = (r.schemaChanges || []).map(c => `
    <div class="insight">
      <div class="body">
        <strong>${esc(c.column)}</strong>
        <p>${esc(c.description)}</p>
        <span class="kind">${esc(c.kind)}</span>
      </div>
    </div>`).join("");

  if (!items) return "";
  return `<div class="card"><h3>Structural differences</h3>${items}</div>`;
}

// The sign is always spelled out, so colour never carries the meaning on its own.
const dir = (v) => (Math.abs(v) < 1e-9 ? "" : v > 0 ? "up" : "down");
const signed = (v, decimals) => (v >= 0 ? "+" : "-") + Math.abs(v).toFixed(decimals);
