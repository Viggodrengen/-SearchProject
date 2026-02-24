using System.Net.Http.Json;
using Shared.Model;

namespace SearchWebApp.Services;

public class SearchApiClient
{
    private readonly HttpClient _httpClient;

    public SearchApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SearchApiResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("/api/search", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SearchResult>(cancellationToken: cancellationToken);
        var backend = response.Headers.TryGetValues("X-LB-Backend", out var backendValues)
            ? backendValues.FirstOrDefault()
            : null;
        var strategy = response.Headers.TryGetValues("X-LB-Strategy", out var strategyValues)
            ? strategyValues.FirstOrDefault()
            : null;
        var instance = response.Headers.TryGetValues("X-SearchApi-Instance", out var instanceValues)
            ? instanceValues.FirstOrDefault()
            : null;

        return new SearchApiResponse
        {
            Result = result,
            Backend = backend,
            Strategy = strategy,
            SearchApiInstance = instance
        };
    }
}

public class SearchApiResponse
{
    public SearchResult? Result { get; set; }

    public string? Backend { get; set; }

    public string? Strategy { get; set; }

    public string? SearchApiInstance { get; set; }
}
