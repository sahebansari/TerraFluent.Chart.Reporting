/* Tests for the Studio SPA's shared core module: formatting, escaping, the API
   client and the settings store. These are the helpers every view depends on. */
import test from "node:test";
import assert from "node:assert/strict";
import { installBrowserGlobals, importModule, jsonResponse } from "./harness.mjs";

const globals = installBrowserGlobals();
const core = await importModule("core.js");

// ── esc: HTML escaping ────────────────────────────────────────────────────

test("esc neutralises the characters that could break out of markup", () => {
  assert.equal(core.esc(`<script>alert("x")</script>`),
    "&lt;script&gt;alert(&quot;x&quot;)&lt;/script&gt;");
  assert.equal(core.esc("a & b"), "a &amp; b");
  // Ampersand must be escaped first, or the other replacements get double-escaped.
  assert.equal(core.esc("&lt;"), "&amp;lt;");
});

test("esc renders null and undefined as an empty string", () => {
  assert.equal(core.esc(null), "");
  assert.equal(core.esc(undefined), "");
  assert.equal(core.esc(0), "0");
  assert.equal(core.esc(false), "false");
});

// ── fmt: compact number formatting ────────────────────────────────────────

test("fmt abbreviates at each magnitude boundary", () => {
  assert.equal(core.fmt(1_500_000_000), "1.50B");
  assert.equal(core.fmt(2_400_000), "2.40M");
  assert.equal(core.fmt(1_200), "1.20k");
  assert.equal(core.fmt(-2_400_000), "-2.40M");
});

test("fmt rounds small numbers to two decimals and shows a dash for no value", () => {
  assert.equal(core.fmt(3.14159), "3.14");
  assert.equal(core.fmt(0), "0");
  assert.equal(core.fmt(null), "—");
  assert.equal(core.fmt(undefined), "—");
  assert.equal(core.fmt(NaN), "—");
});

// ── scoreClass: severity banding ──────────────────────────────────────────

test("scoreClass bands a 0-100 score into high, mid and low", () => {
  assert.equal(core.scoreClass(100), "");
  assert.equal(core.scoreClass(70), "");
  assert.equal(core.scoreClass(69), "mid");
  assert.equal(core.scoreClass(40), "mid");
  assert.equal(core.scoreClass(39), "low");
  assert.equal(core.scoreClass(0), "low");
});

// ── qs: query-string building ─────────────────────────────────────────────

test("qs omits null, undefined and empty values but keeps false and zero", () => {
  assert.equal(core.qs({ a: 1, b: null, c: undefined, d: "" }), "?a=1");
  assert.equal(core.qs({ flag: false, count: 0 }), "?flag=false&count=0");
  assert.equal(core.qs({}), "");
  assert.equal(core.qs(null), "");
});

test("qs percent-encodes values", () => {
  assert.equal(core.qs({ name: "Q1 Sales & Co" }), "?name=Q1+Sales+%26+Co");
});

// ── countRows ─────────────────────────────────────────────────────────────

test("countRows excludes the header row for CSV", () => {
  assert.equal(core.countRows({ format: "Csv", data: "a,b\n1,2\n3,4\n" }), 2);
  assert.equal(core.countRows({ format: "Csv", data: "a,b" }), 0);
});

test("countRows counts array entries for JSON, auto-detecting from the payload", () => {
  assert.equal(core.countRows({ format: "Json", data: '[{"a":1},{"a":2}]' }), 2);
  assert.equal(core.countRows({ format: "Auto", data: '[{"a":1}]' }), 1);
  assert.equal(core.countRows({ format: "Json", data: "not json" }), "?");
});

// ── Additivity rules ──────────────────────────────────────────────────────

test("isSummable defers to the column's additive flag, defaulting to true", () => {
  assert.equal(core.isSummable({ additive: true }), true);
  assert.equal(core.isSummable({ additive: false }), false);
  assert.equal(core.isSummable({}), true);      // flag absent => summable
  assert.equal(core.isSummable(null), true);
});

test("aggregationsForColumns drops Sum when any column is non-additive", () => {
  assert.deepEqual(core.aggregationsForColumns([{ additive: true }]), core.ALL_AGGREGATIONS);

  const mixed = core.aggregationsForColumns([{ additive: true }, { additive: false }]);
  assert.ok(!mixed.includes("Sum"), "Sum must not be offered for a non-additive measure");
  assert.ok(mixed.includes("Average"));
});

test("aggregationsForColumns accepts a single column as well as an array", () => {
  assert.ok(!core.aggregationsForColumns({ additive: false }).includes("Sum"));
});

// ── Render helpers ────────────────────────────────────────────────────────

