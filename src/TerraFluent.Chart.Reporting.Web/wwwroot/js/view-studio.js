/* Chart Studio view — build a chart from your dataset or from scratch, with advanced
   options, theme gallery, multi-format export, copy-embed, load-JSON and shareable links. */
import {
  $, $$, content, state, esc, toast, loading, errorBox,
  api, ensureCatalogue, ensureColumns, analyzeRequest, activateScripts, showCodeModal, requireDataset, settings, aggregationsForColumns,
} from "./core.js";
import { icon } from "./icons.js";
import {
  STUDIO_TYPES, STUDIO_DEFAULT, STUDIO_NO_AXIS, STUDIO_STACKABLE,
  buildStudioSeries, seriesToRows, studioKind, feasibleChartTypes,
} from "./studio-core.js";

// Legend position preset -> Legend option fragment, and its inverse.
const LEGEND_PRESETS = {
  Bottom: { enabled: true, align: "center", verticalAlign: "bottom", layout: "horizontal" },
  Top:    { enabled: true, align: "center", verticalAlign: "top", layout: "horizontal" },
  Right:  { enabled: true, align: "right", verticalAlign: "middle", layout: "vertical" },
  Left:   { enabled: true, align: "left", verticalAlign: "middle", layout: "vertical" },
  None:   { enabled: false },
};

// Numeric measure columns per the API (ColumnProfile.IsMeasure = Numeric/Currency/Percentage),
// plus the metric semantic roles (RevenueMetric/CostMetric/ProfitMetric/QuantityMetric) and any
// column that carries numeric summary stats (sum/mean are only populated for numeric columns).
const MEASURE_TYPES = ["Numeric", "Currency", "Percentage"];
function isMeasureColumn(c) {
  if (!c) return false;
  return MEASURE_TYPES.includes(c.type)
    || (typeof c.role === "string" && c.role.endsWith("Metric"))
    || c.sum != null || c.mean != null;
}

function legendToPreset(lg) {
  if (!lg) return "Bottom";
  if (lg.enabled === false) return "None";
  if (lg.align === "right") return "Right";
  if (lg.align === "left") return "Left";
  if (lg.verticalAlign === "top") return "Top";
  return "Bottom";
}

