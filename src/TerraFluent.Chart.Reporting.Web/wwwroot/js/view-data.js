/* Data Source view — paste/upload/sample dataset selection and preview. */
import {
  $, $$, api, content, state, esc, toast, setDataset, countRows,
  ensureValidation, navigate, loading, errorBox, isLiveDataset,
} from "./core.js";
import { icon } from "./icons.js";
import { SAMPLES } from "./samples.js";

export function renderDataView() {
  const ds = state.dataset;
  content.innerHTML = `
    <div class="card">
      <div class="section-head">
        <div class="titles"><h2>Data Source</h2><p>Provide the dataset that every analysis, dashboard and chart will use.</p></div>
      </div>
      <div class="privacy-note">
        <span class="privacy-ico" aria-hidden="true">${icon("lock")}</span>
        <div><strong>Your data stays private.</strong> It is processed in memory only — never written to a database, disk, or browser storage. Nothing is retained after you leave; analytic sessions self-expire after 30 minutes.</div>
      </div>
      <div class="row" style="margin-bottom:16px">
        <label class="field" style="flex:2 1 240px">Dataset name
          <input type="text" id="dsNameInput" placeholder="e.g. Global Sales 2024" value="${esc(ds?.name || "")}">
        </label>
        <label class="field" style="flex:1 1 140px">Format
          <select id="dsFormat">
            <option value="Auto">Auto-detect</option>
            <option value="Csv">CSV</option>
            <option value="Json">JSON array</option>
          </select>
        </label>
      </div>
      <div class="tabs" id="dataTabs">
        <button class="tab active" data-tab="paste">${icon("clipboard")} Paste data</button>
        <button class="tab" data-tab="upload">${icon("upload")} Upload file</button>
        <button class="tab" data-tab="samples">${icon("layers")} Samples</button>
        <button class="tab" data-tab="connections">${icon("database")} Connections</button>
      </div>

      <div class="tab-panes">
      <div data-pane="paste">
        <label class="field">Raw data <span class="hint">CSV (with header row) or a JSON array of objects</span>
          <textarea id="dsData" placeholder="Month,Region,Revenue&#10;2024-01,EU,12000&#10;2024-02,EU,13500">${esc(ds?.data || "")}</textarea>
        </label>
      </div>
      <div data-pane="upload" hidden>
        <div class="dropzone" id="dsDrop">
          <input type="file" id="dsFile" accept=".csv,.json,.txt,.xlsx" hidden>
          <div class="dz-ico">${icon("upload", 28)}</div>
          <p class="dz-title">Drag &amp; drop a file here</p>
          <p class="dz-sub">CSV, JSON, TXT or XLSX — or</p>
          <button class="btn subtle sm" id="dsBrowse" type="button">${icon("folder")} Browse files</button>
          <p class="dz-file" id="dsFileName"></p>
        </div>
        <p class="hint" style="margin-top:8px">The file contents replace the raw data above.</p>
      </div>
      <div data-pane="samples" hidden>
        <div class="sample-grid">
          ${sampleCard("sales", "Monthly Sales", "24 months · region &amp; product · revenue/cost/units · one anomaly")}
          ${sampleCard("web", "Web Traffic", "12 weeks · channel · sessions/conversions")}
          ${sampleCard("hr", "Headcount (JSON)", "Departments · headcount &amp; attrition as JSON")}
          ${sampleCard("orders", "E-commerce Orders", "~5,000 orders · channel/category/country · totals &amp; status")}
          ${sampleCard("sensors", "IoT Sensor Readings", "~5,000 readings · devices/sites · temperature/vibration alerts")}
          ${sampleCard("tickets", "Support Tickets", "~5,000 tickets · priority/channel/agent · resolution &amp; CSAT")}
        </div>
      </div>
      <div data-pane="connections" hidden>
        <p class="sub" style="margin-top:0">
          Live sources an administrator has configured on the server. Selecting one pulls the data
          fresh each time it is analysed — credentials stay on the server and are never sent to your
          browser, and nothing is stored.
        </p>
        <div id="connList">${loading("Loading connections…")}</div>
      </div>
      </div>

      <div class="row" style="margin-top:18px">
        <button class="btn" id="useData">${icon("check")} Use this dataset</button>
        <div class="spacer"></div>
        <span id="dsCount" class="stat-pill">${ds ? `<strong>Active:</strong> ${esc(ds.name)}` : "No active dataset"}</span>
      </div>
    </div>
    <div id="dsPreview"></div>`;

  // Tabs
  $$("#dataTabs .tab").forEach(t => t.onclick = () => {
    $$("#dataTabs .tab").forEach(x => x.classList.toggle("active", x === t));
    $$("[data-pane]").forEach(p => (p.hidden = p.dataset.pane !== t.dataset.tab));
  });
  if (ds?.format) $("#dsFormat").value = ds.format;

  // Load a dropped or browsed file into the paste pane for review. Workbooks are binary, so they are
  // normalised to CSV by the API first — everything downstream then works on plain text as usual.
  const loadFile = async (file) => {
    if (!file) return;
    const stem = file.name.replace(/\.[^.]+$/, "");
    const isXlsx = /\.xlsx$/i.test(file.name);

    if (isXlsx) {
      $("#dsFileName").textContent = `${file.name} — converting…`;
      try {
        const res = await api.binary("/api/analytics/convert/xlsx", await file.arrayBuffer(), {
          contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
          query: { datasetName: stem },
        });
        $("#dsData").value = res.data;
        $("#dsFormat").value = "Csv";
        toast(`Converted ${file.name} — ${res.rowCount} rows × ${res.columnCount} columns.`);
      } catch (err) {
        $("#dsFileName").textContent = file.name;
        return toast(err.message || "Could not read that workbook.", true);
      }
    } else {
      $("#dsData").value = await file.text();
      toast(`Loaded ${file.name} — review and click ‘Use this dataset’.`);
    }

    $("#dsNameInput").value = stem;
    $("#dsFileName").textContent = file.name;
    $$("#dataTabs .tab").forEach(x => x.classList.toggle("active", x.dataset.tab === "paste"));
    $$("[data-pane]").forEach(p => (p.hidden = p.dataset.pane !== "paste"));
  };

  $("#dsFile").onchange = (e) => loadFile(e.target.files[0]);
  $("#dsBrowse").onclick = () => $("#dsFile").click();

  const dz = $("#dsDrop");
  // Clicking anywhere in the zone (except the input itself) opens the file picker.
  dz.onclick = (e) => { if (e.target !== $("#dsFile")) $("#dsFile").click(); };
  ["dragenter", "dragover"].forEach(ev => dz.addEventListener(ev, e => {
    e.preventDefault(); dz.classList.add("dragover");
  }));
  ["dragleave", "dragend", "drop"].forEach(ev => dz.addEventListener(ev, e => {
    e.preventDefault(); dz.classList.remove("dragover");
  }));
  dz.addEventListener("drop", e => {
    if (e.dataTransfer && e.dataTransfer.files.length) loadFile(e.dataTransfer.files[0]);
  });

  $$("[data-sample]").forEach(b => b.onclick = () => {
    const s = SAMPLES[b.dataset.sample];
    $("#dsData").value = s.data;
    $("#dsNameInput").value = s.name;
    $("#dsFormat").value = s.format;
    $$("#dataTabs .tab").forEach(x => x.classList.toggle("active", x.dataset.tab === "paste"));
    $$("[data-pane]").forEach(p => (p.hidden = p.dataset.pane !== "paste"));
    toast(`Loaded sample: ${s.name}`);
  });

  loadConnections();

  $("#useData").onclick = async () => {
    const data = $("#dsData").value.trim();
    if (!data) return toast("Paste some data or load a sample first.", true);
    setDataset({
      name: $("#dsNameInput").value.trim() || "Dataset",
      data,
      format: $("#dsFormat").value,
    });
    renderPreview();
    // Validate immediately; if the data has quality issues (warnings or errors), send the user to
    // Data Quality to review them.
    const btn = $("#useData"); if (btn) btn.disabled = true;
    try {
      const v = await ensureValidation();
      if (v && !v.isClean) {
        toast("Data-quality issues found \u2014 opening Data Quality.", true);
        navigate("quality");
        return;
      }
      toast("Dataset is ready. Head to Dashboard or Analyze.");
    } finally { if (btn) btn.disabled = false; }
  };

  if (ds) renderPreview();
}

