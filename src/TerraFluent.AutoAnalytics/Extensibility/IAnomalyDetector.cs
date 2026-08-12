using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Analytics;
using TerraFluent.AutoAnalytics.Profiling;

namespace TerraFluent.AutoAnalytics.Extensibility;

/// <summary>
/// A pluggable anomaly-detection strategy. Registered detectors run alongside the built-in
/// Z-score/IQR/spike detector and their results are merged.
/// </summary>
public interface IAnomalyDetector
{
    IReadOnlyList<AnomalyResult> Detect(DatasetProfile profile);
}
