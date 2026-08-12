using System.Globalization;
using System.Text;
using TerraFluent.AutoAnalytics.Dashboard;
using TerraFluent.AutoAnalytics.Engine;

namespace TerraFluent.Chart.Reporting.Samples;

/// <summary>
/// Showcases the TerraFluent.AutoAnalytics engine: builds a raw sales dataset, runs the deterministic
/// analysis pipeline, and writes the auto-generated dashboard to <c>dashboard.html</c>. The HTML
/// rendering itself lives in <c>IndexHtml.cs</c> (<see cref="Program.BuildDashboardHtml"/>).
/// </summary>
internal static class AutoAnalyticsDashboardSample
{
    /// <summary>Runs the analysis and writes <c>dashboard.html</c> into the output folder.</summary>
    public static void Generate(string outputDir)
    {
        string csv = BuildSampleSalesCsv();

        // One call performs schema discovery, validation, profiling, analytics,
        // insight generation and chart recommendation — all deterministic, no AI.
        AnalyticsResult result = AnalyticsEngine.AnalyzeCsv(csv, new AnalyticsOptions
        {
            DatasetName = "Global Sales 2023–2024",
            MaxRecommendations = 24
        });

        DashboardDefinition dashboard = DashboardBuilder.Generate(result);
        string html = Program.BuildDashboardHtml(dashboard, result);
        File.WriteAllText(Path.Combine(outputDir, "dashboard.html"), html, Encoding.UTF8);
    }

    // ── Sample data: 24 monthly rows, 3 regions, 3 products, with one deliberate anomaly ─────────
    private static string BuildSampleSalesCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Month,Region,Product,Revenue,Cost,Units");

        string[] regions = { "North America", "Europe", "Asia Pacific" };
        string[] products = { "Alpha", "Beta", "Gamma" };
        var start = new DateTime(2023, 1, 1);

        for (int i = 0; i < 24; i++)
        {
            var month = start.AddMonths(i);
            string region = regions[i % regions.Length];
            string product = products[(i / 2) % products.Length];

            // Steadily rising revenue with mild seasonality.
            double baseRevenue = 12000 + i * 650 + Math.Sin(i / 2.0) * 1400;
            // Inject a single anomalous spike in month 15.
            if (i == 15) baseRevenue *= 3.1;

            double revenue = Math.Round(baseRevenue, 0);
            double cost = Math.Round(revenue * (0.55 + (i % 3) * 0.03), 0);
            int units = (int)Math.Round(revenue / 95.0);

            sb.AppendLine(string.Join(",",
                month.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                region, product,
                revenue.ToString(CultureInfo.InvariantCulture),
                cost.ToString(CultureInfo.InvariantCulture),
                units.ToString(CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }
}
