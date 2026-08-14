using System.Collections.Concurrent;
using TerraFluent.AutoAnalytics.Agent;

namespace TerraFluent.Chart.Reporting.Api.Services;

/// <summary>
/// A bounded, thread-safe in-memory store of active analytic sessions with sliding-expiration TTL.
/// Sessions are evicted when they exceed the idle timeout, and oldest-first once capacity is reached.
/// Intended for single-instance hosting; a distributed cache would be required to scale out.
/// </summary>
public sealed class SessionStore
{
    private sealed class Entry
    {
        public required AnalyticSession Session { get; init; }
        public DateTimeOffset ExpiresAt { get; set; }
    }

    private readonly ConcurrentDictionary<string, Entry> _sessions = new();
    private readonly int _capacity;
    private readonly TimeSpan _idleTimeout;

    public SessionStore(IConfiguration configuration)
    {
        _capacity = Math.Max(1, configuration.GetValue("Session:Capacity", 200));
        _idleTimeout = TimeSpan.FromMinutes(Math.Max(1, configuration.GetValue("Session:IdleTimeoutMinutes", 30)));
    }

    /// <summary>Stores a session and returns its new id.</summary>
    public string Add(AnalyticSession session)
    {
        Prune();
        string id = Guid.NewGuid().ToString("N");
        _sessions[id] = new Entry { Session = session, ExpiresAt = DateTimeOffset.UtcNow + _idleTimeout };
        EnforceCapacity();
        return id;
    }

    /// <summary>Retrieves a session by id, refreshing its TTL, or <see langword="null"/> when absent/expired.</summary>
    public AnalyticSession? Get(string id)
    {
        if (!_sessions.TryGetValue(id, out var entry))
            return null;

        if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(id, out _);
            return null;
        }

        entry.ExpiresAt = DateTimeOffset.UtcNow + _idleTimeout; // sliding expiration
        return entry.Session;
    }

    private void Prune()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var kvp in _sessions)
            if (kvp.Value.ExpiresAt <= now)
                _sessions.TryRemove(kvp.Key, out _);
    }

    private void EnforceCapacity()
    {
        while (_sessions.Count > _capacity)
        {
            var oldest = _sessions.OrderBy(kvp => kvp.Value.ExpiresAt).FirstOrDefault();
            if (oldest.Key is null) break;
            _sessions.TryRemove(oldest.Key, out _);
        }
    }
}
