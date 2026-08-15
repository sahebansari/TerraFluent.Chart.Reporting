/* Catalogue view — capabilities exposed by the rendering engine. */
import { content, state, esc, loading, errorBox, ensureCatalogue } from "./core.js";

export async function renderCatalogueView() {
  content.innerHTML = `<div class="card">${loading("Loading catalogue…")}</div>`;
  try {
    await ensureCatalogue();
  } catch (e) { content.innerHTML = errorBox(e); return; }
  const cat = state.catalogue;
  const chips = arr => arr.map(x => `<span class="stat-pill">${esc(x)}</span>`).join("");
  const modes = cat.renderModes.map(m => `
    <div class="insight"><div class="score low">${esc(m.name[0])}</div>
    <div class="body"><strong>${esc(m.name)}</strong><p>${esc(m.description || "")}</p></div></div>`).join("");
  content.innerHTML = `
    <div class="card"><div class="titles"><h2>API Catalogue</h2><p>Capabilities exposed by the rendering engine.</p></div></div>
    <div class="grid cols-2">
      <div class="card"><h3>Chart types <span class="badge muted">${cat.chartTypes.length}</span></h3>
        <div class="stat-row">${chips(cat.chartTypes.map(t => t.name))}</div></div>
      <div class="card"><h3>Themes <span class="badge muted">${cat.themes.length}</span></h3>
        <div class="stat-row">${chips(cat.themes)}</div></div>
    </div>
    <div class="card"><h3>Render modes</h3>${modes}</div>`;
}
