using System.Collections.Concurrent;
using System.Threading;

namespace SearchLoadBalancer.LoadBalancing;

public class LoadBalancerStatsStore
{
    private readonly ConcurrentDictionary<string, BackendCounters> _counters = new(StringComparer.OrdinalIgnoreCase);
    private long _totalIncomingRequests;

    public void Initialize(IEnumerable<BackendEndpoint> backends)
    {
        foreach (var backend in backends)
        {
            _counters.TryAdd(backend.Name, new BackendCounters(backend.Name, backend.BaseUrl));
        }
    }

    public void RecordIncomingRequest()
    {
        Interlocked.Increment(ref _totalIncomingRequests);
    }

    public void RecordAttempt(BackendEndpoint backend)
    {
        var counters = _counters.GetOrAdd(backend.Name, _ => new BackendCounters(backend.Name, backend.BaseUrl));
        Interlocked.Increment(ref counters.Attempts);
    }

    public void RecordSuccess(BackendEndpoint backend)
    {
        var counters = _counters.GetOrAdd(backend.Name, _ => new BackendCounters(backend.Name, backend.BaseUrl));
        Interlocked.Increment(ref counters.Successes);
    }

    public void RecordFailure(BackendEndpoint backend)
    {
        var counters = _counters.GetOrAdd(backend.Name, _ => new BackendCounters(backend.Name, backend.BaseUrl));
        Interlocked.Increment(ref counters.Failures);
    }

    public LoadBalancerStatsSnapshot Snapshot(string strategy)
    {
        var backends = _counters.Values
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => new BackendStatsSnapshot
            {
                Name = x.Name,
                BaseUrl = x.BaseUrl,
                Attempts = x.Attempts,
                Successes = x.Successes,
                Failures = x.Failures
            })
            .ToList();

        return new LoadBalancerStatsSnapshot
        {
            Strategy = strategy,
            TotalIncomingRequests = _totalIncomingRequests,
            Backends = backends
        };
    }

    private sealed class BackendCounters
    {
        public BackendCounters(string name, string baseUrl)
        {
            Name = name;
            BaseUrl = baseUrl;
        }

        public string Name { get; }

        public string BaseUrl { get; }

        public long Attempts;

        public long Successes;

        public long Failures;
    }
}

public class LoadBalancerStatsSnapshot
{
    public string Strategy { get; set; } = string.Empty;

    public long TotalIncomingRequests { get; set; }

    public List<BackendStatsSnapshot> Backends { get; set; } = new();
}

public class BackendStatsSnapshot
{
    public string Name { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public long Attempts { get; set; }

    public long Successes { get; set; }

    public long Failures { get; set; }
}
