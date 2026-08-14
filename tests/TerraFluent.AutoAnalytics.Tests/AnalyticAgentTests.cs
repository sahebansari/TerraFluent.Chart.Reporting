using System.Linq;
using TerraFluent.AutoAnalytics.Agent;
using TerraFluent.AutoAnalytics.Agent.Skills;
using TerraFluent.AutoAnalytics.Engine;
using Xunit;

namespace TerraFluent.AutoAnalytics.Tests;

public class AnalyticAgentTests
{
    // Rising revenue concentrated in East, with one deliberate spike in the last East row.
    private const string SalesCsv =
        "Month,Region,Revenue,Cost\n" +
        "2024-01,East,10000,6000\n" +
        "2024-02,West,7000,4200\n" +
        "2024-03,East,12000,7000\n" +
        "2024-04,West,7500,4400\n" +
        "2024-05,East,14000,8000\n" +
        "2024-06,West,8000,4600\n" +
        "2024-07,East,16000,9000\n" +
        "2024-08,West,8500,4800\n" +
        "2024-09,East,45000,9200\n";

    private static AnalyticAgent BuildAgent() => new(
        new AnalyticsEngine(),
        new IAnalyticSkill[] { new TrendSkill(), new AnomalySkill(), new CorrelationSkill(), new DominanceSkill(), new ForecastSkill(), new RootCauseSkill(), new SegmentSkill(), new ComparisonSkill() });

    [Fact]
    public void Explore_ProducesTraceWithStepsAndInsights()
    {
        var trace = BuildAgent().InvestigateCsv(SalesCsv);

        Assert.NotEmpty(trace.Steps);
        Assert.NotEmpty(trace.Insights);
        Assert.False(string.IsNullOrWhiteSpace(trace.Headline));
        Assert.Contains("step", trace.Narrative);
    }

    [Fact]
    public void Explore_ChainsAnomalyOrTrendIntoDominanceDrillDown()
    {
        var trace = BuildAgent().InvestigateCsv(SalesCsv);

        // The agent must reach a Dominance drill-down as a follow-up (not a broad Explore sweep).
        var dominanceStep = trace.Steps.FirstOrDefault(s => s.SkillName == "Dominance");
        Assert.NotNull(dominanceStep);
        Assert.False(string.IsNullOrWhiteSpace(dominanceStep!.Trigger));
    }

    [Fact]
    public void Investigation_IsDeterministic()
    {
        var agent = BuildAgent();
        var a = agent.InvestigateCsv(SalesCsv);
        var b = agent.InvestigateCsv(SalesCsv);

        Assert.Equal(a.Headline, b.Headline);
        Assert.Equal(a.Steps.Count, b.Steps.Count);
        Assert.Equal(
            a.Insights.Select(i => i.Title),
            b.Insights.Select(i => i.Title));
    }

    [Fact]
    public void Insights_AreDeduplicatedByTitle()
    {
        var trace = BuildAgent().InvestigateCsv(SalesCsv);
        var titles = trace.Insights.Select(i => i.Title).ToList();
        Assert.Equal(titles.Count, titles.Distinct().Count());
    }

    [Fact]
    public void TargetedAnomalyQuestion_RestrictsToAnomalySkill()
    {
        var trace = BuildAgent().InvestigateCsv(SalesCsv, "show me anomalies in Revenue");

        Assert.NotEmpty(trace.Steps);
        // An anomaly goal runs the anomaly skill plus its dominance/root-cause drill-downs.
        Assert.All(trace.Steps, s => Assert.Contains(s.SkillName, new[] { "Anomaly", "Dominance", "RootCause" }));
    }

    [Fact]
    public void GoalParser_MapsKeywordsToKinds()
    {
        var result = AnalyticsEngine.AnalyzeCsv(SalesCsv);

        Assert.Equal(GoalKind.Trend, GoalParser.Parse("what is the revenue trend?", result.Profile).Kind);
        Assert.Equal(GoalKind.Anomaly, GoalParser.Parse("any outliers?", result.Profile).Kind);
        Assert.Equal(GoalKind.Correlation, GoalParser.Parse("what drives revenue?", result.Profile).Kind);
        Assert.Equal(GoalKind.Dominance, GoalParser.Parse("which region leads?", result.Profile).Kind);
        Assert.Equal(GoalKind.Explore, GoalParser.Parse(null, result.Profile).Kind);
    }

    [Fact]
    public void GoalParser_ResolvesMentionedColumns()
    {
        var result = AnalyticsEngine.AnalyzeCsv(SalesCsv);
        var goal = GoalParser.Parse("show me the Revenue trend", result.Profile);

        Assert.Equal(GoalKind.Trend, goal.Kind);
        Assert.Contains("Revenue", goal.TargetColumns);
    }

    [Fact]
    public void ExploreChainsIntoForecastAndRootCause()
    {
        var trace = BuildAgent().InvestigateCsv(SalesCsv);

        // The rising-revenue trend should trigger forecast + root-cause drill-downs.
        Assert.Contains(trace.Steps, s => s.SkillName == "Forecast");
        Assert.Contains(trace.Steps, s => s.SkillName == "RootCause");
    }

    [Fact]
    public void ForecastQuestion_ProducesForecastInsightAndChart()
    {
        var trace = BuildAgent().InvestigateCsv(SalesCsv, "forecast Revenue");

        Assert.Contains(trace.Steps, s => s.SkillName == "Forecast");
        Assert.Contains(trace.Insights, i => i.Title.Contains("projected"));
        Assert.Contains(trace.Charts, c => c.Spec.Title.Contains("forecast"));
    }

    [Fact]
    public void RootCauseQuestion_AttributesChangeToACategory()
    {
        var trace = BuildAgent().InvestigateCsv(SalesCsv, "why did Revenue change?");

        Assert.Contains(trace.Steps, s => s.SkillName == "RootCause");
        Assert.All(trace.Steps, s => Assert.Equal("RootCause", s.SkillName));
        Assert.NotEmpty(trace.Insights);
    }

    [Fact]
    public void GoalParser_MapsForecastAndRootCause()
    {
        var result = AnalyticsEngine.AnalyzeCsv(SalesCsv);

        Assert.Equal(GoalKind.Forecast, GoalParser.Parse("forecast revenue next quarter", result.Profile).Kind);
        Assert.Equal(GoalKind.RootCause, GoalParser.Parse("why did revenue drop?", result.Profile).Kind);
        // "what drives revenue" must still map to Correlation (not RootCause).
        Assert.Equal(GoalKind.Correlation, GoalParser.Parse("what drives revenue?", result.Profile).Kind);
    }

    [Fact]
    public void SegmentQuestion_ProducesSegmentStepAndChart()
    {
        var trace = BuildAgent().InvestigateCsv(SalesCsv, "segment the data into natural groups");

        Assert.Contains(trace.Steps, s => s.SkillName == "Segment");
        Assert.All(trace.Steps, s => Assert.Equal("Segment", s.SkillName));
        Assert.Contains(trace.Charts, c => c.Spec.Title.Contains("Segment"));
    }

    [Fact]
    public void GoalParser_MapsSegmentKeywords()
    {
        var result = AnalyticsEngine.AnalyzeCsv(SalesCsv);
        Assert.Equal(GoalKind.Segment, GoalParser.Parse("cluster the customers", result.Profile).Kind);
        Assert.Equal(GoalKind.Segment, GoalParser.Parse("find natural segments", result.Profile).Kind);
    }
}
