/* Chart Studio view — compose any chart type and render it server-side to SVG. */
import {
  $, $$, content, state, esc, toast, loading, errorBox,
  api, ensureCatalogue, activateScripts, showCodeModal,
} from "./core.js";

const STUDIO_PALETTE = ["#2563eb", "#16a34a", "#f59e0b", "#dc2626", "#7c3aed", "#0891b2", "#db2777", "#65a30d"];
// Fixed nodes/links sample — Sankey data is structural and cannot be typed into the value editor.
const SANKEY_SAMPLE = {
  name: "Flow", type: "Sankey",
  sankeyNodes: [{ name: "Visits" }, { name: "Signups" }, { name: "Trials" }, { name: "Paid" }, { name: "Churned" }],
  sankeyLinks: [{ from: 0, to: 1, value: 600 }, { from: 1, to: 2, value: 380 }, { from: 2, to: 3, value: 180 }, { from: 2, to: 4, value: 120 }],
};

// Per-chart-type descriptor: starter data + the data shape each type expects.
//   kind "simple"     → series.data (plain numbers)
//   kind "range"      → series.rangeData, each value token "low/high"
//   kind "box"        → series.boxPlotData, token "low/q1/median/q3/high"
//   kind "ohlc"       → series.ohlcData, token "open/high/low/close"
//   kind "bubble"     → series.bubbleData, token "x/y/size"
//   kind "heatmap"    → one series; each editor row = a heatmap row, values = its cells
//   kind "parliament" → one series; each editor row = a party, value = seat count
//   kind "gantt"      → one series; each editor row = a task, value token "start/end"
//   kind "sankey"     → fixed structural sample (SANKEY_SAMPLE)
const STUDIO_TYPES = {
  Line:        { title: "Monthly Revenue",     kind: "simple", xTitle: "Month", yTitle: "Amount", cats: "Jan, Feb, Mar, Apr, May, Jun", series: [["Revenue", "120, 150, 170, 140, 190, 210"], ["Cost", "80, 90, 110, 95, 120, 130"]] },
  Spline:      { title: "Temperature Trend",   kind: "simple", xTitle: "Day", yTitle: "Temperature (°C)", cats: "Mon, Tue, Wed, Thu, Fri",       series: [["City A", "18, 21, 19, 24, 22"], ["City B", "14, 16, 15, 18, 17"]] },
  Area:        { title: "Traffic Over Time",   kind: "simple", xTitle: "Week", yTitle: "Visits", cats: "Week 1, Week 2, Week 3, Week 4", series: [["Visits", "1200, 1800, 1500, 2100"]] },
  Stream:      { title: "Topic Volume",        kind: "simple", xTitle: "Month", yTitle: "Volume", cats: "Jan, Feb, Mar, Apr, May, Jun",  series: [["Sports", "30, 45, 38, 52, 60, 48"], ["News", "50, 42, 55, 40, 47, 53"], ["Music", "20, 28, 24, 33, 30, 36"]] },
  Column:      { title: "Quarterly Sales",     kind: "simple", xTitle: "Quarter", yTitle: "Sales", cats: "Q1, Q2, Q3, Q4",                series: [["2024", "120, 150, 170, 210"], ["2025", "140, 160, 200, 240"]] },
  Bar:         { title: "Product Ranking",     kind: "simple", xTitle: "Product", yTitle: "Units", cats: "Alpha, Bravo, Charlie, Delta",  series: [["Units", "320, 280, 190, 150"]] },
  Pie:         { title: "Market Share",        kind: "simple", cats: "Chrome, Safari, Edge, Firefox, Other", series: [["Share", "63, 19, 5, 3, 10"]] },
  Funnel:      { title: "Sales Funnel",        kind: "simple", cats: "Visits, Leads, Trials, Paid",   series: [["Count", "1000, 600, 300, 120"]] },
  Treemap:     { title: "Storage Usage",       kind: "simple", cats: "Docs, Media, Apps, System, Free", series: [["GB", "45, 120, 30, 25, 60"]] },
  Radar:       { title: "Skill Assessment",    kind: "simple", cats: "Speed, Power, Range, Defense, Focus", series: [["Player A", "80, 70, 90, 60, 85"], ["Player B", "65, 85, 70, 75, 60"]] },
  Scatter:     { title: "Sample Distribution", kind: "simple", xTitle: "Sample", yTitle: "Value", cats: "A, B, C, D, E, F",              series: [["Sample", "60, 72, 65, 80, 90, 55"]] },
  Waterfall:   { title: "Cash Flow",           kind: "simple", xTitle: "Stage", yTitle: "Amount", cats: "Start, Sales, Refunds, Costs, End", series: [["Amount", "100, 80, -20, -30, 130"]] },
  Gauge:       { title: "CPU Load",            kind: "simple", showCats: false, cats: "Load",         series: [["Value", "72"]] },
  DataRing:    { title: "Goal Progress",       kind: "simple", showCats: false, cats: "Progress",     series: [["Value", "68"]] },

  ColumnRange: { title: "Daily Temp Range",    kind: "range", xTitle: "Day", yTitle: "Temperature (°C)", hint: "low/high per point", cats: "Mon, Tue, Wed, Thu, Fri", series: [["Temp °C", "8/18, 10/21, 7/17, 9/20, 11/23"]] },
  AreaRange:   { title: "Forecast Band",       kind: "range", xTitle: "Day", yTitle: "Value", hint: "low/high per point", cats: "Mon, Tue, Wed, Thu, Fri", series: [["Range", "8/18, 10/21, 7/17, 9/20, 11/23"]] },
  Dumbbell:    { title: "Before vs After",     kind: "range", xTitle: "Team", yTitle: "Score", hint: "start/end per point", cats: "Team A, Team B, Team C, Team D", series: [["Change", "30/55, 42/60, 25/48, 38/52"]] },
  ErrorBar:    { title: "Measurement Error",   kind: "range", xTitle: "Sample", yTitle: "Measurement", hint: "low/high per point", cats: "S1, S2, S3, S4, S5", series: [["Range", "18/24, 20/27, 15/22, 22/29, 19/25"]] },

  BoxPlot:     { title: "Score Distribution",  kind: "box", xTitle: "Group", yTitle: "Score", hint: "low/q1/median/q3/high per point", cats: "Group A, Group B, Group C", series: [["Scores", "5/10/15/20/25, 8/13/17/22/28, 6/11/14/19/24"]] },

  Candlestick: { title: "Price Action",        kind: "ohlc", xTitle: "Day", yTitle: "Price", hint: "open/high/low/close per point", cats: "Mon, Tue, Wed, Thu, Fri", series: [["ACME", "120/130/110/125, 125/135/120/130, 130/138/126/128, 128/132/118/121, 121/129/119/127"]] },
  Ohlc:        { title: "Price Action",        kind: "ohlc", xTitle: "Day", yTitle: "Price", hint: "open/high/low/close per point", cats: "Mon, Tue, Wed, Thu, Fri", series: [["ACME", "120/130/110/125, 125/135/120/130, 130/138/126/128, 128/132/118/121, 121/129/119/127"]] },

  Bubble:      { title: "Product Mix",         kind: "bubble", showCats: false, xTitle: "Price", yTitle: "Rating", hint: "x/y/size per point", cats: "", series: [["Line A", "10/20/5, 30/40/12, 25/15/8, 45/35/20"], ["Line B", "15/32/9, 38/22/14, 50/48/6, 22/40/18"]] },

  Heatmap:     { title: "Activity Heatmap",    kind: "heatmap", hint: "one number per column", cats: "Mon, Tue, Wed, Thu, Fri", series: [["Morning", "5, 7, 3, 8, 6"], ["Afternoon", "9, 4, 6, 7, 5"], ["Evening", "2, 8, 5, 4, 9"]] },

  Parliament:  { title: "Seats by Party",      kind: "parliament", showCats: false, hint: "one seat count per party", cats: "", series: [["Progressive", "120"], ["Liberal", "95"], ["Conservative", "140"], ["Independent", "20"]] },

  Gantt:       { title: "Project Schedule",    kind: "gantt", showCats: false, xTitle: "Timeline (days)", hint: "start/end (numeric)", cats: "", series: [["Design", "0/5"], ["Build", "5/14"], ["Test", "12/18"], ["Launch", "18/20"]] },

  Sankey:      { title: "Conversion Flow",     kind: "sankey", showCats: false, hint: "renders a fixed sample flow", cats: "", series: [["Flow", "fixed sample"]] },
};
const STUDIO_DEFAULT = { title: "Sample Chart", kind: "simple", xTitle: "Category", yTitle: "Value", cats: "Q1, Q2, Q3, Q4", series: [["Series 1", "120, 150, 170, 210"]] };
// Types with no cartesian value axes — the axis-title inputs are hidden for these.
const STUDIO_NO_AXIS = new Set(["Pie", "Funnel", "Treemap", "Radar", "Gauge", "DataRing", "Heatmap", "Parliament", "Sankey"]);

