using System.Linq;
using TerraFluent.AutoAnalytics.Agent;
using TerraFluent.AutoAnalytics.Agent.Skills;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Enums;
using Xunit;

namespace TerraFluent.AutoAnalytics.Tests;

public class ComparisonAndSessionTests
{
    private const string MonthlyCsv =
        "Month,Region,Revenue,Cost\n" +
        "2024-01,East,10000,6000\n" +
        "2024-02,West,11000,6300\n" +
        "2024-03,East,12500,7000\n" +
        "2024-04,West,13000,7200\n" +
        "2024-05,East,14000,7800\n" +
        "2024-06,West,15500,8300\n" +
        "2024-07,East,17000,9000\n" +
        "2024-08,West,18500,9600\n";

    // Two years of the same months so year-over-year can be computed.
    private const string TwoYearCsv =
        "Month,Revenue\n" +
        "2023-01,100\n" +
        "2023-02,120\n" +
        "2024-01,150\n" +
        "2024-02,180\n";

    private static AnalyticAgent BuildAgent() => new(
        new AnalyticsEngine(),
        new IAnalyticSkill[]
        {
            new TrendSkill(), new AnomalySkill(), new CorrelationSkill(), new DominanceSkill(),
            new ForecastSkill(), new RootCauseSkill(), new SegmentSkill(), new ComparisonSkill()
        });

    // ── Period-over-period engine ────────────────────────────────────────────────

    [Fact]
    public void Compare_ComputesMonthOverMonthChange()
    {
        var profile = AnalyticsEngine.AnalyzeCsv(MonthlyCsv).Profile;
        var result = new PeriodComparisonEngine().Compare(profile, "Revenue", DateGranularity.Monthly);

        Assert.NotNull(result);
        Assert.False(result!.IsEmpty);
        // Latest (Aug 18500) vs previous (Jul 17000) = +1500 / 17000 ≈ 8.8%.
        Assert.Equal(1500, result.LatestChangeAbs, 3);
        Assert.True(result.LatestChangePct > 0.08 && result.LatestChangePct < 0.09);
    }

    [Fact]
    public void Compare_ComputesYearOverYear()
    {
        var profile = AnalyticsEngine.AnalyzeCsv(TwoYearCsv).Profile;
        var result = new PeriodComparisonEngine().Compare(profile, "Revenue", DateGranularity.Monthly)!;

        // Latest 2024-02 (180) vs same month prior year 2023-02 (120) = +50%.
        Assert.NotNull(result.YearOverYearPct);
        Assert.Equal(0.5, result.YearOverYearPct!.Value, 3);
    }

    [Fact]
    public void CompareQuestion_ProducesComparisonStep()
    {
        var trace = BuildAgent().InvestigateCsv(MonthlyCsv, "compare Revenue to last month");

        Assert.Contains(trace.Steps, s => s.SkillName == "Comparison");
        Assert.Contains(trace.Insights, i => i.Title.Contains("month-over-month"));
    }

    [Fact]
    public void GoalParser_MapsCompareKeywords()
    {
        var profile = AnalyticsEngine.AnalyzeCsv(MonthlyCsv).Profile;
        Assert.Equal(GoalKind.Compare, GoalParser.Parse("month over month change", profile).Kind);
        Assert.Equal(GoalKind.Compare, GoalParser.Parse("compare revenue vs last quarter", profile).Kind);
        // "next month" stays a Forecast, not a Compare.
        Assert.Equal(GoalKind.Forecast, GoalParser.Parse("what will revenue be next month?", profile).Kind);
    }

    // ── Session memory ───────────────────────────────────────────────────────────

    [Fact]
    public void Session_DoesNotRepeatInsightsAcrossTurns()
    {
        var session = BuildAgent().StartSession(new Data.Sources.CsvDataSource(MonthlyCsv).Load());

        var first = session.Ask(null);            // open-ended explore
        var second = session.Ask(null);           // same question again

        Assert.NotEmpty(first.Insights);
        // Everything was already surfaced, so the second turn adds nothing new.
        Assert.Empty(second.Insights);
        Assert.Equal(2, session.History.Count);
    }

    [Fact]
    public void Session_AnswersDirectedQuestion_EvenWhenAskedBefore()
    {
        var session = BuildAgent().StartSession(new Data.Sources.CsvDataSource(MonthlyCsv).Load());

        var first = session.Ask("which region has the highest revenue");
        var second = session.Ask("which region has the highest revenue"); // same explicit question again

        // The user's own directed question is answered every time — not suppressed as a "repeat".
        Assert.NotEmpty(first.Insights);
        Assert.NotEmpty(second.Insights);
        Assert.NotEqual("No new findings for this question.", second.Headline);
        Assert.Equal(first.Headline, second.Headline);
    }

    [Fact]
    public void Session_ResolvesPronounToPreviousFocus()
    {
        var session = BuildAgent().StartSession(new Data.Sources.CsvDataSource(MonthlyCsv).Load());

        session.Ask("forecast Revenue");              // focus becomes Revenue
        var followUp = session.Ask("now compare it");  // "it" → Revenue

        Assert.Contains(followUp.Steps, s => s.SkillName == "Comparison");
        Assert.Contains(followUp.Insights, i => i.RelatedColumns.Contains("Revenue"));
    }

    [Fact]
    public void Session_HistoryRecordsEachTurn()
    {
        var session = BuildAgent().StartSession(new Data.Sources.CsvDataSource(MonthlyCsv).Load());
        session.Ask("segment the data");
        session.Ask("forecast Revenue");

        Assert.Equal(2, session.History.Count);
        Assert.All(session.History, t => Assert.False(string.IsNullOrWhiteSpace(t.Goal)));
    }
}
