/* ==========================================================================
   Guide view — an in-app, self-contained user guide. Explains every screen
   (navigation path, what it is, how to use it, key details), the common
   workflows, and the concepts/formulas the engine uses. No API calls, no
   external libraries — renders from the app's own styles.
   ========================================================================== */
import { content } from "./core.js";
import { icon } from "./icons.js";

// One screen entry: how to reach it, what it is, how to use it, and the details.
const SCREENS = [
  {
    id: "data", name: "Data Source", ico: "database",
    lead: "The entry point for every workflow.",
    nav: "Left sidebar → <strong>Data Source</strong>. The app opens here, and any data-consuming screen opened without an active dataset sends you back here first.",
    body: "Provide data by pasting it directly, uploading a file (CSV / JSON / TXT, with drag-and-drop), or picking one of the built-in samples. Give the dataset a name, choose a format (<strong>Auto-detect</strong>, CSV, or a JSON array of objects) and click <strong>Use this dataset</strong>. The first rows appear as a preview, the dataset badge in the sidebar updates, and the data is validated immediately — if it is clean you're invited straight on to Dashboard or Analyze; if not, you're routed to Data Quality.",
    points: [
      "Three ways in: <strong>paste</strong> text, <strong>upload</strong> a file (drag-and-drop supported), or load a <strong>sample</strong>.",
      "Formats: <em>Auto-detect</em> (the safe default), <em>CSV</em>, or a <em>JSON array</em> of row objects.",
      "Six samples span different shapes — Monthly Sales (24 months, one deliberate anomaly), Web Traffic (12 weeks), Headcount (JSON), and three ~5,000-row sets: E-commerce Orders, IoT Sensor Readings and Support Tickets.",
      "Loading a dataset replaces the previous one; the sidebar badge always shows what's currently active.",
      "Everything is processed <strong>in memory only</strong> and self-expires after 30 minutes — nothing is written to a database, disk or browser storage.",
    ],
  },
  {
    id: "dashboard", name: "Dashboard", ico: "grid",
    lead: "A smart dashboard, assembled automatically.",
    nav: "Left sidebar → <strong>Dashboard</strong>. Requires an active, clean dataset — otherwise the router diverts you to Data Source (no data) or Data Quality (unresolved issues) first.",
    body: "The engine inspects your dataset and lays out the charts it judged most informative, so you get a useful overview with zero configuration. Alongside the charts sit KPI tiles summarising the headline measures. Nothing here is hand-built — regenerate it any time and it re-derives from the current data.",
    points: [
      "<strong>KPI tiles</strong> summarise the leading measures; use the <em>KPIs</em> control to choose how many headline tiles appear.",
      "Automatically chosen chart mix: trend, category comparison, distribution, smoothed (moving-average), cumulative and composition charts — only the ones that suit your data.",
      "<strong>Generate dashboard</strong> rebuilds it from scratch after you change the dataset or KPI count.",
      "Charts are interactive: hover for tooltips and use the in-chart export menu for PNG / SVG / PDF.",
      "<strong>Export dashboard</strong> downloads the whole board as one self-contained HTML page you can email or archive.",
    ],
  },
  {
    id: "analyze", name: "Analyze", ico: "bar-chart",
    lead: "The full analysis report.",
    nav: "Left sidebar → <strong>Analyze</strong>. Requires an active, clean dataset.",
    body: "Where the Dashboard gives a visual overview, Analyze gives the complete written report: a headline, the key findings, every ranked insight the engine produced, the charts it recommends (each with the reason it was chosen), and a per-column profile of your data. It's the place to understand <em>why</em> the app is telling you something.",
    points: [
      "A plain-English <strong>headline</strong> and a short list of <strong>key findings</strong> up top.",
      "Ranked <strong>insights</strong>, each with a 0–100 importance score and an explanation of how it was scored.",
      "A grid of <strong>recommended charts</strong>, each carrying a suitability score and a self-explaining reason.",
      "A <strong>column profile</strong> table: type, role, distinct count, completeness, and min/max/mean/sum per measure.",
      "<strong>Export analysis</strong> produces a self-contained HTML report mirroring this page.",
    ],
  },
  {
    id: "agent", name: "Ask the Agent", ico: "message",
    lead: "Natural-language Q&A over your data.",
    nav: "Left sidebar → <strong>Ask the Agent</strong>. Requires an active, clean dataset.",
    body: "Ask a question in plain English — or pick one of the suggestions generated from your own columns — and the agent parses it into an intent, follows the evidence, and answers with an explainable reasoning trace, the supporting insights, and any charts it built along the way. It's deterministic: the same question on the same data always yields the same answer.",
    points: [
      "<strong>Single question</strong> runs a one-shot analysis; <strong>Conversation</strong> keeps context across turns.",
      "In Conversation mode, follow-ups like &ldquo;forecast it&rdquo; resolve the pronoun to the last measure discussed.",
      "<strong>Suggested questions</strong> are generated from your dataset's actual columns, so they always apply.",
      "Understood intents include trend, correlation, anomaly, dominance, forecast, root-cause, period comparison and segmentation.",
      "Every answer exposes its <strong>reasoning path</strong> — you can see exactly which steps produced the result.",
    ],
  },
  {
    id: "aggregate", name: "Aggregate", ico: "sigma",
    lead: "An interactive group-by / pivot builder.",
    nav: "Left sidebar → <strong>Aggregate</strong>. Requires an active, clean dataset.",
    body: "Summarise a measure across one or two dimensions without writing any query. Pick what to measure, what to group by, and how to combine values, then compute — the result renders as a table (or a pivot grid, when a second dimension is chosen) alongside a matching chart.",
    points: [
      "Choose a <strong>measure</strong>, a <strong>dimension</strong>, an optional <strong>second dimension</strong> (to pivot), and an <strong>aggregation</strong>.",
      "Aggregations: Sum, Average, Count, Min, Max, Median.",
      "Non-additive measures (age, rates, scores…) hide <em>Sum</em> and default to <em>Average</em>, because a grand total would be meaningless.",
      "Adding a second dimension turns the output into a pivot grid; one dimension gives a simple grouped table.",
      "<strong>Export result</strong> saves the computed table and chart.",
    ],
  },
  {
    id: "quality", name: "Data Quality", ico: "check-circle",
    lead: "The validation report for your dataset.",
    nav: "Left sidebar → <strong>Data Quality</strong>. Also reached automatically (once per dataset) when you try to run analytics on data that isn't clean.",
    body: "Before you trust the analysis, this screen tells you whether the data is sound. It runs a fixed battery of checks, grades each issue by severity, and gives an overall verdict. This is also the gate the router uses: if a dataset has warnings or errors, you're diverted here once so problems are seen before analytics run.",
    points: [
      "Overall verdict: <strong>Clean</strong>, <strong>Warnings</strong>, or <strong>Errors</strong>.",
      "Issue table columns: severity, code, column, message, and affected count.",
      "Six checks: empty / low-row datasets, missing values, duplicate rows, duplicate keys, invalid formats, and out-of-range values.",
      "Severity scales with impact — e.g. missing values are an Error at ≥50%, a Warning at ≥10%, otherwise informational.",
      "Fix the source data and re-paste, or click <strong>Validate</strong> to re-check; once Clean, continue to Dashboard / Analyze.",
    ],
  },
  {
    id: "studio", name: "Chart Studio", ico: "trending-up",
    lead: "A full chart composer.",
    nav: "Left sidebar → <strong>Chart Studio</strong>. Always available — it can start from your dataset or from a blank chart.",
    body: "Hand-craft a chart from scratch or seed it from an aggregation of your data, then tune every aspect with a live preview updating as you type. When it looks right, download it, copy it into another page, or create a share link.",
    points: [
      "<strong>Load from your dataset</strong> seeds the editor from an aggregation, or build from scratch.",
      "Choose a <strong>chart type</strong>, <strong>theme</strong> and <strong>render mode</strong> (Static / Animated / Interactive).",
      "Edit the title, categories, axis titles and one or more series; tune size, background, legend, grid lines, data labels and stacking.",
      "A <strong>live preview</strong> re-renders as you edit — no round-trips to think about.",
      "Output options: download <strong>SVG / PNG</strong>, <strong>Copy SVG/embed</strong>, or create a <strong>share link</strong>.",
    ],
  },
  {
    id: "catalogue", name: "Catalogue", ico: "list",
    lead: "A gallery of the chart library's capabilities.",
    nav: "Left sidebar → <strong>Catalogue</strong>. Always available — no dataset required.",
    body: "A visual reference of every chart type the renderer supports, each drawn from sample data. Use it to decide what to build in Chart Studio, and experiment freely in the embedded playground without touching your own data.",
    points: [
      "Every supported chart type rendered from representative sample data.",
      "An embedded <strong>playground</strong> to experiment with types, themes and options live.",
      "Handy as a decision aid — see how a type looks before composing it in Chart Studio.",
      "Works with no dataset loaded, so it's always browsable.",
    ],
  },
  {
    id: "settings", name: "Settings", ico: "settings",
    lead: "Browser-local preferences that seed new charts.",
    nav: "Left sidebar → <strong>Settings</strong>. Always available.",
    body: "Set the defaults new charts start from, so Chart Studio and the aggregate view begin the way you prefer. These are personal preferences stored in this browser only — they never touch a server and never change your data.",
    points: [
      "Defaults for chart type, theme, render mode, legend position and size.",
      "Toggles for grid lines, the export menu and data labels.",
      "The default aggregation used when grouping a measure.",
      "Whether Chart Studio auto-loads a summarised chart of your dataset on open.",
      "Stored in this browser's local storage only — never on a server.",
    ],
  },
];

