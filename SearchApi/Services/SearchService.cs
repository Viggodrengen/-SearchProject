using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SearchApi.Repository;
using Shared.Model;

namespace SearchApi.Services;

public class SearchService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly SearchCacheOptions _cacheOptions;
    private const string CacheGenerationKey = "search:cache-generation";

    private readonly ILogger<SearchService> _logger;

    public SearchService(
        IDistributedCache cache,
        IOptions<SearchCacheOptions> cacheOptions,
        ILogger<SearchService> logger)
    {
        _cache = cache;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
    }

    public async Task<SearchServiceResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var searchStopwatch = Stopwatch.StartNew();
        var terms = (request.Query ?? string.Empty).Split(" ", StringSplitOptions.RemoveEmptyEntries);
        var maxAmount = Math.Clamp(request.MaxAmount, 1, 100);

        if (terms.Length == 0)
        {
            SearchMetrics.RecordCacheStatus("bypass", request.Database);
            searchStopwatch.Stop();
            SearchMetrics.RecordSearchDuration(searchStopwatch.Elapsed.TotalMilliseconds, "bypass", request.Database);
            return new SearchServiceResponse(new SearchResult
            {
                Query = Array.Empty<string>(),
                NoOfHits = 0,
                TimeUsed = TimeSpan.Zero
            }, "bypass");
        }

        var cacheGeneration = await GetCacheGenerationAsync(cancellationToken);
        var cacheKey = BuildCacheKey(request, terms, maxAmount, cacheGeneration);
        var cacheStatus = _cacheOptions.Enabled ? "miss" : "disabled";
        if (_cacheOptions.Enabled)
        {
            var cacheLookup = await TryGetCachedResultAsync(cacheKey, cancellationToken);
            cacheStatus = cacheLookup.Status;
            if (cacheLookup.Result is not null)
            {
                _logger.LogInformation("Search cache hit for key {CacheKey}", cacheKey);
                SearchMetrics.RecordCacheStatus("hit", request.Database);
                searchStopwatch.Stop();
                cacheLookup.Result.TimeUsed = searchStopwatch.Elapsed;
                SearchMetrics.RecordSearchDuration(searchStopwatch.Elapsed.TotalMilliseconds, "hit", request.Database);
                return new SearchServiceResponse(cacheLookup.Result, "hit");
            }

            _logger.LogInformation("Search cache {CacheStatus} for key {CacheKey}", cacheStatus, cacheKey);
            SearchMetrics.RecordCacheStatus(cacheStatus, request.Database);
        }

        var databaseReason = cacheStatus switch
        {
            "fallback" => "cache_unavailable",
            "disabled" => "cache_disabled",
            _ => "cache_miss"
        };
        SearchMetrics.RecordDatabaseRequest(databaseReason, request.Database);

        // Keep persistence behind the repository abstraction; the search algorithm stays database-agnostic.
        using var database = DatabaseFactory.Create(request.Database);
        var config = new SearchConfig { CaseSensitive = request.CaseSensitive };
        var logic = new SearchLogic(database, config);

        var result = logic.Search(terms, maxAmount);

        if (_cacheOptions.Enabled)
        {
            await TrySetCachedResultAsync(cacheKey, result, cancellationToken);
        }

        if (!_cacheOptions.Enabled)
        {
            SearchMetrics.RecordCacheStatus("disabled", request.Database);
        }

        searchStopwatch.Stop();
        SearchMetrics.RecordSearchDuration(searchStopwatch.Elapsed.TotalMilliseconds, cacheStatus, request.Database);

        return new SearchServiceResponse(result, cacheStatus);
    }

    public async Task<string> ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        var generation = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        try
        {
            await _cache.SetStringAsync(
                CacheGenerationKey,
                generation,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
                },
                cancellationToken);

            _logger.LogInformation("Search cache generation changed to {Generation}", generation);
            return generation;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not clear search cache by changing generation.");
            throw;
        }
    }

    private async Task<string> GetCacheGenerationAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _cache.GetStringAsync(CacheGenerationKey, cancellationToken) ?? "1";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read cache generation. Falling back to default generation.");
            return "1";
        }
    }

    private async Task<(SearchResult? Result, string Status)> TryGetCachedResultAsync(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            var json = await _cache.GetStringAsync(cacheKey, cancellationToken);
            return string.IsNullOrWhiteSpace(json)
                ? (null, "miss")
                : (JsonSerializer.Deserialize<SearchResult>(json, SerializerOptions), "hit");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read search result from cache. Falling back to database.");
            return (null, "fallback");
        }
    }

    private async Task TrySetCachedResultAsync(string cacheKey, SearchResult result, CancellationToken cancellationToken)
    {
        try
        {
            var ttl = TimeSpan.FromSeconds(Math.Max(1, _cacheOptions.TtlSeconds));
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };
            var json = JsonSerializer.Serialize(result, SerializerOptions);

            await _cache.SetStringAsync(cacheKey, json, options, cancellationToken);
            _logger.LogInformation("Stored search result in cache for key {CacheKey} with TTL {TtlSeconds}s", cacheKey, ttl.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not store search result in cache. Returning database result without caching.");
        }
    }

    private static string BuildCacheKey(SearchRequest request, string[] terms, int maxAmount, string cacheGeneration)
    {
        var comparer = request.CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var normalizedTerms = new List<string>();
        var seen = new HashSet<string>(comparer);

        foreach (var term in terms)
        {
            var cacheTerm = request.CaseSensitive ? term : term.ToLowerInvariant();
            if (seen.Add(cacheTerm))
            {
                normalizedTerms.Add(cacheTerm);
            }
        }

        var rawKey = string.Join('|',
            "v1",
            cacheGeneration,
            (request.Database ?? "sqlite").Trim().ToLowerInvariant(),
            request.CaseSensitive ? "case-sensitive" : "case-insensitive",
            maxAmount.ToString(),
            string.Join(' ', normalizedTerms));

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey))).ToLowerInvariant();
        return $"search:{hash}";
    }
}

public record SearchServiceResponse(SearchResult Result, string CacheStatus);
