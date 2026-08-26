using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Extensibility;

namespace TerraFluent.AutoAnalytics.Engine;

/// <summary>Tunable, deterministic options controlling the analysis pipeline and plugin registration.</summary>
public sealed class AnalyticsOptions
{
    /// <summary>Optional friendly name for the dataset (falls back to the source name).</summary>
    public string? DatasetName { get; set; }

    /// <summary>Maximum insights returned (highest-scoring kept). Default 25.</summary>
    public int MaxInsights { get; set; } = 25;

    /// <summary>Maximum chart recommendations returned (highest-scoring kept). Default 12.</summary>
    public int MaxRecommendations { get; set; } = 12;

    /// <summary>Cap on measure×dimension group combinations evaluated. Default 12.</summary>
    public int MaxGroupCombinations { get; set; } = 12;

    /// <summary>Absolute z-score threshold for anomaly flagging. Default 3.0.</summary>
    public double ZScoreThreshold { get; set; } = 3.0;

    /// <summary>When <see langword="true"/>, a validation error aborts analysis with an exception.</summary>
    public bool ThrowOnValidationError { get; set; }

    /// <summary>Project each measure forward during analysis. Default <see langword="true"/>.</summary>
    public bool EnableForecasting { get; set; } = true;

    /// <summary>Periods projected forward when forecasting is enabled. Default 3.</summary>
    public int ForecastHorizon { get; set; } = Analytics.ForecastEngine.DefaultHorizon;

    /// <summary>Compare each measure across calendar periods (MoM/QoQ/YoY). Default <see langword="true"/>.</summary>
    public bool EnablePeriodComparison { get; set; } = true;

    /// <summary>Cluster rows into natural segments across the measures. Default <see langword="true"/>.</summary>
    public bool EnableSegmentation { get; set; } = true;

    /// <summary>Attribute each detected anomaly to a dimension where possible. Default <see langword="true"/>.</summary>
    public bool EnableAnomalyExplanation { get; set; } = true;

    /// <summary>Custom analytics rules run after the built-in analytics phase.</summary>
    public IList<IAnalyticsRule> Rules { get; } = new List<IAnalyticsRule>();

    /// <summary>Custom insight generators that augment the built-in insight engine.</summary>
    public IList<IInsightGenerator> InsightGenerators { get; } = new List<IInsightGenerator>();

    /// <summary>Custom chart recommenders that augment the built-in engine.</summary>
    public IList<IChartRecommender> ChartRecommenders { get; } = new List<IChartRecommender>();

    /// <summary>Custom anomaly detectors merged with the built-in detector.</summary>
    public IList<IAnomalyDetector> AnomalyDetectors { get; } = new List<IAnomalyDetector>();
}