// The common step-by-step workflows.
const WORKFLOWS = [
  {
    title: "Load data & read the auto-analysis",
    steps: [
      "Open the app → you land on <strong>Data Source</strong>.",
      "Click <strong>Samples → Monthly Sales → Load</strong> (or paste your own CSV/JSON).",
      "Click <strong>Use this dataset</strong> — the preview appears and the data is validated.",
      "Click <strong>Analyze</strong> in the sidebar.",
      "Read the headline, key findings, ranked insights and recommended charts.",
    ],
  },
  {
    title: "Build the smart dashboard",
    steps: [
      "With a clean dataset active, click <strong>Dashboard</strong>.",
      "Set <strong>KPIs</strong> and click <strong>Generate dashboard</strong>.",
      "Hover charts for tooltips; use the in-chart export menu for PNG/SVG/PDF.",
      "Optionally click <strong>Export dashboard</strong> for a self-contained page.",
    ],
  },
  {
    title: "Ask a question",
    steps: [
      "Click <strong>Ask the Agent</strong>.",
      "Choose <strong>Single question</strong> or <strong>Conversation</strong>.",
      "Click a suggested question (e.g. &ldquo;Why did Revenue change?&rdquo;) or type your own → <strong>Ask</strong>.",
      "Read the answer, expand the reasoning path, inspect insights and supporting charts.",
      "In Conversation mode, ask a follow-up such as &ldquo;forecast it&rdquo; — context carries over.",
    ],
  },
  {
    title: "Group / pivot a measure",
    steps: [
      "Click <strong>Aggregate</strong>.",
      "Pick a <strong>Measure</strong> and a <strong>Dimension</strong>.",
      "Optionally add a <strong>Second dimension</strong> to pivot and an <strong>Aggregation</strong>.",
      "Click <strong>Compute</strong> → read the table/pivot and chart, then <strong>Export result</strong>.",
    ],
  },
  {
    title: "Fix data-quality issues",
    steps: [
      "On <strong>Data Source</strong>, load data that has problems (a duplicate key, missing values…).",
      "Click <strong>Use this dataset</strong> → you're diverted to <strong>Data Quality</strong>.",
      "Review each issue (severity, code, column, affected count).",
      "Correct the source data and re-paste, or click <strong>Validate</strong> to re-check.",
      "Once <strong>Clean</strong>, continue to Dashboard / Analyze.",
    ],
  },
  {
    title: "Compose & share a chart",
    steps: [
      "Click <strong>Chart Studio</strong>.",
      "Click <strong>Load from your dataset</strong> (or set type/categories/series manually).",
      "Adjust theme, render mode, axis titles, size and options; watch the preview.",
      "<strong>Download PNG/SVG</strong>, <strong>Copy embed</strong>, or <strong>Share link</strong>.",
    ],
  },
];

