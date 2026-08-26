/* ==========================================================================
   ADP Analytics & Reporting Studio — SPA entry point.
   Wires the sidebar router to each view module and boots the app.
   ========================================================================== */
import { $, $$, content, toast, setDataset, checkApi, initChartZoom, state, ensureValidation, registerNavigate } from "./core.js";
import { renderIcons } from "./icons.js";
import { SAMPLES } from "./samples.js";
import { renderDataView } from "./view-data.js";
import { renderDashboardView } from "./view-dashboard.js";
import { renderAnalyzeView } from "./view-analyze.js";
import { renderAgentView } from "./view-agent.js";
import { renderAggregateView } from "./view-aggregate.js";
import { renderCompareView } from "./view-compare.js";
import { renderQualityView } from "./view-quality.js";
import { renderStudioView } from "./view-studio.js";
import { renderCatalogueView } from "./view-catalogue.js";
import { renderGuideView } from "./view-guide.js";
import { renderSettingsView } from "./view-settings.js";

const VIEWS = {
  data:      { title: "Data Source",   render: renderDataView },
  dashboard: { title: "Dashboard",     render: renderDashboardView },
  analyze:   { title: "Analyze",       render: renderAnalyzeView },
  agent:     { title: "Ask the Agent", render: renderAgentView },
  aggregate: { title: "Aggregate",     render: renderAggregateView },
  compare:   { title: "Compare",       render: renderCompareView },
  quality:   { title: "Data Quality",  render: renderQualityView },
  studio:    { title: "Chart Studio",  render: renderStudioView },
  catalogue: { title: "Catalogue",     render: renderCatalogueView },
  guide:     { title: "User Guide",    render: renderGuideView },
  settings:  { title: "Settings",      render: renderSettingsView },
};

// ── Router ──────────────────────────────────────────────────────────────
// Views that consume the active dataset. Opening one requires a dataset (else divert to Data
// Source) and, on first entry after loading data, a clean bill of health (else divert to Data
// Quality so issues are reviewed before running analytics).
const GUARDED = new Set(["dashboard", "analyze", "agent", "aggregate", "compare", "studio"]);
let current = "data";
async function setView(name) {
  if (!VIEWS[name]) name = "data";

  if (GUARDED.has(name)) {
    if (!state.dataset) {
      toast("Load a dataset first.", true);
      name = "data";
    } else if (!state.qualityAcknowledged) {
      const v = await ensureValidation();
      if (v && !v.isClean) {
        toast("Review the data-quality issues before continuing.", true);
        name = "quality";
      }
    }
  }
  if (name === "quality") state.qualityAcknowledged = true;

  current = name;
  $$(".nav-item").forEach(b => b.classList.toggle("active", b.dataset.view === name));
  $("#viewTitle").textContent = VIEWS[name].title;
  VIEWS[name].render();
  if (window.matchMedia("(max-width: 860px)").matches) $("#sidebar").classList.add("collapsed");
}

// ── Boot ────────────────────────────────────────────────────────────────
function init() {
  renderIcons();
  registerNavigate(setView);
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
