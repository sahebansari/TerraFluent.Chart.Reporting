/* Aggregate view — group a measure by one dimension, or pivot across two. */
import {
  $, content, state, esc, fmt, loading, errorBox,
  api, analyzeRequest, requireDataset, ensureColumns,
} from "./core.js";

export async function renderAggregateView() {
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
