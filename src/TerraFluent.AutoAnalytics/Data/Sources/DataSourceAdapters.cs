using System;
using System.Threading;
using System.Threading.Tasks;

namespace TerraFluent.AutoAnalytics.Data.Sources;

/// <summary>
/// Presents a synchronous <see cref="IDataSource"/> through the asynchronous interface, so callers
/// that accept live sources can also take a CSV, JSON or workbook source without a second code path.
/// </summary>
/// <remarks>
/// The wrapped load is genuinely synchronous, so it completes inline rather than being pushed onto
/// the thread pool — offloading in-memory work would add latency without freeing anything.
/// </remarks>
public sealed class SyncDataSourceAdapter : IAsyncDataSource
{
    private readonly IDataSource _inner;

    public SyncDataSourceAdapter(IDataSource inner)
        => _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    public Task<Dataset> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_inner.Load());
    }
}

/// <summary>
/// Presents an <see cref="IAsyncDataSource"/> through the synchronous interface for callers that
/// cannot await — notably the static <c>AnalyticsEngine.Analyze</c> convenience entry points.
/// </summary>
/// <remarks>
/// Blocking on a network fetch is a deliberate trade-off, not an oversight: prefer
/// <see cref="IAsyncDataSource.LoadAsync"/> wherever the call site can await. The wait is performed
/// on the returned task's awaiter rather than <c>.Result</c> so the original exception surfaces
/// directly instead of wrapped in an <see cref="AggregateException"/>.
/// </remarks>
public sealed class AsyncDataSourceAdapter : IDataSource
{
    private readonly IAsyncDataSource _inner;

    public AsyncDataSourceAdapter(IAsyncDataSource inner)
        => _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    public Dataset Load() => _inner.LoadAsync().GetAwaiter().GetResult();
}

/// <summary>Conversions between the synchronous and asynchronous data-source interfaces.</summary>
public static class DataSourceExtensions
{
    /// <summary>Views a synchronous source as an asynchronous one.</summary>
    public static IAsyncDataSource AsAsync(this IDataSource source) =>
        source as IAsyncDataSource ?? new SyncDataSourceAdapter(source);

    /// <summary>Views an asynchronous source as a synchronous one, blocking on the fetch.</summary>
    public static IDataSource AsSync(this IAsyncDataSource source) =>
        source as IDataSource ?? new AsyncDataSourceAdapter(source);
}