// The seven-phase pipeline, one card each.
const PIPELINE = [
  ["1 · Schema discovery", "Samples every cell and votes on each column's type (numeric, date, category, currency, %, boolean, identifier), infers a business role, and decides whether a measure is <em>additive</em> (summable) or should be averaged."],
  ["2 · Validation", "Six checks grade issues Info / Warning / Error: empty / low-row datasets, missing values (≥50% Error, ≥10% Warning), duplicate rows, duplicate keys (Error), invalid formats, and out-of-range values."],
  ["3 · Profiling", "Full descriptive statistics per measure: mean, median, mode, variance, std-dev, quartiles/IQR, and shape (skewness, kurtosis)."],
  ["4 · Analytics", "Correlation, trend, anomaly, driver (root-cause), period-over-period, segmentation, forecasting, moving average, cumulative and composition."],
  ["5 · Insights", "Findings become ranked plain-English insights, each with a weighted, normalised 0–100 importance score."],
  ["6 · Chart recommendation", "Rule-based chart selection, each with a suitability score and a self-explaining reason."],
  ["7 · Summary", "The top insight becomes the headline; the next few become key findings."],
];

// Key formulas, rendered as plain HTML (unicode) — no math library needed.
const FORMULAS = [
  ["Mean &amp; standard deviation",
    "x&#772; = (1/n) Σ x&#7522; &nbsp;·&nbsp; s = &radic;( (1/(n&minus;1)) Σ (x&#7522; &minus; x&#772;)&sup2; )"],
  ["Percentiles (type-7, linear interpolation)",
    "rank r = (p/100)(n&minus;1); value interpolates between the order statistics at &lfloor;r&rfloor; and &lceil;r&rceil;. Median = P&#8325;&#8320;, IQR = Q&#8323; &minus; Q&#8321;."],
  ["Skewness (Fisher–Pearson), z&#7522; = (x&#7522;&minus;x&#772;)/s",
    "g&#8321; = [ n / ((n&minus;1)(n&minus;2)) ] Σ z&#7522;&sup3;"],
  ["Pearson correlation (linear)",
    "r = Σ(x&#7522;&minus;x&#772;)(y&#7522;&minus;y&#772;) / &radic;( Σ(x&#7522;&minus;x&#772;)&sup2; · Σ(y&#7522;&minus;y&#772;)&sup2; )"],
  ["Spearman correlation (monotonic)",
    "Pearson computed on the <em>ranks</em> of x and y (tied values share the average rank)."],
  ["Significance of a correlation",
    "t = r&radic;((n&minus;2)/(1&minus;r&sup2;)) with n&minus;2 degrees of freedom → two-tailed p-value; &ldquo;significant&rdquo; when p &lt; 0.05."],
  ["Trend — least squares fit quality",
    "R&sup2; = (Σ(x&#7522;&minus;x&#772;)(y&#7522;&minus;y&#772;))&sup2; / (Σ(x&#7522;&minus;x&#772;)&sup2; · Σ(y&#7522;&minus;y&#772;)&sup2;). A robust Theil–Sen slope (median of pairwise slopes) guards the direction against outliers."],
  ["Anomaly — modified z-score (robust)",
    "M&#7522; = 0.6745·(x&#7522; &minus; x&#771;) / MAD, where MAD = median|x&#7522; &minus; x&#771;|; flagged when |M&#7522;| &gt; 3.5. Also an IQR fence (Q&#8321;&minus;1.5·IQR, Q&#8323;+1.5·IQR) and a spike rule (jump &gt; 3× the median step)."],
  ["Driver — share of change",
    "Split rows into an earlier and later half; for each category &Delta; = late &minus; early, and its share of the move = &Delta; / &Delta;<sub>total</sub>."],
  ["Forecasting — Holt's linear method",
    "&#8467;&#7511; = &alpha;·x&#7511; + (1&minus;&alpha;)(&#8467;&#7511;&#8331;&#8321; + b&#7511;&#8331;&#8321;); &nbsp; b&#7511; = &beta;(&#8467;&#7511; &minus; &#8467;&#7511;&#8331;&#8321;) + (1&minus;&beta;)b&#7511;&#8331;&#8321;; &nbsp; forecast x&#770;&#7511;&#8330;&#8341; = &#8467;&#7511; + h·b&#7511;. Band ± 1.96·&sigma;·&radic;h. Holt–Winters adds a seasonal term when a season is detected."],
  ["Segmentation — deterministic k-means",
    "Features z-score standardised; k-means++ seeding under a fixed seed; k = clamp(&lfloor;n/6&rfloor;, 2, 4); tightness = within-cluster sum of squares."],
];

