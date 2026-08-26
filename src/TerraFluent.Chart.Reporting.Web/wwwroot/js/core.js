/* ==========================================================================
   Core — shared state, DOM/format helpers, API client, modals and the
   cross-view render helpers used by every Chart Studio view.
   ========================================================================== */
import { icon } from "./icons.js";

// ── State ───────────────────────────────────────────────────────────────
export const state = {
  dataset: null,        // { name, data, format }
  columns: [],          // cached ColumnProfileDto[]
  measureCount: 0,      // number of numeric measures in the active dataset
  catalogue: null,      // { chartTypes, themes, renderModes }
  session: null,        // { id, turns: [] }
  validation: null,     // cached data-quality report for the active dataset
  qualityAcknowledged: false, // true once the user has seen Data Quality for this dataset
};

// ── User settings / preferences (persisted in browser localStorage) ──────
// Only preferences are stored — never dataset contents, which stay in memory.
const SETTINGS_KEY = "tf.studio.settings.v1";
export const DEFAULT_SETTINGS = {
  defaultTheme:       "Vivid",
  defaultRenderMode:  "Interactive",
  defaultChartType:   "Column",
  legendPosition:     "Bottom",
  defaultAggregation: "Sum",
  showGridLines:      true,
  showExportMenu:     true,
  showDataLabels:     false,
  defaultWidth:       640,
  defaultHeight:      360,
  autoPrefetchStudio: true,
};

export let settings = loadSettings();

export function loadSettings() {
  try {
    const raw = localStorage.getItem(SETTINGS_KEY);
    return raw ? { ...DEFAULT_SETTINGS, ...JSON.parse(raw) } : { ...DEFAULT_SETTINGS };
  } catch { return { ...DEFAULT_SETTINGS }; }
}

export function saveSettings(patch) {
  settings = { ...settings, ...patch };
  try { localStorage.setItem(SETTINGS_KEY, JSON.stringify(settings)); } catch { /* storage unavailable */ }
  return settings;
}

export function resetSettings() {
  settings = { ...DEFAULT_SETTINGS };
  try { localStorage.removeItem(SETTINGS_KEY); } catch { /* storage unavailable */ }
  return settings;
}


// ── DOM helpers ─────────────────────────────────────────────────────────
export const $  = (sel, root = document) => root.querySelector(sel);
export const $$ = (sel, root = document) => Array.from(root.querySelectorAll(sel));
export const content = $("#content");

// Browsers do not run <script> tags inserted via innerHTML, which disables the
// interactive chart JS (export menu, tooltips). Replace each with a live script node.
// The data-tf-live guard keeps it idempotent across the studio call and the observer.
export function reviveScript(old) {
  const s = document.createElement("script");
  for (const a of Array.from(old.attributes)) s.setAttribute(a.name, a.value);
  s.setAttribute("data-tf-live", "1");
  s.textContent = old.textContent;
  old.parentNode.replaceChild(s, old);
}
export function activateScripts(root) {
  root.querySelectorAll("script:not([data-tf-live])").forEach(reviveScript);
}

// ── Chart zoom (lightbox) ────────────────────────────────────────────────
// A single shared modal enlarges any rendered chart. A MutationObserver decorates
// every <figure> holding an <svg> with a zoom button, so this works on all pages.
export function ensureChartModal() {
  let modal = $("#chartModal");
  if (modal) return modal;
  modal = document.createElement("div");
  modal.id = "chartModal";
  modal.className = "chart-modal";
  modal.innerHTML = `
    <div class="chart-modal-inner" role="dialog" aria-modal="true" aria-label="Enlarged chart">
      <button class="chart-modal-close" type="button" aria-label="Close">${icon("x")}</button>
      <div class="chart-modal-body"></div>
    </div>`;
  document.body.appendChild(modal);
  const close = () => { modal.classList.remove("open"); $(".chart-modal-body", modal).innerHTML = ""; };
  modal.addEventListener("click", e => { if (e.target === modal) close(); });
  $(".chart-modal-close", modal).onclick = close;
  document.addEventListener("keydown", e => { if (e.key === "Escape" && modal.classList.contains("open")) close(); });
  return modal;
}

