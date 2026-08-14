/* ==========================================================================
   ADP Analytics & Reporting Studio — SPA controller
   Talks to the TerraFluent Chart & Analytics API through the same-origin
   /api/* reverse proxy hosted by this web app.
   ========================================================================== */
(() => {
  "use strict";

  // ── State ───────────────────────────────────────────────────────────────
  const state = {
    dataset: null,        // { name, data, format }
    columns: [],          // cached ColumnProfileDto[]
    catalogue: null,      // { chartTypes, themes, renderModes }
    session: null,        // { id, turns: [] }
  };

  const VIEWS = {
    data:      { title: "Data Source",   render: renderDataView },
    dashboard: { title: "Dashboard",     render: renderDashboardView },
    analyze:   { title: "Analyze",       render: renderAnalyzeView },
    agent:     { title: "Ask the Agent", render: renderAgentView },
    aggregate: { title: "Aggregate",     render: renderAggregateView },
    quality:   { title: "Data Quality",  render: renderQualityView },
    studio:    { title: "Chart Studio",  render: renderStudioView },
    catalogue: { title: "Catalogue",     render: renderCatalogueView },
  };

  // ── DOM helpers ─────────────────────────────────────────────────────────
  const $  = (sel, root = document) => root.querySelector(sel);
  const $$ = (sel, root = document) => Array.from(root.querySelectorAll(sel));
  const content = $("#content");

  // Browsers do not run <script> tags inserted via innerHTML, which disables the
  // interactive chart JS (export menu, tooltips). Replace each with a live script node.
  // The data-tf-live guard keeps it idempotent across the studio call and the observer.
  function reviveScript(old) {
    const s = document.createElement("script");
    for (const a of Array.from(old.attributes)) s.setAttribute(a.name, a.value);
    s.setAttribute("data-tf-live", "1");
    s.textContent = old.textContent;
    old.parentNode.replaceChild(s, old);
  }
  function activateScripts(root) {
    root.querySelectorAll("script:not([data-tf-live])").forEach(reviveScript);
  }

  // ── Chart zoom (lightbox) ────────────────────────────────────────────────
  // A single shared modal enlarges any rendered chart. A MutationObserver decorates
  // every <figure> holding an <svg> with a zoom button, so this works on all pages.
  function ensureChartModal() {
    let modal = $("#chartModal");
    if (modal) return modal;
    modal = document.createElement("div");
    modal.id = "chartModal";
    modal.className = "chart-modal";
    modal.innerHTML = `
      <div class="chart-modal-inner" role="dialog" aria-modal="true" aria-label="Enlarged chart">
        <button class="chart-modal-close" type="button" aria-label="Close">✕</button>
        <div class="chart-modal-body"></div>
      </div>`;
    document.body.appendChild(modal);
    const close = () => { modal.classList.remove("open"); $(".chart-modal-body", modal).innerHTML = ""; };
    modal.addEventListener("click", e => { if (e.target === modal) close(); });
    $(".chart-modal-close", modal).onclick = close;
    document.addEventListener("keydown", e => { if (e.key === "Escape" && modal.classList.contains("open")) close(); });
    return modal;
  }

  function openChartModal(svg) {
    if (!svg) return;
    const modal = ensureChartModal();
    const body = $(".chart-modal-body", modal);
    const clone = svg.cloneNode(true);

    // Derive intrinsic size, then scale up to fill the viewport preserving aspect ratio.
    let vw = parseFloat(clone.getAttribute("width")) || 0;
    let vh = parseFloat(clone.getAttribute("height")) || 0;
    const vb = clone.getAttribute("viewBox");
    if (vb) { const p = vb.split(/[\s,]+/).map(Number); if (p.length === 4) { vw = p[2]; vh = p[3]; } }
    if (!vb && vw && vh) clone.setAttribute("viewBox", `0 0 ${vw} ${vh}`);
    const ar = (vw && vh) ? vw / vh : 16 / 9;
    const maxW = window.innerWidth * 0.9;
    const maxH = window.innerHeight * 0.85;
    let dw = maxW, dh = dw / ar;
    if (dh > maxH) { dh = maxH; dw = dh * ar; }
    clone.removeAttribute("width");
    clone.removeAttribute("height");
    clone.style.width = Math.round(dw) + "px";
    clone.style.height = Math.round(dh) + "px";
    clone.style.maxWidth = "100%";

    body.innerHTML = "";
    body.appendChild(clone);
    // Revive the chart's scripts inside the clone so the export menu and tooltips work in the
    // enlarged view. cloneNode copies the data-tf-live guard, so force fresh nodes here.
    clone.querySelectorAll("script").forEach(reviveScript);
    modal.classList.add("open");
  }

  function enhanceFigure(fig) {
    if (!fig || fig.dataset.zoomable === "1" || !fig.querySelector("svg")) return;
    fig.dataset.zoomable = "1";
    if (getComputedStyle(fig).position === "static") fig.style.position = "relative";
    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "chart-zoom";
    btn.title = "Enlarge chart";
    btn.setAttribute("aria-label", "Enlarge chart");
    btn.innerHTML = `<svg viewBox="0 0 24 24" width="16" height="16" aria-hidden="true"><g fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="6"/><path d="M20 20l-4.3-4.3M11 8.5v5M8.5 11h5"/></g></svg>`;
    btn.onclick = e => { e.stopPropagation(); openChartModal(fig.querySelector("svg")); };
    fig.appendChild(btn);
  }

  function initChartZoom() {
    ensureChartModal();
    const scan = node => {
      if (node.nodeType !== 1) return;
      if (node.matches && node.matches("figure")) enhanceFigure(node);
      if (node.querySelectorAll) node.querySelectorAll("figure").forEach(enhanceFigure);
      // Revive inert chart <script> tags so the export menu / tooltips work on every page.
      if (node.matches && node.matches("script:not([data-tf-live])")) reviveScript(node);
      if (node.querySelectorAll) node.querySelectorAll("script:not([data-tf-live])").forEach(reviveScript);
    };
    new MutationObserver(muts => muts.forEach(m => m.addedNodes.forEach(scan)))
      .observe(content, { childList: true, subtree: true });
    content.querySelectorAll("figure").forEach(enhanceFigure);
  }

  function esc(s) {
    return String(s ?? "")
      .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
  }
  function fmt(n) {
    if (n === null || n === undefined || Number.isNaN(n)) return "—";
    const a = Math.abs(n);
    if (a >= 1e9) return (n / 1e9).toFixed(2) + "B";
    if (a >= 1e6) return (n / 1e6).toFixed(2) + "M";
    if (a >= 1e3) return (n / 1e3).toFixed(2) + "k";
    return (Math.round(n * 100) / 100).toLocaleString();
  }
  function scoreClass(s) { return s >= 70 ? "" : s >= 40 ? "mid" : "low"; }

  let toastTimer;
  function toast(msg, isErr = false) {
    const t = $("#toast");
    t.textContent = msg;
    t.className = "toast show" + (isErr ? " err" : "");
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => (t.className = "toast"), 3200);
  }

  // ── API client (same-origin proxy) ──────────────────────────────────────
  const api = {
    async json(path, { method = "GET", body, query } = {}) {
      const url = path + qs(query);
      const opts = { method, headers: {} };
      if (body !== undefined) {
        opts.headers["Content-Type"] = "application/json";
        opts.body = JSON.stringify(body);
      }
      const res = await fetch(url, opts);
      if (!res.ok) throw await problem(res);
      const ct = res.headers.get("content-type") || "";
      return ct.includes("json") ? res.json() : res.text();
    },
    async text(path, { method = "POST", body, contentType, accept, query } = {}) {
      const opts = { method, headers: {} };
      if (accept) opts.headers["Accept"] = accept;
      if (body !== undefined) {
        opts.headers["Content-Type"] = contentType || "application/json";
        opts.body = typeof body === "string" ? body : JSON.stringify(body);
      }
      const res = await fetch(path + qs(query), opts);
      if (!res.ok) throw await problem(res);
      return res.text();
    },
  };

  function qs(obj) {
    if (!obj) return "";
    const p = new URLSearchParams();
    for (const [k, v] of Object.entries(obj)) {
      if (v !== undefined && v !== null && v !== "") p.append(k, v);
    }
    const s = p.toString();
    return s ? "?" + s : "";
  }

  async function problem(res) {
    let msg = `Request failed (${res.status})`;
    try {
      const data = await res.json();
      if (data.detail || data.title) msg = data.detail || data.title;
      if (Array.isArray(data.findings) && data.findings.length) {
        msg += ": " + data.findings.map(f => f.message).join("; ");
      } else if (Array.isArray(data.errors)) {
        msg += ": " + data.errors.map(e => e.message || e).join("; ");
      }
    } catch { /* non-JSON error body */ }
    const err = new Error(msg);
    err.status = res.status;
    return err;
  }

  // Build the AnalyzeRequest shared by all analytics endpoints.
  function analyzeRequest(extra = {}) {
    return {
      data: state.dataset.data,
      format: state.dataset.format || "Auto",
      datasetName: state.dataset.name || "Dataset",
      ...extra,
    };
  }

  function requireDataset() {
    if (state.dataset) return true;
    content.innerHTML = emptyState(
      "▦", "No dataset loaded",
      "Open <strong>Data Source</strong> to paste, upload, or load a sample dataset before running analytics.",
      `<button class="btn" data-goto="data">Go to Data Source</button>`
    );
    return false;
  }

  function emptyState(icon, title, msg, actions = "") {
    return `<div class="card"><div class="empty">
      <div class="em-ico">${icon}</div>
      <h3>${title}</h3><p>${msg}</p>${actions ? `<div style="margin-top:14px">${actions}</div>` : ""}
    </div></div>`;
  }
  function loading(label = "Working…") {
    return `<div class="loading"><span class="spinner"></span> ${esc(label)}</div>`;
  }
  function errorBox(e) {
    return `<div class="alert error"><strong>Could not complete the request.</strong><br>${esc(e.message)}</div>`;
  }

  // ── Router ──────────────────────────────────────────────────────────────
  let current = "data";
  function setView(name) {
    if (!VIEWS[name]) name = "data";
    current = name;
    $$(".nav-item").forEach(b => b.classList.toggle("active", b.dataset.view === name));
    $("#viewTitle").textContent = VIEWS[name].title;
    VIEWS[name].render();
    if (window.matchMedia("(max-width: 860px)").matches) $("#sidebar").classList.add("collapsed");
  }

  // ═══════════════════════════════════════════════════════════════════════
  //  DATA SOURCE
  // ═══════════════════════════════════════════════════════════════════════
  function renderDataView() {
    const ds = state.dataset;
    content.innerHTML = `
      <div class="card">
        <div class="section-head">
          <div class="titles"><h2>Data Source</h2><p>Provide the dataset that every analysis, dashboard and chart will use.</p></div>
        </div>
        <div class="row" style="margin-bottom:16px">
          <label class="field" style="flex:2 1 240px">Dataset name
            <input type="text" id="dsNameInput" placeholder="e.g. Global Sales 2024" value="${esc(ds?.name || "")}">
          </label>
          <label class="field" style="flex:1 1 140px">Format
            <select id="dsFormat">
              <option value="Auto">Auto-detect</option>
              <option value="Csv">CSV</option>
              <option value="Json">JSON array</option>
            </select>
          </label>
        </div>
        <div class="tabs" id="dataTabs">
          <button class="tab active" data-tab="paste">Paste data</button>
          <button class="tab" data-tab="upload">Upload file</button>
          <button class="tab" data-tab="samples">Samples</button>
        </div>

        <div data-pane="paste">
          <label class="field">Raw data <span class="hint">CSV (with header row) or a JSON array of objects</span>
            <textarea id="dsData" placeholder="Month,Region,Revenue&#10;2024-01,EU,12000&#10;2024-02,EU,13500">${esc(ds?.data || "")}</textarea>
          </label>
        </div>
        <div data-pane="upload" hidden>
          <input type="file" id="dsFile" accept=".csv,.json,.txt">
          <p class="hint" style="margin-top:8px">The file contents replace the raw data above.</p>
        </div>
        <div data-pane="samples" hidden>
          <div class="grid cols-3">
            ${sampleCard("sales", "Monthly Sales", "24 months · region &amp; product · revenue/cost/units · one anomaly")}
            ${sampleCard("web", "Web Traffic", "12 weeks · channel · sessions/conversions")}
            ${sampleCard("hr", "Headcount (JSON)", "Departments · headcount &amp; attrition as JSON")}
          </div>
        </div>

        <div class="row" style="margin-top:18px">
          <button class="btn" id="useData">Use this dataset</button>
          <div class="spacer"></div>
          <span id="dsCount" class="stat-pill">${ds ? `<strong>Active:</strong> ${esc(ds.name)}` : "No active dataset"}</span>
        </div>
      </div>
      <div id="dsPreview"></div>`;

    // Tabs
    $$("#dataTabs .tab").forEach(t => t.onclick = () => {
      $$("#dataTabs .tab").forEach(x => x.classList.toggle("active", x === t));
      $$("[data-pane]").forEach(p => (p.hidden = p.dataset.pane !== t.dataset.tab));
    });
    if (ds?.format) $("#dsFormat").value = ds.format;

    $("#dsFile").onchange = async (e) => {
      const file = e.target.files[0];
      if (!file) return;
      $("#dsData").value = await file.text();
      $("#dsNameInput").value = file.name.replace(/\.[^.]+$/, "");
      $$("#dataTabs .tab").forEach(x => x.classList.toggle("active", x.dataset.tab === "paste"));
      $$("[data-pane]").forEach(p => (p.hidden = p.dataset.pane !== "paste"));
      toast("File loaded — review and click ‘Use this dataset’.");
    };

    $$("[data-sample]").forEach(b => b.onclick = () => {
      const s = SAMPLES[b.dataset.sample];
      $("#dsData").value = s.data;
      $("#dsNameInput").value = s.name;
      $("#dsFormat").value = s.format;
      $$("#dataTabs .tab").forEach(x => x.classList.toggle("active", x.dataset.tab === "paste"));
      $$("[data-pane]").forEach(p => (p.hidden = p.dataset.pane !== "paste"));
      toast(`Loaded sample: ${s.name}`);
    });

    $("#useData").onclick = () => {
      const data = $("#dsData").value.trim();
      if (!data) return toast("Paste some data or load a sample first.", true);
      setDataset({
        name: $("#dsNameInput").value.trim() || "Dataset",
        data,
        format: $("#dsFormat").value,
      });
      renderPreview();
      toast("Dataset is ready. Head to Dashboard or Analyze.");
    };

    if (ds) renderPreview();
  }

  function sampleCard(key, title, desc) {
    return `<div class="chart-card" style="cursor:default">
      <strong>${title}</strong>
      <p class="cap" style="margin-top:6px">${desc}</p>
      <button class="btn subtle sm" data-sample="${key}" style="margin-top:10px">Load</button>
    </div>`;
  }

  function setDataset(ds) {
    state.dataset = ds;
    state.columns = [];
    state.session = null;
    const badge = $("#dsBadge");
    badge.classList.add("ready");
    $("#dsName").textContent = ds.name;
    $("#dsMeta").textContent = `${ds.format} · ${countRows(ds)} rows`;
  }

  function countRows(ds) {
    if ((ds.format === "Json") || (ds.format === "Auto" && ds.data.trim().startsWith("["))) {
      try { return JSON.parse(ds.data).length; } catch { return "?"; }
    }
    return Math.max(0, ds.data.trim().split(/\r?\n/).length - 1);
  }

  function renderPreview() {
    const host = $("#dsPreview");
    if (!host) return;
    const ds = state.dataset;
    $("#dsCount").innerHTML = `<strong>Active:</strong> ${esc(ds.name)} · ${countRows(ds)} rows`;
    const isJson = ds.format === "Json" || (ds.format === "Auto" && ds.data.trim().startsWith("["));
    let head = [], rows = [];
    try {
      if (isJson) {
        const arr = JSON.parse(ds.data);
        head = Object.keys(arr[0] || {});
        rows = arr.slice(0, 8).map(o => head.map(h => o[h]));
      } else {
        const lines = ds.data.trim().split(/\r?\n/);
        head = lines[0].split(",");
        rows = lines.slice(1, 9).map(l => l.split(","));
      }
    } catch { host.innerHTML = ""; return; }

    host.innerHTML = `<div class="card">
      <h3>Preview <span class="hint">first ${rows.length} rows</span></h3>
      <div class="table-wrap"><table class="data"><thead><tr>${
        head.map(h => `<th>${esc(h)}</th>`).join("")
      }</tr></thead><tbody>${
        rows.map(r => `<tr>${r.map(c => `<td>${esc(c)}</td>`).join("")}</tr>`).join("")
      }</tbody></table></div></div>`;
  }

  // ═══════════════════════════════════════════════════════════════════════
  //  DASHBOARD
  // ═══════════════════════════════════════════════════════════════════════
  function renderDashboardView() {
    if (!requireDataset()) return;
    content.innerHTML = `
      <div class="card">
        <div class="section-head">
          <div class="titles"><h2>Auto Dashboard</h2><p>KPIs, trend, comparison and distribution charts generated automatically from <strong>${esc(state.dataset.name)}</strong>.</p></div>
          <div class="row">
            <label class="field">KPIs<input type="number" id="maxKpis" value="4" min="1" max="12" style="width:80px"></label>
            <button class="btn" id="genDash" style="align-self:flex-end">Generate dashboard</button>
          </div>
        </div>
      </div>
      <div id="dashOut"></div>`;

    $("#genDash").onclick = async () => {
      const out = $("#dashOut");
      out.innerHTML = loading("Building dashboard…");
      try {
        const d = await api.json("/api/analytics/dashboard", {
          method: "POST", body: analyzeRequest(), query: { maxKpis: $("#maxKpis").value },
        });
        out.innerHTML = renderDashboard(d);
      } catch (e) { out.innerHTML = errorBox(e); }
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
      ${kpis ? `<div class="grid cols-4" style="margin-bottom:20px">${kpis}</div>` : ""}
      ${chartSection("Trends", d.trendCharts)}
      ${chartSection("Comparisons", d.comparisonCharts)}
      ${chartSection("Distributions", d.distributionCharts)}
      ${insightBlock("Anomalies", d.anomalies)}
      ${insightBlock("Key insights", d.keyInsights)}`;
  }

  function chartCard(svg, title, reason, score) {
    return `<div class="chart-card"><figure>${svg || ""}</figure>
      <div class="cap"><span class="tag">${esc(title || "")}</span>${
        score !== undefined ? ` · suitability ${score}` : ""
      }${reason ? `<br>${esc(reason)}` : ""}</div></div>`;
  }

  function insightBlock(title, arr) {
    if (!arr || !arr.length) return "";
    return `<div class="card"><h3>${title}</h3>${arr.map(insightRow).join("")}</div>`;
  }
  function insightRow(i) {
    return `<div class="insight">
      <div class="score ${scoreClass(i.importanceScore)}">${i.importanceScore ?? ""}</div>
      <div class="body">
        <strong>${esc(i.title)}</strong>
        <p>${esc(i.description)}</p>
        ${i.kind ? `<span class="kind">${esc(i.kind)}</span>` : ""}
      </div></div>`;
  }

  // ═══════════════════════════════════════════════════════════════════════
  //  ANALYZE
  // ═══════════════════════════════════════════════════════════════════════
  function renderAnalyzeView() {
    if (!requireDataset()) return;
    content.innerHTML = `
      <div class="card">
        <div class="section-head">
          <div class="titles"><h2>Full Analysis</h2><p>Profiling, validation, ranked insights and chart recommendations.</p></div>
          <div class="row">
            <label class="field" style="flex-direction:row;align-items:center;gap:8px;font-weight:600">
              <input type="checkbox" id="incSvg" checked style="width:auto"> Preview charts
            </label>
            <label class="field">Max insights<input type="number" id="maxIns" value="12" min="1" max="100" style="width:90px"></label>
            <button class="btn" id="runAnalyze" style="align-self:flex-end">Run analysis</button>
          </div>
        </div>
      </div>
      <div id="anOut"></div>`;

    $("#runAnalyze").onclick = async () => {
      const out = $("#anOut");
      out.innerHTML = loading("Analyzing dataset…");
      try {
        const r = await api.json("/api/analytics/analyze", {
          method: "POST",
          body: analyzeRequest({ maxInsights: +$("#maxIns").value, maxRecommendations: 12 }),
          query: { includeSvg: $("#incSvg").checked },
        });
        state.columns = r.columns || [];
        out.innerHTML = renderAnalysis(r);
      } catch (e) { out.innerHTML = errorBox(e); }
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

  // ═══════════════════════════════════════════════════════════════════════
  //  ASK THE AGENT
  // ═══════════════════════════════════════════════════════════════════════
  function renderAgentView() {
    if (!requireDataset()) return;
    content.innerHTML = `
      <div class="tabs" id="agentTabs">
        <button class="tab active" data-tab="oneshot">Single question</button>
        <button class="tab" data-tab="session">Conversation</button>
      </div>
      <div data-apane="oneshot">
        <div class="card">
          <div class="titles"><h2>Ask a question</h2><p>The deterministic agent parses your question, follows the evidence and returns an explainable trace.</p></div>
          <div class="row" style="margin-top:14px">
            <input type="text" id="askQ" placeholder="e.g. why did revenue change?" style="flex:1 1 320px">
            <button class="btn" id="askBtn">Ask</button>
          </div>
          <div style="margin-top:12px">
            <span class="hint">Suggested questions for this dataset</span>
            <div class="row" id="askSuggest" style="margin-top:8px"><span class="hint"><span class="spinner"></span> Building questions from your columns\u2026</span></div>
          </div>
        </div>
        <div id="askOut"></div>
      </div>
      <div data-apane="session" hidden>
        <div class="card">
          <div class="section-head">
            <div class="titles"><h2>Multi-turn conversation</h2><p>The session remembers previous turns and resolves follow-ups like “forecast it”.</p></div>
            <button class="btn ghost" id="startSession">Start / reset session</button>
          </div>
          <div class="chat" id="chatLog"></div>
          <div class="row" style="margin-top:14px">
            <input type="text" id="chatQ" placeholder="Ask a follow-up…" style="flex:1 1 320px" disabled>
            <button class="btn" id="chatBtn" disabled>Send</button>
          </div>
        </div>
      </div>`;

    $$("#agentTabs .tab").forEach(t => t.onclick = () => {
      $$("#agentTabs .tab").forEach(x => x.classList.toggle("active", x === t));
      $$("[data-apane]").forEach(p => (p.hidden = p.dataset.apane !== t.dataset.tab));
    });

    const ask = async () => {
      const q = $("#askQ").value.trim();
      const out = $("#askOut");
      out.innerHTML = loading("Investigating…");
      try {
        const r = await api.json("/api/analytics/ask", {
          method: "POST", body: analyzeRequest({ question: q || null }), query: { includeSvg: true },
        });
        out.innerHTML = renderAsk(r);
      } catch (e) { out.innerHTML = errorBox(e); }
    };
    $("#askBtn").onclick = ask;
    $("#askQ").addEventListener("keydown", e => { if (e.key === "Enter") ask(); });
    // Suggested questions are generated from the active dataset's columns.
    $("#askSuggest").addEventListener("click", e => {
      const b = e.target.closest("[data-suggest]");
      if (b) { $("#askQ").value = b.dataset.suggest; ask(); }
    });
    ensureColumns().then(() => {
      const host = $("#askSuggest");
      if (!host) return; // the view may have changed while columns loaded
      const questions = buildAgentQuestions(state.columns);
      host.innerHTML = questions.length
        ? questions.map(q => `<button class="btn subtle sm" data-suggest="${esc(q)}">${esc(q)}</button>`).join("")
        : `<span class="hint">Type a question about your data above to begin.</span>`;
    }).catch(() => {
      const host = $("#askSuggest");
      if (host) host.innerHTML = `<span class="hint">Type a question about your data above to begin.</span>`;
    });

    $("#startSession").onclick = async () => {
      try {
        const r = await api.json("/api/analytics/sessions", { method: "POST", body: analyzeRequest() });
        state.session = { id: r.sessionId, turns: [] };
        $("#chatLog").innerHTML = `<div class="bubble agent"><strong>Session ready</strong><span class="narr">${esc(r.summary?.headline || "Ask a question to begin.")}</span></div>`;
        $("#chatQ").disabled = false; $("#chatBtn").disabled = false; $("#chatQ").focus();
        toast("Session started.");
      } catch (e) { toast(e.message, true); }
    };

    const send = async () => {
      if (!state.session) return;
      const q = $("#chatQ").value.trim();
      if (!q) return;
      const log = $("#chatLog");
      log.insertAdjacentHTML("beforeend", `<div class="bubble user">${esc(q)}</div>`);
      $("#chatQ").value = "";
      const pending = document.createElement("div");
      pending.className = "bubble agent";
      pending.innerHTML = loading("Thinking…");
      log.appendChild(pending);
      log.scrollTop = log.scrollHeight;
      try {
        const r = await api.json(`/api/analytics/sessions/${state.session.id}/ask`, {
          method: "POST", body: { question: q },
        });
        pending.innerHTML = `<strong>${esc(r.headline)}</strong><span class="narr">${esc(r.narrative)}</span>${
          (r.steps && r.steps.length) ? `<span class="chips">${r.steps.map(s => `<em>${esc(s.skill)}</em>`).join("")}</span>` : ""
        }`;
      } catch (e) { pending.innerHTML = `<span style="color:var(--adp-red-dark)">${esc(e.message)}</span>`; }
      log.scrollTop = log.scrollHeight;
    };
    $("#chatBtn").onclick = send;
    $("#chatQ").addEventListener("keydown", e => { if (e.key === "Enter") send(); });
  }

  function renderAsk(r) {
    const steps = (r.steps || []).map(s => `
      <li><span class="skill">${esc(s.skill)}</span><span class="rationale">${esc(s.rationale)}</span>
      ${s.trigger ? `<span class="trigger">↳ ${esc(s.trigger)}</span>` : ""}</li>`).join("");
    const charts = (r.charts || []).map(c => chartCard(c.svg, c.title, c.reason, c.suitabilityScore)).join("");
    return `
      <div class="card">
        <span class="badge muted">${esc(r.goal)}</span>
        <h2 style="margin:10px 0 4px">${esc(r.headline)}</h2>
        <p class="sub">${esc(r.narrative)}</p>
        ${steps ? `<h3>Reasoning path</h3><ul class="steps">${steps}</ul>` : ""}
      </div>
      ${(r.insights && r.insights.length) ? `<div class="card"><h3>Insights</h3>${r.insights.map(insightRow).join("")}</div>` : ""}
      ${charts ? `<div class="card"><h3>Supporting charts</h3><div class="chart-stack">${charts}</div></div>` : ""}`;
  }

  // ═══════════════════════════════════════════════════════════════════════
  //  AGGREGATE
  // ═══════════════════════════════════════════════════════════════════════
  async function renderAggregateView() {
    if (!requireDataset()) return;
    content.innerHTML = `<div class="card">${loading("Reading columns…")}</div>`;
    await ensureColumns();

    const measures = state.columns.filter(c => c.type === "Numeric" || c.role === "Measure");
    const dims = state.columns.filter(c => c.role === "Dimension" || c.type === "String" || c.type === "DateTime");
    const opt = (c) => `<option value="${esc(c.name)}">${esc(c.name)}</option>`;
    const pool = state.columns.length ? state.columns : [];

    content.innerHTML = `
      <div class="card">
        <div class="titles"><h2>Aggregation &amp; Pivot</h2><p>Group a measure by one dimension, or pivot across two.</p></div>
        <div class="row" style="margin-top:14px">
          <label class="field" style="flex:1 1 180px">Measure
            <select id="aggMeasure">${(measures.length ? measures : pool).map(opt).join("")}</select></label>
          <label class="field" style="flex:1 1 180px">Dimension
            <select id="aggDim">${(dims.length ? dims : pool).map(opt).join("")}</select></label>
          <label class="field" style="flex:1 1 180px">Second dimension
            <select id="aggDim2"><option value="">— none (pivot off) —</option>${(dims.length ? dims : pool).map(opt).join("")}</select></label>
          <label class="field" style="flex:1 1 140px">Aggregation
            <select id="aggKind">${["Sum","Average","Count","Min","Max","Median"].map(k => `<option>${k}</option>`).join("")}</select></label>
          <button class="btn" id="aggRun" style="align-self:flex-end">Compute</button>
        </div>
      </div>
      <div id="aggOut"></div>`;

    $("#aggRun").onclick = async () => {
      const out = $("#aggOut");
      out.innerHTML = loading("Aggregating…");
      try {
        const r = await api.json("/api/analytics/aggregate", {
          method: "POST", body: analyzeRequest(),
          query: {
            measure: $("#aggMeasure").value, dimension: $("#aggDim").value,
            secondDimension: $("#aggDim2").value, aggregation: $("#aggKind").value,
          },
        });
        out.innerHTML = renderAggregation(r);
      } catch (e) { out.innerHTML = errorBox(e); }
    };
    if (measures.length && dims.length) $("#aggRun").click();
  }

  function renderAggregation(r) {
    if (r.isPivot) {
      const cellMap = new Map((r.cells || []).map(c => [c.row + "||" + c.column, c.value]));
      const head = `<th></th>${(r.columnKeys || []).map(c => `<th class="num">${esc(c)}</th>`).join("")}`;
      const body = (r.rowKeys || []).map(row => `<tr><th>${esc(row)}</th>${
        (r.columnKeys || []).map(col => {
          const v = cellMap.get(row + "||" + col);
          return `<td class="num">${v == null ? "—" : fmt(v)}</td>`;
        }).join("")
      }</tr>`).join("");
      return `<div class="card"><h3>${esc(r.aggregation)} of ${esc(r.measure)} — ${esc(r.dimension)} × ${esc(r.secondDimension)}</h3>
        <div class="table-wrap"><table class="data"><thead><tr>${head}</tr></thead><tbody>${body}</tbody></table></div></div>`;
    }
    const buckets = r.buckets || [];
    const max = Math.max(1, ...buckets.map(b => Math.abs(b.value)));
    const rows = buckets.map(b => {
      const pct = (Math.abs(b.value) / max) * 100;
      return `<tr>
        <td><strong>${esc(b.key)}</strong></td>
        <td class="num bar-cell"><div class="bar" style="width:${pct}%"></div><span>${fmt(b.value)}</span></td>
        <td class="num">${b.count}</td></tr>`;
    }).join("");
    return `<div class="card"><h3>${esc(r.aggregation)} of ${esc(r.measure)} by ${esc(r.dimension)}</h3>
      <div class="table-wrap"><table class="data">
        <thead><tr><th>${esc(r.dimension)}</th><th class="num">${esc(r.aggregation)}</th><th class="num">Count</th></tr></thead>
        <tbody>${rows}</tbody></table></div></div>`;
  }

  async function ensureColumns() {
    if (state.columns.length) return;
    try {
      const r = await api.json("/api/analytics/analyze", {
        method: "POST", body: analyzeRequest(), query: { includeSvg: false },
      });
      state.columns = r.columns || [];
    } catch { state.columns = []; }
  }

  // ═══════════════════════════════════════════════════════════════════════
  //  DATA QUALITY
  // ═══════════════════════════════════════════════════════════════════════
  function renderQualityView() {
    if (!requireDataset()) return;
    content.innerHTML = `
      <div class="card">
        <div class="section-head">
          <div class="titles"><h2>Data Quality</h2><p>Validation report for <strong>${esc(state.dataset.name)}</strong>.</p></div>
          <button class="btn" id="valRun">Validate</button>
        </div>
      </div>
      <div id="valOut"></div>`;

    $("#valRun").onclick = async () => {
      const out = $("#valOut");
      out.innerHTML = loading("Validating…");
      try {
        const v = await api.json("/api/analytics/validate", { method: "POST", body: analyzeRequest() });
        out.innerHTML = renderValidation(v);
      } catch (e) { out.innerHTML = errorBox(e); }
    };
    $("#valRun").click();
  }

  function renderValidation(v) {
    const badge = v.hasErrors ? `<span class="badge err">Has errors</span>`
      : v.isClean ? `<span class="badge ok">Clean</span>` : `<span class="badge warn">Warnings only</span>`;
    const sev = s => ({ Error: "err", Warning: "warn", Info: "info" }[s] || "muted");
    const rows = (v.issues || []).map(i => `
      <tr>
        <td><span class="badge ${sev(i.severity)}">${esc(i.severity)}</span></td>
        <td class="mono">${esc(i.code)}</td>
        <td>${esc(i.column || "—")}</td>
        <td>${esc(i.message)}</td>
        <td class="num">${i.affectedCount}</td>
      </tr>`).join("") || `<tr><td colspan="5" style="text-align:center;color:var(--text-muted)">No issues found.</td></tr>`;
    return `<div class="card">
      <div class="row" style="justify-content:space-between"><h3 style="margin:0">Validation result</h3>${badge}</div>
      <div class="table-wrap" style="margin-top:14px"><table class="data">
        <thead><tr><th>Severity</th><th>Code</th><th>Column</th><th>Message</th><th class="num">Affected</th></tr></thead>
        <tbody>${rows}</tbody></table></div></div>`;
  }

  // ═══════════════════════════════════════════════════════════════════════
  //  CHART STUDIO
  // ═══════════════════════════════════════════════════════════════════════
  async function renderStudioView() {
    content.innerHTML = `<div class="card">${loading("Loading catalogue…")}</div>`;
    await ensureCatalogue();
    const cat = state.catalogue;

    const typeOpts = cat.chartTypes.map(t => `<option>${esc(t.name)}</option>`).join("");
    const modeOpts = cat.renderModes.map(m => `<option${m.name === "Interactive" ? " selected" : ""}>${esc(m.name)}</option>`).join("");
    const themeOpts = cat.themes.map(t => `<option${t === "Vivid" ? " selected" : ""}>${esc(t)}</option>`).join("");

    content.innerHTML = `
      <div class="grid" style="grid-template-columns: minmax(320px, 420px) 1fr; align-items:start">
        <div class="card">
          <h2>Chart Studio</h2>
          <p class="sub">Compose a chart and render it server-side to SVG.</p>
          <label class="field">Title<input type="text" id="cTitle" value="Quarterly Revenue"></label>
          <label class="field" style="margin-top:12px">Categories <span class="hint">comma separated</span>
            <input type="text" id="cCats" value="Q1, Q2, Q3, Q4"></label>
          <div id="seriesList"></div>
          <button class="btn subtle sm" id="addSeries" style="margin-top:10px">+ Add series</button>
          <div class="row" style="margin-top:14px">
            <label class="field" style="flex:1 1 120px">Chart type<select id="cType">${typeOpts}</select></label>
            <label class="field" style="flex:0 0 96px">Background<input type="color" id="cBg" value="#ffffff" style="height:38px;padding:3px"></label>
          </div>
          <div class="row" style="margin-top:12px">
            <label class="field" style="flex:1 1 120px">Render mode<select id="cMode">${modeOpts}</select></label>
            <label class="field" style="flex:1 1 90px">Height<input type="number" id="cHeight" value="360" min="150" max="900"></label>
          </div>
          <div class="row" style="margin-top:12px">
            <label class="field" style="flex:1 1 120px">Theme<select id="cTheme">${themeOpts}</select></label>
            <label class="field" style="flex:1 1 160px;flex-direction:row;align-items:center;gap:8px;font-weight:600;align-self:flex-end">
              <input type="checkbox" id="cExport" checked style="width:auto"> Export menu
            </label>
          </div>
          <div class="row" style="margin-top:16px">
            <button class="btn" id="renderChart">Render</button>
            <button class="btn ghost" id="downloadSvg">Download SVG</button>
          </div>
        </div>
        <div class="card">
          <h3>Preview</h3>
          <div id="studioOut" class="chart-card" style="min-height:280px;display:flex;align-items:center;justify-content:center">
            <span class="hint">Configure a chart and click Render.</span>
          </div>
        </div>
      </div>`;

    let seriesCount = 0;
    const addSeries = (name = "Revenue", vals = "120, 150, 170, 210", type = "") => {
      const id = ++seriesCount;
      const wrap = document.createElement("div");
      wrap.className = "row";
      wrap.style.marginTop = "10px";
      wrap.dataset.series = id;
      wrap.innerHTML = `
        <label class="field" style="flex:1 1 110px">Series<input type="text" class="sName" value="${esc(name)}"></label>
        <label class="field" style="flex:2 1 180px">Values<input type="text" class="sVals" value="${esc(vals)}"></label>
        <button class="icon-btn sDel" title="Remove" style="align-self:flex-end">✕</button>`;
      $("#seriesList").appendChild(wrap);
      wrap.querySelector(".sDel").onclick = () => wrap.remove();
    };
    $("#addSeries").onclick = () => addSeries("Series " + (seriesCount + 1), "0, 0, 0, 0");
    addSeries();

    let lastSvg = "";
    const render = async () => {
      const out = $("#studioOut");
      out.innerHTML = loading("Rendering…");
      try {
        const cats = $("#cCats").value.split(",").map(s => s.trim()).filter(Boolean);
        const type = $("#cType").value;
        const series = $$("#seriesList [data-series]").map(w => ({
          name: w.querySelector(".sName").value.trim() || "Series",
          type,
          data: w.querySelector(".sVals").value.split(",").map(s => {
            const n = parseFloat(s.trim()); return Number.isNaN(n) ? null : n;
          }),
        }));
        const options = {
          title: { text: $("#cTitle").value },
          xAxis: { categories: cats },
          series,
          renderMode: $("#cMode").value,
          primaryChartType: type,
          height: +$("#cHeight").value,
          themeName: $("#cTheme").value,
          exportMenuEnabled: $("#cExport").checked,
        };
        // Only override the theme background when the user picks a non-default colour,
        // otherwise let the selected theme control its own background.
        const bg = $("#cBg").value;
        if (bg && bg.toLowerCase() !== "#ffffff") options.backgroundColor = bg;
        lastSvg = await api.text("/api/charts/render", {
          method: "POST", body: options, accept: "image/svg+xml", query: { format: "svg" },
        });
        out.innerHTML = `<figure style="width:100%">${lastSvg}</figure>`;
        activateScripts(out);
      } catch (e) { out.innerHTML = errorBox(e); }
    };
    $("#renderChart").onclick = render;
    $("#downloadSvg").onclick = () => {
      if (!lastSvg) return toast("Render a chart first.", true);
      const blob = new Blob([lastSvg], { type: "image/svg+xml" });
      const a = document.createElement("a");
      a.href = URL.createObjectURL(blob);
      a.download = ($("#cTitle").value || "chart").replace(/\s+/g, "-").toLowerCase() + ".svg";
      a.click();
      URL.revokeObjectURL(a.href);
    };
    render();
  }

  // ═══════════════════════════════════════════════════════════════════════
  //  CATALOGUE
  // ═══════════════════════════════════════════════════════════════════════
  async function renderCatalogueView() {
    content.innerHTML = `<div class="card">${loading("Loading catalogue…")}</div>`;
    try {
      await ensureCatalogue();
    } catch (e) { content.innerHTML = errorBox(e); return; }
    const cat = state.catalogue;
    const chips = arr => arr.map(x => `<span class="stat-pill">${esc(x)}</span>`).join("");
    const modes = cat.renderModes.map(m => `
      <div class="insight"><div class="score low">${esc(m.name[0])}</div>
      <div class="body"><strong>${esc(m.name)}</strong><p>${esc(m.description || "")}</p></div></div>`).join("");
    content.innerHTML = `
      <div class="card"><div class="titles"><h2>API Catalogue</h2><p>Capabilities exposed by the rendering engine.</p></div></div>
      <div class="grid cols-2">
        <div class="card"><h3>Chart types <span class="badge muted">${cat.chartTypes.length}</span></h3>
          <div class="stat-row">${chips(cat.chartTypes.map(t => t.name))}</div></div>
        <div class="card"><h3>Themes <span class="badge muted">${cat.themes.length}</span></h3>
          <div class="stat-row">${chips(cat.themes)}</div></div>
      </div>
      <div class="card"><h3>Render modes</h3>${modes}</div>`;
  }

  async function ensureCatalogue() {
    if (state.catalogue) return;
    const [chartTypes, themes, renderModes] = await Promise.all([
      api.json("/api/charts/catalogue/chart-types"),
      api.json("/api/charts/catalogue/themes"),
      api.json("/api/charts/catalogue/render-modes"),
    ]);
    state.catalogue = { chartTypes, themes, renderModes };
  }

  // ── API health indicator ────────────────────────────────────────────────
  async function checkApi() {
    const el = $("#apiStatus"), txt = $("#apiStatusText");
    try {
      await api.json("/api/charts/catalogue/themes");
      el.className = "api-status up"; txt.textContent = "API online";
    } catch {
      el.className = "api-status down"; txt.textContent = "API offline";
    }
  }

  // ── Dataset-driven question suggestions ─────────────────────────────────
  // Builds up to 20 relevant analyst questions from the active dataset's column profiles.
  // Type/Role values are the API's enum names (Numeric/Currency/…, RevenueMetric/CategoryDimension/…).
  function buildAgentQuestions(columns) {
    const cols = Array.isArray(columns) ? columns : [];
    const MEASURE_TYPES = ["Numeric", "Currency", "Percentage"];
    const DIM_TYPES = ["Category", "Boolean", "Text"];

    const isId = c => c.type === "Identifier" || c.role === "IdentifierRole";
    const isMeasure = c => MEASURE_TYPES.includes(c.type) && !isId(c);
    const isDate = c => c.type === "Date"; // only a real date axis enables trend/forecast/compare
    const isDim = c => !isId(c) && DIM_TYPES.includes(c.type)
      && (c.distinctCount ?? 0) >= 2 && (c.distinctCount ?? 0) <= 50;

    const rolePri = r => ({ RevenueMetric: 5, ProfitMetric: 4, CostMetric: 3, QuantityMetric: 2 }[r] || 1);
    const measures = cols.filter(isMeasure).sort((a, b) =>
      rolePri(b.role) - rolePri(a.role) || Math.abs(b.sum ?? b.mean ?? 0) - Math.abs(a.sum ?? a.mean ?? 0));
    const dims = cols.filter(isDim).sort((a, b) => (a.distinctCount ?? 99) - (b.distinctCount ?? 99));
    const hasDate = cols.some(isDate);

    const out = [];
    const seen = new Set();
    const add = s => { const k = s.toLowerCase(); if (!seen.has(k)) { seen.add(k); out.push(s); } };
    const full = () => out.length >= 20;

    const m = measures.map(c => c.name), d = dims.map(c => c.name);
    const m0 = m[0], m1 = m[1], d0 = d[0];

    // Headline mix — one of each capability up front, most relevant first.
    if (m0 && d0) add(`Which ${d0} has the highest ${m0}?`);
    if (m0 && hasDate) add(`What is the trend of ${m0} over time?`);
    if (m0 && hasDate) add(`Forecast ${m0} for the coming periods`);
    if (m0) add(`Are there any anomalies in ${m0}?`);
    if (m0 && m1) add(`Is there a relationship between ${m0} and ${m1}?`);
    if (measures.length >= 2) add(`Segment the data into natural groups`);
    if (m0 && d0) add(`Show the average ${m0} by ${d0}`);
    if (m0 && hasDate) add(`Why did ${m0} change?`);

    // Rankings across every dimension × measure.
    for (const dim of d) { for (const meas of m) { add(`Which ${dim} has the highest ${meas}?`); if (full()) break; } if (full()) break; }
    // Trends / outliers for the remaining measures.
    for (const meas of m) { if (full()) break; if (hasDate) add(`How has ${meas} changed over time?`); add(`Are there any outliers in ${meas}?`); }
    // Distribution, comparison and pairwise relationships to round out the set.
    if (m0) add(`How is ${m0} distributed?`);
    if (m0 && hasDate) add(`Compare ${m0} month over month`);
    for (let i = 0; i < m.length && !full(); i++)
      for (let j = i + 1; j < m.length && !full(); j++)
        add(`Is there a relationship between ${m[i]} and ${m[j]}?`);
    for (const dim of d) { for (const meas of m) { add(`Show the average ${meas} by ${dim}`); if (full()) break; } if (full()) break; }

    return out.slice(0, 20);
  }

  // ── Sample datasets ─────────────────────────────────────────────────────
  const SAMPLES = {
    sales: { name: "Global Sales 2023–2024", format: "Csv", data: buildSalesCsv() },
    web:   { name: "Web Traffic", format: "Csv", data:
`Week,Channel,Sessions,Conversions
2024-W01,Organic,4200,180
2024-W01,Paid,3100,210
2024-W02,Organic,4550,201
2024-W02,Paid,2980,190
2024-W03,Organic,4810,220
2024-W03,Paid,3320,240
2024-W04,Organic,5010,244
2024-W04,Paid,3500,265
2024-W05,Organic,5320,270
2024-W05,Paid,3610,281
2024-W06,Organic,5590,299
2024-W06,Paid,3990,320` },
    hr: { name: "Headcount", format: "Json", data:
`[
  { "Department": "Engineering", "Headcount": 128, "AttritionRate": 0.07, "OpenRoles": 12 },
  { "Department": "Sales", "Headcount": 86, "AttritionRate": 0.14, "OpenRoles": 9 },
  { "Department": "Marketing", "Headcount": 34, "AttritionRate": 0.11, "OpenRoles": 3 },
  { "Department": "Support", "Headcount": 52, "AttritionRate": 0.18, "OpenRoles": 6 },
  { "Department": "Finance", "Headcount": 21, "AttritionRate": 0.05, "OpenRoles": 1 },
  { "Department": "Operations", "Headcount": 44, "AttritionRate": 0.09, "OpenRoles": 4 }
]` },
  };

  function buildSalesCsv() {
    const regions = ["North America", "Europe", "Asia Pacific"];
    const products = ["Alpha", "Beta", "Gamma"];
    let rows = ["Month,Region,Product,Revenue,Cost,Units"];
    for (let i = 0; i < 24; i++) {
      const d = new Date(Date.UTC(2023, i, 1));
      const region = regions[i % regions.length];
      const product = products[Math.floor(i / 2) % products.length];
      let base = 12000 + i * 650 + Math.sin(i / 2) * 1400;
      if (i === 15) base *= 3.1;
      const revenue = Math.round(base);
      const cost = Math.round(revenue * (0.55 + (i % 3) * 0.03));
      const units = Math.round(revenue / 95);
      rows.push([d.toISOString().slice(0, 10), region, product, revenue, cost, units].join(","));
    }
    return rows.join("\n");
  }

  // ── Boot ────────────────────────────────────────────────────────────────
  function init() {
    $$(".nav-item").forEach(b => b.onclick = () => setView(b.dataset.view));
    $("#menuToggle").onclick = () => $("#sidebar").classList.toggle("collapsed");
    $("#loadSampleTop").onclick = () => {
      setDataset({ ...SAMPLES.sales });
      toast("Sample sales dataset loaded.");
      setView(current === "data" ? "dashboard" : current);
    };
    content.addEventListener("click", e => {
      const goto = e.target.closest("[data-goto]");
      if (goto) setView(goto.dataset.goto);
    });
    checkApi();
    initChartZoom();
    setView("data");
  }

  document.addEventListener("DOMContentLoaded", init);
})();
