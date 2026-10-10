using System.Collections.Concurrent;
using System.Diagnostics.Tracing;

namespace TerraFluent.Chart.Reporting.Benchmarks;

/// <summary>
/// Shows which object types dominate allocations in the large scenarios, using the runtime's
/// sampled <c>GCAllocationTick</c> events (one roughly every 100 KB allocated). No external
/// profiler needed; the percentages are approximate but point straight at the hot type.
/// </summary>
public static class AllocationProfiler
{
    private static readonly string[] ProfiledScenarios =
    {
        "line 5k pts (interactive)",
        "line 50k pts (static)",
        "line 2k pts + data labels (interactive)",
        "scatter 5k (interactive)",
    };

    public static void Run()
    {
        using var listener = new AllocationTickListener();
        foreach (var name in ProfiledScenarios)
        {
            var scenario = LargeScenarios.All.First(s => s.Name == name);
            scenario.Render();   // warm-up / JIT outside the sample window

            listener.Recording = true;
            for (int i = 0; i < 3; i++) scenario.Render();
            listener.Recording = false;
            Thread.Sleep(500);   // let the event pipe drain

            listener.PrintAndReset(name);
        }
    }

    private sealed class AllocationTickListener : EventListener
    {
        private readonly ConcurrentDictionary<string, long> _bytesByType = new();
        public volatile bool Recording;

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name == "Microsoft-Windows-DotNETRuntime")
                EnableEvents(source, EventLevel.Verbose, (EventKeywords)0x1 /* GC */);
        }

        protected override void OnEventWritten(EventWrittenEventArgs e)
        {
            if (!Recording || e.EventName == null || !e.EventName.StartsWith("GCAllocationTick", StringComparison.Ordinal))
                return;
            var names = e.PayloadNames!;
            int typeIdx   = names.IndexOf("TypeName");
            int amountIdx = names.IndexOf("AllocationAmount64");
            if (amountIdx < 0) amountIdx = names.IndexOf("AllocationAmount");
            if (amountIdx < 0) return;

            string type  = typeIdx >= 0 ? e.Payload![typeIdx]?.ToString() ?? "?" : "?";
            long  amount = Convert.ToInt64(e.Payload![amountIdx]);
            _bytesByType.AddOrUpdate(type, amount, (_, v) => v + amount);
        }

        public void PrintAndReset(string title)
        {
            long total = _bytesByType.Values.Sum();
            Console.WriteLine($"--- {title}: top allocated types (sampled) ---");
            if (total == 0) { Console.WriteLine("  (no samples)"); return; }
            foreach (var kv in _bytesByType.OrderByDescending(k => k.Value).Take(8))
                Console.WriteLine($"  {kv.Value * 100.0 / total,5:F1}%  {kv.Key}");
            _bytesByType.Clear();
        }
    }
}