export async function renderStudioView() {
  if (!requireDataset()) return;
  content.innerHTML = `<div class="card">${loading("Loading catalogue…")}</div>`;
  await ensureCatalogue();
  await ensureColumns();
  const cat = state.catalogue;

  // Limit the chart-type list to those the current dataset can actually produce.
  const cols = state.columns || [];
  const shape = {
    measures: cols.filter(isMeasureColumn).length,
    dims: cols.filter(c => ["Category", "Boolean", "Text"].includes(c.type)).length,
    dates: cols.filter(c => c.type === "Date").length,
  };
  const feasible = feasibleChartTypes(shape);
  const typePool = cat.chartTypes.map(t => t.name).filter(n => feasible.has(n));
  const availableTypes = typePool.length ? typePool : cat.chartTypes.map(t => t.name);
  const defType = availableTypes[0];

  const typeOpts = availableTypes.map(n => `<option${n === defType ? " selected" : ""}>${esc(n)}</option>`).join("");
  const modeOpts = cat.renderModes.map(m => `<option${m.name === settings.defaultRenderMode ? " selected" : ""}>${esc(m.name)}</option>`).join("");
  const themeOpts = cat.themes.map(t => `<option${t === settings.defaultTheme ? " selected" : ""}>${esc(t)}</option>`).join("");
  const legendOpts = Object.keys(LEGEND_PRESETS).map(p => `<option${p === settings.legendPosition ? " selected" : ""}>${p}</option>`).join("");

  const hasDataset = !!state.dataset;

  content.innerHTML = `
    <div class="card" style="margin-bottom:16px">
      <div class="section-head" style="margin-bottom:12px">
        <div class="titles"><h2>Chart Studio</h2><p>Compose a chart from your dataset or from scratch, then export or share it.</p></div>
        <button class="btn blue" id="stLoadDsBtn">${icon("corner-down-right")} ${hasDataset ? "Load from your dataset" : "Load a dataset first"}</button>
      </div>
      <div id="stDsPanel" class="privacy-note" style="display:none;background:#eef2ff;border-color:#c7d2fe">
        <div style="flex:1">
          <div class="row" style="align-items:flex-end;gap:12px">
            <label class="field" style="flex:1 1 160px">Category (dimension)<select id="stDsDim"></select></label>
            <label class="field" style="flex:1 1 130px">Aggregation<select id="stDsAgg">${["Sum","Average","Count","Min","Max","Median"].map(k => `<option>${k}</option>`).join("")}</select></label>
            <button class="btn" id="stDsLoad" style="align-self:flex-end">${icon("corner-down-right")} Load</button>
          </div>
          <div class="field" style="margin-top:10px">Measures <span class="hint">tick one or more</span>
            <div id="stDsMeasures" class="measure-checks"></div>
          </div>
        </div>
      </div>
      <div class="row" style="align-items:flex-end;gap:14px;margin-top:14px">
        <label class="field" style="flex:1 1 180px">Chart type<select id="stType">${typeOpts}</select></label>
        <label class="field" style="flex:1 1 160px">Theme<select id="stTheme">${themeOpts}</select></label>
        <label class="field" style="flex:1 1 160px">Render mode<select id="stMode">${modeOpts}</select></label>
        <button class="btn subtle" id="stGallery" style="align-self:flex-end">${icon("droplet")} Theme gallery</button>
      </div>
    </div>
    <div class="grid" style="grid-template-columns: minmax(320px, 440px) 1fr; align-items:start">
      <div class="card">
        <h3>Data</h3>
        <label class="field">Title<input type="text" id="stTitle" value="Quarterly Revenue"></label>
        <label class="field" id="stCatsField" style="margin-top:12px">Categories <span class="hint">comma separated</span>
          <input type="text" id="stCats" value="Q1, Q2, Q3, Q4"></label>
        <div class="row" id="stAxisTitles" style="margin-top:12px">
          <label class="field" style="flex:1 1 140px">X-axis title<input type="text" id="stXTitle" value=""></label>
          <label class="field" style="flex:1 1 140px">Y-axis title<input type="text" id="stYTitle" value=""></label>
        </div>
        <p class="hint" id="stDataFmt" style="margin:12px 0 0;display:none"></p>
        <div id="stSeriesList"></div>
        <button class="btn subtle sm" id="stAddSeries" style="margin-top:10px">${icon("plus")} Add series</button>

        <div class="row" style="margin-top:14px">
          <label class="field" style="flex:1 1 80px">Height<input type="number" id="stHeight" value="400" min="150" max="900"></label>
          <label class="field" style="flex:1 1 80px">Width<input type="number" id="stWidth" value="640" min="200" max="1600"></label>
          <label class="field" style="flex:1 1 130px;flex-direction:row;align-items:center;gap:8px;font-weight:600;align-self:flex-end">
            <input type="checkbox" id="stResponsive" style="width:auto"> Responsive
          </label>
        </div>
        <div class="row" style="margin-top:12px">
          <label class="field" style="flex:0 0 96px">Background<input type="color" id="stBg" value="${esc(settings.backgroundColor)}" style="height:38px;padding:3px"></label>
          <label class="field" style="flex:1 1 130px;flex-direction:row;align-items:center;gap:8px;font-weight:600;align-self:flex-end">
            <input type="checkbox" id="stExport"${settings.showExportMenu ? " checked" : ""} style="width:auto"> Export menu
          </label>
          <label class="field" style="flex:1 1 130px;flex-direction:row;align-items:center;gap:8px;font-weight:600;align-self:flex-end">
            <input type="checkbox" id="stGrid"${settings.showGridLines ? " checked" : ""} style="width:auto"> Show grid lines
          </label>
        </div>

        <details class="studio-advanced" style="margin-top:16px">
          <summary>Advanced options</summary>
          <div class="row" style="margin-top:12px">
            <label class="field" style="flex:1 1 150px;flex-direction:row;align-items:center;gap:8px;font-weight:600">
              <input type="checkbox" id="stDataLabels"${settings.showDataLabels ? " checked" : ""} style="width:auto"> Data labels
            </label>
            <label class="field" style="flex:1 1 150px;flex-direction:row;align-items:center;gap:8px;font-weight:600">
              <input type="checkbox" id="stLogY" style="width:auto"> Log Y-axis
            </label>
          </div>
          <div class="row" style="margin-top:12px">
            <label class="field" style="flex:1 1 150px">Legend<select id="stLegend">${legendOpts}</select></label>
            <label class="field" style="flex:1 1 150px">Stacking<select id="stStack">${["None","Normal","Percent"].map(k => `<option>${k}</option>`).join("")}</select></label>
          </div>
        </details>

        <div class="row" style="margin-top:16px">
          <button class="btn" id="stRender">${icon("play")} Render</button>
          <button class="btn subtle" id="stViewJson">${icon("code")} View JSON</button>
        </div>
      </div>

      <div class="card">
        <h3>Preview</h3>
        <div id="stOut" class="chart-card" style="min-height:280px;display:flex;align-items:center;justify-content:center">
          <span class="hint">Configure a chart and click Render.</span>
        </div>
        <div class="row" style="margin-top:14px;flex-wrap:wrap">
          <button class="btn blue" id="stDlSvg">${icon("download")} Download SVG</button>
          <button class="btn blue" id="stDlPng">${icon("image")} Download PNG</button>
          <button class="btn subtle" id="stCopySvg">${icon("copy")} Copy SVG</button>
          <button class="btn subtle" id="stCopyEmbed">${icon("code")} Copy embed</button>
          <button class="btn subtle" id="stShare">${icon("share")} Share link</button>
        </div>
      </div>
    </div>`;

  // ── Series editor rows ──────────────────────────────────────────────────
  let seriesCount = 0;
  const addSeries = (name = "Revenue", vals = "120, 150, 170, 210") => {
    const id = ++seriesCount;
    const wrap = document.createElement("div");
    wrap.className = "row";
    wrap.style.marginTop = "10px";
    wrap.dataset.series = id;
    wrap.innerHTML = `
      <label class="field" style="flex:1 1 110px">Series<input type="text" class="sName" value="${esc(name)}"></label>
      <label class="field" style="flex:2 1 180px">Values<input type="text" class="sVals" value="${esc(vals)}"></label>
      <button class="icon-btn sDel" title="Remove" style="align-self:flex-end">${icon("x")}</button>`;
    $("#stSeriesList").appendChild(wrap);
    wrap.querySelector(".sDel").onclick = () => wrap.remove();
  };
  $("#stAddSeries").onclick = () => addSeries("Series " + (seriesCount + 1), "0, 0, 0, 0");

  const setSeriesRows = (rows) => {
    $("#stSeriesList").innerHTML = "";
    seriesCount = 0;
    (rows || []).forEach(r => addSeries(r.name, r.vals));
  };

  // ── Preset / type-driven UI ─────────────────────────────────────────────
  // Type-driven layout only: toggles field visibility for the chosen type without
  // touching the user's current title/categories/series data.
  const applyTypeLayout = (type) => {
    const p = STUDIO_TYPES[type] || STUDIO_DEFAULT;
    $("#stCatsField").style.display = p.showCats === false ? "none" : "";
    const hasAxes = !STUDIO_NO_AXIS.has(type);
    $("#stAxisTitles").style.display = hasAxes ? "" : "none";
    $("#stDataFmt").textContent = p.hint ? `Values format: ${p.hint}` : "";
    $("#stDataFmt").style.display = p.hint ? "" : "none";
    const hideSeries = p.kind === "sankey";
    $("#stSeriesList").style.display = hideSeries ? "none" : "";
    $("#stAddSeries").style.display = hideSeries ? "none" : "";
    $("#stStack").disabled = !STUDIO_STACKABLE.has(type);
  };

  // Full preset: layout + sample starter data. Used for the initial editor state only.
  const applyPreset = (type) => {
    const p = STUDIO_TYPES[type] || STUDIO_DEFAULT;
    applyTypeLayout(type);
    $("#stTitle").value = p.title;
    $("#stCats").value = p.cats || "";
    const hasAxes = !STUDIO_NO_AXIS.has(type);
    $("#stXTitle").value = hasAxes ? (p.xTitle || "") : "";
    $("#stYTitle").value = hasAxes ? (p.yTitle || "") : "";
    setSeriesRows(p.series.map(([name, vals]) => ({ name, vals })));
  };
  applyPreset($("#stType").value);

  // ── Build the /api/charts/render payload from the editor state ───────────
  const buildOptions = () => {
    const type = $("#stType").value;
    const cats = $("#stCats").value.split(",").map(s => s.trim()).filter(Boolean);
    const rows = $$("#stSeriesList [data-series]").map(w => ({
      name: w.querySelector(".sName").value.trim() || "Series",
      tokens: w.querySelector(".sVals").value.split(",").map(s => s.trim()).filter(Boolean),
    }));
    const series = buildStudioSeries(type, rows);
    if ($("#stDataLabels").checked) series.forEach(s => { s.dataLabel = { enabled: true }; });

    const xt = $("#stXTitle").value.trim();
    const yt = $("#stYTitle").value.trim();
    const showGrid = $("#stGrid").checked;
    const xAxis = { categories: cats };
    if (xt) xAxis.title = xt;
    if (!showGrid) xAxis.gridLineVisible = false;

    const options = {
      title: { text: $("#stTitle").value },
      xAxis,
      series,
      renderMode: $("#stMode").value,
      primaryChartType: type,
      height: +$("#stHeight").value,
      themeName: $("#stTheme").value,
      exportMenuEnabled: $("#stExport").checked,
      stacking: $("#stStack").disabled ? "None" : $("#stStack").value,
      legend: LEGEND_PRESETS[$("#stLegend").value] || LEGEND_PRESETS.Bottom,
      fontScale: settings.fontScale,
    };
    // Responsive omits width (SVG emits width="100%"); otherwise use the fixed width.
    if (!$("#stResponsive").checked) options.width = +$("#stWidth").value;

    let yAxis = yt ? { title: yt } : {};
    if ($("#stLogY").checked) yAxis.type = "Logarithmic";
    if (!showGrid) yAxis.gridLineVisible = false;
    if (Object.keys(yAxis).length) options.yAxis = yAxis;

    const bg = $("#stBg").value;
    if (bg && bg.toLowerCase() !== "#ffffff") options.backgroundColor = bg;
    return options;
  };

  // ── Repopulate the editor from a full options object (Load JSON / Share) ──
  const applyOptions = (o) => {
    const type = o.primaryChartType || (o.series && o.series[0] && o.series[0].type) || $("#stType").value;
    if ([...$("#stType").options].some(op => op.value === type)) $("#stType").value = type;
    applyPreset(type); // sets visibility toggles + sample data (overwritten below)

    $("#stTitle").value = (o.title && o.title.text) || "";
    $("#stCats").value = ((o.xAxis && o.xAxis.categories) || []).join(", ");
    $("#stXTitle").value = (o.xAxis && o.xAxis.title) || "";
    $("#stYTitle").value = (o.yAxis && o.yAxis.title) || "";
    if (o.themeName && [...$("#stTheme").options].some(op => op.value === o.themeName)) $("#stTheme").value = o.themeName;
    if (o.renderMode && [...$("#stMode").options].some(op => op.value === o.renderMode)) $("#stMode").value = o.renderMode;
    if (o.height) $("#stHeight").value = o.height;
    if (o.width) { $("#stResponsive").checked = false; $("#stWidth").value = o.width; }
    else if (o.width === null || o.width === undefined) { /* leave as-is */ }
    $("#stExport").checked = o.exportMenuEnabled !== false;
    $("#stGrid").checked = !((o.xAxis && o.xAxis.gridLineVisible === false)
      || (o.yAxis && o.yAxis.gridLineVisible === false));
    $("#stBg").value = o.backgroundColor || "#ffffff";
    $("#stDataLabels").checked = !!(o.series && o.series[0] && o.series[0].dataLabel && o.series[0].dataLabel.enabled);
    $("#stLogY").checked = !!(o.yAxis && o.yAxis.type === "Logarithmic");
    $("#stLegend").value = legendToPreset(o.legend);
    if (!$("#stStack").disabled) $("#stStack").value = o.stacking || "None";

    const rows = seriesToRows(type, o.series);
    if (rows) setSeriesRows(rows);
  };

  // ── Render ───────────────────────────────────────────────────────────────
  let lastSvg = "";
  let lastOptions = null;
  const render = async () => {
    const out = $("#stOut");
    out.innerHTML = loading("Rendering…");
    try {
      lastOptions = buildOptions();
      lastSvg = await api.text("/api/charts/render", {
        method: "POST", body: lastOptions, accept: "image/svg+xml", query: { format: "svg" },
      });
      out.innerHTML = `<figure style="width:100%">${lastSvg}</figure>`;
      activateScripts(out);
    } catch (e) { out.innerHTML = errorBox(e); }
  };

  $("#stRender").onclick = render;
  $("#stType").onchange = () => { applyTypeLayout($("#stType").value); render(); };
  $("#stTheme").onchange = render;
  $("#stMode").onchange = render;
  $("#stGrid").onchange = render;

  $("#stViewJson").onclick = () =>
    showCodeModal("Chart options — API request payload", JSON.stringify(buildOptions(), null, 2));

  // ── Export / share actions ───────────────────────────────────────────────
  const fileBase = () => ($("#stTitle").value || "chart").replace(/\s+/g, "-").toLowerCase();

  // Absolute /api/charts/shared URL for the current chart (view-only: no export menu).
  const buildShareUrl = () => {
    const shareOptions = { ...buildOptions(), exportMenuEnabled: false };
    const encoded = btoa(unescape(encodeURIComponent(JSON.stringify(shareOptions))));
    return `${location.origin}/api/charts/shared?options=${encodeURIComponent(encoded)}`;
  };

  $("#stDlSvg").onclick = () => {
    if (!lastSvg) return toast("Render a chart first.", true);
    downloadBlob(new Blob([lastSvg], { type: "image/svg+xml" }), fileBase() + ".svg");
  };
  $("#stDlPng").onclick = async () => {
    if (!lastOptions) return toast("Render a chart first.", true);
    try {
      // The API returns a self-contained page that rasterizes to PNG and auto-downloads.
      const html = await api.text("/api/charts/render", { method: "POST", body: lastOptions, accept: "image/png", query: { format: "png" } });
      const frame = document.createElement("iframe");
      frame.style.display = "none";
      frame.srcdoc = html;
      document.body.appendChild(frame);
      setTimeout(() => frame.remove(), 8000);
    } catch (e) { toast(e.message, true); }
  };
  $("#stCopySvg").onclick = async () => {
    if (!lastSvg) return toast("Render a chart first.", true);
    await copyText(lastSvg);
    toast("SVG copied to clipboard.");
  };
  $("#stCopyEmbed").onclick = async () => {
    await copyText(`<img alt="${esc($("#stTitle").value || "chart")}" src="${buildShareUrl()}">`);
    toast("Embed <img> tag copied.");
  };
  $("#stShare").onclick = async () => {
    await copyText(buildShareUrl());
    toast("Shareable chart link copied to clipboard.");
  };

  // ── Theme gallery ────────────────────────────────────────────────────────
  $("#stGallery").onclick = () => openThemeGallery(cat.themes, buildOptions(), (theme, showGrid) => {
    $("#stTheme").value = theme;
    $("#stGrid").checked = showGrid;
    render();
  });

  // ── Load from dataset ────────────────────────────────────────────────────
  const dsBtn = $("#stLoadDsBtn");
  const dsPanel = $("#stDsPanel");
  dsBtn.disabled = !hasDataset;
  dsBtn.onclick = async () => {
    if (!hasDataset) return toast("Load a dataset on the Data Source page first.", true);
    const open = dsPanel.style.display !== "none";
    if (open) { dsPanel.style.display = "none"; return; }
    dsPanel.style.display = "";
    if (!$("#stDsDim").options.length) await populateDatasetFields();
  };

  // Pick a sensible default aggregation from the selected dimension's column type.
  const defaultAggForDim = (dimName) => {
    const cols = state.columns || [];
    const col = cols.find(c => c.name === dimName);
    const hasNumericMeasure = cols.some(isMeasureColumn);
    if (!hasNumericMeasure) return "Count";
    // High-cardinality text reads better as a count; grouped categories/dates default to a sum.
    return (col && col.type === "Text") ? "Count" : "Sum";
  };
  // Selected measure columns (checked boxes) resolved to their profiles.
  const checkedMeasureCols = () => {
    const cols = state.columns || [];
    return $$("#stDsMeasures input:checked").map(cb => cols.find(c => c.name === cb.value)).filter(Boolean);
  };
  // Rebuild the aggregation list from the selected measures: non-additive measures (age, tenure…)
  // drop "Sum". Preserve the current choice when still valid, else fall back to a sensible default.
  const rebuildAggOptions = () => {
    const sel = $("#stDsAgg");
    if (!sel) return;
    const allowed = aggregationsForColumns(checkedMeasureCols());
    const prev = sel.value;
    sel.innerHTML = allowed.map(k => `<option>${k}</option>`).join("");
    let want = allowed.includes(prev) ? prev : defaultAggForDim($("#stDsDim").value);
    if (!allowed.includes(want)) want = allowed.includes("Average") ? "Average" : allowed[0];
    sel.value = want;
  };
  const applyDefaultAgg = () => {
    const sel = $("#stDsAgg");
    const allowed = aggregationsForColumns(checkedMeasureCols());
    let agg = defaultAggForDim($("#stDsDim").value);
    if (!allowed.includes(agg)) agg = allowed.includes("Average") ? "Average" : allowed[0];
    if ([...sel.options].some(o => o.value === agg)) sel.value = agg;
  };

  const populateDatasetFields = async () => {
    await ensureColumns();
    const cols = state.columns || [];
    const measures = cols.filter(isMeasureColumn);
    // GroupBy needs categorical dimensions (with Labels); Date/numeric columns have none, so keep only categoricals.
    const dims = cols.filter(c => ["Category", "Boolean", "Text"].includes(c.type));
    const dimPool = dims.length ? dims : cols.filter(c => c.type !== "Date");
    const opt = c => `<option value="${esc(c.name)}">${esc(c.name)}</option>`;
    $("#stDsDim").innerHTML = dimPool.map(opt).join("");
    const measurePool = measures.length ? measures : cols;
    $("#stDsMeasures").innerHTML = measurePool.map((c, i) =>
      `<label class="measure-check"><input type="checkbox" value="${esc(c.name)}"${i === 0 ? " checked" : ""}> ${esc(c.name)}</label>`).join("");
    // Ticking/unticking a measure re-evaluates whether Sum is offered.
    $$("#stDsMeasures input").forEach(cb => cb.onchange = rebuildAggOptions);
    rebuildAggOptions();
  };
  $("#stDsDim").onchange = applyDefaultAgg;

  // Fetch the aggregated buckets for the current dim/agg/measures and render the chart.
  const loadFromDataset = async ({ silent = false } = {}) => {
    const dim = $("#stDsDim").value;
    const measures = $$("#stDsMeasures input:checked").map(cb => cb.value);
    const agg = $("#stDsAgg").value;
    if (!dim || !measures.length) {
      if (!silent) toast("Pick a dimension and at least one measure.", true);
      return false;
    }
    const btn = $("#stDsLoad"); if (btn) btn.disabled = true;
    try {
      const results = await Promise.all(measures.map(m =>
        api.json("/api/analytics/aggregate", { method: "POST", body: analyzeRequest(), query: { measure: m, dimension: dim, aggregation: agg } })));
      const cats = (results[0].buckets || []).map(b => b.key);
      const aggType = "Column";
      $("#stType").value = aggType;
      applyPreset(aggType);
      $("#stTitle").value = `${agg} of ${measures.join(", ")} by ${dim}`;
      $("#stCats").value = cats.join(", ");
      $("#stXTitle").value = dim;
      $("#stYTitle").value = measures.length === 1 ? measures[0] : agg;
      setSeriesRows(results.map((r, i) => {
        const map = new Map((r.buckets || []).map(b => [b.key, b.value]));
        // Round aggregated values to at most 2 decimals so the editor shows clean numbers.
        return { name: measures[i], vals: cats.map(k => { const v = map.get(k); return v == null ? 0 : Math.round(v * 100) / 100; }).join(", ") };
      }));
      await render();
      if (!silent) toast("Loaded from dataset.");
      return true;
    } catch (e) { if (!silent) toast(e.message, true); return false; }
    finally { if (btn) btn.disabled = false; }
  };
  $("#stDsLoad") && ($("#stDsLoad").onclick = () => loadFromDataset());

  // ── Restore a shared chart from the URL hash, else prefetch summarized data ─
  const shared = /(?:^|#|&)studio=([^&]+)/.exec(location.hash || "");
  if (shared) {
    try { applyOptions(JSON.parse(decodeURIComponent(escape(atob(shared[1]))))); }
    catch { /* malformed link — fall back to defaults */ }
    render();
  } else {
    // On first open, choose default dim/aggregation and render a summarized chart.
    await populateDatasetFields();
    const ok = await loadFromDataset({ silent: true });
    if (!ok) render();
  }
}

// ── Helpers ────────────────────────────────────────────────────────────────
function downloadBlob(blob, name) {
  const a = document.createElement("a");
  a.href = URL.createObjectURL(blob);
  a.download = name;
  document.body.appendChild(a); a.click(); document.body.removeChild(a);
  setTimeout(() => URL.revokeObjectURL(a.href), 1000);
}

async function copyText(text) {
  try { await navigator.clipboard.writeText(text); }
  catch {
    const ta = document.createElement("textarea");
    ta.value = text; document.body.appendChild(ta); ta.select();
    document.execCommand("copy"); ta.remove();
  }
}

// A small modal with a textarea for pasting chart-options JSON.
// Renders the current chart across every theme in a grid so a theme can be chosen visually.
async function openThemeGallery(themes, baseOptions, onPick) {
  let modal = $("#themeGalleryModal");
  if (!modal) {
    modal = document.createElement("div");
    modal.id = "themeGalleryModal";
    modal.className = "chart-modal";
    modal.innerHTML = `
      <div class="chart-modal-inner" role="dialog" aria-modal="true" aria-label="Theme gallery" style="max-width:1180px;width:94%;max-height:88vh;overflow:auto">
        <button class="chart-modal-close" type="button" aria-label="Close">${icon("x")}</button>
        <div class="row" style="margin:0 0 12px;padding-right:32px;justify-content:space-between;align-items:center;gap:12px;flex-wrap:wrap">
          <h3 style="margin:0">Theme gallery <span class="hint">click a theme to apply</span></h3>
          <label class="row" style="gap:6px;align-items:center;cursor:pointer;font-size:13px">
            <input type="checkbox" id="themeGalleryGridToggle" checked> Show grid lines
          </label>
        </div>
        <div id="themeGalleryGrid" class="grid" style="grid-template-columns:repeat(2,1fr);gap:14px"></div>
      </div>`;
    document.body.appendChild(modal);
    const close = () => modal.classList.remove("open");
    modal.addEventListener("click", e => { if (e.target === modal) close(); });
    modal.querySelector(".chart-modal-close").onclick = close;
    document.addEventListener("keydown", e => { if (e.key === "Escape" && modal.classList.contains("open")) close(); });
  }
  const grid = $("#themeGalleryGrid");
  const gridToggle = modal.querySelector("#themeGalleryGridToggle");
  // Reflect the chart's current grid setting each time the (cached) modal opens.
  gridToggle.checked = !(baseOptions.xAxis && baseOptions.xAxis.gridLineVisible === false);
  modal.classList.add("open");

  // Render every theme preview at the current grid-line setting, then wire up click-to-apply.
  const renderPreviews = async () => {
    grid.innerHTML = `<div class="loading" style="grid-column:1/-1">${loading("Rendering themes…")}</div>`;
    const showGrid = gridToggle.checked;
    const previews = await Promise.all(themes.map(async theme => {
      const opts = {
        ...baseOptions, themeName: theme, renderMode: "Animated", exportMenuEnabled: false, width: 520, height: 320,
        // Let each theme use its own background — ignore the studio's chosen colour.
        backgroundColor: undefined,
        xAxis: { ...(baseOptions.xAxis || {}), gridLineVisible: showGrid },
        yAxis: { ...(baseOptions.yAxis || {}), gridLineVisible: showGrid },
      };
      try {
        const svg = await api.text("/api/charts/render", { method: "POST", body: opts, accept: "image/svg+xml", query: { format: "svg" } });
        return { theme, svg };
      } catch { return { theme, svg: "" }; }
    }));

    grid.innerHTML = previews.map(p =>
      `<figure class="chart-card" data-theme="${esc(p.theme)}" style="cursor:pointer;margin:0">
         <div style="width:100%">${p.svg || `<span class="hint">Failed to render</span>`}</div>
         <figcaption class="cap"><strong>${esc(p.theme)}</strong></figcaption>
       </figure>`).join("");
    grid.querySelectorAll("[data-theme]").forEach(fig => fig.onclick = () => {
      onPick(fig.dataset.theme, gridToggle.checked);
      modal.classList.remove("open");
      toast(`Applied "${fig.dataset.theme}" theme.`);
    });
  };

  gridToggle.onchange = renderPreviews;
  await renderPreviews();
}
