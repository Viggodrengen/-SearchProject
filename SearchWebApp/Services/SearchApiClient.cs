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

    public async Task<SearchResult?> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/search", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SearchResult>(cancellationToken: cancellationToken);
    }
}
