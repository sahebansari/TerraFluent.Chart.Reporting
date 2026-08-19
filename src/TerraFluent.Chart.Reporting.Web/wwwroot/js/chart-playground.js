/* Chart playground — the interactive chart composer, mounted inside the Catalogue page.
   Pick a chart type + theme, edit sample data, render server-side to SVG. */
import {
  $, $$, content, state, esc, toast, loading, errorBox,
  api, ensureCatalogue, activateScripts, showCodeModal,
} from "./core.js";
import { icon } from "./icons.js";
import { STUDIO_TYPES, STUDIO_DEFAULT, STUDIO_NO_AXIS, buildStudioSeries } from "./studio-core.js";

// Renders the composer into the given host element (defaults to the main content area).
export async function renderPlayground(host = content) {
  host.innerHTML = `<div class="card">${loading("Loading catalogue…")}</div>`;
  await ensureCatalogue();
  const cat = state.catalogue;

  const typeOpts = cat.chartTypes.map(t => `<option>${esc(t.name)}</option>`).join("");
  const modeOpts = cat.renderModes.map(m => `<option${m.name === "Interactive" ? " selected" : ""}>${esc(m.name)}</option>`).join("");
  const themeOpts = cat.themes.map(t => `<option${t === "Vivid" ? " selected" : ""}>${esc(t)}</option>`).join("");

  host.innerHTML = `
    <div class="card" style="margin-bottom:16px">
      <div class="titles" style="margin-bottom:12px"><h2>Chart Playground</h2><p>Pick a chart type and theme — the editor fills matching sample data automatically.</p></div>
      <div class="row" style="align-items:flex-end;gap:14px">
        <label class="field" style="flex:1 1 180px">Chart type<select id="pgType">${typeOpts}</select></label>
        <label class="field" style="flex:1 1 160px">Theme<select id="pgTheme">${themeOpts}</select></label>
        <label class="field" style="flex:1 1 160px">Render mode<select id="pgMode">${modeOpts}</select></label>
      </div>
    </div>
    <div class="grid" style="grid-template-columns: minmax(320px, 420px) 1fr; align-items:start">
      <div class="card">
        <h3>Data</h3>
        <p class="sub">Compose the chart and render it server-side to SVG.</p>
        <label class="field">Title<input type="text" id="pgTitle" value="Quarterly Revenue"></label>
        <label class="field" id="pgCatsField" style="margin-top:12px">Categories <span class="hint">comma separated</span>
          <input type="text" id="pgCats" value="Q1, Q2, Q3, Q4"></label>
        <div class="row" id="pgAxisTitles" style="margin-top:12px">
          <label class="field" style="flex:1 1 140px">X-axis title<input type="text" id="pgXTitle" value=""></label>
          <label class="field" style="flex:1 1 140px">Y-axis title<input type="text" id="pgYTitle" value=""></label>
        </div>
        <p class="hint" id="pgDataFmt" style="margin:12px 0 0;display:none"></p>
        <div id="pgSeriesList"></div>
        <button class="btn subtle sm" id="pgAddSeries" style="margin-top:10px">${icon("plus")} Add series</button>
        <div class="row" style="margin-top:14px">
          <label class="field" style="flex:1 1 90px">Height<input type="number" id="pgHeight" value="360" min="150" max="900"></label>
          <label class="field" style="flex:0 0 96px">Background<input type="color" id="pgBg" value="#ffffff" style="height:38px;padding:3px"></label>
          <label class="field" style="flex:1 1 140px;flex-direction:row;align-items:center;gap:8px;font-weight:600;align-self:flex-end">
            <input type="checkbox" id="pgExport" checked style="width:auto"> Export menu
          </label>
        </div>
        <div class="row" style="margin-top:16px">
          <button class="btn" id="pgRender">${icon("play")} Render</button>
          <button class="btn blue" id="pgDownloadSvg">${icon("download")} Download SVG</button>
          <button class="btn blue" id="pgViewJson">${icon("code")} View JSON</button>
        </div>
      </div>
      <div class="card">
        <h3>Preview</h3>
        <div id="pgOut" class="chart-card" style="min-height:280px;display:flex;align-items:center;justify-content:center">
          <span class="hint">Configure a chart and click Render.</span>
        </div>
      </div>
    </div>`;

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
    $("#pgSeriesList").appendChild(wrap);
    wrap.querySelector(".sDel").onclick = () => wrap.remove();
  };
  $("#pgAddSeries").onclick = () => addSeries("Series " + (seriesCount + 1), "0, 0, 0, 0");

  // Load the starter data that matches the selected chart type into the editor.
  const applyPreset = (type) => {
    const p = STUDIO_TYPES[type] || STUDIO_DEFAULT;
    $("#pgTitle").value = p.title;
    $("#pgCats").value = p.cats || "";
    $("#pgCatsField").style.display = p.showCats === false ? "none" : "";
    const hasAxes = !STUDIO_NO_AXIS.has(type);
    $("#pgAxisTitles").style.display = hasAxes ? "" : "none";
    $("#pgXTitle").value = hasAxes ? (p.xTitle || "") : "";
    $("#pgYTitle").value = hasAxes ? (p.yTitle || "") : "";
    $("#pgDataFmt").textContent = p.hint ? `Values format: ${p.hint}` : "";
    $("#pgDataFmt").style.display = p.hint ? "" : "none";
    const hideSeries = p.kind === "sankey";
    $("#pgSeriesList").style.display = hideSeries ? "none" : "";
    $("#pgAddSeries").style.display = hideSeries ? "none" : "";
    $("#pgSeriesList").innerHTML = "";
    seriesCount = 0;
    p.series.forEach(([name, vals]) => addSeries(name, vals));
  };
  applyPreset($("#pgType").value);

  const buildOptions = () => {
    const cats = $("#pgCats").value.split(",").map(s => s.trim()).filter(Boolean);
    const type = $("#pgType").value;
    const rows = $$("#pgSeriesList [data-series]").map(w => ({
      name: w.querySelector(".sName").value.trim() || "Series",
      tokens: w.querySelector(".sVals").value.split(",").map(s => s.trim()).filter(Boolean),
    }));
    const series = buildStudioSeries(type, rows);
    const xt = $("#pgXTitle").value.trim();
    const yt = $("#pgYTitle").value.trim();
    const xAxis = { categories: cats };
    if (xt) xAxis.title = xt;
    const options = {
      title: { text: $("#pgTitle").value },
      xAxis,
      series,
      renderMode: $("#pgMode").value,
      primaryChartType: type,
      height: +$("#pgHeight").value,
      themeName: $("#pgTheme").value,
      exportMenuEnabled: $("#pgExport").checked,
    };
    if (yt) options.yAxis = { title: yt };
    const bg = $("#pgBg").value;
    if (bg && bg.toLowerCase() !== "#ffffff") options.backgroundColor = bg;
    return options;
  };

  let lastSvg = "";
  const render = async () => {
    const out = $("#pgOut");
    out.innerHTML = loading("Rendering…");
    try {
      lastSvg = await api.text("/api/charts/render", {
        method: "POST", body: buildOptions(), accept: "image/svg+xml", query: { format: "svg" },
      });
      out.innerHTML = `<figure style="width:100%">${lastSvg}</figure>`;
      activateScripts(out);
    } catch (e) { out.innerHTML = errorBox(e); }
  };
  $("#pgRender").onclick = render;
  $("#pgViewJson").onclick = () =>
    showCodeModal("Chart options — API request payload", JSON.stringify(buildOptions(), null, 2));
  $("#pgType").onchange = () => { applyPreset($("#pgType").value); render(); };
  $("#pgTheme").onchange = render;
  $("#pgMode").onchange = render;
  $("#pgDownloadSvg").onclick = () => {
    if (!lastSvg) return toast("Render a chart first.", true);
    const blob = new Blob([lastSvg], { type: "image/svg+xml" });
    const a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = ($("#pgTitle").value || "chart").replace(/\s+/g, "-").toLowerCase() + ".svg";
    a.click();
    URL.revokeObjectURL(a.href);
  };
  render();
}
