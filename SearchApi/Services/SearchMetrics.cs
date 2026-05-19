using System.Diagnostics.Metrics;

namespace SearchApi.Services;

public static class SearchMetrics
{
    public const string MeterName = "SearchProject.SearchApi";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    private static readonly Counter<long> CacheRequests = Meter.CreateCounter<long>(
        "search.cache.requests",
        unit: "requests",
        description: "Number of search cache decisions by status.");

    private static readonly Histogram<double> SearchDuration = Meter.CreateHistogram<double>(
        "search.duration",
        unit: "ms",
        description: "End-to-end search duration measured inside SearchService, tagged by cache status and database.");

    public static void RecordCacheStatus(string status, string? database)
    {
        CacheRequests.Add(
            1,
            new KeyValuePair<string, object?>("cache.status", status),
            new KeyValuePair<string, object?>("database", string.IsNullOrWhiteSpace(database) ? "unknown" : database));
    }

    public static void RecordSearchDuration(double milliseconds, string status, string? database)
    {
        SearchDuration.Record(
            milliseconds,
            new KeyValuePair<string, object?>("cache.status", status),
            new KeyValuePair<string, object?>("database", string.IsNullOrWhiteSpace(database) ? "unknown" : database));
    }
}
