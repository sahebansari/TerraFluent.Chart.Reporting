using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using TerraFluent.Chart.Reporting.Builder;
using TerraFluent.Chart.Reporting.Models;

// Run: dotnet run -c Release --project tests/TerraFluent.Chart.Reporting.Benchmarks
BenchmarkRunner.Run<RenderBenchmarks>();

[MemoryDiagnoser]
[SimpleJob]
public class RenderBenchmarks
{
    // Shared data arrays — allocated once, reused across all iterations.
    private static readonly double?[] _data100 = GenerateData(100);
    private static readonly double?[] _data500 = GenerateData(500);
    private static readonly string[]  _cats100 = GenerateCategories(100);
    private static readonly HeatmapPoint[] _heatmap20x20 = GenerateHeatmap(20, 20);

    // ── Static mode (PDF / email) ──────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Line 100pts Static")]
    public string Line100_Static() =>
        ChartBuilder.Create()
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(_cats100))
            .Series(s => s.AddLine("Series A", _data100))
            .RenderToSvg();

    [Benchmark(Description = "Column 100pts Static")]
    public string Column100_Static() =>
        ChartBuilder.Create()
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(_cats100))
            .Series(s => s.AddColumn("Series A", _data100))
            .RenderToSvg();

    [Benchmark(Description = "Pie 20 slices Static")]
    public string Pie20_Static() =>
        ChartBuilder.Create()
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(GenerateCategories(20)))
            .Series(s => s.AddPie("Share", GenerateData(20)))
            .RenderToSvg();

    // ── Animated mode (Blazor / browser) ─────────────────────────────────

    [Benchmark(Description = "Line 100pts Animated")]
    public string Line100_Animated() =>
        ChartBuilder.Create()
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(_cats100))
            .Series(s => s.AddLine("Series A", _data100))
            .RenderToSvg();

    // ── Interactive mode (full JS) ────────────────────────────────────────

    [Benchmark(Description = "Line 100pts Interactive")]
    public string Line100_Interactive() =>
        ChartBuilder.Create()
            .AsInteractive()
            .XAxis(x => x.Categories.AddRange(_cats100))
            .Series(s => s.AddLine("Series A", _data100))
            .RenderToSvg();

    [Benchmark(Description = "Multi-series 3×100pts Interactive")]
    public string MultiSeries3x100_Interactive() =>
        ChartBuilder.Create()
            .AsInteractive()
            .XAxis(x => x.Categories.AddRange(_cats100))
            .Series(s => s
                .AddLine("Series A", _data100)
                .AddLine("Series B", _data100)
                .AddLine("Series C", _data100))
            .RenderToSvg();

    // ── Large dataset ─────────────────────────────────────────────────────

    [Benchmark(Description = "Line 500pts Static")]
    public string Line500_Static() =>
        ChartBuilder.Create()
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(GenerateCategories(500)))
            .Series(s => s.AddLine("Series A", _data500))
            .RenderToSvg();

    // ── Heatmap ──────────────────────────────────────────────────────────

    [Benchmark(Description = "Heatmap 20×20 Animated")]
    public string Heatmap20x20_Animated() =>
        ChartBuilder.Create()
            .AsAnimated()
            .XAxis(x => x.Categories.AddRange(GenerateCategories(20)))
            .Series(s => s.AddHeatmap("Heat", _heatmap20x20,
                cfg => cfg.HeatmapRowLabels.AddRange(GenerateCategories(20))))
            .RenderToSvg();

    // ── RenderToDataUri (base64 round-trip) ──────────────────────────────

    [Benchmark(Description = "DataUri Line 100pts")]
    public string DataUri_Line100() =>
        ChartBuilder.Create()
            .AsStatic()
            .XAxis(x => x.Categories.AddRange(_cats100))
            .Series(s => s.AddLine("Series A", _data100))
            .RenderToDataUri();

    // ── Helpers ──────────────────────────────────────────────────────────

    private static double?[] GenerateData(int count)
    {
        var rng = new System.Random(42);
        var arr = new double?[count];
        for (int i = 0; i < count; i++) arr[i] = rng.NextDouble() * 1000.0;
        return arr;
    }

    private static string[] GenerateCategories(int count)
    {
        var arr = new string[count];
        for (int i = 0; i < count; i++) arr[i] = $"Cat{i + 1}";
        return arr;
    }

    private static HeatmapPoint[] GenerateHeatmap(int cols, int rows)
    {
        var rng = new System.Random(42);
        var pts = new HeatmapPoint[cols * rows];
        int idx = 0;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                pts[idx++] = new HeatmapPoint(c, r, rng.NextDouble() * 100.0);
        return pts;
    }
}
