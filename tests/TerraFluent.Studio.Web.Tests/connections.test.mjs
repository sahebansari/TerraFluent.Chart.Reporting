/* Tests for the live-connection path in the SPA: a connection is referenced by name only, so the
   dataset the browser holds has no rows and no credential in it. */
import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { resolve } from "node:path";
import { installBrowserGlobals, importModule, JS_DIR } from "./harness.mjs";

installBrowserGlobals();
const core = await importModule("core.js");
const dataViewSource = await readFile(resolve(JS_DIR, "view-data.js"), "utf8");

// ── datasetPayload ────────────────────────────────────────────────────────

test("an inline dataset posts its rows", () => {
  const payload = core.datasetPayload({ name: "Q1", data: "a,b\n1,2", format: "Csv" });

  assert.deepEqual(payload, { data: "a,b\n1,2", format: "Csv", datasetName: "Q1" });
});

test("a live dataset posts only its connection name", () => {
  const payload = core.datasetPayload({ name: "Sales", connectionName: "Sales" });

  assert.deepEqual(payload, { connectionName: "Sales", datasetName: "Sales" });
  // The whole point: no rows, no URL, no credential leaves the browser.
  assert.ok(!("data" in payload));
  assert.ok(!("format" in payload));
});

test("datasetPayload defaults a missing name and tolerates no dataset", () => {
  assert.equal(core.datasetPayload({ data: "x" }).datasetName, "Dataset");
  assert.equal(core.datasetPayload({ connectionName: "Live" }).datasetName, "Live");
  assert.deepEqual(core.datasetPayload(null), {});
});

test("isLiveDataset distinguishes the two kinds", () => {
  assert.equal(core.isLiveDataset({ connectionName: "Sales" }), true);
  assert.equal(core.isLiveDataset({ data: "a,b" }), false);
  assert.equal(core.isLiveDataset(null), false);
});

// ── Row counting ──────────────────────────────────────────────────────────

test("countRows reports a dash for a live dataset rather than guessing", () => {
  // The row count is only known once the server pulls it, so the UI must not invent one.
  assert.equal(core.countRows({ name: "Sales", connectionName: "Sales" }), "—");
  assert.equal(core.countRows(null), "—");
  assert.equal(core.countRows({ format: "Csv", data: "a,b\n1,2\n3,4" }), 2);
});

// ── Wiring ────────────────────────────────────────────────────────────────

test("the Data Source view exposes a Connections tab", () => {
  assert.match(dataViewSource, /data-tab="connections"/);
  assert.match(dataViewSource, /data-pane="connections"/);
});

test("the view lists connections from the server rather than holding any config", () => {
  assert.match(dataViewSource, /api\.json\("\/api\/analytics\/connections"\)/);

  // Selecting a connection stores only its name.
  assert.match(dataViewSource, /setDataset\(\{ name, connectionName: name \}\)/);
});

test("no credential field is ever read from the connections response", () => {
  // Guards against someone later rendering a field the API deliberately does not return.
  for (const forbidden of ["target", "connectionString", "password", "authorization", "settings"]) {
    assert.ok(
      !new RegExp(`c\\.${forbidden}`, "i").test(dataViewSource),
      `the view must not read a '${forbidden}' field off a connection`);
  }
});
