using Shared.Model;
using SearchWebApp.Services;

namespace SearchWebApp.Interfaces;

public interface ISearchApiClient
{
    Task<SearchApiResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
}
