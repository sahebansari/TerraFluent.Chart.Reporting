using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Data;
using TerraFluent.AutoAnalytics.Data.Sources;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Insights;
using TerraFluent.AutoAnalytics.Profiling;
using TerraFluent.AutoAnalytics.Recommendation;
using TerraFluent.AutoAnalytics.Schema;
using TerraFluent.AutoAnalytics.Validation;

namespace TerraFluent.AutoAnalytics.Engine;

/// <summary>
/// The public entry point that orchestrates the full deterministic pipeline:
/// schema discovery → validation → profiling → analytics → insights → chart recommendation → summary.
/// </summary>
public sealed class AnalyticsEngine
{
    private readonly SchemaDiscoveryEngine _schema;
    private readonly DataValidationEngine _validation;
    private readonly DataProfilingEngine _profiling;
    private readonly InsightGenerationEngine _insights;
    private readonly ChartRecommendationEngine _recommender;

    public AnalyticsEngine(
        SchemaDiscoveryEngine? schema = null,
        DataValidationEngine? validation = null,
        DataProfilingEngine? profiling = null,
        InsightGenerationEngine? insights = null,
        ChartRecommendationEngine? recommender = null)
    {
        _schema = schema ?? new SchemaDiscoveryEngine();
        _validation = validation ?? new DataValidationEngine();
        _profiling = profiling ?? new DataProfilingEngine();
        _insights = insights ?? new InsightGenerationEngine();
        _recommender = recommender ?? new ChartRecommendationEngine();
    }

    // ── Static convenience entry points ───────────────────────────────────────

    /// <summary>Analyzes any registered <see cref="IDataSource"/>.</summary>
    public static AnalyticsResult Analyze(IDataSource source, AnalyticsOptions? options = null)
        => new AnalyticsEngine().Run(source?.Load() ?? throw new ArgumentNullException(nameof(source)), options);

    /// <summary>Analyzes an already-materialised <see cref="Dataset"/>.</summary>
    public static AnalyticsResult Analyze(Dataset dataset, AnalyticsOptions? options = null)
        => new AnalyticsEngine().Run(dataset, options);

    /// <summary>Analyzes raw CSV text.</summary>
    public static AnalyticsResult AnalyzeCsv(string csv, AnalyticsOptions? options = null)
        => Analyze(new CsvDataSource(csv), options);

    /// <summary>Analyzes a JSON array of objects.</summary>
    public static AnalyticsResult AnalyzeJson(string json, AnalyticsOptions? options = null)
        => Analyze(new JsonDataSource(json), options);

    /// <summary>Analyzes an ADO.NET <see cref="DataTable"/>.</summary>
    public static AnalyticsResult AnalyzeDataTable(DataTable table, AnalyticsOptions? options = null)
        => Analyze(new DataTableDataSource(table), options);

    /// <summary>Analyzes any <see cref="IEnumerable{T}"/> of POCOs.</summary>
    public static AnalyticsResult Analyze<T>(IEnumerable<T> items, AnalyticsOptions? options = null)
        => Analyze(new EnumerableDataSource<T>(items), options);

    /// <summary>Asynchronous wrapper around <see cref="Run"/> for large datasets.</summary>
    public Task<AnalyticsResult> RunAsync(Dataset dataset, AnalyticsOptions? options = null, CancellationToken ct = default)
        => Task.Run(() => Run(dataset, options), ct);

    /// <summary>
    /// Fetches a live source and analyses it. The rows are held only for the duration of the call —
    /// nothing is cached between requests.
    /// </summary>
    public static async Task<AnalyticsResult> AnalyzeAsync(
        IAsyncDataSource source, AnalyticsOptions? options = null, CancellationToken ct = default)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        var dataset = await source.LoadAsync(ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        return new AnalyticsEngine().Run(dataset, options);
    }

    // ── Core pipeline ─────────────────────────────────────────────────────────

