/* Tests for buildAgentQuestions — the dataset-driven suggestion list on the
   "Ask the Agent" screen. It reads the API's column profiles, so the rules it
   encodes (identifiers excluded, date required for time-based questions,
   measures ranked by semantic role) are worth pinning down. */
import test from "node:test";
import assert from "node:assert/strict";
import { installBrowserGlobals, importModule } from "./harness.mjs";

installBrowserGlobals();
const { buildAgentQuestions } = await importModule("core.js");

const col = (name, extra = {}) => ({ name, distinctCount: 5, ...extra });

const SALES_COLUMNS = [
  col("OrderId", { type: "Identifier", role: "IdentifierRole", distinctCount: 500 }),
  col("Month", { type: "Date" }),
  col("Region", { type: "Category", distinctCount: 3 }),
  col("Product", { type: "Category", distinctCount: 4 }),
  col("Revenue", { type: "Currency", role: "RevenueMetric", sum: 500000 }),
  col("Cost", { type: "Currency", role: "CostMetric", sum: 300000 }),
];

test("returns nothing useful for an empty or non-array profile", () => {
  assert.deepEqual(buildAgentQuestions([]), []);
  assert.deepEqual(buildAgentQuestions(null), []);
  assert.deepEqual(buildAgentQuestions(undefined), []);
});

test("covers each agent capability for a full dataset", () => {
  const questions = buildAgentQuestions(SALES_COLUMNS);
  const all = questions.join("\n");

  assert.match(all, /highest Revenue/);           // dominance
  assert.match(all, /trend of Revenue over time/); // trend
  assert.match(all, /Forecast Revenue/);           // forecast
  assert.match(all, /anomalies in Revenue/);       // anomaly
  assert.match(all, /relationship between Revenue and Cost/); // correlation
  assert.match(all, /natural groups/);             // segmentation
  assert.match(all, /Why did Revenue change/);     // root cause
});

test("never suggests questions about an identifier column", () => {
  const all = buildAgentQuestions(SALES_COLUMNS).join("\n");
  assert.ok(!all.includes("OrderId"), "identifiers are excluded from analysis");
});

test("omits time-based questions when the dataset has no date column", () => {
  const undated = SALES_COLUMNS.filter(c => c.type !== "Date");
  const all = buildAgentQuestions(undated).join("\n");

  assert.ok(!/over time/.test(all), "no date column means no trend question");
  assert.ok(!/Forecast/.test(all), "no date column means no forecast question");
  assert.ok(!/month over month/.test(all));
  // Dimension-based questions still work.
  assert.match(all, /highest Revenue/);
});

test("ranks the highest-value semantic role first", () => {
  const shuffled = [
    col("Units", { type: "Numeric", role: "QuantityMetric", sum: 9000 }),
    col("Cost", { type: "Currency", role: "CostMetric", sum: 300000 }),
    col("Revenue", { type: "Currency", role: "RevenueMetric", sum: 500000 }),
    col("Region", { type: "Category", distinctCount: 3 }),
  ];

  // Revenue outranks Cost, which outranks Units — so the headline question names Revenue.
  assert.match(buildAgentQuestions(shuffled)[0], /Revenue/);
});

test("skips a high-cardinality text column as a grouping dimension", () => {
  const withFreeText = [
    ...SALES_COLUMNS,
    col("Notes", { type: "Text", distinctCount: 480 }),
  ];

  const all = buildAgentQuestions(withFreeText).join("\n");
  assert.ok(!all.includes("Notes"), "a 480-value text column is not a dimension");
});

test("produces a deduplicated list capped at twenty questions", () => {
  // A wide dataset generates far more candidate questions than the cap allows.
  const wide = [
    col("Month", { type: "Date" }),
    ...Array.from({ length: 6 }, (_, i) =>
      col(`Dim${i}`, { type: "Category", distinctCount: 4 })),
    ...Array.from({ length: 6 }, (_, i) =>
      col(`Measure${i}`, { type: "Numeric", sum: 1000 * (i + 1) })),
  ];

  const questions = buildAgentQuestions(wide);
  assert.equal(questions.length, 20);
  assert.equal(new Set(questions.map(q => q.toLowerCase())).size, 20, "questions must be unique");
});

test("is deterministic for the same profile", () => {
  assert.deepEqual(buildAgentQuestions(SALES_COLUMNS), buildAgentQuestions(SALES_COLUMNS));
});

test("handles a measure-only dataset without a dimension", () => {
  const measuresOnly = [
    col("Revenue", { type: "Currency", role: "RevenueMetric", sum: 1000 }),
    col("Cost", { type: "Currency", role: "CostMetric", sum: 600 }),
  ];

  const questions = buildAgentQuestions(measuresOnly);
  assert.ok(questions.length > 0);
  assert.match(questions.join("\n"), /relationship between Revenue and Cost/);
});
