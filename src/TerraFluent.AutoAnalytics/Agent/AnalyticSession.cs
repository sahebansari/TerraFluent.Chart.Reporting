using System;
using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Engine;

namespace TerraFluent.AutoAnalytics.Agent;

/// <summary>A single question/answer exchange recorded in an <see cref="AnalyticSession"/>.</summary>
public sealed class SessionTurn
{
    public required string Question { get; init; }
    public required string Goal { get; init; }
    public required string Headline { get; init; }
    public int NewInsightCount { get; init; }
}

/// <summary>
/// A stateful, multi-turn analytic conversation over one dataset. The underlying
/// <see cref="AnalyticsResult"/> is computed once; each <see cref="Ask"/> plans against it while
/// carrying memory forward: work already done is not repeated, insights already reported are not
/// re-surfaced, and pronoun references ("break that down", "forecast it") resolve to the columns
/// referenced by the previous turn. Deterministic and non-AI.
/// </summary>
public sealed class AnalyticSession
{
    private static readonly HashSet<string> ReferenceWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "it", "that", "this", "those", "these", "them", "they", "again", "same"
    };

    private readonly AnalyticAgent _agent;
    private readonly HashSet<string> _executed = new(StringComparer.Ordinal);
    private readonly HashSet<string> _insightTitles = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<SessionTurn> _turns = new();
    private IReadOnlyList<string> _lastColumns = Array.Empty<string>();

    /// <summary>The one-shot analysis the session reasons over.</summary>
    public AnalyticsResult Result { get; }

    /// <summary>The ordered history of questions asked and answered in this session.</summary>
    public IReadOnlyList<SessionTurn> History => _turns;

    internal AnalyticSession(AnalyticAgent agent, AnalyticsResult result)
    {
        _agent = agent;
        Result = result;
    }

    /// <summary>Asks a follow-up question, building on everything asked earlier in the session.</summary>
    public InvestigationTrace Ask(string? question)
    {
        var goals = GoalParser.ParseAll(question, Result.Profile)
            .Select(g => ResolveReferences(g, question))
            .ToList();
        var primary = goals[0];

        var trace = _agent.Investigate(Result, goals, _executed, _insightTitles);

        _lastColumns = FocusColumns(primary, trace);
        _turns.Add(new SessionTurn
        {
            Question = question ?? primary.Describe(),
            Goal = string.Join(" + ", goals.Select(g => g.Describe()).Distinct(StringComparer.OrdinalIgnoreCase)),
            Headline = trace.Headline,
            NewInsightCount = trace.Insights.Count
        });

        return trace;
    }

    // If the goal named no columns but the question used a pronoun, reuse the previous turn's focus.
    private AnalyticGoal ResolveReferences(AnalyticGoal goal, string? question)
    {
        if (goal.TargetColumns.Count > 0 || _lastColumns.Count == 0 || string.IsNullOrWhiteSpace(question))
            return goal;

        var words = question.Split(new[] { ' ', '\t', ',', '.', '?', '!', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
        if (!words.Any(w => ReferenceWords.Contains(w)))
            return goal;

        return new AnalyticGoal
        {
            Kind = goal.Kind,
            TargetColumns = _lastColumns,
            RawText = goal.RawText,
            Trigger = $"resolved reference to {string.Join(", ", _lastColumns)}"
        };
    }

    // The columns this turn focused on: the goal's targets, else the top insight's related columns.
    private static IReadOnlyList<string> FocusColumns(AnalyticGoal goal, InvestigationTrace trace)
    {
        if (goal.TargetColumns.Count > 0) return goal.TargetColumns;
        var top = trace.Insights.FirstOrDefault();
        return top?.RelatedColumns.ToList() ?? (IReadOnlyList<string>)Array.Empty<string>();
    }
}