test("insightRow escapes untrusted insight text", () => {
  const html = core.insightRow({
    title: "<b>boom</b>",
    description: `"quoted" & <i>tagged</i>`,
    kind: "Forecast",
    importanceScore: 82,
  });

  assert.ok(html.includes("&lt;b&gt;boom&lt;/b&gt;"));
  assert.ok(!html.includes("<b>boom</b>"));
  assert.ok(html.includes("82"));
});

test("insightRow labels an anomaly score as severity and others as importance", () => {
  const anomaly = core.insightRow({ title: "t", description: "d", kind: "Anomaly", importanceScore: 90 });
  const forecast = core.insightRow({ title: "t", description: "d", kind: "Forecast", importanceScore: 90 });

  assert.ok(anomaly.includes("Severity score"));
  assert.ok(forecast.includes("Importance score"));
  // Only non-anomaly insights get the key-insight treatment.
  assert.ok(!anomaly.includes("keyinsight"));
  assert.ok(forecast.includes("keyinsight"));
});

test("insightBlock renders nothing for an empty list", () => {
  assert.equal(core.insightBlock("Insights", []), "");
  assert.equal(core.insightBlock("Insights", null), "");
  assert.ok(core.insightBlock("Insights", [{ title: "a", description: "b" }]).includes("Insights"));
});

test("chartCard escapes its caption but embeds the SVG verbatim", () => {
  const html = core.chartCard("<svg id='c'></svg>", "Revenue & Cost", "<reason>", 77);

  assert.ok(html.includes("<svg id='c'></svg>"), "the rendered SVG must not be escaped");
  assert.ok(html.includes("Revenue &amp; Cost"));
  assert.ok(html.includes("&lt;reason&gt;"));
  assert.ok(html.includes("suitability 77"));
});

// ── Settings store ────────────────────────────────────────────────────────

test("settings round-trip through localStorage and reset to defaults", () => {
  const saved = core.saveSettings({ defaultChartType: "Line" });
  assert.equal(saved.defaultChartType, "Line");
  assert.equal(core.loadSettings().defaultChartType, "Line");

  // Unspecified keys keep their default.
  assert.equal(saved.defaultTheme, core.DEFAULT_SETTINGS.defaultTheme);

  const reset = core.resetSettings();
  assert.deepEqual(reset, core.DEFAULT_SETTINGS);
  assert.equal(globals.storage.getItem("tf.studio.settings.v1"), null);
});

// ── API client ────────────────────────────────────────────────────────────

test("api.json posts a JSON body and appends the query string", async () => {
  const calls = [];
  globalThis.fetch = async (url, opts) => {
    calls.push({ url, opts });
    return jsonResponse({ ok: 1 });
  };

  const result = await core.api.json("/api/analytics/analyze", {
    method: "POST",
    body: { data: "a,b" },
    query: { includeSvg: true },
  });

  assert.deepEqual(result, { ok: 1 });
  assert.equal(calls[0].url, "/api/analytics/analyze?includeSvg=true");
  assert.equal(calls[0].opts.headers["Content-Type"], "application/json");
  assert.equal(calls[0].opts.body, JSON.stringify({ data: "a,b" }));
});

test("api.binary posts raw bytes under the caller's content type", async () => {
  const calls = [];
  globalThis.fetch = async (url, opts) => {
    calls.push({ url, opts });
    return jsonResponse({ rowCount: 6, columnCount: 3, data: "Month,Region\n" });
  };

  const bytes = new Uint8Array([0x50, 0x4b, 0x03, 0x04]).buffer;
  const result = await core.api.binary("/api/analytics/convert/xlsx", bytes, {
    contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    query: { datasetName: "Q1" },
  });

  assert.equal(result.rowCount, 6);
  assert.equal(calls[0].url, "/api/analytics/convert/xlsx?datasetName=Q1");
  assert.equal(calls[0].opts.method, "POST");
  assert.equal(calls[0].opts.headers["Content-Type"],
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
  assert.equal(calls[0].opts.body, bytes, "the ArrayBuffer must be forwarded untouched");
});

test("api surfaces a ProblemDetails body as the thrown error message", async () => {
  globalThis.fetch = async () => jsonResponse(
    { title: "Bad Request", detail: "The uploaded file is not a readable .xlsx workbook." },
    { ok: false, status: 400 });

  await assert.rejects(
    () => core.api.binary("/api/analytics/convert/xlsx", new ArrayBuffer(4)),
    (err) => {
      assert.equal(err.status, 400);
      assert.match(err.message, /not a readable \.xlsx workbook/);
      return true;
    });
});

test("api falls back to a status message when the error body is not JSON", async () => {
  globalThis.fetch = async () => ({
    ok: false,
    status: 500,
    headers: { get: () => "text/plain" },
    json: async () => { throw new Error("not json"); },
    text: async () => "boom",
  });

  await assert.rejects(
    () => core.api.json("/api/analytics/analyze"),
    (err) => {
      assert.match(err.message, /Request failed \(500\)/);
      return true;
    });
});
