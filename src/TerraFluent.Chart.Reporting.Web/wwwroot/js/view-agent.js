/* Ask the Agent view — single-question trace and multi-turn conversation. */
import {
  $, $$, content, state, esc, toast, loading, errorBox,
  api, analyzeRequest, requireDataset, ensureColumns, buildAgentQuestions,
  chartCard, insightRow,
} from "./core.js";

export function renderAgentView() {
  if (!requireDataset()) return;
  content.innerHTML = `
    <div class="tabs" id="agentTabs">
      <button class="tab active" data-tab="oneshot">Single question</button>
      <button class="tab" data-tab="session">Conversation</button>
    </div>
    <div data-apane="oneshot">
      <div class="card">
        <div class="titles"><h2>Ask a question</h2><p>The deterministic agent parses your question, follows the evidence and returns an explainable trace.</p></div>
        <div class="row" style="margin-top:14px">
          <input type="text" id="askQ" placeholder="e.g. why did revenue change?" style="flex:1 1 320px">
          <button class="btn" id="askBtn">Ask</button>
        </div>
        <div style="margin-top:12px">
          <span class="hint">Suggested questions for this dataset</span>
          <div class="row" id="askSuggest" style="margin-top:8px"><span class="hint"><span class="spinner"></span> Building questions from your columns\u2026</span></div>
        </div>
      </div>
      <div id="askOut"></div>
    </div>
    <div data-apane="session" hidden>
      <div class="card">
        <div class="section-head">
          <div class="titles"><h2>Multi-turn conversation</h2><p>The session remembers previous turns and resolves follow-ups like “forecast it”.</p></div>
          <button class="btn blue" id="startSession">Start / reset session</button>
        </div>
        <div class="chat" id="chatLog"></div>
        <div class="row" style="margin-top:14px">
          <input type="text" id="chatQ" placeholder="Ask a follow-up…" style="flex:1 1 320px" disabled>
          <button class="btn" id="chatBtn" disabled>Send</button>
        </div>
      </div>
    </div>`;

  $$("#agentTabs .tab").forEach(t => t.onclick = () => {
    $$("#agentTabs .tab").forEach(x => x.classList.toggle("active", x === t));
    $$("[data-apane]").forEach(p => (p.hidden = p.dataset.apane !== t.dataset.tab));
  });

  const ask = async () => {
    const q = $("#askQ").value.trim();
    const out = $("#askOut");
    out.innerHTML = loading("Investigating…");
    try {
      const r = await api.json("/api/analytics/ask", {
        method: "POST", body: analyzeRequest({ question: q || null }), query: { includeSvg: true },
      });
      out.innerHTML = `
        <div class="row" style="justify-content:flex-end;margin-bottom:12px">
          <button class="btn blue" id="exportAnswer">⬇ Export answer</button>
        </div>
        ${renderAsk(r)}`;
      $("#exportAnswer").onclick = () => exportAgentAnswer(r, q);
    } catch (e) { out.innerHTML = errorBox(e); }
  };
  $("#askBtn").onclick = ask;
  $("#askQ").addEventListener("keydown", e => { if (e.key === "Enter") ask(); });
  // Suggested questions are generated from the active dataset's columns.
  $("#askSuggest").addEventListener("click", e => {
    const b = e.target.closest("[data-suggest]");
    if (b) { $("#askQ").value = b.dataset.suggest; ask(); }
  });
  ensureColumns().then(() => {
    const host = $("#askSuggest");
    if (!host) return; // the view may have changed while columns loaded
    const questions = buildAgentQuestions(state.columns);
    host.innerHTML = questions.length
      ? questions.map(q => `<button class="btn subtle sm" data-suggest="${esc(q)}">${esc(q)}</button>`).join("")
      : `<span class="hint">Type a question about your data above to begin.</span>`;
  }).catch(() => {
    const host = $("#askSuggest");
    if (host) host.innerHTML = `<span class="hint">Type a question about your data above to begin.</span>`;
  });

  $("#startSession").onclick = async () => {
    try {
      const r = await api.json("/api/analytics/sessions", { method: "POST", body: analyzeRequest() });
      state.session = { id: r.sessionId, turns: [] };
      $("#chatLog").innerHTML = `<div class="bubble agent"><strong>Session ready</strong><span class="narr">${esc(r.summary?.headline || "Ask a question to begin.")}</span></div>`;
      $("#chatQ").disabled = false; $("#chatBtn").disabled = false; $("#chatQ").focus();
      toast("Session started.");
    } catch (e) { toast(e.message, true); }
  };

  const send = async () => {
    if (!state.session) return;
    const q = $("#chatQ").value.trim();
    if (!q) return;
    const log = $("#chatLog");
    log.insertAdjacentHTML("beforeend", `<div class="bubble user">${esc(q)}</div>`);
    $("#chatQ").value = "";
    const pending = document.createElement("div");
    pending.className = "bubble agent";
    pending.innerHTML = loading("Thinking…");
    log.appendChild(pending);
    log.scrollTop = log.scrollHeight;
    try {
      const r = await api.json(`/api/analytics/sessions/${state.session.id}/ask`, {
        method: "POST", body: { question: q },
      });
      pending.innerHTML = `<strong>${esc(r.headline)}</strong><span class="narr">${esc(r.narrative)}</span>${
        (r.steps && r.steps.length) ? `<span class="chips">${r.steps.map(s => `<em>${esc(s.skill)}</em>`).join("")}</span>` : ""
      }`;
    } catch (e) { pending.innerHTML = `<span style="color:var(--adp-red-dark)">${esc(e.message)}</span>`; }
    log.scrollTop = log.scrollHeight;
  };
  $("#chatBtn").onclick = send;
  $("#chatQ").addEventListener("keydown", e => { if (e.key === "Enter") send(); });
}