// Turns the editor rows into the correctly shaped series payload for the chosen chart type.
function buildStudioSeries(type, rows) {
  const desc = STUDIO_TYPES[type] || STUDIO_DEFAULT;
  const numOf = t => { const n = parseFloat(String(t).trim()); return Number.isNaN(n) ? null : n; };
  const tupleOf = (t, n) => {
    const p = String(t).split("/").map(x => parseFloat(x.trim()));
    return (p.length >= n && p.every(v => !Number.isNaN(v))) ? p : null;
  };
  switch (desc.kind) {
    case "range":
      return rows.map(r => ({ name: r.name, type, rangeData: r.tokens.map(t => tupleOf(t, 2)).filter(Boolean).map(([low, high]) => ({ low, high })) }));
    case "box":
      return rows.map(r => ({ name: r.name, type, boxPlotData: r.tokens.map(t => tupleOf(t, 5)).filter(Boolean).map(([low, q1, median, q3, high]) => ({ low, q1, median, q3, high })) }));
    case "ohlc":
      return rows.map(r => ({ name: r.name, type, ohlcData: r.tokens.map(t => tupleOf(t, 4)).filter(Boolean).map(([open, high, low, close]) => ({ open, high, low, close })) }));
    case "bubble":
      return rows.map(r => ({ name: r.name, type, bubbleData: r.tokens.map(t => tupleOf(t, 3)).filter(Boolean).map(([x, y, z]) => ({ x, y, z })) }));
    case "heatmap": {
      const s = { name: (rows[0] && rows[0].name) || "Heatmap", type, heatmapRowLabels: rows.map(r => r.name), heatmapData: [] };
      rows.forEach((r, ri) => r.tokens.forEach((t, ci) => { const v = numOf(t); if (v !== null) s.heatmapData.push({ col: ci, row: ri, value: v }); }));
      return [s];
    }
    case "parliament":
      return [{ name: "Parliament", type, parliamentData: rows.map((r, i) => ({ name: r.name, color: STUDIO_PALETTE[i % STUDIO_PALETTE.length], seats: Math.round(numOf(r.tokens[0]) || 0) })) }];
    case "gantt":
      return [{ name: "Schedule", type, ganttData: rows.map(r => { const p = tupleOf(r.tokens[0], 2); return p ? { name: r.name, start: p[0], end: p[1] } : null; }).filter(Boolean) }];
    case "sankey":
      return [JSON.parse(JSON.stringify(SANKEY_SAMPLE))];
    default:
      return rows.map(r => ({ name: r.name, type, data: r.tokens.map(numOf) }));
  }
}

