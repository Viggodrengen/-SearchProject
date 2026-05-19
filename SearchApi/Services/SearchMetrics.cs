using System.Collections.Concurrent;
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

    private static readonly Counter<long> DatabaseRequests = Meter.CreateCounter<long>(
        "search.database.requests",
        unit: "requests",
        description: "Number of searches that reached the database, tagged by reason and database.");

    private static readonly ConcurrentDictionary<string, LatestSearchDurationMeasurement> LatestSearchDurations = new();

    private static readonly ObservableGauge<double> LatestSearchDuration = Meter.CreateObservableGauge(
        "search.duration.latest",
        ObserveLatestSearchDurations,
        unit: "ms",
        description: "Latest observed search duration measured inside SearchService, tagged by cache status and database.");

    public static void RecordCacheStatus(string status, string? database)
    {
        CacheRequests.Add(
            1,
            new KeyValuePair<string, object?>("cache.status", status),
            new KeyValuePair<string, object?>("database", string.IsNullOrWhiteSpace(database) ? "unknown" : database));
    }

    public static void RecordSearchDuration(double milliseconds, string status, string? database)
    {
        var normalizedDatabase = string.IsNullOrWhiteSpace(database) ? "unknown" : database;
        SearchDuration.Record(
            milliseconds,
            new KeyValuePair<string, object?>("cache.status", status),
            new KeyValuePair<string, object?>("database", normalizedDatabase));

        LatestSearchDurations[$"{status}|{normalizedDatabase}"] = new LatestSearchDurationMeasurement(milliseconds, status, normalizedDatabase);
    }

    public static void RecordDatabaseRequest(string reason, string? database)
    {
        DatabaseRequests.Add(
            1,
            new KeyValuePair<string, object?>("reason", reason),
            new KeyValuePair<string, object?>("database", string.IsNullOrWhiteSpace(database) ? "unknown" : database));
    }

    private static IEnumerable<Measurement<double>> ObserveLatestSearchDurations()
    {
        foreach (var latest in LatestSearchDurations.Values)
        {
            yield return new Measurement<double>(
                latest.Milliseconds,
                new KeyValuePair<string, object?>("cache.status", latest.Status),
                new KeyValuePair<string, object?>("database", latest.Database));
        }
    }

    private sealed record LatestSearchDurationMeasurement(double Milliseconds, string Status, string Database);
}
