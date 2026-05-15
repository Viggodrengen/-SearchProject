using SearchApi.Interfaces;
using Shared.Model;

namespace SearchApi.Services;

public class SearchLogic
{
    private readonly IDatabase _database;
    private readonly SearchConfig _config;

    public SearchLogic(IDatabase database, SearchConfig config)
    {
        _database = database;
        _config = config;
    }

    public SearchResult Search(string[] query, int maxAmount)
    {
        var start = DateTime.UtcNow;

        var terms = NormalizeTerms(query);
        var ignored = new List<string>();
        var termGroups = new List<(string term, HashSet<int> ids)>();

        foreach (var term in terms)
        {
            var ids = _database.GetWordIds(term, _config.CaseSensitive).Distinct().ToHashSet();
            if (ids.Count == 0)
            {
                ignored.Add(term);
                continue;
            }

            termGroups.Add((term, ids));
        }

        if (termGroups.Count == 0)
        {
            return new SearchResult
            {
                Query = query,
                NoOfHits = 0,
                Ignored = ignored,
                TimeUsed = DateTime.UtcNow - start
            };
        }

        var allWordIds = termGroups
            .SelectMany(group => group.ids)
            .Distinct()
            .ToList();

        var matchesByDocument = _database.GetDocumentWordMatches(allWordIds);
        var ranked = new List<(int docId, int hits, List<string> missingTerms)>();

        foreach (var documentMatch in matchesByDocument)
        {
            var docId = documentMatch.Key;
            var presentWordIds = documentMatch.Value;
            var hits = 0;
            var missingTerms = new List<string>();

            foreach (var (term, ids) in termGroups)
            {
                if (ids.Overlaps(presentWordIds))
                {
                    hits++;
                }
                else
                {
                    missingTerms.Add(term);
                }
            }

            if (hits > 0)
            {
                missingTerms.AddRange(ignored);
                ranked.Add((docId, hits, missingTerms));
            }
        }

        ranked.Sort((left, right) =>
        {
            var byHits = right.hits.CompareTo(left.hits);
            return byHits != 0 ? byHits : left.docId.CompareTo(right.docId);
        });

        var top = ranked.Take(maxAmount).ToList();
        var documentHits = new List<DocumentHit>();

        foreach (var candidate in top)
        {
            var doc = _database.GetDocDetails(candidate.docId);
            if (doc is null)
            {
                continue;
            }

            documentHits.Add(new DocumentHit
            {
                Document = doc,
                NoOfHits = candidate.hits,
                Missing = candidate.missingTerms
            });
        }

        return new SearchResult
        {
            Query = query,
            NoOfHits = ranked.Count,
            DocumentHits = documentHits,
            Ignored = ignored,
            TimeUsed = DateTime.UtcNow - start
        };
    }

    private string[] NormalizeTerms(string[] query)
    {
        var comparer = _config.CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var unique = new HashSet<string>(comparer);
        var result = new List<string>();

        foreach (var term in query)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                continue;
            }

            if (!unique.Add(term))
            {
                continue;
            }

            result.Add(term);
        }

        return result.ToArray();
    }
}
