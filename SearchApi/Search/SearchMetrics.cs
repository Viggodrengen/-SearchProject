using System.Diagnostics.Metrics;

namespace SearchApi.Search;

public static class SearchMetrics
{
    public const string MeterName = "SearchProject.SearchApi";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    private static readonly Counter<long> CacheRequests = Meter.CreateCounter<long>(
        "search.cache.requests",
        unit: "requests",
        description: "Number of search cache decisions by status.");

    public static void RecordCacheStatus(string status, string? database)
    {
        CacheRequests.Add(
            1,
            new KeyValuePair<string, object?>("cache.status", status),
            new KeyValuePair<string, object?>("database", string.IsNullOrWhiteSpace(database) ? "unknown" : database));
    }
}
