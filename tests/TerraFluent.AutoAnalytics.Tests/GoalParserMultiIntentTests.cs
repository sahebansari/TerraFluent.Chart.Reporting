using System.Linq;
using TerraFluent.AutoAnalytics.Agent;
using TerraFluent.AutoAnalytics.Agent.Skills;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Profiling;
using Xunit;

namespace TerraFluent.AutoAnalytics.Tests;

/// <summary>
/// Covers intent <em>scoring</em> in <see cref="GoalParser"/>: a compound question must raise every
/// intent it expresses, ranked, rather than stopping at the first keyword table entry that matches.
/// </summary>
public class GoalParserMultiIntentTests
{
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

    private static DatasetProfile Profile() => AnalyticsEngine.AnalyzeCsv(SalesCsv).Profile;

    private static AnalyticAgent BuildAgent() => new(
        new AnalyticsEngine(),
        new IAnalyticSkill[]
        {
            new TrendSkill(), new AnomalySkill(), new CorrelationSkill(), new DominanceSkill(),
            new ForecastSkill(), new RootCauseSkill(), new SegmentSkill(), new ComparisonSkill()
        });

    // ── Ranking ───────────────────────────────────────────────────────────────

    [Fact]
    public void ParseAll_RaisesEveryIntentInACompoundQuestion()
    {
        var kinds = GoalParser.ParseAll("why is the top region declining?", Profile())
            .Select(g => g.Kind)
            .ToList();

        Assert.Contains(GoalKind.RootCause, kinds);
        Assert.Contains(GoalKind.Dominance, kinds);
        Assert.Contains(GoalKind.Trend, kinds);
    }

    [Fact]
    public void ParseAll_RanksTheMoreSpecificIntentFirst()
    {
        // "year over year" is a three-word Compare phrase; "trend" is a single Trend word — the
        // more specific phrase must win the headline slot.
        var goals = GoalParser.ParseAll("what is the year over year trend?", Profile());

        Assert.Equal(GoalKind.Compare, goals[0].Kind);
        Assert.Contains(GoalKind.Trend, goals.Select(g => g.Kind));
    }

    [Fact]
    public void ParseAll_HonoursTheIntentCap()
    {
        // A deliberately intent-dense question, capped to two goals.
        var goals = GoalParser.ParseAll(
            "why did the top region's revenue spike and what is the forecast trend?",
            Profile(), maxIntents: 2);

        Assert.Equal(2, goals.Count);
        Assert.Distinct(goals.Select(g => g.Kind));
    }

    [Fact]
    public void ParseAll_ReturnsExploreForEmptyOrUnmatchedQuestions()
    {
        var profile = Profile();

        Assert.Equal(GoalKind.Explore, Assert.Single(GoalParser.ParseAll(null, profile)).Kind);
        Assert.Equal(GoalKind.Explore, Assert.Single(GoalParser.ParseAll("   ", profile)).Kind);

        var unmatched = Assert.Single(GoalParser.ParseAll("hello there", profile));
        Assert.Equal(GoalKind.Explore, unmatched.Kind);
        Assert.Equal("hello there", unmatched.RawText);
    }

    [Fact]
    public void ParseAll_CarriesResolvedColumnsOntoEveryGoal()
    {
        var goals = GoalParser.ParseAll("why is Revenue declining?", Profile());

        Assert.True(goals.Count >= 2);
        Assert.All(goals, g => Assert.Contains("Revenue", g.TargetColumns));
    }

    // ── Word-boundary matching ────────────────────────────────────────────────

    [Fact]
    public void ParseAll_DoesNotMatchAKeywordBuriedInsideAnotherWord()
    {
        // "almost" must not trip the Dominance keyword "most"; the question is pure Trend.
        var kinds = GoalParser.ParseAll("revenue almost doubled — what is the trend?", Profile())
            .Select(g => g.Kind)
            .ToList();

        Assert.Contains(GoalKind.Trend, kinds);
        Assert.DoesNotContain(GoalKind.Dominance, kinds);
    }

    [Fact]
    public void ParseAll_StillMatchesStemsAtWordStart()
    {
        // The "correlat" stem must still fire on "correlated".
        var kinds = GoalParser.ParseAll("are cost and revenue correlated?", Profile())
            .Select(g => g.Kind)
            .ToList();

        Assert.Contains(GoalKind.Correlation, kinds);
    }

    // ── Parse stays single-goal ───────────────────────────────────────────────

    [Fact]
    public void Parse_ReturnsTheHighestRankedGoalOnly()
    {
        var profile = Profile();
        var single = GoalParser.Parse("why is the top region declining?", profile);
        var all = GoalParser.ParseAll("why is the top region declining?", profile);

        Assert.Equal(all[0].Kind, single.Kind);
    }

    // ── Agent integration ─────────────────────────────────────────────────────

    [Fact]
    public void Agent_AnswersAllIntentsOfACompoundQuestion()
    {
        var result = AnalyticsEngine.AnalyzeCsv(SalesCsv);
        var trace = BuildAgent().Investigate(result, "why is the top region declining?");

        var skills = trace.Steps.Select(s => s.SkillName).ToList();
        Assert.Contains("RootCause", skills);
        Assert.Contains("Dominance", skills);
        Assert.NotEmpty(trace.Insights);

        // The narrative names each intent that was pursued.
        Assert.Contains("+", trace.Narrative);
    }

    [Fact]
    public void Agent_IsDeterministicAcrossRepeatedRuns()
    {
        var result = AnalyticsEngine.AnalyzeCsv(SalesCsv);
        var first = BuildAgent().Investigate(result, "why is the top region declining?");
        var second = BuildAgent().Investigate(result, "why is the top region declining?");

        Assert.Equal(
            first.Steps.Select(s => s.SkillName),
            second.Steps.Select(s => s.SkillName));
        Assert.Equal(first.Headline, second.Headline);
        Assert.Equal(first.Narrative, second.Narrative);
    }

    [Fact]
    public void Session_RecordsEveryIntentInTheTurnGoal()
    {
        var result = AnalyticsEngine.AnalyzeCsv(SalesCsv);
        var session = BuildAgent().StartSession(result);

        session.Ask("why is the top region declining?");

        var turn = Assert.Single(session.History);
        Assert.Contains("+", turn.Goal);
    }
}
