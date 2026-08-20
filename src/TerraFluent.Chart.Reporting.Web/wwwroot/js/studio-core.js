/* Studio core — chart-type descriptors and series shaping shared by the
   Chart Studio page and the Catalogue playground. */

export const STUDIO_PALETTE = ["#2563eb", "#16a34a", "#f59e0b", "#dc2626", "#7c3aed", "#0891b2", "#db2777", "#65a30d"];

// Fixed nodes/links sample — Sankey data is structural and cannot be typed into the value editor.
export const SANKEY_SAMPLE = {
  name: "Flow", type: "Sankey",
  sankeyNodes: [{ name: "Visits" }, { name: "Signups" }, { name: "Trials" }, { name: "Paid" }, { name: "Churned" }],
  sankeyLinks: [{ from: 0, to: 1, value: 600 }, { from: 1, to: 2, value: 380 }, { from: 2, to: 3, value: 180 }, { from: 2, to: 4, value: 120 }],
};

// Per-chart-type descriptor: starter data + the data shape each type expects.
//   kind "simple"     -> series.data (plain numbers)
//   kind "range"      -> series.rangeData, each value token "low/high"
//   kind "box"        -> series.boxPlotData, token "low/q1/median/q3/high"
//   kind "ohlc"       -> series.ohlcData, token "open/high/low/close"
//   kind "bubble"     -> series.bubbleData, token "x/y/size"
//   kind "heatmap"    -> one series; each editor row = a heatmap row, values = its cells
//   kind "parliament" -> one series; each editor row = a party, value = seat count
//   kind "gantt"      -> one series; each editor row = a task, value token "start/end"
//   kind "sankey"     -> fixed structural sample (SANKEY_SAMPLE)
export const STUDIO_TYPES = {
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

export const STUDIO_DEFAULT = { title: "Sample Chart", kind: "simple", xTitle: "Category", yTitle: "Value", cats: "Q1, Q2, Q3, Q4", series: [["Series 1", "120, 150, 170, 210"]] };

// Minimum dataset shape each chart type needs to be generated from aggregated dataset values.
// Only "simple" category-vs-measure charts are listed — range/box/ohlc/bubble/heatmap/gantt/sankey
// need structured tuple data the dataset flow can't produce, so they are never dataset-feasible.
// m = numeric measures, d = grouping dimensions (Category/Boolean/Text), axis = any labelling
// column (dimension OR date). Missing keys = no minimum.
export const STUDIO_TYPE_REQUIREMENTS = {
  Line:      { m: 1, axis: 1 },
  Spline:    { m: 1, axis: 1 },
  Area:      { m: 1, axis: 1 },
  Stream:    { m: 1, axis: 1 },
  Column:    { m: 1, axis: 1 },
  Bar:       { m: 1, axis: 1 },
  Waterfall: { m: 1, axis: 1 },
  Scatter:   { m: 1, axis: 1 },
  Pie:       { m: 1, d: 1 },
  Funnel:    { m: 1, d: 1 },
  Treemap:   { m: 1, d: 1 },
  Radar:     { m: 1, d: 1 },
  Gauge:     { m: 1 },
  DataRing:  { m: 1 },
};

// Returns the Set of chart-type names the given dataset shape can produce.
export function feasibleChartTypes({ measures = 0, dims = 0, dates = 0 } = {}) {
  const axis = dims + dates;
  const ok = new Set();
  for (const [type, req] of Object.entries(STUDIO_TYPE_REQUIREMENTS)) {
    if ((req.m || 0) > measures) continue;
    if ((req.d || 0) > dims) continue;
    if ((req.axis || 0) > axis) continue;
    ok.add(type);
  }
  return ok;
}

// Types with no cartesian value axes — the axis-title inputs are hidden for these.
export const STUDIO_NO_AXIS = new Set(["Pie", "Funnel", "Treemap", "Radar", "Gauge", "DataRing", "Heatmap", "Parliament", "Sankey"]);

// Types that support stacking (multi-series cartesian).
export const STUDIO_STACKABLE = new Set(["Column", "Bar", "Area", "Line"]);

// Turns the editor rows into the correctly shaped series payload for the chosen chart type.
export function buildStudioSeries(type, rows) {
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
      return rows.map(r => {
        const data = r.tokens.map(numOf);
        const s = { name: r.name, type, data };
        // Explicit total flags for Waterfall: last bar is the cumulative total, the rest are
        // absolute/delta bars — so the renderer never has to guess (avoids hiding the first bar).
        if (type === "Waterfall") s.waterfallTotals = data.map((_, i) => i === data.length - 1);
        return s;
      });
  }
}

// The data "kind" for a chart type (simple/range/box/ohlc/bubble/heatmap/parliament/gantt/sankey).
export function studioKind(type) {
  return (STUDIO_TYPES[type] || STUDIO_DEFAULT).kind;
}

// Inverse of buildStudioSeries — turns an options.series[] payload back into editor rows
// [{ name, vals }] so a loaded/shared chart repopulates the composer. Returns null for sankey.
export function seriesToRows(type, series) {
  const kind = studioKind(type);
  const j = arr => (arr || []).join(", ");
  const s = series || [];
  switch (kind) {
    case "range":
      return s.map(x => ({ name: x.name, vals: j((x.rangeData || []).map(d => `${d.low}/${d.high}`)) }));
    case "box":
      return s.map(x => ({ name: x.name, vals: j((x.boxPlotData || []).map(d => `${d.low}/${d.q1}/${d.median}/${d.q3}/${d.high}`)) }));
    case "ohlc":
      return s.map(x => ({ name: x.name, vals: j((x.ohlcData || []).map(d => `${d.open}/${d.high}/${d.low}/${d.close}`)) }));
    case "bubble":
      return s.map(x => ({ name: x.name, vals: j((x.bubbleData || []).map(d => `${d.x}/${d.y}/${d.z}`)) }));
    case "heatmap": {
      const one = s[0] || {};
      const labels = one.heatmapRowLabels || [];
      const cells = one.heatmapData || [];
      const cols = Math.max(0, ...cells.map(c => c.col + 1));
      return labels.map((name, ri) => {
        const tokens = [];
        for (let ci = 0; ci < cols; ci++) {
          const cell = cells.find(c => c.row === ri && c.col === ci);
          tokens.push(cell ? cell.value : 0);
        }
        return { name, vals: j(tokens) };
      });
    }
    case "parliament":
      return ((s[0] || {}).parliamentData || []).map(p => ({ name: p.name, vals: String(p.seats) }));
    case "gantt":
      return ((s[0] || {}).ganttData || []).map(g => ({ name: g.name, vals: `${g.start}/${g.end}` }));
    case "sankey":
      return null;
    default:
      return s.map(x => ({ name: x.name, vals: j(x.data) }));
  }
}

