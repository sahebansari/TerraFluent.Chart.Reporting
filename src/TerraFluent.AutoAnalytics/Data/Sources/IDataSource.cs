namespace TerraFluent.AutoAnalytics.Data.Sources;

/// <summary>
/// Adapter that converts a specific external input format into the unified <see cref="Dataset"/>.
/// Implement this interface to add support for new input formats and register it with the engine.
/// </summary>
public interface IDataSource
{
    /// <summary>Reads the source and produces a normalised <see cref="Dataset"/>.</summary>
    Dataset Load();
}