// ── Live connections ──────────────────────────────────────────────────────
// The server returns names and descriptions only; there is no URL or credential to render.
async function loadConnections() {
  const host = $("#connList");
  if (!host) return;

  let connections;
  try {
    connections = await api.json("/api/analytics/connections");
  } catch (e) {
    host.innerHTML = errorBox(e);
    return;
  }

  if (!connections.length) {
    host.innerHTML = `<p class="sub">No connections are configured on this server.
      An administrator adds them under <code>Connections</code> in the API's configuration.</p>`;
    return;
  }

  host.innerHTML = `<div class="sample-grid">${connections.map(c => `
    <div class="chart-card" style="cursor:default">
      <strong>${esc(c.name)}</strong>
      <p class="cap" style="margin-top:6px">${esc(c.description || "Live source")}</p>
      <p class="cap" style="margin-top:4px">
        <span class="badge muted">${esc(c.kind)}</span>${c.maxRows ? ` · up to ${c.maxRows.toLocaleString()} rows` : ""}
      </p>
      <button class="btn subtle sm" data-connection="${esc(c.name)}" style="margin-top:10px">
        ${icon("check")} Use this connection
      </button>
    </div>`).join("")}</div>`;

  $$("[data-connection]").forEach(b => b.onclick = () => useConnection(b.dataset.connection));
}