export function renderGuideView() {
  const screenCards = SCREENS.map(s => `
    <section class="card guide-screen" id="guide-${s.id}">
      <div class="guide-screen-head">
        <span class="guide-screen-ico">${icon(s.ico, 18)}</span>
        <div><h3>${s.name}</h3><p class="guide-lead">${s.lead}</p></div>
      </div>
      <p class="guide-nav"><span class="guide-nav-label">${icon("compass", 13)} Getting there</span> ${s.nav}</p>
      <p class="guide-text">${s.body}</p>
      <ul class="guide-points">${s.points.map(p => `<li>${p}</li>`).join("")}</ul>
    </section>`).join("");

  const workflowCards = WORKFLOWS.map(w => `
    <div class="card guide-flow">
      <h4>${w.title}</h4>
      <ol>${w.steps.map(st => `<li>${st}</li>`).join("")}</ol>
    </div>`).join("");

  const pipelineCards = PIPELINE.map(([t, d]) => `
    <div class="guide-phase"><strong>${t}</strong><p>${d}</p></div>`).join("");

  const formulaRows = FORMULAS.map(([t, f]) => `
    <div class="guide-formula"><div class="gf-label">${t}</div><div class="gf-body"><code>${f}</code></div></div>`).join("");

  content.innerHTML = `
    ${guideStyles()}
    <div class="card guide-hero">
      <div class="titles">
        <h2>User Guide</h2>
        <p>Everything the Analytics &amp; Reporting Studio can do — the screens, the workflows, and the maths behind the insights. The same input always produces the same output: results are reproducible and auditable.</p>
      </div>
      <div class="privacy-note" style="margin-top:14px">
        <span class="privacy-ico" aria-hidden="true">${icon("lock")}</span>
        <div><strong>Your data stays private.</strong> It is processed in memory only — never written to a database, disk, or browser storage. Analytic sessions self-expire after 30 minutes.</div>
      </div>
      <div class="guide-jump">
        <a href="#guide-engine">${icon("sigma", 14)} How analysis works</a>
        <a href="#guide-screens">${icon("grid", 14)} The screens</a>
        <a href="#guide-workflows">${icon("play", 14)} Workflows</a>
      </div>
    </div>

    <h3 class="guide-h" id="guide-engine">How the analysis works</h3>
    <p class="guide-sub">Everything is produced by a deterministic seven-phase pipeline — no AI, no cloud. Each phase feeds the next:</p>
    <div class="card guide-engine-card">
      ${buildPipelineDiagram()}
      <div class="guide-phases">${pipelineCards}</div>
    </div>

    <h3 class="guide-h" id="guide-screens">The screens</h3>
    <p class="guide-sub">Nine views, reached from the left sidebar. Dashboard, Analyze, Ask the Agent, Aggregate and Chart Studio need an active, clean dataset; Catalogue and Settings are always available.</p>
    <div class="guide-screen-grid">${screenCards}</div>

    <h3 class="guide-h" id="guide-workflows">Common workflows</h3>
    <p class="guide-sub">Step-by-step paths for the things you'll do most.</p>
    <div class="guide-flow-grid">${workflowCards}</div>

    <h3 class="guide-h">Key formulas</h3>
    <p class="guide-sub">The core statistics behind the profiles, insights and forecasts.</p>
    <div class="card guide-formulas">${formulaRows}</div>

    <div class="card guide-foot">
      <p>A deeper written reference (with diagrams) lives in the repository at
      <code>docs/web-app/README.md</code>.</p>
    </div>`;

  // Smooth in-page jumps for the anchor chips.
  content.querySelectorAll(".guide-jump a").forEach(a => {
    a.addEventListener("click", e => {
      const t = document.querySelector(a.getAttribute("href"));
      if (t) { e.preventDefault(); t.scrollIntoView({ behavior: "smooth", block: "start" }); }
    });
  });
}

