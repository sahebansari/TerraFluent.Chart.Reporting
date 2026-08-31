/* Settings view — global chart preferences applied across every chart-rendering
   view (Chart Studio and the auto-generated charts). Persisted to localStorage. */
import {
  $, content, state, esc, toast, loading, errorBox,
  ensureCatalogue, settings, saveSettings, resetSettings, DEFAULT_SETTINGS,
} from "./core.js";
import { icon } from "./icons.js";

const LEGEND_POSITIONS = ["Bottom", "Top", "Right", "Left", "None"];
const FONT_SIZES = [["Small", 1.0], ["Medium", 1.2], ["Large", 1.4]];

export async function renderSettingsView() {
  content.innerHTML = `<div class="card">${loading("Loading settings…")}</div>`;
  try {
    await ensureCatalogue();
  } catch (e) { content.innerHTML = errorBox(e); return; }
  const cat = state.catalogue;

  const opts = (arr, sel) => arr.map(v =>
    `<option${v === sel ? " selected" : ""}>${esc(v)}</option>`).join("");
  const modeOpts = cat.renderModes.map(m =>
    `<option${m.name === settings.defaultRenderMode ? " selected" : ""}>${esc(m.name)}</option>`).join("");
  const fontOpts = FONT_SIZES.map(([label, v]) =>
    `<option value="${v}"${v === settings.fontScale ? " selected" : ""}>${label}</option>`).join("");
  const check = (on) => on ? " checked" : "";

  content.innerHTML = `
    <div class="card" style="margin-bottom:16px">
      <div class="section-head" style="margin-bottom:4px">
        <div class="titles"><h2>Settings</h2><p>Global chart preferences applied everywhere — the Chart Studio editor and the auto-generated charts on Analyze, Dashboard, Compare and Ask the Agent. Saved in this browser only — never on a server.</p></div>
      </div>
    </div>

    <div class="grid" style="grid-template-columns:repeat(auto-fit,minmax(340px,1fr));align-items:start;gap:16px">
      <div class="card">
        <h3>Chart style</h3>
        <div class="row" style="margin-top:12px;gap:12px">
          <label class="field" style="flex:1 1 150px">Default theme<select id="setTheme">${opts(cat.themes, settings.defaultTheme)}</select></label>
          <label class="field" style="flex:1 1 150px">Default render mode<select id="setMode">${modeOpts}</select></label>
        </div>
        <div class="row" style="margin-top:12px;gap:12px">
          <label class="field" style="flex:1 1 150px">Legend position<select id="setLegend">${opts(LEGEND_POSITIONS, settings.legendPosition)}</select></label>
          <label class="field" style="flex:1 1 150px">Text size<select id="setFontScale">${fontOpts}</select></label>
        </div>
        <div class="row" style="margin-top:12px;gap:12px">
          <label class="field" style="flex:1 1 150px">Background <span class="hint">white = use theme background</span>
            <input type="color" id="setBg" value="${esc(settings.backgroundColor)}" style="height:38px;padding:3px">
          </label>
        </div>
      </div>

      <div class="card">
        <h3>Chart display</h3>
        <div class="row" style="margin-top:12px;gap:16px;flex-wrap:wrap">
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
    defaultTheme:      $("#setTheme").value,
    defaultRenderMode: $("#setMode").value,
    legendPosition:    $("#setLegend").value,
    fontScale:         parseFloat($("#setFontScale").value) || DEFAULT_SETTINGS.fontScale,
    backgroundColor:   $("#setBg").value,
    showGridLines:     $("#setGrid").checked,
    showExportMenu:    $("#setExport").checked,
    showDataLabels:    $("#setLabels").checked,
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
