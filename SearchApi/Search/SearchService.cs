using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Shared.Model;

namespace SearchApi.Search;

public class SearchService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly SearchCacheOptions _cacheOptions;
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
        var terms = (request.Query ?? string.Empty).Split(" ", StringSplitOptions.RemoveEmptyEntries);
        var maxAmount = Math.Clamp(request.MaxAmount, 1, 100);

        if (terms.Length == 0)
        {
            SearchMetrics.RecordCacheStatus("bypass", request.Database);
            return new SearchServiceResponse(new SearchResult
            {
                Query = Array.Empty<string>(),
                NoOfHits = 0,
                TimeUsed = TimeSpan.Zero
            }, "bypass");
        }

        var cacheKey = BuildCacheKey(request, terms, maxAmount);
        if (_cacheOptions.Enabled)
        {
            var cached = await TryGetCachedResultAsync(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.LogInformation("Search cache hit for key {CacheKey}", cacheKey);
                SearchMetrics.RecordCacheStatus("hit", request.Database);
                return new SearchServiceResponse(cached, "hit");
            }

            _logger.LogInformation("Search cache miss for key {CacheKey}", cacheKey);
            SearchMetrics.RecordCacheStatus("miss", request.Database);
        }

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

        return new SearchServiceResponse(result, _cacheOptions.Enabled ? "miss" : "disabled");
    }

    private async Task<SearchResult?> TryGetCachedResultAsync(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            var json = await _cache.GetStringAsync(cacheKey, cancellationToken);
            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<SearchResult>(json, SerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read search result from cache. Falling back to database.");
            return null;
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

    private static string BuildCacheKey(SearchRequest request, string[] terms, int maxAmount)
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
            (request.Database ?? "sqlite").Trim().ToLowerInvariant(),
            request.CaseSensitive ? "case-sensitive" : "case-insensitive",
            maxAmount.ToString(),
            string.Join(' ', normalizedTerms));

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey))).ToLowerInvariant();
        return $"search:{hash}";
    }
}

public record SearchServiceResponse(SearchResult Result, string CacheStatus);
