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

    // ── Core pipeline ─────────────────────────────────────────────────────────

    /// <summary>Runs the full pipeline against a dataset.</summary>
    public AnalyticsResult Run(Dataset dataset, AnalyticsOptions? options = null)
    {
        if (dataset is null) throw new ArgumentNullException(nameof(dataset));
        options ??= new AnalyticsOptions();

        // Phase 1 — schema.
        var schema = _schema.Discover(dataset);

        // Phase 2 — validation.
        var validation = _validation.Validate(dataset, schema);
        if (options.ThrowOnValidationError && validation.HasErrors)
            throw new InvalidOperationException(
                "Validation failed: " + string.Join("; ",
                    validation.Issues.Where(i => i.Severity == ValidationSeverity.Error).Select(i => i.Message)));

        // Phase 3 — profiling.
        var profile = _profiling.Profile(dataset, schema);

        // Phase 4/5 — analytics.
        var relationships = new RelationshipEngine(options.MaxGroupCombinations);
        var trends = new TrendEngine();
        var anomalyEngine = new AnomalyEngine(options.ZScoreThreshold);

        var anomalies = anomalyEngine.Detect(profile).ToList();
        foreach (var detector in options.AnomalyDetectors)
            anomalies.AddRange(detector.Detect(profile));

        var trendResults  = trends.DetectTrends(profile);
        var movingAvgs    = new MovingAverageEngine().Compute(profile);
        var cumulSeries   = new CumulativeSeriesEngine().Compute(profile);
        var compositions  = new CompositionEngine().Compute(profile);

        var findings = new AnalyticsFindings
        {
            Correlations    = relationships.Correlations(profile),
            Groups          = relationships.GroupAnalyses(profile),
            Trends          = trendResults,
            Anomalies       = anomalies,
            MovingAverages  = movingAvgs,
            CumulativeSeries = cumulSeries,
            Compositions    = compositions
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
        var recommendations = new List<RecommendedChart>(_recommender.Recommend(profile, findings));
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