function renderAsk(r) {
  const steps = (r.steps || []).map(s => `
    <li><span class="skill">${esc(s.skill)}</span><span class="rationale">${esc(s.rationale)}</span>
    ${s.trigger ? `<span class="trigger">↳ ${esc(s.trigger)}</span>` : ""}</li>`).join("");
  const charts = (r.charts || []).map(c => chartCard(c.svg, c.title, c.reason, c.suitabilityScore)).join("");
  return `
    <div class="card">
      <span class="badge muted">${esc(r.goal)}</span>
      <h2 style="margin:10px 0 4px">${esc(r.headline)}</h2>
      <p class="sub">${esc(r.narrative)}</p>
      ${steps ? `<h3>Reasoning path</h3><ul class="steps">${steps}</ul>` : ""}
    </div>
    ${(r.insights && r.insights.length) ? `<div class="card"><h3>Insights</h3>${r.insights.map(insightRow).join("")}</div>` : ""}
    ${charts ? `<div class="card"><h3>Supporting charts</h3><div class="chart-stack">${charts}</div></div>` : ""}`;
}

// Builds a self-contained HTML page from the agent answer and downloads it client-side
// (no server round-trip, so no data leaves the browser).
function exportAgentAnswer(r, question) {
  const stripScripts = svg => String(svg || "").replace(/<script[\s\S]*?<\/script>/gi, "");
  const steps = (r.steps || []).map(s =>
    `<li><span class="skill">${esc(s.skill)}</span> ${esc(s.rationale)}${s.trigger ? ` <em>↳ ${esc(s.trigger)}</em>` : ""}</li>`).join("");
  const insights = (r.insights || []).map(i => {
    const anomaly = String(i.kind ?? "").toLowerCase() === "anomaly";
    return `<li><span class="score${anomaly ? " anomaly" : ""}">${i.importanceScore ?? ""}</span>
      <div><strong>${esc(i.title)}</strong><span>${esc(i.description)}</span></div></li>`;
  }).join("");
  const charts = (r.charts || []).map(c =>
    `<figure class="chart"><h4>${esc(c.title || "")}</h4>${stripScripts(c.svg)}${c.reason ? `<figcaption>${esc(c.reason)}</figcaption>` : ""}</figure>`).join("");
  const dsName = state.dataset?.name || "Dataset";

  // Prefer the question for the page title and file name; fall back to the answer headline.
  const q = (question || "").trim();
  const pageTitle = q || r.headline || `${dsName} — Agent Answer`;
  const slugSource = q || r.headline || dsName;
  const fileSlug = (slugSource.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "").slice(0, 60) || "agent-answer");

  const html = `<!DOCTYPE html>
<html lang="en"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>${esc(pageTitle)}</title>
<style>
  * { box-sizing: border-box; }
  body { margin: 0; font-family: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif; color: #1e293b; background: #f1f5f9; }
  header { background: #5b21b6; color: #fff; padding: 26px 32px; }
  header .goal { display: inline-block; background: rgba(255,255,255,.18); font-size: 12px; font-weight: 600; padding: 3px 10px; border-radius: 999px; }
  header h1 { margin: 12px 0 6px; font-size: 22px; }
  header p { margin: 0; color: rgba(255,255,255,.85); font-size: 14px; max-width: 900px; }
  main { padding: 26px 32px; max-width: 1100px; margin: 0 auto; }
  section { background: #fff; border-radius: 14px; padding: 20px 22px; margin-bottom: 22px; box-shadow: 0 1px 3px rgba(0,0,0,.08); }
  h2, h3 { color: #334155; margin: 0 0 14px; font-size: 16px; }
  ul.steps { list-style: none; margin: 0; padding: 0; }
  ul.steps li { padding: 8px 0; border-bottom: 1px solid #eef2f7; font-size: 14px; }
  ul.steps .skill { display: inline-block; background: #ede9fe; color: #5b21b6; font-weight: 700; font-size: 12px; padding: 2px 9px; border-radius: 999px; margin-right: 8px; }
  ul.steps em { color: #64748b; font-style: normal; }
  ul.insights { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 10px; }
  ul.insights li { display: flex; gap: 12px; align-items: flex-start; background: #f8fafc; border-radius: 12px; padding: 12px 14px; }
  ul.insights .score { flex: 0 0 auto; min-width: 34px; text-align: center; background: #5b21b6; color: #fff; font-weight: 700; font-size: 13px; border-radius: 8px; padding: 4px 10px; }
  ul.insights .score.anomaly { background: #ef4444; }
  ul.insights strong { display: block; }
  ul.insights span { color: #64748b; font-size: 14px; }
  .chart-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(420px, 1fr)); gap: 20px; }
  figure.chart { margin: 0; background: #f8fafc; border-radius: 12px; padding: 16px; }
  figure.chart h4 { margin: 0 0 10px; font-size: 14px; color: #334155; }
  figure.chart svg { width: 100%; height: auto; }
  figure.chart figcaption { margin-top: 10px; font-size: 13px; color: #64748b; }
  footer { text-align: center; padding: 20px; color: #94a3b8; font-size: 13px; }
</style></head><body>
<header>
  <span class="goal">${esc(r.goal || "Investigation")}</span>
  <h1>${esc(r.headline || "")}</h1>
  <p>${esc(r.narrative || "")}</p>
</header>
<main>
  ${steps ? `<section><h2>Reasoning path</h2><ul class="steps">${steps}</ul></section>` : ""}
  ${insights ? `<section><h2>Insights</h2><ul class="insights">${insights}</ul></section>` : ""}
  ${charts ? `<section><h2>Supporting charts</h2><div class="chart-grid">${charts}</div></section>` : ""}
</main>
<footer>Generated by TerraFluent.AutoAnalytics — deterministic, no AI.</footer>
</body></html>`;

  const blob = new Blob([html], { type: "text/html" });
  const a = document.createElement("a");
  a.href = URL.createObjectURL(blob);
  a.download = fileSlug + ".html";
  document.body.appendChild(a); a.click(); document.body.removeChild(a);
  setTimeout(() => URL.revokeObjectURL(a.href), 1000);
  toast("Answer exported.");
}
