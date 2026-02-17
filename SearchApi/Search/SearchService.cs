using Shared.Model;

namespace SearchApi.Search;

public class SearchService
{
    public SearchResult Search(SearchRequest request)
    {
        var terms = (request.Query ?? string.Empty).Split(" ", StringSplitOptions.RemoveEmptyEntries);
        var maxAmount = Math.Clamp(request.MaxAmount, 1, 100);

        if (terms.Length == 0)
        {
            return new SearchResult
            {
                Query = Array.Empty<string>(),
                NoOfHits = 0,
                TimeUsed = TimeSpan.Zero
            };
        }

        using var database = DatabaseFactory.Create(request.Database);
        var config = new SearchConfig { CaseSensitive = request.CaseSensitive };
        var logic = new SearchLogic(database, config);

        return logic.Search(terms, maxAmount);
    }
}
