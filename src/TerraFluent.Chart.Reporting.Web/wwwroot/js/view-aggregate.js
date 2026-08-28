/* Aggregate view — group a measure by one dimension, or pivot across two. */
import {
  $, content, state, esc, fmt, loading, errorBox, toast,
  api, analyzeRequest, requireDataset, ensureColumns, aggregationsForColumns,
} from "./core.js";
import { icon } from "./icons.js";

export async function renderAggregateView() {
  if (!requireDataset()) return;
  content.innerHTML = `<div class="card">${loading("Reading columns…")}</div>`;
  await ensureColumns();

  const measures = state.columns.filter(c => ["Numeric", "Currency", "Percentage"].includes(c.type) || c.role === "Measure");
  // GroupBy needs categorical dimensions (with Labels); Date/numeric columns have none, so keep only categoricals.
  const dims = state.columns.filter(c => ["Category", "Boolean", "Text"].includes(c.type));
  const opt = (c) => `<option value="${esc(c.name)}">${esc(c.name)}</option>`;
  const dimPool = dims.length ? dims : state.columns.filter(c => c.type !== "Date");
  const measurePool = measures.length ? measures : state.columns;
  const measureCol = (name) => measurePool.find(c => c.name === name) || state.columns.find(c => c.name === name);
  // Aggregation options adapt to the chosen measure: non-additive columns (age, tenure…) drop "Sum".
  const aggOptionsHtml = (col) => {
    const allowed = aggregationsForColumns([col]);
    const def = allowed.includes("Sum") ? "Sum" : (allowed.includes("Average") ? "Average" : allowed[0]);
    return allowed.map(k => `<option${k === def ? " selected" : ""}>${k}</option>`).join("");
  };

  content.innerHTML = `
    <div class="card">
      <div class="titles"><h2>Aggregation &amp; Pivot</h2><p>Group a measure by one dimension, or pivot across two.</p></div>
      <div class="row" style="margin-top:14px">
        <label class="field" style="flex:1 1 180px">Measure
          <select id="aggMeasure">${measurePool.map(opt).join("")}</select></label>
        <label class="field" style="flex:1 1 180px">Dimension
          <select id="aggDim">${dimPool.map(opt).join("")}</select></label>
        <label class="field" style="flex:1 1 180px">Second dimension
          <select id="aggDim2"><option value="">— none (pivot off) —</option>${dimPool.map(opt).join("")}</select></label>
        <label class="field" style="flex:1 1 140px">Aggregation
          <select id="aggKind">${aggOptionsHtml(measurePool[0])}</select></label>
        <button class="btn" id="aggRun" style="align-self:flex-end">${icon("sigma")} Compute</button>
      </div>
    </div>
    <div id="aggOut"></div>`;

  // Rebuild the aggregation list whenever the measure changes so Sum can't be picked for a
  // non-additive column.
  $("#aggMeasure").onchange = () => { $("#aggKind").innerHTML = aggOptionsHtml(measureCol($("#aggMeasure").value)); };

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
      out.innerHTML = `
        <div class="row" style="justify-content:flex-end;margin-bottom:12px">
          <button class="btn blue" id="exportAgg">${icon("download")} Export result</button>
        </div>
        ${renderAggregation(r)}`;
      $("#exportAgg").onclick = () => exportAggregation(r);
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

// Heading text describing the aggregation, shared by the on-screen card and the export.
function aggHeading(r) {
  return r.isPivot
    ? `${r.aggregation} of ${r.measure} — ${r.dimension} × ${r.secondDimension}`
    : `${r.aggregation} of ${r.measure} by ${r.dimension}`;
}

// Builds a self-contained HTML page from the computed aggregation and downloads it client-side.
function exportAggregation(r) {
  let table;
  if (r.isPivot) {
    const cellMap = new Map((r.cells || []).map(c => [c.row + "||" + c.column, c.value]));
    const head = `<th></th>${(r.columnKeys || []).map(c => `<th class="num">${esc(c)}</th>`).join("")}`;
    const body = (r.rowKeys || []).map(row => `<tr><th>${esc(row)}</th>${
      (r.columnKeys || []).map(col => {
        const v = cellMap.get(row + "||" + col);
        return `<td class="num">${v == null ? "—" : fmt(v)}</td>`;
      }).join("")
    }</tr>`).join("");
    table = `<table><thead><tr>${head}</tr></thead><tbody>${body}</tbody></table>`;
  } else {
    const rows = (r.buckets || []).map(b =>
      `<tr><th>${esc(b.key)}</th><td class="num">${fmt(b.value)}</td><td class="num">${b.count}</td></tr>`).join("");
    table = `<table><thead><tr><th>${esc(r.dimension)}</th><th class="num">${esc(r.aggregation)}</th><th class="num">Count</th></tr></thead><tbody>${rows}</tbody></table>`;
  }

  const dsName = state.dataset?.name || "Dataset";
  const heading = aggHeading(r);
  const fileSlug = (heading.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "").slice(0, 60) || "aggregation");

  const html = `<!DOCTYPE html>
<html lang="en"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>${esc(heading)}</title>
<style>
  * { box-sizing: border-box; }
  body { margin: 0; font-family: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif; color: #1e293b; background: #f1f5f9; }
  header { background: #5b21b6; color: #fff; padding: 26px 32px; }
  header h1 { margin: 0 0 4px; font-size: 20px; }
  header p { margin: 0; color: rgba(255,255,255,.85); font-size: 13px; }
  main { padding: 26px 32px; max-width: 1100px; margin: 0 auto; }
  section { background: #fff; border-radius: 14px; padding: 20px 22px; box-shadow: 0 1px 3px rgba(0,0,0,.08); overflow-x: auto; }
  table { border-collapse: collapse; width: 100%; font-size: 13px; }
  th, td { padding: 10px 14px; text-align: left; border-bottom: 1px solid #eef2f7; white-space: nowrap; }
  thead th { background: #f8fafc; color: #64748b; font-size: 12px; text-transform: uppercase; letter-spacing: .03em; }
  td.num, th.num { text-align: right; font-variant-numeric: tabular-nums; }
  tbody th { font-weight: 700; }
  footer { text-align: center; padding: 20px; color: #94a3b8; font-size: 13px; }
</style></head><body>
<header>
  <h1>${esc(heading)}</h1>
  <p>${esc(dsName)}</p>
</header>
<main><section>${table}</section></main>
<footer>Generated by TerraFluent.AutoAnalytics — deterministic, no AI.</footer>
</body></html>`;

  const blob = new Blob([html], { type: "text/html" });
  const a = document.createElement("a");
  a.href = URL.createObjectURL(blob);
  a.download = fileSlug + ".html";
  document.body.appendChild(a); a.click(); document.body.removeChild(a);
  setTimeout(() => URL.revokeObjectURL(a.href), 1000);
  toast("Result exported.");
}
