using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TerraFluent.AutoAnalytics.Agent;
using TerraFluent.AutoAnalytics.Agent.Skills;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Enums;
using Xunit;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

namespace TerraFluent.AutoAnalytics.Tests;

/// <summary>
/// Covers anomaly attribution: tracing an outlier back to its row, naming when it happened, and
/// deciding whether any dimension accounts for it.
/// </summary>
public class AnomalyExplanationTests
{
    // ── Fixtures ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Two regions on very different scales. Every Enterprise row is ~10x a SMB row, so an Enterprise
    /// value looks extreme against the dataset but is perfectly ordinary for its own segment.
    /// </summary>
    private static string SegmentMixCsv()
    {
        var sb = new StringBuilder("Month,Segment,Revenue\n");
        var start = new DateTime(2024, 1, 1);
        for (int m = 0; m < 12; m++)
        {
            string month = start.AddMonths(m).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            sb.Append(month).Append(",SMB,").Append(1000 + m * 10).Append('\n');
            sb.Append(month).Append(",SMB,").Append(1050 + m * 10).Append('\n');
            sb.Append(month).Append(",SMB,").Append(980 + m * 10).Append('\n');
            sb.Append(month).Append(",Enterprise,").Append(20000 + m * 50).Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>
    /// A uniform dataset with one deliberate spike that belongs to no distinctive segment — its own
    /// category's rows are all normal, so nothing explains it.
    /// </summary>
    private static string GenuineSpikeCsv()
    {
        var sb = new StringBuilder("Month,Region,Revenue\n");
        var start = new DateTime(2024, 1, 1);
        string[] regions = { "East", "West" };
        for (int m = 0; m < 12; m++)
        {
            string month = start.AddMonths(m).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            for (int r = 0; r < regions.Length; r++)
            {
                // One extreme value in East, month 9. Everything else sits in a tight band.
                double revenue = (m == 9 && r == 0) ? 250_000 : 10_000 + m * 20 + r * 15;
                sb.Append(month).Append(',').Append(regions[r]).Append(',')
                  .Append(revenue.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }
        }
        return sb.ToString();
    }

    private static AnalyticsResult Analyze(string csv, AnalyticsOptions? options = null) =>
        AnalyticsEngine.AnalyzeCsv(csv, options ?? new AnalyticsOptions { MaxInsights = 100, MaxRecommendations = 60 });

    // ── Row traceability (the blocker this feature rested on) ─────────────────

    [Fact]
    public void AnomalyPoint_CarriesTheDatasetRowNotJustItsPositionAmongPresentValues()
    {
        // A missing cell early in the column makes the compacted position diverge from the row,
        // which is exactly the case that made anomalies untraceable before.
        const string csv =
            "Month,Region,Revenue\n" +
            "2024-01-01,East,100\n" +
            "2024-02-01,East,\n" +      // missing => not in NumericValues, but still a row
            "2024-03-01,East,105\n" +
            "2024-04-01,East,98\n" +
            "2024-05-01,East,102\n" +
            "2024-06-01,East,99\n" +
            "2024-07-01,East,101\n" +
            "2024-08-01,East,5000\n";

        var findings = Analyze(csv).Findings;
        var peak = findings.Anomalies.Single(a => a.Measure == "Revenue").Anomalies[0];

        Assert.Equal(5000, peak.Value);
        Assert.Equal(7, peak.RowIndex);           // eighth row of the dataset
        Assert.Equal(6, peak.Index);              // seventh non-missing value
        Assert.NotEqual(peak.Index, peak.RowIndex);
    }

    [Fact]
    public void AnomalyPoint_IsLabelledWithTheDateOfItsRow()
    {
        var findings = Analyze(GenuineSpikeCsv()).Findings;
        var peak = findings.Anomalies.Single(a => a.Measure == "Revenue").Anomalies[0];

        Assert.Equal(250_000, peak.Value);
        Assert.Equal("2024-10-01", peak.Label);   // the tenth month (index 9)
    }

    [Fact]
    public void AnomalyPoint_FallsBackToARowPositionWithoutADateColumn()
    {
        const string csv =
            "Region,Revenue\nEast,100\nEast,105\nEast,98\nEast,102\nEast,99\nEast,5000\n";

        var peak = Analyze(csv).Findings.Anomalies.Single().Anomalies[0];
        Assert.Equal("row 6", peak.Label);
    }

    // ── Explanation: segment mix ──────────────────────────────────────────────

    [Fact]
    public void Explanation_RecognisesAnOutlierThatIsNormalForItsSegment()
    {
        var findings = Analyze(SegmentMixCsv()).Findings;

        var explanation = Assert.Single(
            findings.AnomalyExplanations.Where(e => e.Measure == "Revenue").Take(1));

        Assert.True(explanation.IsExplained);
        var best = explanation.BestExplanation!;
        Assert.Equal("Segment", best.Dimension);
        Assert.Equal("Enterprise", best.Category);
        Assert.True(best.ExplainedFraction >= 0.5);
        // Ordinary for its own segment: close to that segment's median.
        Assert.InRange(best.ResidualRatio, 0.9, 1.1);
    }

    [Fact]
    public void Explanation_RecognisesAGenuinelyUnexplainedSpike()
    {
        var findings = Analyze(GenuineSpikeCsv()).Findings;
        var explanation = findings.AnomalyExplanations.First(e => e.Measure == "Revenue");

        Assert.False(explanation.IsExplained);
        var best = explanation.BestExplanation!;
        // Even within its own region the value is far from normal.
        Assert.True(best.ResidualRatio > 5);
        Assert.True(best.ExplainedFraction < 0.5);
    }

    [Fact]
    public void Explanation_ExcludesTheAnomalousRowFromItsOwnBaseline()
    {
        var findings = Analyze(GenuineSpikeCsv()).Findings;
        var best = findings.AnomalyExplanations.First(e => e.Measure == "Revenue").BestExplanation!;

        // 12 East rows exist; the outlier itself must not be one of the 11 used for the median.
        Assert.Equal(11, best.CategoryCount);
        Assert.True(best.CategoryMedian < 20_000, "the spike must not drag its own baseline up");
    }

    [Fact]
    public void Explanation_TiesBackToTheAnomalyRowItDescribes()
    {
        var findings = Analyze(GenuineSpikeCsv()).Findings;
        var peak = findings.Anomalies.Single(a => a.Measure == "Revenue").Anomalies[0];
        var explanation = findings.AnomalyExplanations.First(e => e.Measure == "Revenue");

        Assert.Equal(peak.RowIndex, explanation.RowIndex);
        Assert.Equal(peak.Label, explanation.WhenLabel);
        Assert.Equal(peak.Value, explanation.Value);
    }

    [Fact]
    public void Explanation_IsSkippedWhenNoDimensionIsUsable()
    {
        // No categorical column at all, so there is nothing to attribute the outlier to.
        const string csv = "Month,Revenue\n" +
            "2024-01-01,100\n2024-02-01,105\n2024-03-01,98\n" +
            "2024-04-01,102\n2024-05-01,99\n2024-06-01,9000\n";

        Assert.Empty(Analyze(csv).Findings.AnomalyExplanations);
    }

    [Fact]
    public void Explanation_CanBeDisabled()
    {
        var findings = Analyze(GenuineSpikeCsv(), new AnalyticsOptions { EnableAnomalyExplanation = false }).Findings;

        Assert.Empty(findings.AnomalyExplanations);
        Assert.NotEmpty(findings.Anomalies);      // detection itself is unaffected
    }

    [Fact]
    public void Explanation_IsDeterministic()
    {
        var first = Analyze(SegmentMixCsv()).Findings.AnomalyExplanations;
        var second = Analyze(SegmentMixCsv()).Findings.AnomalyExplanations;

        Assert.Equal(
            first.Select(e => (e.Measure, e.RowIndex, e.BestExplanation?.Category, e.IsExplained)),
            second.Select(e => (e.Measure, e.RowIndex, e.BestExplanation?.Category, e.IsExplained)));
    }

    // ── The merged narrative ──────────────────────────────────────────────────

    [Fact]
    public void AnomalyInsight_ReportsSegmentMixRatherThanACrisis()
    {
        var insight = Analyze(SegmentMixCsv()).Insights.First(i => i.Kind == InsightKind.Anomaly);

        Assert.Contains("normal for that", insight.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("segment mix", insight.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Enterprise", insight.Description);
        Assert.Equal("Segment=Enterprise", insight.Evidence["explainedBy"]);
        Assert.Contains("Segment", insight.RelatedColumns);
    }

    [Fact]
    public void AnomalyInsight_CallsOutAnUnexplainedOutlier()
    {
        var insight = Analyze(GenuineSpikeCsv()).Insights.First(i => i.Kind == InsightKind.Anomaly);

        Assert.Contains("unexplained", insight.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No dimension accounts for it", insight.Description);
        Assert.Contains("2024-10-01", insight.Description);   // the "when", from the row's date
        Assert.True(insight.Evidence.ContainsKey("explainedFraction"));
    }

    [Fact]
    public void AnomalyInsight_StaysASingleInsightPerMeasure()
    {
        // The explanation is folded into the anomaly insight rather than emitted alongside it.
        var anomalyInsights = Analyze(GenuineSpikeCsv()).Insights
            .Where(i => i.Kind == InsightKind.Anomaly && i.RelatedColumns.Contains("Revenue"))
            .ToList();

        Assert.Single(anomalyInsights);
    }

    // ── Chart ─────────────────────────────────────────────────────────────────

    [Fact]
    public void AnomalyContextChart_ComparesTheOutlierAgainstEachSegmentNorm()
    {
        var recommendations = Analyze(GenuineSpikeCsv()).Recommendations;
        var chart = recommendations.First(r => r.Spec.Title.Contains("vs segment norms", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(2, chart.Spec.Series.Count);
        var norms = chart.Spec.Series[0];
        var outlier = chart.Spec.Series[1];

        Assert.Equal(chart.Spec.Categories.Count, norms.Values.Count);
        Assert.All(outlier.Values, v => Assert.Equal(250_000, v));
        Assert.All(norms.Values, v => Assert.True(v < 250_000));

        // An unexplained anomaly is the more urgent finding, so it outranks an explained one.
        Assert.True(chart.SuitabilityScore >= 80);
    }

    [Fact]
    public void AnomalyContextChart_RendersToSvg()
    {
        var chart = Analyze(GenuineSpikeCsv()).Recommendations
            .First(r => r.Spec.Title.Contains("vs segment norms", StringComparison.OrdinalIgnoreCase));

        string svg = Recommendation.ChartConfigBuilder.ToSvg(chart.Spec);
        Assert.StartsWith("<svg", svg);
        Assert.DoesNotContain("NaN", svg);
    }

    // ── Agent ─────────────────────────────────────────────────────────────────

    private static AnalyticAgent BuildAgent() => new(
        new AnalyticsEngine(),
        new IAnalyticSkill[]
        {
            new TrendSkill(), new AnomalySkill(), new CorrelationSkill(), new DominanceSkill(),
            new ForecastSkill(), new RootCauseSkill(), new SegmentSkill(), new ComparisonSkill()
        });

    [Fact]
    public void Agent_SkipsRootCauseDrillDownForAnAlreadyExplainedAnomaly()
    {
        var result = Analyze(SegmentMixCsv());
        var trace = BuildAgent().Investigate(result, "any anomalies in revenue?");

        var anomalyStep = trace.Steps.First(s => s.SkillName == "Anomaly");
        Assert.Contains("already attributed to a segment", anomalyStep.Rationale);
    }

    [Fact]
    public void Agent_StillDrillsIntoAnUnexplainedAnomaly()
    {
        var result = Analyze(GenuineSpikeCsv());
        var trace = BuildAgent().Investigate(result, "any anomalies in revenue?");

        var anomalyStep = trace.Steps.First(s => s.SkillName == "Anomaly");
        Assert.Contains("raised root-cause drill-downs", anomalyStep.Rationale);
    }
}
