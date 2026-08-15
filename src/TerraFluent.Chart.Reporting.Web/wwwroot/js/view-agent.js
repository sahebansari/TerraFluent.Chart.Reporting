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
      out.innerHTML = renderAsk(r);
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