// A responsive SVG diagram of the deterministic seven-phase pipeline.
function buildPipelineDiagram() {
  const steps = ["Schema", "Validation", "Profiling", "Analytics", "Insights", "Charts", "Summary"];
  const W = 1180, H = 128, pad = 16, gap = 24, n = steps.length;
  const nodeW = (W - pad * 2 - gap * (n - 1)) / n;
  const nodeH = 76, nodeY = 30, cx = i => pad + i * (nodeW + gap) + nodeW / 2;

  const rects = steps.map((s, i) => {
    const x = pad + i * (nodeW + gap);
    return `
      <g class="pipe-node">
        <rect x="${x.toFixed(1)}" y="${nodeY}" width="${nodeW.toFixed(1)}" height="${nodeH}" rx="14"
          fill="url(#guidePipeGrad)" filter="url(#guidePipeShadow)"/>
        <circle cx="${cx(i).toFixed(1)}" cy="${nodeY + 24}" r="14" fill="rgba(255,255,255,.22)"/>
        <text x="${cx(i).toFixed(1)}" y="${nodeY + 29}" text-anchor="middle" class="pipe-num">${i + 1}</text>
        <text x="${cx(i).toFixed(1)}" y="${nodeY + 57}" text-anchor="middle" class="pipe-label">${s}</text>
      </g>`;
  }).join("");

  const arrows = steps.slice(0, -1).map((_, i) => {
    const x1 = pad + i * (nodeW + gap) + nodeW + 3;
    const x2 = x1 + gap - 6;
    const y = nodeY + nodeH / 2;
    return `<line x1="${x1.toFixed(1)}" y1="${y}" x2="${x2.toFixed(1)}" y2="${y}" class="pipe-arrow" marker-end="url(#guideArrow)"/>`;
  }).join("");

  return `
  <div class="guide-diagram">
    <svg viewBox="0 0 ${W} ${H}" role="img" aria-label="Seven-phase analysis pipeline" preserveAspectRatio="xMidYMid meet">
      <defs>
        <linearGradient id="guidePipeGrad" x1="0" y1="0" x2="${W}" y2="0" gradientUnits="userSpaceOnUse">
          <stop offset="0" stop-color="#6366f1"/>
          <stop offset=".5" stop-color="#8b5cf6"/>
          <stop offset="1" stop-color="#14b8a6"/>
        </linearGradient>
        <marker id="guideArrow" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto">
          <path d="M0 0 L10 5 L0 10 z" fill="#94a3b8"/>
        </marker>
        <filter id="guidePipeShadow" x="-10%" y="-25%" width="120%" height="160%">
          <feDropShadow dx="0" dy="2" stdDeviation="3" flood-color="#4338ca" flood-opacity="0.18"/>
        </filter>
      </defs>
      ${arrows}
      ${rects}
    </svg>
  </div>`;
}