async function useConnection(name) {
  // Only the name is held locally; the rows are fetched server-side on each request.
  setDataset({ name, connectionName: name });
  renderPreview();

  try {
    const v = await ensureValidation();
    if (v && !v.isClean) {
      toast("Data-quality issues found — opening Data Quality.", true);
      navigate("quality");
      return;
    }
    toast(`Connected to ${name}. Head to Dashboard or Analyze.`);
  } catch (e) {
    toast(e.message || "Could not read that connection.", true);
  }
}

function sampleCard(key, title, desc) {
  return `<div class="chart-card" style="cursor:default">
    <strong>${title}</strong>
    <p class="cap" style="margin-top:6px">${desc}</p>
    <button class="btn subtle sm" data-sample="${key}" style="margin-top:10px">${icon("database")} Load</button>
  </div>`;
}

function renderPreview() {
  const host = $("#dsPreview");
  if (!host) return;
  const ds = state.dataset;

  // A live connection has no local rows to preview — the server pulls them per request.
  if (isLiveDataset(ds)) {
    $("#dsCount").innerHTML = `<strong>Active:</strong> ${esc(ds.name)} · live connection`;
    host.innerHTML = `<div class="card">
      <h3>${esc(ds.name)} <span class="hint">live connection</span></h3>
      <p class="sub">Rows are pulled from the server-configured source each time you analyse, and
      are never stored here. Open Dashboard or Analyze to run against the current data.</p>
    </div>`;
    return;
  }

  $("#dsCount").innerHTML = `<strong>Active:</strong> ${esc(ds.name)} · ${countRows(ds)} rows`;
  const isJson = ds.format === "Json" || (ds.format === "Auto" && ds.data.trim().startsWith("["));
  let head = [], rows = [];
  try {
    if (isJson) {
      const arr = JSON.parse(ds.data);
      head = Object.keys(arr[0] || {});
      rows = arr.slice(0, 8).map(o => head.map(h => o[h]));
    } else {
      const lines = ds.data.trim().split(/\r?\n/);
      head = lines[0].split(",");
      rows = lines.slice(1, 9).map(l => l.split(","));
    }
  } catch { host.innerHTML = ""; return; }

  host.innerHTML = `<div class="card">
    <h3>Preview <span class="hint">first ${rows.length} of ${countRows(ds)} rows</span></h3>
    <div class="table-wrap"><table class="data"><thead><tr>${
      head.map(h => `<th>${esc(h)}</th>`).join("")
    }</tr></thead><tbody>${
      rows.map(r => `<tr>${r.map(c => `<td>${esc(c)}</td>`).join("")}</tr>`).join("")
    }</tbody></table></div></div>`;
}
