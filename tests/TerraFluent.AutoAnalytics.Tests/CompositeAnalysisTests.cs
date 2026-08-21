using System.Linq;
using System.Text;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Recommendation;
using Xunit;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;
using Stacking = TerraFluent.Chart.Reporting.Enums.Stacking;

namespace TerraFluent.AutoAnalytics.Tests;

public class CompositeAnalysisTests
{
    // 12 months, each month carries BOTH regions (genuine co-occurrence) so a stacked breakdown
    // is meaningful. Revenue is additive; Age is a non-additive per-row attribute.
    private static string TwoRegionMonthlyCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Month,Region,Revenue,Age");
        var start = new System.DateTime(2023, 1, 1);
        for (int i = 0; i < 12; i++)
        {
            var month = start.AddMonths(i).ToString("yyyy-MM-dd");
            sb.AppendLine($"{month},East,{1000 + i * 50},{30 + i}");
            sb.AppendLine($"{month},West,{700 + i * 40},{40 + i}");
        }
        return sb.ToString();
    }

    // Each month has exactly ONE region (rotating) — a one-hot layout that must NOT become a stack.
    private static string RotatingRegionMonthlyCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Month,Region,Revenue");
        string[] regions = { "East", "West", "North" };
        var start = new System.DateTime(2023, 1, 1);
        for (int i = 0; i < 12; i++)
        {
            var month = start.AddMonths(i).ToString("yyyy-MM-dd");
            sb.AppendLine($"{month},{regions[i % 3]},{1000 + i * 50}");
        }
        return sb.ToString();
    }

    [Fact]
    public void MovingAverage_ProducesGaplessSpline()
    {
        var result = AnalyticsEngine.AnalyzeCsv(TwoRegionMonthlyCsv());

        Assert.NotEmpty(result.Findings.MovingAverages);

        var spline = result.Recommendations.FirstOrDefault(r =>
            r.ChartType == ChartType.Spline && r.Spec.Title.Contains("Moving Avg"));
        Assert.NotNull(spline);

        // No leading warm-up gap: every plotted value is present.
        Assert.All(spline!.Spec.Series.Single().Values, v => Assert.True(v.HasValue));
        Assert.Equal(spline.Spec.Categories.Count, spline.Spec.Series.Single().Values.Count);
    }

    [Fact]
    public void Cumulative_IsAdditiveOnlyAndMonotonic()
    {
        var result = AnalyticsEngine.AnalyzeCsv(TwoRegionMonthlyCsv());

        var cumul = result.Findings.CumulativeSeries;
        Assert.Contains(cumul, c => c.Measure == "Revenue");
        // Age is non-additive: a running total is meaningless, so it is never produced.
        Assert.DoesNotContain(cumul, c => c.Measure == "Age");

        var revenue = cumul.First(c => c.Measure == "Revenue");
        for (int i = 1; i < revenue.Points.Count; i++)
            Assert.True(revenue.Points[i].Cumulative >= revenue.Points[i - 1].Cumulative);
    }

    [Fact]
    public void Composition_StacksGenuineCoOccurrence()
    {
        var result = AnalyticsEngine.AnalyzeCsv(TwoRegionMonthlyCsv());

        var stacked = result.Recommendations.FirstOrDefault(r =>
            r.Spec.StackingMode == Stacking.Normal && r.Spec.Series.Count >= 2);
        Assert.NotNull(stacked);
        Assert.True(stacked!.Spec.Series.Count >= 2);
        // A composition is only over an additive measure — never the averaged Age attribute.
        Assert.DoesNotContain("Age", stacked.Spec.Title);
    }

    [Fact]
    public void Composition_RejectsOneHotRotatingData()
    {
        var result = AnalyticsEngine.AnalyzeCsv(RotatingRegionMonthlyCsv());

        // Each period has a single region, so no honest stacked breakdown exists.
        Assert.Empty(result.Findings.Compositions);
        Assert.DoesNotContain(result.Recommendations, r => r.Spec.StackingMode != Stacking.None);
    }
}
