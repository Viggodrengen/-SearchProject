using SearchApi.Domain;
using Shared.Model;
using Xunit;

namespace SearchProject.Tests;

public class SearchLogicTests
{
    [Fact]
    public void Search_RanksDocumentsByNumberOfMatchedTerms()
    {
        var database = new InMemorySearchDatabase(
            words: new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase)
            {
                ["redis"] = [1],
                ["cache"] = [2]
            },
            matches: new Dictionary<int, HashSet<int>>
            {
                [10] = [1, 2],
                [20] = [1]
            });

        var logic = new SearchLogic(database, new SearchConfig { CaseSensitive = false });

        var result = logic.Search(["redis", "cache"], maxAmount: 10);

        Assert.Equal(2, result.NoOfHits);
        var documents = result.DocumentHits.Select(hit => Assert.IsType<BEDocument>(hit.Document)).ToArray();
        Assert.Equal([10, 20], documents.Select(document => document.Id).ToArray());
        Assert.Equal(2, result.DocumentHits[0].NoOfHits);
        Assert.Equal(1, result.DocumentHits[1].NoOfHits);
        Assert.Equal(["cache"], result.DocumentHits[1].Missing);
    }

    [Fact]
    public void Search_IgnoresUnknownTermsAndIncludesThemAsMissingOnMatches()
    {
        var database = new InMemorySearchDatabase(
            words: new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase)
            {
                ["kubernetes"] = [5]
            },
            matches: new Dictionary<int, HashSet<int>>
            {
                [42] = [5]
            });

        var logic = new SearchLogic(database, new SearchConfig { CaseSensitive = false });

        var result = logic.Search(["kubernetes", "unknown"], maxAmount: 10);

        Assert.Equal(["unknown"], result.Ignored);
        var hit = Assert.Single(result.DocumentHits);
        var document = Assert.IsType<BEDocument>(hit.Document);
        Assert.Equal(42, document.Id);
        Assert.Equal(["unknown"], hit.Missing);
    }

    [Fact]
    public void Search_RemovesDuplicateTermsCaseInsensitively()
    {
        var database = new InMemorySearchDatabase(
            words: new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase)
            {
                ["api"] = [7]
            },
            matches: new Dictionary<int, HashSet<int>>
            {
                [1] = [7]
            });

        var logic = new SearchLogic(database, new SearchConfig { CaseSensitive = false });

        var result = logic.Search(["API", "api"], maxAmount: 10);

        var hit = Assert.Single(result.DocumentHits);
        Assert.Equal(1, hit.NoOfHits);
    }

    private sealed class InMemorySearchDatabase : ISearchIndexRepository
    {
        private readonly Dictionary<string, List<int>> _words;
        private readonly Dictionary<int, HashSet<int>> _matches;

        public InMemorySearchDatabase(Dictionary<string, List<int>> words, Dictionary<int, HashSet<int>> matches)
        {
            _words = words;
            _matches = matches;
        }

        public List<int> GetWordIds(string word, bool caseSensitive) =>
            _words.TryGetValue(word, out var ids) ? ids : [];

        public BEDocument? GetDocDetails(int docId) => new()
        {
            Id = docId,
            Url = $"https://example.test/doc/{docId}",
            CreationTime = DateTime.UtcNow,
            IdxTime = DateTime.UtcNow
        };

        public Dictionary<int, HashSet<int>> GetDocumentWordMatches(List<int> wordIds) =>
            _matches
                .Where(match => match.Value.Overlaps(wordIds))
                .ToDictionary(match => match.Key, match => match.Value);

        public void Dispose()
        {
        }
    }
}