export async function renderStudioView() {
  content.innerHTML = `<div class="card">${loading("Loading catalogue…")}</div>`;
  await ensureCatalogue();
  const cat = state.catalogue;

  const typeOpts = cat.chartTypes.map(t => `<option>${esc(t.name)}</option>`).join("");
  const modeOpts = cat.renderModes.map(m => `<option${m.name === "Interactive" ? " selected" : ""}>${esc(m.name)}</option>`).join("");
  const themeOpts = cat.themes.map(t => `<option${t === "Vivid" ? " selected" : ""}>${esc(t)}</option>`).join("");

  content.innerHTML = `
    <div class="card" style="margin-bottom:16px">
      <div class="titles" style="margin-bottom:12px"><h2>Chart Studio</h2><p>Pick a chart type and theme — the editor fills matching sample data automatically.</p></div>
      <div class="row" style="align-items:flex-end;gap:14px">
        <label class="field" style="flex:1 1 180px">Chart type<select id="cType">${typeOpts}</select></label>
        <label class="field" style="flex:1 1 160px">Theme<select id="cTheme">${themeOpts}</select></label>
        <label class="field" style="flex:1 1 160px">Render mode<select id="cMode">${modeOpts}</select></label>
      </div>
    </div>
    <div class="grid" style="grid-template-columns: minmax(320px, 420px) 1fr; align-items:start">
      <div class="card">
        <h3>Data</h3>
        <p class="sub">Compose the chart and render it server-side to SVG.</p>
        <label class="field">Title<input type="text" id="cTitle" value="Quarterly Revenue"></label>
        <label class="field" id="catsField" style="margin-top:12px">Categories <span class="hint">comma separated</span>
          <input type="text" id="cCats" value="Q1, Q2, Q3, Q4"></label>
        <div class="row" id="axisTitles" style="margin-top:12px">
          <label class="field" style="flex:1 1 140px">X-axis title<input type="text" id="cXTitle" value=""></label>
          <label class="field" style="flex:1 1 140px">Y-axis title<input type="text" id="cYTitle" value=""></label>
        </div>
        <p class="hint" id="dataFmt" style="margin:12px 0 0;display:none"></p>
        <div id="seriesList"></div>
        <button class="btn subtle sm" id="addSeries" style="margin-top:10px">+ Add series</button>
        <div class="row" style="margin-top:14px">
          <label class="field" style="flex:1 1 90px">Height<input type="number" id="cHeight" value="360" min="150" max="900"></label>
          <label class="field" style="flex:0 0 96px">Background<input type="color" id="cBg" value="#ffffff" style="height:38px;padding:3px"></label>
          <label class="field" style="flex:1 1 140px;flex-direction:row;align-items:center;gap:8px;font-weight:600;align-self:flex-end">
            <input type="checkbox" id="cExport" checked style="width:auto"> Export menu
          </label>
        </div>
        <div class="row" style="margin-top:16px">
          <button class="btn" id="renderChart">Render</button>
          <button class="btn blue" id="downloadSvg">Download SVG</button>
          <button class="btn blue" id="viewJson">View JSON</button>
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
  const addSeries = (name = "Revenue", vals = "120, 150, 170, 210") => {
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

  // Load the starter data that matches the selected chart type into the editor.
  const applyPreset = (type) => {
    const p = STUDIO_TYPES[type] || STUDIO_DEFAULT;
    $("#cTitle").value = p.title;
    $("#cCats").value = p.cats || "";
    $("#catsField").style.display = p.showCats === false ? "none" : "";
    // Axis titles apply only to cartesian charts.
    const hasAxes = !STUDIO_NO_AXIS.has(type);
    $("#axisTitles").style.display = hasAxes ? "" : "none";
    $("#cXTitle").value = hasAxes ? (p.xTitle || "") : "";
    $("#cYTitle").value = hasAxes ? (p.yTitle || "") : "";
    $("#dataFmt").textContent = p.hint ? `Values format: ${p.hint}` : "";
    $("#dataFmt").style.display = p.hint ? "" : "none";
    // Sankey data is structural (nodes + links), so the value editor does not apply.
    const hideSeries = p.kind === "sankey";
    $("#seriesList").style.display = hideSeries ? "none" : "";
    $("#addSeries").style.display = hideSeries ? "none" : "";
    $("#seriesList").innerHTML = "";
    seriesCount = 0;
    p.series.forEach(([name, vals]) => addSeries(name, vals));
  };
  applyPreset($("#cType").value);

  // Assemble the /api/charts/render request payload from the current editor state.
  const buildStudioOptions = () => {
    const cats = $("#cCats").value.split(",").map(s => s.trim()).filter(Boolean);
    const type = $("#cType").value;
    const rows = $$("#seriesList [data-series]").map(w => ({
      name: w.querySelector(".sName").value.trim() || "Series",
      tokens: w.querySelector(".sVals").value.split(",").map(s => s.trim()).filter(Boolean),
    }));
    const series = buildStudioSeries(type, rows);
    const xt = $("#cXTitle").value.trim();
    const yt = $("#cYTitle").value.trim();
    const xAxis = { categories: cats };
    if (xt) xAxis.title = xt;
    const options = {
      title: { text: $("#cTitle").value },
      xAxis,
      series,
      renderMode: $("#cMode").value,
      primaryChartType: type,
      height: +$("#cHeight").value,
      themeName: $("#cTheme").value,
      exportMenuEnabled: $("#cExport").checked,
    };
    if (yt) options.yAxis = { title: yt };
    // Only override the theme background when the user picks a non-default colour,
    // otherwise let the selected theme control its own background.
    const bg = $("#cBg").value;
    if (bg && bg.toLowerCase() !== "#ffffff") options.backgroundColor = bg;
    return options;
  };

  let lastSvg = "";
  const render = async () => {
    const out = $("#studioOut");
    out.innerHTML = loading("Rendering…");
    try {
      const options = buildStudioOptions();
      lastSvg = await api.text("/api/charts/render", {
        method: "POST", body: options, accept: "image/svg+xml", query: { format: "svg" },
      });
      out.innerHTML = `<figure style="width:100%">${lastSvg}</figure>`;
      activateScripts(out);
    } catch (e) { out.innerHTML = errorBox(e); }
  };
  $("#renderChart").onclick = render;
  $("#viewJson").onclick = () =>
    showCodeModal("Chart options — API request payload", JSON.stringify(buildStudioOptions(), null, 2));
  // Selecting a chart type refills the editor with matching sample data, then re-renders.
  $("#cType").onchange = () => { applyPreset($("#cType").value); render(); };
  $("#cTheme").onchange = render;
  $("#cMode").onchange = render;
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
