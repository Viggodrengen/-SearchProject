using System.Net.Http.Json;
using Shared.Model;

namespace ConsoleSearch;

public class SearchApiClient
    : IDisposable
{
    private readonly HttpClient _httpClient;

    public SearchApiClient(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl, UriKind.Absolute)
        };
    }

    public async Task<SearchResult?> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/search", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SearchResult>(cancellationToken: cancellationToken);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
