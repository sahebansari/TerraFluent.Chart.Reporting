using System;
using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Data;
using TerraFluent.AutoAnalytics.Data.Sources;
using TerraFluent.AutoAnalytics.Engine;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Recommendation;

namespace TerraFluent.AutoAnalytics.Agent;

/// <summary>
/// A deterministic (non-AI) analytic agent. It runs the one-shot <see cref="AnalyticsEngine"/> to
/// build an <see cref="AgentContext"/>, then plans: it selects the skills that can handle the goal,
/// executes them, and follows the evidence by enqueuing the follow-up goals they raise (e.g. an
/// anomaly triggers a root-cause breakdown). The ordered <see cref="InvestigationTrace"/> it returns
/// is a fully explainable reasoning path — identical input always yields identical output.
/// </summary>
public sealed class AnalyticAgent
{
    private const int DefaultMaxSteps = 16;

    private readonly AnalyticsEngine _engine;
    private readonly IReadOnlyList<IAnalyticSkill> _skills;
    private readonly int _maxSteps;

    public AnalyticAgent(AnalyticsEngine engine, IEnumerable<IAnalyticSkill> skills, int maxSteps = DefaultMaxSteps)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _skills = (skills ?? throw new ArgumentNullException(nameof(skills))).ToList();
        _maxSteps = maxSteps < 1 ? DefaultMaxSteps : maxSteps;
    }

    // ── Convenience entry points ────────────────────────────────────────────────

    /// <summary>Analyses raw CSV and investigates it against an optional natural-language question.</summary>
    public InvestigationTrace InvestigateCsv(string csv, string? question = null, AnalyticsOptions? options = null)
        => Investigate(new CsvDataSource(csv).Load(), question, options);

    /// <summary>Analyses a JSON array and investigates it against an optional natural-language question.</summary>
    public InvestigationTrace InvestigateJson(string json, string? question = null, AnalyticsOptions? options = null)
        => Investigate(new JsonDataSource(json).Load(), question, options);

    /// <summary>Analyses a dataset and investigates it against an optional natural-language question.</summary>
    public InvestigationTrace Investigate(Dataset dataset, string? question = null, AnalyticsOptions? options = null)
    {
        var result = _engine.Run(dataset, options);
        var goal = GoalParser.Parse(question, result.Profile);
        return Investigate(result, goal);
    }

    /// <summary>
    /// Investigates an already-computed analysis result against a natural-language question. Every
    /// intent the question expresses is pursued, strongest first.
    /// </summary>
    public InvestigationTrace Investigate(AnalyticsResult result, string? question)
    {
        if (result is null) throw new ArgumentNullException(nameof(result));
        return Investigate(result, GoalParser.ParseAll(question, result.Profile),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    // ── Core planning loop ──────────────────────────────────────────────────────

    /// <summary>Investigates an already-computed analysis result against an explicit goal.</summary>
    public InvestigationTrace Investigate(AnalyticsResult result, AnalyticGoal goal)
    {
        if (result is null) throw new ArgumentNullException(nameof(result));
        return Investigate(result, goal ?? AnalyticGoal.Explore(),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Core planner used by both one-shot investigations and multi-turn <see cref="AnalyticSession"/>s.
    /// <paramref name="executed"/> (skill+goal signatures already run) and <paramref name="priorTitles"/>
    /// (insight titles already reported) carry memory across turns: seeded sets let a session skip work
    /// it has already done and suppress insights it already surfaced. Both sets are updated in place.
    /// </summary>
    internal InvestigationTrace Investigate(
        AnalyticsResult result, AnalyticGoal goal, HashSet<string> executed, HashSet<string> priorTitles)
        => Investigate(result, new[] { goal ?? AnalyticGoal.Explore() }, executed, priorTitles);

    /// <summary>
    /// Core planner over a ranked set of seed goals — every intent the user's question expressed.
    /// The first goal is the headline intent; the rest are pursued in order before the agent's own
    /// evidence-driven follow-ups.
    /// </summary>
    internal InvestigationTrace Investigate(
        AnalyticsResult result, IReadOnlyList<AnalyticGoal> goals,
        HashSet<string> executed, HashSet<string> priorTitles)
    {
        var seeds = (goals is null || goals.Count == 0)
            ? new List<AnalyticGoal> { AnalyticGoal.Explore() }
            : goals.Where(g => g is not null).ToList();
        if (seeds.Count == 0) seeds.Add(AnalyticGoal.Explore());

        var goal = seeds[0];
        var context = AgentContext.FromResult(result);

        var steps = new List<InvestigationStep>();
        var insights = new List<Insight>();
        var charts = new List<RecommendedChart>();

        var queue = new Queue<AnalyticGoal>();
        var seedSet = new HashSet<AnalyticGoal>(ReferenceEqualityComparer.Instance);
        foreach (var seed in seeds)
        {
            queue.Enqueue(seed);
            seedSet.Add(seed);
        }

        while (queue.Count > 0 && steps.Count < _maxSteps)
        {
            var current = queue.Dequeue();

            // The user's own directed question is always answered, even if an earlier turn already
            // ran it; only open-ended exploration and the agent's autonomous follow-ups de-duplicate.
            bool answerDirectly = seedSet.Contains(current) && current.Kind != GoalKind.Explore;

            foreach (var skill in _skills)
            {
                if (steps.Count >= _maxSteps) break;
                if (!skill.CanHandle(current, context)) continue;

                // De-duplicate on skill + goal signature so drill-downs (and prior turns) never repeat work.
                string signature = $"{skill.Name}|{current.Signature}";
                bool firstRun = executed.Add(signature);
                if (!firstRun && !answerDirectly) continue;

                context.Goal = current;
                var outcome = skill.Execute(context);
                if (outcome.IsEmpty) continue;

                // Suppress insights already reported in an earlier turn — except for the user's own
                // directed question, which is answered in full every time it is asked.
                var freshInsights = answerDirectly
                    ? outcome.Insights.ToList()
                    : outcome.Insights.Where(i => !priorTitles.Contains(i.Title)).ToList();
                if (freshInsights.Count == 0)
                {
                    // Still enqueue follow-ups so the agent can reach new ground beyond a repeated finding.
                    foreach (var followUp in outcome.FollowUps) queue.Enqueue(followUp);
                    continue;
                }
                foreach (var i in freshInsights) priorTitles.Add(i.Title);

                steps.Add(new InvestigationStep
                {
                    SkillName = skill.Name,
                    Goal = current.Describe(),
                    Trigger = current.Trigger,
                    Rationale = outcome.Rationale,
                    Insights = freshInsights
                });

                insights.AddRange(freshInsights);
                charts.AddRange(outcome.Charts);

                foreach (var followUp in outcome.FollowUps)
                    queue.Enqueue(followUp);
            }
        }

        var rankedInsights = insights
            .GroupBy(i => i.Title, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderByDescending(i => i.ImportanceScore)
            .ToList();

        var uniqueCharts = charts
            .GroupBy(c => c.Spec.Title, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        string headline = rankedInsights.Count > 0
            ? rankedInsights[0].Title
            : "No new findings for this question.";

        // Name every intent the question raised, so a compound question reads as one investigation.
        string intents = string.Join(" + ", seeds.Select(s => s.Describe()).Distinct(StringComparer.OrdinalIgnoreCase));
        string narrative = $"Investigated '{intents}': ran {steps.Count} step(s), " +
                           $"surfaced {rankedInsights.Count} insight(s) and {uniqueCharts.Count} supporting chart(s).";

        return new InvestigationTrace
        {
            Goal = goal.RawText ?? goal.Describe(),
            Steps = steps,
            Insights = rankedInsights,
            Charts = uniqueCharts,
            Headline = headline,
            Narrative = narrative
        };
    }

    // ── Sessions ────────────────────────────────────────────────────────────────

    /// <summary>Starts a multi-turn session over an already-computed analysis result.</summary>
    public AnalyticSession StartSession(AnalyticsResult result)
    {
        if (result is null) throw new ArgumentNullException(nameof(result));
        return new AnalyticSession(this, result);
    }

    /// <summary>Analyses a dataset and starts a multi-turn session over it.</summary>
    public AnalyticSession StartSession(Dataset dataset, AnalyticsOptions? options = null)
        => StartSession(_engine.Run(dataset ?? throw new ArgumentNullException(nameof(dataset)), options));
}
