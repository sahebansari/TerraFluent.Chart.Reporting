/* Data Quality view — validation report for the active dataset. */
import {
  $, content, state, esc, loading, errorBox,
  api, analyzeRequest, requireDataset,
} from "./core.js";

export function renderQualityView() {
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
