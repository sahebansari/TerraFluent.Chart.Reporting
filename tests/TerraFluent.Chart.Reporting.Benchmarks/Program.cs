using BenchmarkDotNet.Running;
using TerraFluent.Chart.Reporting.Benchmarks;

// Usage (always Release):
//   dotnet run -c Release --project tests/TerraFluent.Chart.Reporting.Benchmarks
//       → BenchmarkDotNet picker for RenderBenchmarks (typical charts) and LargeChartBenchmarks
//   dotnet run -c Release --project tests/TerraFluent.Chart.Reporting.Benchmarks -- --filter *Large*
//       → any BenchmarkDotNet arguments are passed through
//   dotnet run -c Release --project tests/TerraFluent.Chart.Reporting.Benchmarks -- quick
//       → fast Stopwatch table (seconds, not minutes) for a before/after check while developing
//   dotnet run -c Release --project tests/TerraFluent.Chart.Reporting.Benchmarks -- profile
//       → which object types dominate allocations in the large scenarios

switch (args.Length > 0 ? args[0].ToLowerInvariant() : string.Empty)
{
    case "quick":
        QuickRunner.Run();
        break;
    case "profile":
        AllocationProfiler.Run();
        break;
    default:
        BenchmarkSwitcher
            .FromTypes(new[] { typeof(RenderBenchmarks), typeof(LargeChartBenchmarks) })
            .Run(args);
        break;
}