    /// <summary>Runs the full pipeline against a dataset.</summary>
    public AnalyticsResult Run(Dataset dataset, AnalyticsOptions? options = null)
    {
        if (dataset is null) throw new ArgumentNullException(nameof(dataset));
        options ??= new AnalyticsOptions();

        // Phase 1 — schema.
        var schema = _schema.Discover(dataset, options.PreAggregated);

        // Phase 2 — validation.
        var validation = _validation.Validate(dataset, schema);
        if (options.ThrowOnValidationError && validation.HasErrors)
            throw new InvalidOperationException(
                "Validation failed: " + string.Join("; ",
                    validation.Issues.Where(i => i.Severity == ValidationSeverity.Error).Select(i => i.Message)));

        // Phase 3 — profiling.
        var profile = _profiling.Profile(dataset, schema);

        // Phase 4/5 — analytics.
        // Already-summarized data has one row per group and no raw granularity: widen the group-by
        // budget so every measure is broken down by each dimension, and skip the time-series engines
        // whose per-period maths has nothing to collapse.
        bool timeSeries = !options.PreAggregated;
        int groupBudget = options.PreAggregated
            ? Math.Min(200, Math.Max(options.MaxGroupCombinations, EligibleGroupPairs(profile)))
            : options.MaxGroupCombinations;

        var relationships = new RelationshipEngine(groupBudget);
        var trends = new TrendEngine();
        var anomalyEngine = new AnomalyEngine(options.ZScoreThreshold);

        var anomalies = anomalyEngine.Detect(profile).ToList();
        foreach (var detector in options.AnomalyDetectors)
            anomalies.AddRange(detector.Detect(profile));

        var trendResults  = trends.DetectTrends(profile);
        var movingAvgs    = timeSeries ? new MovingAverageEngine().Compute(profile) : new List<MovingAverageResult>();
        var cumulSeries   = timeSeries ? new CumulativeSeriesEngine().Compute(profile) : new List<CumulativeSeriesResult>();
        var compositions  = new CompositionEngine().Compute(profile);

        // Forward-looking and cross-period analytics. These were previously reachable only through
        // the agent's skills, so a plain /analyze or dashboard could never show a projection.
        var forecasts = options.EnableForecasting && timeSeries
            ? new ForecastEngine(options.ForecastHorizon).Compute(profile)
            : new List<MeasureForecast>();

        var periodComparisons = options.EnablePeriodComparison && timeSeries
            ? ComparePeriods(profile)
            : new List<PeriodComparisonResult>();

        var segmentation = options.EnableSegmentation
            ? new SegmentationEngine().Segment(profile)
            : null;

        // Ask of each outlier whether any dimension accounts for it, so the narrative can say
        // "unusual, but normal for EU" instead of leaving the reader to chase it down.
        var anomalyExplanations = options.EnableAnomalyExplanation
            ? new AnomalyExplanationEngine().Explain(profile, anomalies)
            : new List<AnomalyExplanation>();

        var findings = new AnalyticsFindings
        {
            Correlations    = relationships.Correlations(profile),
            Groups          = relationships.GroupAnalyses(profile),
            Trends          = trendResults,
            Anomalies       = anomalies,
            MovingAverages  = movingAvgs,
            CumulativeSeries = cumulSeries,
            Compositions    = compositions,
            Forecasts       = forecasts,
            PeriodComparisons = periodComparisons,
            Segmentation    = segmentation is { IsEmpty: false } ? segmentation : null,
            AnomalyExplanations = anomalyExplanations
        };

        // Phase 6 — insights (built-in + plugins).
        var insights = new List<Insight>(_insights.Generate(profile, findings));
        foreach (var gen in options.InsightGenerators)
            insights.AddRange(gen.Generate(profile, findings));
        foreach (var rule in options.Rules)
            insights.AddRange(rule.Evaluate(profile, findings));

        insights = insights
            .OrderByDescending(i => i.ImportanceScore)
            .Take(options.MaxInsights)
            .ToList();

        // Phase 7 — chart recommendations (built-in + plugins).
        var recommendations = new List<RecommendedChart>(_recommender.Recommend(profile, findings, options.PreAggregated));
        foreach (var rec in options.ChartRecommenders)
            recommendations.AddRange(rec.Recommend(profile, findings));

        recommendations = recommendations
            .OrderByDescending(r => r.SuitabilityScore)
            .Take(options.MaxRecommendations)
            .ToList();

        var summary = BuildSummary(options.DatasetName ?? dataset.Name, profile, validation, insights, recommendations);

        return new AnalyticsResult
        {
            Profile = profile,
            Validation = validation,
            Findings = findings,
            Insights = insights,
            Recommendations = recommendations,
            Summary = summary
        };
    }

    // Count of (low-cardinality dimension × measure) pairs the group-by engine can form — used to
    // size the group-by budget so a wide, already-summarized dataset gets a breakdown for every field.
    private static int EligibleGroupPairs(DatasetProfile profile)
    {
        int dims = profile.Categories.Count(c => c.Categorical is { DistinctCount: >= 2 and <= 50 });
        int measures = profile.Measures.Count(m => m.NumericValues.Count > 0);
        return dims * measures;
    }

    // Period-over-period comparison for each measure; measures without enough history drop out.
    private static List<PeriodComparisonResult> ComparePeriods(DatasetProfile profile)
    {
        var engine = new PeriodComparisonEngine();
        var results = new List<PeriodComparisonResult>();
        foreach (var measure in profile.Measures)
        {
            var comparison = engine.Compare(profile, measure.Name);
            if (comparison is { IsEmpty: false }) results.Add(comparison);
        }
        return results;
    }

    private static AnalyticsSummary BuildSummary(
        string name, DatasetProfile profile, ValidationReport validation,
        IReadOnlyList<Insight> insights, IReadOnlyList<RecommendedChart> recommendations)
    {
        string quality = validation.HasErrors ? "Errors"
                       : validation.CountOf(ValidationSeverity.Warning) > 0 ? "Warnings"
                       : "Clean";

        return new AnalyticsSummary
        {
            DatasetName = name,
            RowCount = profile.RowCount,
            ColumnCount = profile.ColumnCount,
            MeasureCount = profile.Measures.Count(),
            DimensionCount = profile.Categories.Count() + profile.DateColumns.Count(),
            InsightCount = insights.Count,
            RecommendationCount = recommendations.Count,
            Headline = insights.Count > 0 ? insights[0].Title : "No significant patterns detected.",
            KeyFindings = insights.Take(3).Select(i => i.Description).ToList(),
            DataQuality = quality
        };
    }
}