function guideStyles() {
  return `<style>
    .guide-hero .guide-jump { display:flex; gap:10px; flex-wrap:wrap; margin-top:16px }
    .guide-jump a { display:inline-flex; align-items:center; gap:6px; padding:7px 12px; border:1px solid var(--border);
      border-radius:999px; font-size:13px; font-weight:600; color:var(--text-muted); text-decoration:none; background:var(--card, #fff) }
    .guide-jump a:hover { border-color:var(--border-strong); color:var(--text) }
    .guide-h { margin:26px 2px 4px; font-size:18px }
    .guide-sub { margin:0 2px 14px; color:var(--text-muted); font-size:14px; max-width:70ch }
    .guide-screen-grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(360px,1fr)); gap:16px; align-items:start }
    .guide-screen { margin-bottom:0 }
    .guide-screen-head { display:flex; align-items:flex-start; gap:12px; margin-bottom:6px }
    .guide-screen-ico { display:inline-flex; width:34px; height:34px; align-items:center; justify-content:center;
      border-radius:9px; background:rgba(99,102,241,.12); color:#4f46e5; flex:0 0 auto }
    .guide-screen-head h3 { margin:0; font-size:16px }
    .guide-lead { margin:2px 0 0; color:var(--text-muted); font-size:13.5px; font-weight:600 }
    .guide-nav { display:flex; gap:8px; align-items:baseline; margin:10px 0 12px; padding:10px 12px;
      background:var(--bg); border:1px solid var(--border); border-radius:var(--radius-sm);
      font-size:13px; line-height:1.55; color:var(--text-muted) }
    .guide-nav-label { display:inline-flex; align-items:center; gap:5px; flex:0 0 auto; font-weight:700;
      text-transform:uppercase; letter-spacing:.03em; font-size:11px; color:#4f46e5 }
    .guide-nav-label svg { vertical-align:middle }
    .guide-text { margin:4px 0 12px; font-size:14px; line-height:1.6 }
    .guide-points { margin:0; padding-left:20px; display:grid; gap:6px }
    .guide-points li { font-size:13.5px; line-height:1.55; color:var(--text) }
    .guide-flow-grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(300px,1fr)); gap:16px; align-items:start }
    .guide-flow h4 { margin:0 0 10px; font-size:15px }
    .guide-flow ol { margin:0; padding-left:20px }
    .guide-flow li { margin:6px 0; font-size:13.5px; line-height:1.55 }
    .guide-diagram { overflow-x:auto; margin:2px 0 18px; padding-bottom:16px; border-bottom:1px solid var(--border) }
    .guide-diagram svg { display:block; width:100%; min-width:680px; height:auto }
    .guide-diagram text { font-family:var(--font, 'Inter', 'Segoe UI', sans-serif) }
    .pipe-num { fill:#fff; font-size:15px; font-weight:700 }
    .pipe-label { fill:#fff; font-size:15px; font-weight:600; letter-spacing:.01em }
    .pipe-arrow { stroke:#94a3b8; stroke-width:2.5 }
    .guide-phases { display:grid; grid-template-columns:repeat(auto-fit,minmax(260px,1fr)); gap:14px }
    .guide-phase { padding:12px 14px; border:1px solid var(--border); border-radius:var(--radius-sm); background:var(--bg) }
    .guide-phase strong { display:block; margin-bottom:4px; font-size:13.5px; color:#4f46e5 }
    .guide-phase p { margin:0; font-size:13px; color:var(--text-muted); line-height:1.55 }
    .guide-formulas .guide-formula { padding:12px 0; border-bottom:1px solid var(--border) }
    .guide-formulas .guide-formula:last-child { border-bottom:0 }
    .gf-label { font-weight:600; font-size:13.5px; margin-bottom:6px }
    .gf-body code { display:block; padding:10px 12px; background:var(--bg); border:1px solid var(--border);
      border-radius:var(--radius-sm); font-size:14px; line-height:1.7; white-space:normal; overflow-wrap:anywhere }
    .guide-foot { margin-top:16px; color:var(--text-muted); font-size:13px }
    .guide-foot code { background:var(--bg); padding:1px 6px; border-radius:5px }
  </style>`;
}
