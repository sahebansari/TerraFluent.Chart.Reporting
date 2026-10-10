# TerraFluent.Chart.Reporting.Benchmarks

Rendering-performance benchmarks for the library. Always run in **Release**.

## Suites

| Suite | What it covers |
|---|---|
| `RenderBenchmarks` | Typical charts: 100–500 points, every render mode, pie, heatmap, `RenderToDataUri`. |
| `LargeChartBenchmarks` | Large and label-heavy charts: 5k/50k-point lines, 5k scatter, 1k-category columns, dense collision-aware data labels, and stacked columns with labels. These shapes exposed real hotspots, so they guard against regressions. |

The large scenarios are defined once in [`LargeScenarios.cs`](LargeScenarios.cs) and shared by
`LargeChartBenchmarks`, `quick` and `profile`, so all three measure the same charts. Input data is
generated once, so only rendering is measured.

## Running

From the repository root:

```bash
# Interactive picker (BenchmarkDotNet) — choose a suite
dotnet run -c Release --project tests/TerraFluent.Chart.Reporting.Benchmarks

# Any BenchmarkDotNet arguments pass through, e.g. one suite or one benchmark
dotnet run -c Release --project tests/TerraFluent.Chart.Reporting.Benchmarks -- --filter *LargeChartBenchmarks*
dotnet run -c Release --project tests/TerraFluent.Chart.Reporting.Benchmarks -- --filter *Line2kLabels*

# Quick table: average ms and KB allocated per render, in seconds rather than minutes
dotnet run -c Release --project tests/TerraFluent.Chart.Reporting.Benchmarks -- quick

# Which object types dominate allocations in the large scenarios (sampled GC events)
dotnet run -c Release --project tests/TerraFluent.Chart.Reporting.Benchmarks -- profile
```

BenchmarkDotNet writes reports to `BenchmarkDotNet.Artifacts/` in the current directory (git-ignored).

**Which one to use:** run `quick` before and after a change to see whether it helped. Use the
BenchmarkDotNet suites when you need statistically sound numbers, for example for a pull request
or release notes. `quick` uses a single process and a fixed iteration count, so expect roughly
±10% noise between runs.

## Reference numbers

`quick` mode, .NET 8, Release. These show the effect of the October 2026 optimisation pass:
the spatial-index label layout, single-pass label substitution, allocation-free number
formatting in path and marker output, and skipping label bookkeeping when no labels are shown.

| Scenario | Before | After |
|---|---|---|
| line 2k pts + data labels (interactive) | 9,551 ms · 2.6 GB | ~150 ms · 17.6 MB |
| stacked col 3x300 + labels + line labels | 1,865 ms · 450 MB | ~45–56 ms · 13.8 MB |
| line 50k pts (static) | 148 ms · 80 MB | ~91 ms · 33.5 MB |
| line 5k pts (interactive) | 100 ms · 39 MB | ~72–92 ms · 34.4 MB |
| small column (6 pts, interactive) | 0.70 ms · 140 KB | ~0.57 ms · 137 KB |

Absolute times depend on the machine. Compare runs made on the same machine.

## Known remaining cost

In interactive and animated modes every data point carries its own hidden tooltip markup (about
1.8 KB per point), which dominates both output size and allocations for large series: 5k points
produce about 9 MB of SVG. `profile` shows this as `System.String` / `System.Char[]`. Replacing it
with a single script-driven tooltip in interactive mode is the largest remaining optimisation.
