using BenchmarkDotNet.Attributes;

namespace TerraFluent.Chart.Reporting.Benchmarks;

/// <summary>
/// Large and label-heavy charts (see <see cref="LargeScenarios"/>). Complements
/// <see cref="RenderBenchmarks"/>, which covers typical 100–500 point charts.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class LargeChartBenchmarks
{
    [Benchmark(Baseline = true, Description = "Small column 6pts Interactive")]
    public string SmallColumn() => LargeScenarios.SmallColumnInteractive();

    [Benchmark(Description = "Showcase stacked+line+labels 6 cats")]
    public string ShowcaseStacked() => LargeScenarios.ShowcaseStackedWithLabels();

    [Benchmark(Description = "Line 5k pts Interactive")]
    public string Line5k() => LargeScenarios.Line5kInteractive();

    [Benchmark(Description = "Line 50k pts Static")]
    public string Line50k() => LargeScenarios.Line50kStatic();

    [Benchmark(Description = "Line 2k pts + data labels")]
    public string Line2kLabels() => LargeScenarios.Line2kWithLabels();

    [Benchmark(Description = "Stacked 3x300 + labels + line labels")]
    public string Stacked300Labels() => LargeScenarios.Stacked300WithLabels();

    [Benchmark(Description = "Scatter 5k Interactive")]
    public string Scatter5k() => LargeScenarios.Scatter5kInteractive();

    [Benchmark(Description = "Column 1k cats Interactive")]
    public string Column1k() => LargeScenarios.Column1kInteractive();
}
