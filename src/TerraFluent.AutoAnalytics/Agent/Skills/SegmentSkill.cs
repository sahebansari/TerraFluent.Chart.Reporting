using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Recommendation;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

namespace TerraFluent.AutoAnalytics.Agent.Skills;

/// <summary>
/// Clusters rows into natural segments across the numeric measures (deterministic k-means). Runs in
/// an open <see cref="GoalKind.Explore"/> when the data is multivariate, or via an explicit
/// <see cref="GoalKind.Segment"/> question. Produces new math (clusters), unlike the selection skills.
/// </summary>
public sealed class SegmentSkill : AnalyticSkillBase
{
    private readonly SegmentationEngine _engine = new();

    public override string Name => "Segment";
    public override string Description => "Clusters rows into natural segments across the measures (k-means).";

    public override bool CanHandle(AnalyticGoal goal, AgentContext context)
    {
        if (goal.Kind is not (GoalKind.Explore or GoalKind.Segment)) return false;
        int measures = context.Profile.Measures.Count();
        // Multivariate segmentation only surfaces in Explore; a Segment goal is allowed with one measure.
        int minMeasures = goal.Kind is GoalKind.Segment ? 1 : 2;
        return measures >= minMeasures && context.Profile.RowCount >= 6;
    }

    public override SkillResult Execute(AgentContext ctx)
    {
        var segmentation = _engine.Segment(ctx.Profile);
        if (segmentation is null || segmentation.IsEmpty)
            return SkillResult.Empty("Not enough measures or rows to segment the data.");

        var insight = BuildInsight(segmentation);
        var chart = BuildChart(segmentation);
        string rationale = $"Clustered {segmentation.RowsClustered} rows into " +
                           $"{segmentation.Segments.Count} segment(s) across {segmentation.Measures.Count} measure(s).";
        return new SkillResult(rationale, new[] { insight }, new[] { chart });
    }

    private static Insight BuildInsight(SegmentationResult s)
    {
        var largest = s.Segments[0];
        string measure = s.Measures[0];
        string sharePct = largest.Share.ToString("P0", CultureInfo.InvariantCulture);
        string breakdown = string.Join("; ", s.Segments.Select(seg =>
            $"{seg.Label} ({seg.Size}, {DescribeCentroid(seg, s.Measures)})"));

        return new Insight
        {
            Kind = InsightKind.Observation,
            Title = $"{s.Segments.Count} natural segments found",
            Description = $"The data splits into {s.Segments.Count} segments across {string.Join(", ", s.Measures.Select(DisplayText.Humanize))}. " +
                          $"The largest is \"{largest.Label}\" holding {sharePct} of rows. Segments: {breakdown}.",
            ImportanceScore = 50 + Math.Min(20, s.Segments.Count * 5),
            RelatedColumns = s.Measures.ToArray(),
            Evidence = new Dictionary<string, string>
            {
                ["segments"] = s.Segments.Count.ToString(CultureInfo.InvariantCulture),
                ["rows"] = s.RowsClustered.ToString(CultureInfo.InvariantCulture),
                ["largestLabel"] = largest.Label,
                ["largestShare"] = largest.Share.ToString("F3", CultureInfo.InvariantCulture)
            }
        };
    }

    private static string DescribeCentroid(SegmentProfile seg, IReadOnlyList<string> measures) =>
        string.Join(", ", measures.Select(m => $"{DisplayText.Humanize(m)}\u2248{FormatNumber(seg.Centroid[m])}"));

    private static RecommendedChart BuildChart(SegmentationResult s)
    {
        var spec = new ChartSpec
        {
            Type = ChartType.Column,
            Title = "Segment sizes",
            XAxisTitle = "Segment",
            YAxisTitle = "Rows",
            Categories = s.Segments.Select(seg => seg.Label).ToList(),
            Series = new[]
            {
                new SeriesSpec { Name = "Rows", Values = s.Segments.Select(seg => (double?)seg.Size).ToList() }
            }
        };

        return new RecommendedChart
        {
            ChartType = ChartType.Column,
            SuitabilityScore = 74,
            Reason = $"Row count per natural segment across {string.Join(", ", s.Measures.Select(DisplayText.Humanize))}.",
            Spec = spec
        };
    }
}
