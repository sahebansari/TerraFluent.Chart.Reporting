/* Tests for the Compare view's wiring: it must be routable, guarded like the other
   dataset-consuming screens, and post both datasets in one stateless request. */
import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { resolve } from "node:path";
import { installBrowserGlobals, importModule, JS_DIR } from "./harness.mjs";

installBrowserGlobals();

const source = await readFile(resolve(JS_DIR, "view-compare.js"), "utf8");
const mainSource = await readFile(resolve(JS_DIR, "main.js"), "utf8");
const indexHtml = await readFile(resolve(JS_DIR, "../index.html"), "utf8");

// ── Module surface ────────────────────────────────────────────────────────

test("the view module exports its renderer", async () => {
  const mod = await importModule("view-compare.js");
  assert.equal(typeof mod.renderCompareView, "function");
});

test("the compare icon is defined and renders inline SVG", async () => {
  const { icon } = await importModule("icons.js");
  const svg = icon("compare");

  assert.match(svg, /^<svg /);
  assert.match(svg, /viewBox="0 0 24 24"/);
  assert.ok(svg.length > 50, "the icon must have actual path content");
});

// ── Routing ───────────────────────────────────────────────────────────────

test("the router registers the compare view", () => {
  assert.match(mainSource, /compare:\s*{\s*title:\s*"Compare"/);
  assert.match(mainSource, /import { renderCompareView } from "\.\/view-compare\.js"/);
});

test("compare is guarded like every other dataset-consuming view", () => {
  // Without a dataset there is nothing to compare against, so it must divert to Data Source.
  const guarded = mainSource.match(/const GUARDED = new Set\(\[([^\]]+)\]\)/)[1];
  assert.match(guarded, /"compare"/);
});

test("the sidebar exposes a Compare nav item", () => {
  assert.match(indexHtml, /data-view="compare"/);
  assert.match(indexHtml, /data-icon="compare"/);
});

// ── Statelessness ─────────────────────────────────────────────────────────

test("both datasets are posted together rather than stored", () => {
  // The privacy stance depends on this: the comparison dataset is never persisted anywhere.
  assert.match(source, /\bbaseline:/);
  assert.match(source, /\bcurrent:/);
  assert.ok(!/localStorage/.test(source), "the comparison dataset must never touch localStorage");
  assert.ok(!/sessionStorage/.test(source), "the comparison dataset must never touch sessionStorage");
});

test("it calls the compare endpoints and nothing else", () => {
  const endpoints = [...source.matchAll(/"(\/api\/[^"]+)"/g)].map(m => m[1]).sort();
  assert.deepEqual(endpoints, [
    "/api/analytics/compare",
    "/api/analytics/compare/html",
    "/api/analytics/convert/xlsx",
  ]);
});

test("uploaded workbooks go through the server-side converter", () => {
  assert.match(source, /\.xlsx\$\/i\.test\(file\.name\)/);
  assert.match(source, /api\.binary\("\/api\/analytics\/convert\/xlsx"/);
});

// ── Rendering ─────────────────────────────────────────────────────────────

const { renderComparison } = await importModule("view-compare.js");

/** A comparison response shaped like the API's, with hostile text in every free-text field. */
const hostileResponse = {
  baselineName: '<img src=x onerror="alert(1)">',
  currentName: "Q2 & Co",
  baselineRowCount: 12,
  currentRowCount: 24,
  isComparable: true,
  compatibilityNote: "<script>alert('note')</script>",
  headline: "<b>Revenue</b> is up 20%",
  sharedMeasures: ["Revenue"],
  schemaChanges: [
    { column: "<svg onload=alert(1)>", kind: "Added", description: "<i>new column</i>" },
  ],
  measureDeltas: [
    {
      measure: "<em>Revenue</em>", statistic: "total", isAdditive: true,
      baselineValue: 180000, currentValue: 144000, delta: -36000, deltaPct: -0.2,
      baselineTrend: "Rising", currentTrend: "Declining", trendReversed: true,
    },
  ],
  categoryShifts: [
    {
      dimension: "<u>Region</u>", category: "EU & NA", measure: "Revenue",
      baselineValue: 60000, currentValue: 72000,
      baselineShare: 0.33, currentShare: 0.5, shareDelta: 0.17,
      isNew: false, isGone: false,
    },
  ],
  insights: [{ title: "<b>t</b>", description: "<i>d</i>", kind: "Comparison", importanceScore: 70 }],
  charts: [],
};

test("renderComparison escapes every free-text field from the API", () => {
  const html = renderComparison(hostileResponse);

  for (const raw of [
    '<img src=x onerror="alert(1)">',
    "<script>alert('note')</script>",
    "<svg onload=alert(1)>",
    "<em>Revenue</em>",
    "<u>Region</u>",
    "<i>new column</i>",
  ]) {
    assert.ok(!html.includes(raw), `unescaped in output: ${raw}`);
  }

  // The escaped forms are present, so the content is shown rather than dropped.
  assert.ok(html.includes("&lt;em&gt;Revenue&lt;/em&gt;"));
  assert.ok(html.includes("EU &amp; NA"));
});

test("renderComparison spells out the sign, so colour is never the only cue", () => {
  const html = renderComparison(hostileResponse);

  // Revenue fell: the cell carries a "down" class AND an explicit minus sign.
  assert.match(html, /class="num down">-36000\.00</);
  assert.match(html, /class="num down">-20\.0%</);

  // The category gained share: "up" class AND an explicit plus sign.
  assert.match(html, /class="num up">\+17\.0 pts</);
});

test("renderComparison surfaces a trend reversal distinctly", () => {
  const html = renderComparison(hostileResponse);
  assert.match(html, /badge warn">Rising → Declining</);
});

test("renderComparison omits empty sections rather than showing blank tables", () => {
  const html = renderComparison({
    ...hostileResponse,
    measureDeltas: [], categoryShifts: [], schemaChanges: [], insights: [], charts: [],
  });

  assert.ok(!html.includes("<h3>Measures</h3>"));
  assert.ok(!html.includes("<h3>Category mix</h3>"));
  assert.ok(!html.includes("<h3>Structural differences</h3>"));
  assert.ok(!html.includes("<h3>Charts</h3>"));
  assert.match(html, /No material differences/);
});

test("renderComparison flags an incomparable pair", () => {
  const html = renderComparison({ ...hostileResponse, isComparable: false });
  assert.match(html, /badge err">Not comparable</);
});

test("renderComparison embeds chart SVG verbatim", () => {
  const html = renderComparison({
    ...hostileResponse,
    charts: [{ svg: "<svg id='c'></svg>", title: "Measures", chartType: "Column", reason: "why" }],
  });

  assert.ok(html.includes("<svg id='c'></svg>"), "rendered chart markup must not be escaped");
});
