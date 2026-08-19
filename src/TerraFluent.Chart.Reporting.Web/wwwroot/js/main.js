/* ==========================================================================
   ADP Analytics & Reporting Studio — SPA entry point.
   Wires the sidebar router to each view module and boots the app.
   ========================================================================== */
import { $, $$, content, toast, setDataset, checkApi, initChartZoom } from "./core.js";
import { renderIcons } from "./icons.js";
import { SAMPLES } from "./samples.js";
import { renderDataView } from "./view-data.js";
import { renderDashboardView } from "./view-dashboard.js";
import { renderAnalyzeView } from "./view-analyze.js";
import { renderAgentView } from "./view-agent.js";
import { renderAggregateView } from "./view-aggregate.js";
import { renderQualityView } from "./view-quality.js";
import { renderStudioView } from "./view-studio.js";
import { renderCatalogueView } from "./view-catalogue.js";
import { renderSettingsView } from "./view-settings.js";

const VIEWS = {
  data:      { title: "Data Source",   render: renderDataView },
  dashboard: { title: "Dashboard",     render: renderDashboardView },
  analyze:   { title: "Analyze",       render: renderAnalyzeView },
  agent:     { title: "Ask the Agent", render: renderAgentView },
  aggregate: { title: "Aggregate",     render: renderAggregateView },
  quality:   { title: "Data Quality",  render: renderQualityView },
  studio:    { title: "Chart Studio",  render: renderStudioView },
  catalogue: { title: "Catalogue",     render: renderCatalogueView },
  settings:  { title: "Settings",      render: renderSettingsView },
};

// ── Router ──────────────────────────────────────────────────────────────
let current = "data";
function setView(name) {
  if (!VIEWS[name]) name = "data";
  current = name;
  $$(".nav-item").forEach(b => b.classList.toggle("active", b.dataset.view === name));
  $("#viewTitle").textContent = VIEWS[name].title;
  VIEWS[name].render();
  if (window.matchMedia("(max-width: 860px)").matches) $("#sidebar").classList.add("collapsed");
}

// ── Boot ────────────────────────────────────────────────────────────────
function init() {
  renderIcons();
  $$(".nav-item").forEach(b => b.onclick = () => setView(b.dataset.view));
  $("#menuToggle").onclick = () => $("#sidebar").classList.toggle("collapsed");
  $("#settingsBtn").onclick = () => setView("settings");
  content.addEventListener("click", e => {
    const goto = e.target.closest("[data-goto]");
    if (goto) setView(goto.dataset.goto);
  });
  checkApi();
  initChartZoom();
  setView("data");
}

document.addEventListener("DOMContentLoaded", init);
