using System.Diagnostics;

namespace TerraFluent.Chart.Reporting.Benchmarks;

/// <summary>
/// Fast, approximate measurement (seconds instead of BenchmarkDotNet's minutes): average time and
/// managed allocation per render for each <see cref="LargeScenarios"/> entry. Good for a quick
/// before/after check while optimising; use BenchmarkDotNet for numbers you intend to publish.
/// </summary>
public static class QuickRunner
{
    public static void Run()
    {
        Console.WriteLine($"{"scenario",-46} {"ms/render",10} {"KB alloc/render",16} {"KB output",10}");
        foreach (var s in LargeScenarios.All)
        {
            for (int w = 0; w < Math.Min(3, s.QuickIterations); w++) s.Render();   // warm-up / JIT
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long before = GC.GetAllocatedBytesForCurrentThread();
            var sw = Stopwatch.StartNew();
            int length = 0;
            for (int i = 0; i < s.QuickIterations; i++) length = s.Render().Length;
            sw.Stop();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Console.WriteLine(
                $"{s.Name,-46} {sw.Elapsed.TotalMilliseconds / s.QuickIterations,10:F2} " +
                $"{allocated / 1024.0 / s.QuickIterations,16:F0} {length * 2 / 1024.0,10:F0}");
        }
    }
}
