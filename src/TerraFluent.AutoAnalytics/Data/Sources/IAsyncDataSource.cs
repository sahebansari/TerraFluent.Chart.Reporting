using System.Threading;
using System.Threading.Tasks;

namespace TerraFluent.AutoAnalytics.Data.Sources;

/// <summary>
/// A data source that is fetched rather than read from memory — a database query, an HTTP endpoint,
/// anything where the call travels and can therefore be slow, fail, or need cancelling.
/// </summary>
/// <remarks>
/// <see cref="IDataSource"/> stays the right shape for files and in-memory collections, where
/// loading is immediate and cannot be meaningfully cancelled. This interface exists so a live source
/// never has to block a request thread or ignore a cancellation token. Implementations must remain
/// deterministic for a given response payload, and must not retain the rows they return.
/// </remarks>
public interface IAsyncDataSource
{
    /// <summary>Fetches the source and produces a normalised <see cref="Dataset"/>.</summary>
    /// <param name="cancellationToken">Cancels an in-flight fetch.</param>
    Task<Dataset> LoadAsync(CancellationToken cancellationToken = default);
}
