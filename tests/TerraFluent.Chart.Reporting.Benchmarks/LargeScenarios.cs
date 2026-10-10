using TerraFluent.Chart.Reporting.Builder;

namespace TerraFluent.Chart.Reporting.Benchmarks;

/// <summary>
/// Large and label-heavy chart scenarios shared by <see cref="LargeChartBenchmarks"/>, the
/// <c>quick</c> runner and the <c>profile</c> mode, so all three always measure the same charts.
/// These are the shapes that exposed real hotspots (quadratic label layout, per-point string
/// churn); input data is generated once so only rendering is measured.
/// </summary>
public static class LargeScenarios
{
    /// <summary>A named render. <see cref="QuickIterations"/> keeps the quick runner to seconds.</summary>
    public sealed record Scenario(string Name, int QuickIterations, Func<string> Render);

    private static readonly double?[] Wave6A   = Wave(6, 1);
    private static readonly double?[] Wave6B   = Wave(6, 2);
    private static readonly double?[] Wave6C   = Wave(6, 3);
    private static readonly double?[] Growth6  = Wave(6, 4, 20);
    private static readonly double?[] Wave300A = Wave(300, 1);
    private static readonly double?[] Wave300B = Wave(300, 2);
    private static readonly double?[] Wave300C = Wave(300, 3);
    private static readonly double?[] Wave300L = Wave(300, 4, 300);
    private static readonly double?[] Wave1k   = Wave(1_000, 1);
    private static readonly double?[] Wave2k   = Wave(2_000, 1);
    private static readonly double?[] Wave5k   = Wave(5_000, 1);
    private static readonly double?[] Wave50k  = Wave(50_000, 1);
    private static readonly string[]  Cats6    = Categories(6);
    private static readonly string[]  Cats300  = Categories(300);
    private static readonly string[]  Cats1k   = Categories(1_000);

    public static string SmallColumnInteractive() =>
        ChartBuilder.Create().AsInteractive()
            .XAxis(x => x.Categories.AddRange(Cats6))
            .Series(s => s.AddColumn("A", Wave6A))
            .RenderToSvg();

    /// <summary>Mirrors the Fluent API showcase: stacked columns + secondary-axis line, all labelled.</summary>
    public static string ShowcaseStackedWithLabels() =>
        ChartBuilder.Create().AsInteractive().Animate().StackNormal()
            .XAxis(x => x.Categories.AddRange(Cats6))
            .YAxis2(y => { y.Min = 0; y.Max = 50; })
            .ShowDataLabels()
            .Series(s => s
                .AddColumn("A", Wave6A).AddColumn("B", Wave6B).AddColumn("C", Wave6C)
                .AddLine("Growth", Growth6, c => c.OnSecondaryAxis()))
            .RenderToSvg();

    public static string Line5kInteractive() =>
        ChartBuilder.Create().AsInteractive()
            .Series(s => s.AddLine("A", Wave5k))
            .RenderToSvg();

    public static string Line50kStatic() =>
        ChartBuilder.Create().AsStatic()
            .Series(s => s.AddLine("A", Wave50k))
            .RenderToSvg();

    /// <summary>Dense collision-aware label layout (was 9.5 s before the spatial index).</summary>
    public static string Line2kWithLabels() =>
        ChartBuilder.Create().AsInteractive().ShowDataLabels()
            .Series(s => s.AddLine("A", Wave2k))
            .RenderToSvg();

    /// <summary>Sliding bar labels + line obstacles + point labels on a dense category axis.</summary>
    public static string Stacked300WithLabels() =>
        ChartBuilder.Create().AsInteractive().StackNormal().ShowDataLabels()
            .XAxis(x => x.Categories.AddRange(Cats300))
            .Series(s => s
                .AddColumn("A", Wave300A).AddColumn("B", Wave300B).AddColumn("C", Wave300C)
                .AddLine("L", Wave300L))
            .RenderToSvg();

    public static string Scatter5kInteractive() =>
        ChartBuilder.Create().AsInteractive()
            .Series(s => s.AddScatter("A", Wave5k))
            .RenderToSvg();

    public static string Column1kInteractive() =>
        ChartBuilder.Create().AsInteractive()
            .XAxis(x => x.Categories.AddRange(Cats1k))
            .Series(s => s.AddColumn("A", Wave1k))
            .RenderToSvg();

    public static readonly IReadOnlyList<Scenario> All = new[]
    {
        new Scenario("small column (6 pts, interactive)",        2000, SmallColumnInteractive),
        new Scenario("showcase stacked + line + labels (6 cats)", 1000, ShowcaseStackedWithLabels),
        new Scenario("line 5k pts (interactive)",                  20, Line5kInteractive),
        new Scenario("line 50k pts (static)",                       5, Line50kStatic),
        new Scenario("line 2k pts + data labels (interactive)",     5, Line2kWithLabels),
        new Scenario("stacked col 3x300 + labels + line labels",    5, Stacked300WithLabels),
        new Scenario("scatter 5k (interactive)",                   10, Scatter5kInteractive),
        new Scenario("column 1k cats (interactive)",               10, Column1kInteractive),
    };

    // Deterministic random walk so every run renders identical charts.
    private static double?[] Wave(int n, int seed, double start = 100)
    {
        var rng = new Random(seed);
        var data = new double?[n];
        double v = start;
        for (int i = 0; i < n; i++)
        {
            v += rng.NextDouble() * 10 - 5;
            data[i] = Math.Round(Math.Abs(v), 1);
        }
        return data;
    }

    private static string[] Categories(int n)
    {
        var cats = new string[n];
        for (int i = 0; i < n; i++) cats[i] = "C" + i;
        return cats;
    }
}
