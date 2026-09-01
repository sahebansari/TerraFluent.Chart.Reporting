# TerraFluent.Chart.Reporting — Copilot Instructions

## Project Purpose
A fluent C# class library that generates SVG charts server-side with zero JavaScript dependency.
Targets `netstandard2.0`, `netstandard2.1`, `net6.0`, `net8.0`, `net10.0`.

## Solution Structure
```
src/TerraFluent.Chart.Reporting/   ← main library
  Builder/    ChartBuilder, SeriesBuilder  (fluent API entry point)
  Models/     ChartOptions, Series, Axis, Legend, ChartTitle, AnimationOptions
  Rendering/  ISvgRenderer, SvgRenderer  (SVG string generation)
  Enums/      ChartType, SvgMode, Easing
tests/TerraFluent.Chart.Reporting.Tests/  ← xUnit tests (net8.0)
```

## Conventions
- No external NuGet dependencies in the library project — pure BCL only.
- All SVG output must XML-escape user-provided strings (use `SvgRenderer.Escape`).
- Fluent methods return `this` (the builder) so calls can be chained.
- `SvgMode.Static` must never emit `<script>` or CSS hover rules.
- Use `CultureInfo.InvariantCulture` for all floating-point formatting in SVG coordinates.
- Multi-targeting: avoid APIs not available in `netstandard2.0` without `#if` guards.

## Supported Chart Types
Line, Spline, Bar/Column, Area, Pie, Scatter, Waterfall, Gauge, DataRing — via `SeriesBuilder.AddLine/AddColumn/AddArea/AddPie/AddScatter/AddWaterfall/AddGauge/AddDataRing`.

## Render Modes
| Mode        | CSS Hover | JS | Safe for     |
|-------------|-----------|-----|--------------|
| Static      | No        | No  | PDF, email   |
| Animated    | Yes       | No  | Browser/Blazor |
| Interactive | Yes       | Yes | Browser only |
