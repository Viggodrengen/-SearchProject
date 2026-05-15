using System.Diagnostics.Metrics;

namespace SearchLoadBalancer.LoadBalancing;

public static class LoadBalancerMetrics
{
    public const string MeterName = "SearchProject.SearchLoadBalancer";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    private static readonly Counter<long> IncomingRequests = Meter.CreateCounter<long>(
        "search.lb.incoming.requests",
        unit: "requests",
        description: "Incoming search requests received by the load balancer.");

    private static readonly Counter<long> BackendAttempts = Meter.CreateCounter<long>(
        "search.lb.backend.attempts",
        unit: "attempts",
        description: "Search backend attempts by backend.");

    private static readonly Counter<long> BackendSuccesses = Meter.CreateCounter<long>(
        "search.lb.backend.successes",
        unit: "responses",
        description: "Successful search backend responses by backend.");

    private static readonly Counter<long> BackendFailures = Meter.CreateCounter<long>(
        "search.lb.backend.failures",
        unit: "failures",
        description: "Failed search backend attempts by backend.");

    public static void RecordIncomingRequest()
    {
        IncomingRequests.Add(1);
    }

    public static void RecordBackendAttempt(BackendEndpoint backend)
    {
        BackendAttempts.Add(1, BackendTags(backend));
    }

    public static void RecordBackendSuccess(BackendEndpoint backend)
    {
        BackendSuccesses.Add(1, BackendTags(backend));
    }

    public static void RecordBackendFailure(BackendEndpoint backend)
    {
        BackendFailures.Add(1, BackendTags(backend));
    }

    private static KeyValuePair<string, object?>[] BackendTags(BackendEndpoint backend)
    {
        return new[]
        {
            new KeyValuePair<string, object?>("backend.name", backend.Name),
            new KeyValuePair<string, object?>("backend.url", backend.BaseUrl)
        };
    }
}
