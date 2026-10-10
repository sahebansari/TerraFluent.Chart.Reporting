# Changelog

All notable changes to TerraFluent.Chart.Reporting are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project
uses [Semantic Versioning](https://semver.org/).

## [2.1.0] — 2026-10-11

This release adds API and changes visual defaults; nothing is removed or broken. Charts look
different, though: bars are capped at 64px, text is larger, and labels avoid each other. Re-check
any pixel-exact snapshots after upgrading.

### Added

- **Multi-colour bars.** `SeriesBuilder.ColorByPoint(params string[] colors)` gives every bar of a
  non-stacked Column or Bar series its own colour. It uses the theme palette, or your own colours
  cycled in order. `ColorByPoint(bool)` toggles it. Also available as `Series.ColorByPoint` and
  `Series.PointColors`.
- **Collision-aware data labels.** On cartesian charts, data labels avoid each other, line series
  and point markers:
  - Column and bar labels slide along their own segment.
  - Labels above a column can step up one tier, or drop just inside the column top.
  - Line and spline labels move above, below or beside their marker.
- **Benchmarks.** `LargeChartBenchmarks` covers large and label-heavy charts. The benchmarks project
  also gains `quick` (seconds-long timing table) and `profile` (allocation by type) modes. See
  `tests/TerraFluent.Chart.Reporting.Benchmarks/README.md`.
- Sample charts **#103 Daily Sales** (date axis with `dd-MMM-yyyy` labels over 22 days) and
  **#104 Multi-Colour Bars**.

### Changed

- **Bar thickness.** Each column or bar is at most **64px** thick and centred in its category slot.
  In grouped charts the cap applies to each bar.
- **Rounded corners.** The corner radius (`BorderRadius`, or 3px under `ModernStyle`) now rounds only
  the bar's free end: the top of a positive column, the right end of a positive bar. The base stays
  square, and in a stack only the outermost segment is rounded. Rounded bars render as `<path>`
  instead of `<rect rx>`.
- **Larger default text.** Axis tick labels are now 12px (was 11px), axis titles 13px (was 12px) and
  data labels 11px (was 10px). `LabelLayoutOptions.MaxFontSize` now defaults to 12.
- **10px readability floor.** Axis labels, axis titles and data labels are never drawn below 10px.
  This applies under a `FontScale` below 1, under label auto-scaling, and to an explicit
  `DataLabel.FontSize`.
- **Date axes.** Labels are thinned by their rendered width, like category axes, so long formats no
  longer overlap. The final date is always shown, and the crowded label just before it is dropped.

### Fixed

- **Left Y-axis range.** The left Y-axis tick labels didn't match the plotted bars in two cases:
  - With a secondary axis, series bound with `OnSecondaryAxis()` were included in the left axis's
    range.
  - With stacking, the axis used the largest single value instead of the stack totals.

  Axis ticks, series, plot bands, plot lines and annotations now share one range.
- **Auto-scaled axis labels.** They had no visible effect: the size was emitted as a `font-size`
  attribute, which the `.axis-label` CSS rule overrides. It is now emitted as an inline style.
- **Pie connector docs.** The documentation now describes the outside-label connector as it is
  drawn: a leader line with a short horizontal bend.

### Performance

Rendering output is byte-for-byte unchanged by these optimisations; all 104 sample charts were
verified identical.

- **Label layout.** It now uses a spatial index and a single-pass placeholder substitution, where it
  used to run all-pairs checks and rescan the whole SVG once per label:
  - A 2,000-point labelled line: **9.5 s → ~150 ms**, and 2.6 GB → 18 MB allocated.
  - Stacked 3×300 columns with labels: **1.9 s → ~45 ms**.
- **No label bookkeeping when labels are off.** When no visible series shows data labels, no
  label-related work is done.
- **Number formatting.** Path and marker coordinates are written without intermediate strings on
  `netstandard2.1` and later. A 50,000-point static line: 148 → ~91 ms, and 80 → ~34 MB allocated.
- **Output methods.** `RenderToStream` / `RenderToStreamAsync` encode through a 16 KB buffer
  instead of allocating one byte array the size of the SVG. `RenderToHtml` avoids extra full copies
  of the SVG.

## [2.0.0] — 2026-09-14

First public release on nuget.org. It renders 26 chart types through a fluent API in three modes:
Static, Animated (SMIL) and Interactive (JS). It ships with 18 built-in themes, modern styling and
zero external dependencies, and targets `netstandard2.0`, `netstandard2.1`, `net6.0`, `net8.0` and
`net10.0`.

[2.1.0]: https://github.com/sahebansari/TerraFluent.Chart.Reporting/compare/v2.0.0...v2.1.0
[2.0.0]: https://github.com/sahebansari/TerraFluent.Chart.Reporting/releases/tag/v2.0.0
