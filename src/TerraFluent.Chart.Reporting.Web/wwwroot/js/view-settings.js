/* Settings view — global chart preferences and app options, persisted to the
   browser's localStorage. These defaults seed the Chart Studio editor. */
import {
  $, content, state, esc, toast, loading, errorBox,
  ensureCatalogue, settings, saveSettings, resetSettings, DEFAULT_SETTINGS,
} from "./core.js";
import { icon } from "./icons.js";

const LEGEND_POSITIONS = ["Bottom", "Top", "Right", "Left", "None"];
const AGGREGATIONS = ["Sum", "Average", "Count", "Min", "Max", "Median"];

export async function renderSettingsView() {
  content.innerHTML = `<div class="card">${loading("Loading settings…")}</div>`;
  try {
    await ensureCatalogue();
  } catch (e) { content.innerHTML = errorBox(e); return; }
  const cat = state.catalogue;

  const opts = (arr, sel) => arr.map(v =>
    `<option${v === sel ? " selected" : ""}>${esc(v)}</option>`).join("");
  const typeOpts = cat.chartTypes.map(t =>
    `<option${t.name === settings.defaultChartType ? " selected" : ""}>${esc(t.name)}</option>`).join("");
  const modeOpts = cat.renderModes.map(m =>
    `<option${m.name === settings.defaultRenderMode ? " selected" : ""}>${esc(m.name)}</option>`).join("");
  const check = (on) => on ? " checked" : "";

  content.innerHTML = `
    <div class="card" style="margin-bottom:16px">
      <div class="section-head" style="margin-bottom:4px">
        <div class="titles"><h2>Settings</h2><p>Global preferences that seed new charts. Saved in this browser only — never on a server.</p></div>
      </div>
    </div>

    <div class="grid" style="grid-template-columns:repeat(auto-fit,minmax(340px,1fr));align-items:start;gap:16px">
      <div class="card">
        <h3>Chart defaults</h3>
        <div class="row" style="margin-top:12px;gap:12px">
          <label class="field" style="flex:1 1 150px">Default chart type<select id="setType">${typeOpts}</select></label>
          <label class="field" style="flex:1 1 150px">Default theme<select id="setTheme">${opts(cat.themes, settings.defaultTheme)}</select></label>
        </div>
        <div class="row" style="margin-top:12px;gap:12px">
          <label class="field" style="flex:1 1 150px">Default render mode<select id="setMode">${modeOpts}</select></label>
          <label class="field" style="flex:1 1 150px">Legend position<select id="setLegend">${opts(LEGEND_POSITIONS, settings.legendPosition)}</select></label>
        </div>
        <div class="row" style="margin-top:12px;gap:12px">
          <label class="field" style="flex:1 1 90px">Width<input type="number" id="setWidth" min="200" max="1600" value="${esc(settings.defaultWidth)}"></label>
          <label class="field" style="flex:1 1 90px">Height<input type="number" id="setHeight" min="150" max="900" value="${esc(settings.defaultHeight)}"></label>
        </div>
        <div class="row" style="margin-top:14px;gap:16px;flex-wrap:wrap">
          <label class="field" style="flex-direction:row;align-items:center;gap:8px;font-weight:600">
            <input type="checkbox" id="setGrid" style="width:auto"${check(settings.showGridLines)}> Show grid lines
          </label>
          <label class="field" style="flex-direction:row;align-items:center;gap:8px;font-weight:600">
            <input type="checkbox" id="setExport" style="width:auto"${check(settings.showExportMenu)}> Export menu
          </label>
          <label class="field" style="flex-direction:row;align-items:center;gap:8px;font-weight:600">
            <input type="checkbox" id="setLabels" style="width:auto"${check(settings.showDataLabels)}> Data labels
          </label>
        </div>
      </div>

      <div class="card">
        <h3>Data &amp; behaviour</h3>
        <div class="row" style="margin-top:12px;gap:12px">
          <label class="field" style="flex:1 1 150px">Default aggregation<select id="setAgg">${opts(AGGREGATIONS, settings.defaultAggregation)}</select></label>
        </div>
        <div class="row" style="margin-top:14px">
          <label class="field" style="flex-direction:row;align-items:center;gap:8px;font-weight:600">
            <input type="checkbox" id="setPrefetch" style="width:auto"${check(settings.autoPrefetchStudio)}>
            Auto-load a summarized chart when opening Chart Studio
          </label>
        </div>
        <p class="hint" style="margin-top:14px">Preferences are stored in this browser's local storage. Clearing site data resets them.</p>
      </div>
    </div>

    <div class="card" style="margin-top:16px">
      <div class="row" style="gap:10px;flex-wrap:wrap">
        <button class="btn blue" id="setSave">${icon("save")} Save settings</button>
        <button class="btn subtle" id="setReset">${icon("refresh")} Reset to defaults</button>
      </div>
    </div>`;

  const collect = () => ({
    defaultChartType:   $("#setType").value,
    defaultTheme:       $("#setTheme").value,
    defaultRenderMode:  $("#setMode").value,
    legendPosition:     $("#setLegend").value,
    defaultAggregation: $("#setAgg").value,
    defaultWidth:       clamp(+$("#setWidth").value, 200, 1600, DEFAULT_SETTINGS.defaultWidth),
    defaultHeight:      clamp(+$("#setHeight").value, 150, 900, DEFAULT_SETTINGS.defaultHeight),
    showGridLines:      $("#setGrid").checked,
    showExportMenu:     $("#setExport").checked,
    showDataLabels:     $("#setLabels").checked,
    autoPrefetchStudio: $("#setPrefetch").checked,
  });

  $("#setSave").onclick = () => {
    saveSettings(collect());
    toast("Settings saved.");
  };
  $("#setReset").onclick = () => {
    resetSettings();
    renderSettingsView();
    toast("Settings reset to defaults.");
  };
}

function clamp(n, min, max, fallback) {
  if (!Number.isFinite(n)) return fallback;
  return Math.min(max, Math.max(min, n));
}
