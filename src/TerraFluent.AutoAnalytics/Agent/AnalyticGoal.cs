using System;
using System.Collections.Generic;
using System.Linq;

namespace TerraFluent.AutoAnalytics.Agent;

/// <summary>
/// A single, deterministic analytic intent for the <see cref="AnalyticAgent"/> to pursue: a
/// <see cref="GoalKind"/>, the columns it targets (empty = all relevant) and, for follow-up goals,
/// a short <see cref="Trigger"/> explaining why the agent raised it.
/// </summary>
public sealed class AnalyticGoal
{
    public GoalKind Kind { get; init; }

    /// <summary>Columns the goal focuses on. Empty means "any relevant column".</summary>
    public IReadOnlyList<string> TargetColumns { get; init; } = Array.Empty<string>();

    /// <summary>The original request text (for display), if any.</summary>
    public string? RawText { get; init; }

    /// <summary>When this goal was raised by the agent as a follow-up, why it was raised.</summary>
    public string? Trigger { get; init; }

    /// <summary>Stable signature used to de-duplicate skill/goal executions during planning.</summary>
    public string Signature =>
        $"{Kind}|{string.Join(",", TargetColumns.Select(c => c.ToLowerInvariant()).OrderBy(c => c))}";

    /// <summary>A human-readable label for the goal.</summary>
    public string Describe() => TargetColumns.Count == 0
        ? Kind.ToString()
        : $"{Kind} of {string.Join(", ", TargetColumns.Select(DisplayText.Humanize))}";

    // ── Factory helpers ─────────────────────────────────────────────────────────

    public static AnalyticGoal Explore(string? rawText = null) =>
        new() { Kind = GoalKind.Explore, RawText = rawText };

    public static AnalyticGoal Trend(params string[] columns) =>
        new() { Kind = GoalKind.Trend, TargetColumns = columns };

    public static AnalyticGoal Correlation(params string[] columns) =>
        new() { Kind = GoalKind.Correlation, TargetColumns = columns };

    public static AnalyticGoal Anomaly(params string[] columns) =>
        new() { Kind = GoalKind.Anomaly, TargetColumns = columns };

    public static AnalyticGoal Dominance(params string[] columns) =>
        new() { Kind = GoalKind.Dominance, TargetColumns = columns };

    public static AnalyticGoal Forecast(params string[] columns) =>
        new() { Kind = GoalKind.Forecast, TargetColumns = columns };

    public static AnalyticGoal RootCause(params string[] columns) =>
        new() { Kind = GoalKind.RootCause, TargetColumns = columns };

    public static AnalyticGoal Segment() =>
        new() { Kind = GoalKind.Segment };

    public static AnalyticGoal Compare(params string[] columns) =>
        new() { Kind = GoalKind.Compare, TargetColumns = columns };

    /// <summary>Creates a follow-up goal annotated with the reason it was raised.</summary>
    public AnalyticGoal AsFollowUp(string trigger) => new()
    {
        Kind = Kind,
        TargetColumns = TargetColumns,
        RawText = RawText,
        Trigger = trigger
    };
}
