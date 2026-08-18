/* Data Source view — paste/upload/sample dataset selection and preview. */
import { $, $$, content, state, esc, toast, setDataset, countRows } from "./core.js";
import { SAMPLES } from "./samples.js";

export function renderDataView() {
  const ds = state.dataset;
  content.innerHTML = `
    <div class="card">
      <div class="section-head">
        <div class="titles"><h2>Data Source</h2><p>Provide the dataset that every analysis, dashboard and chart will use.</p></div>
      </div>
      <div class="privacy-note">
        <span class="privacy-ico" aria-hidden="true">🔒</span>
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
        <button class="tab active" data-tab="paste">Paste data</button>
        <button class="tab" data-tab="upload">Upload file</button>
        <button class="tab" data-tab="samples">Samples</button>
      </div>

      <div class="tab-panes">
      <div data-pane="paste">
        <label class="field">Raw data <span class="hint">CSV (with header row) or a JSON array of objects</span>
          <textarea id="dsData" placeholder="Month,Region,Revenue&#10;2024-01,EU,12000&#10;2024-02,EU,13500">${esc(ds?.data || "")}</textarea>
        </label>
      </div>
      <div data-pane="upload" hidden>
        <div class="dropzone" id="dsDrop">
          <input type="file" id="dsFile" accept=".csv,.json,.txt" hidden>
          <div class="dz-ico">⬆</div>
          <p class="dz-title">Drag &amp; drop a file here</p>
          <p class="dz-sub">CSV, JSON or TXT — or</p>
          <button class="btn subtle sm" id="dsBrowse" type="button">Browse files</button>
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
      </div>

      <div class="row" style="margin-top:18px">
        <button class="btn" id="useData">Use this dataset</button>
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

  // Load a dropped or browsed file into the paste pane for review.
  const loadFile = async (file) => {
    if (!file) return;
    $("#dsData").value = await file.text();
    $("#dsNameInput").value = file.name.replace(/\.[^.]+$/, "");
    $("#dsFileName").textContent = file.name;
    $$("#dataTabs .tab").forEach(x => x.classList.toggle("active", x.dataset.tab === "paste"));
    $$("[data-pane]").forEach(p => (p.hidden = p.dataset.pane !== "paste"));
    toast(`Loaded ${file.name} — review and click ‘Use this dataset’.`);
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

  $("#useData").onclick = () => {
    const data = $("#dsData").value.trim();
    if (!data) return toast("Paste some data or load a sample first.", true);
    setDataset({
      name: $("#dsNameInput").value.trim() || "Dataset",
      data,
      format: $("#dsFormat").value,
    });
    renderPreview();
    toast("Dataset is ready. Head to Dashboard or Analyze.");
  };

  if (ds) renderPreview();
}

function sampleCard(key, title, desc) {
  return `<div class="chart-card" style="cursor:default">
    <strong>${title}</strong>
    <p class="cap" style="margin-top:6px">${desc}</p>
    <button class="btn subtle sm" data-sample="${key}" style="margin-top:10px">Load</button>
  </div>`;
}

function renderPreview() {
  const host = $("#dsPreview");
  if (!host) return;
  const ds = state.dataset;
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