export function openChartModal(svg) {
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

export function enhanceFigure(fig) {
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

export function initChartZoom() {
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

// ── Code / JSON viewer modal ──────────────────────────────────────────────
// A shared popup that shows read-only text (e.g. the chart-options request payload) with copy.
export function showCodeModal(title, code) {
  let modal = $("#codeModal");
  if (!modal) {
    modal = document.createElement("div");
    modal.id = "codeModal";
    modal.className = "chart-modal";
    modal.innerHTML = `
      <div class="chart-modal-inner" role="dialog" aria-modal="true" aria-label="JSON payload" style="max-width:760px;width:92%">
        <button class="chart-modal-close" type="button" aria-label="Close">${icon("x")}</button>
        <h3 id="codeModalTitle" style="margin:0 0 12px;padding-right:32px"></h3>
        <pre id="codeModalBody" style="max-height:60vh;overflow:auto;margin:0;padding:14px;border-radius:8px;background:#0f172a;color:#e2e8f0;font:12px/1.5 ui-monospace,SFMono-Regular,Menlo,Consolas,monospace;white-space:pre;-webkit-user-select:text;user-select:text"></pre>
        <div class="row" style="margin-top:12px;justify-content:flex-end">
          <button class="btn" id="codeModalCopy">${icon("copy")} Copy</button>
        </div>
      </div>`;
    document.body.appendChild(modal);
    const close = () => modal.classList.remove("open");
    modal.addEventListener("click", e => { if (e.target === modal) close(); });
    $(".chart-modal-close", modal).onclick = close;
    document.addEventListener("keydown", e => { if (e.key === "Escape" && modal.classList.contains("open")) close(); });
  }
  $("#codeModalTitle", modal).textContent = title;
  $("#codeModalBody", modal).textContent = code;
  $("#codeModalCopy", modal).onclick = async () => {
    try {
      await navigator.clipboard.writeText(code);
    } catch {
      // Clipboard API needs a secure context; fall back to a hidden textarea.
      const ta = document.createElement("textarea");
      ta.value = code;
      document.body.appendChild(ta);
      ta.select();
      document.execCommand("copy");
      ta.remove();
    }
    toast("Copied to clipboard.");
  };
  modal.classList.add("open");
}

export function esc(s) {
  return String(s ?? "")
    .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}
export function fmt(n) {
  if (n === null || n === undefined || Number.isNaN(n)) return "—";
  const a = Math.abs(n);
  if (a >= 1e9) return (n / 1e9).toFixed(2) + "B";
  if (a >= 1e6) return (n / 1e6).toFixed(2) + "M";
  if (a >= 1e3) return (n / 1e3).toFixed(2) + "k";
  return (Math.round(n * 100) / 100).toLocaleString();
}
export function scoreClass(s) { return s >= 70 ? "" : s >= 40 ? "mid" : "low"; }

let toastTimer;
export function toast(msg, isErr = false) {
  const t = $("#toast");
  t.textContent = msg;
  t.className = "toast show" + (isErr ? " err" : "");
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => (t.className = "toast"), 3200);
}

// ── API client (same-origin proxy) ──────────────────────────────────────
export const api = {
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
  // Posts raw bytes (e.g. an uploaded .xlsx) and reads a JSON response.
  async binary(path, bytes, { method = "POST", contentType = "application/octet-stream", query } = {}) {
    const res = await fetch(path + qs(query), {
      method,
      headers: { "Content-Type": contentType },
      body: bytes,
    });
    if (!res.ok) throw await problem(res);
    return res.json();
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

export function qs(obj) {
  if (!obj) return "";
  const p = new URLSearchParams();
  for (const [k, v] of Object.entries(obj)) {
    if (v !== undefined && v !== null && v !== "") p.append(k, v);
  }
  const s = p.toString();
  return s ? "?" + s : "";
}

export async function problem(res) {
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

// The dataset half of an AnalyzeRequest. A live connection sends only its *name* — the URL and any
// credentials stay on the server — while a pasted/uploaded dataset sends its rows inline.
export function datasetPayload(ds = state.dataset) {
  if (!ds) return {};
  return ds.connectionName
    ? { connectionName: ds.connectionName, datasetName: ds.name || ds.connectionName }
    : { data: ds.data, format: ds.format || "Auto", datasetName: ds.name || "Dataset" };
}

// Build the AnalyzeRequest shared by all analytics endpoints.
export function analyzeRequest(extra = {}) {
  return { ...datasetPayload(), ...extra };
}

/** True when the active dataset is pulled from a server-side connection rather than held locally. */
export const isLiveDataset = (ds = state.dataset) => Boolean(ds?.connectionName);

// Style overrides (from user settings) for the server-rendered recommendation charts shown on the
// Analyze, Dashboard and Ask-the-Agent pages. Spread into an endpoint's query object.
export function chartStyleQuery() {
  return {
    theme:      settings.defaultTheme,
    renderMode: settings.defaultRenderMode,
    exportMenu: settings.showExportMenu,
    gridLines:  settings.showGridLines,
  };
}


export function requireDataset() {
  if (state.dataset) return true;
  content.innerHTML = emptyState(
    icon("database", 40), "No dataset loaded",
    "Open <strong>Data Source</strong> to paste, upload, or load a sample dataset before running analytics.",
    `<button class="btn" data-goto="data">${icon("database")} Go to Data Source</button>`
  );
  return false;
}

export function emptyState(icon, title, msg, actions = "") {
  return `<div class="card"><div class="empty">
    <div class="em-ico">${icon}</div>
    <h3>${title}</h3><p>${msg}</p>${actions ? `<div style="margin-top:14px">${actions}</div>` : ""}
  </div></div>`;
}
export function loading(label = "Working…") {
  return `<div class="loading"><span class="spinner"></span> ${esc(label)}</div>`;
}
export function errorBox(e) {
  return `<div class="alert error"><strong>Could not complete the request.</strong><br>${esc(e.message)}</div>`;
}

export function setDataset(ds) {
  state.dataset = ds;
  state.columns = [];
  state.measureCount = 0;
  state.session = null;
  state.validation = null;
  state.qualityAcknowledged = false;
  const badge = $("#dsBadge");
  badge.classList.add("ready");
  $("#dsName").textContent = ds.name;
  $("#dsMeta").textContent = `${ds.format} · ${countRows(ds)} rows`;
}

export function countRows(ds) {
  // A live connection has no local copy to count — the row count is only known once the server
  // pulls it, so the caller shows the source rather than a number.
  if (!ds || typeof ds.data !== "string") return "—";
  if ((ds.format === "Json") || (ds.format === "Auto" && ds.data.trim().startsWith("["))) {
    try { return JSON.parse(ds.data).length; } catch { return "?"; }
  }
  return Math.max(0, ds.data.trim().split(/\r?\n/).length - 1);
}

export async function ensureColumns() {
  if (state.columns.length) return;
  try {
    const r = await api.json("/api/analytics/analyze", {
      method: "POST", body: analyzeRequest(), query: { includeSvg: false },
    });
    state.columns = r.columns || [];
    state.measureCount = r.summary?.measureCount ?? 0;
  } catch { state.columns = []; }
}

// Runs (and caches) the data-quality validation for the active dataset. Returns the report,
// or null when there is no dataset / the request fails.
export async function ensureValidation() {
  if (!state.dataset) return null;
  if (state.validation) return state.validation;
  try {
    state.validation = await api.json("/api/analytics/validate", { method: "POST", body: analyzeRequest() });
  } catch { state.validation = null; }
  return state.validation;
}

// Programmatic navigation: the router (main.js) registers its setView here so any view module
// can route without importing main.js (which would create a circular dependency).
let _navigate = null;
export function registerNavigate(fn) { _navigate = fn; }
export function navigate(name) { if (_navigate) _navigate(name); }

export async function ensureCatalogue() {
  if (state.catalogue) return;
  const [chartTypes, themes, renderModes] = await Promise.all([
    api.json("/api/charts/catalogue/chart-types"),
    api.json("/api/charts/catalogue/themes"),
    api.json("/api/charts/catalogue/render-modes"),
  ]);
  state.catalogue = { chartTypes, themes, renderModes };
}

// ── Analytics Agent health indicator ────────────────────────────────────
export async function checkApi() {
  const el = $("#apiStatus"), txt = $("#apiStatusText");
  txt.textContent = "Agent"; // label is constant; only the dot colour reflects status
  try {
    await api.json("/api/charts/catalogue/themes");
    el.className = "api-status up";
  } catch {
    el.className = "api-status down";
  }
}

// ── Shared render helpers (dashboard / analyze / agent) ───────────────────
export function chartCard(svg, title, reason, score) {
  return `<div class="chart-card"><figure>${svg || ""}</figure>
    <div class="cap"><span class="tag">${esc(title || "")}</span>${
      score !== undefined ? ` · suitability ${score}` : ""
    }${reason ? `<br>${esc(reason)}` : ""}</div></div>`;
}

export function insightBlock(title, arr) {
  if (!arr || !arr.length) return "";
  return `<div class="card"><h3>${title}</h3>${arr.map(insightRow).join("")}</div>`;
}
export function insightRow(i) {
  const isAnomaly = String(i.kind ?? "").toLowerCase() === "anomaly";
  const scoreTitle = isAnomaly
    ? "Severity score (0–100): how far this anomaly stands out from the norm"
    : "Importance score (0–100): how noteworthy this insight is";
  return `<div class="insight">
    <div class="score tip ${scoreClass(i.importanceScore)}${isAnomaly ? "" : " keyinsight"}" data-tip="${esc(scoreTitle)}" aria-label="${esc(scoreTitle)}">${i.importanceScore ?? ""}</div>
    <div class="body">
      <strong>${esc(i.title)}</strong>
      <p>${esc(i.description)}</p>
      ${i.kind ? `<span class="kind">${esc(i.kind)}</span>` : ""}
    </div></div>`;
}

// ── Aggregation helpers ─────────────────────────────────────────────────
// Non-additive measures (age, tenure, ratios, rates…) must never be summed. The API flags each
// column via `additive`; when it is false we drop "Sum" and default callers to "Average".
export const ALL_AGGREGATIONS = ["Sum", "Average", "Count", "Min", "Max", "Median"];
export const isSummable = (col) => !col || col.additive !== false;
export function aggregationsForColumns(cols) {
  const arr = Array.isArray(cols) ? cols : [cols];
  const anyNonAdditive = arr.some(c => c && c.additive === false);
  return anyNonAdditive ? ALL_AGGREGATIONS.filter(a => a !== "Sum") : ALL_AGGREGATIONS;
}

// ── Dataset-driven question suggestions ─────────────────────────────────
// Builds up to 20 relevant analyst questions from the active dataset's column profiles.
// Type/Role values are the API's enum names (Numeric/Currency/…, RevenueMetric/CategoryDimension/…).
export function buildAgentQuestions(columns) {
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

