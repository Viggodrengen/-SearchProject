using System;
using System.Collections.Generic;
using Shared.Model;

namespace SearchApi.Domain;

public interface ISearchIndexRepository : IDisposable
{
    List<int> GetWordIds(string word, bool caseSensitive);

    BEDocument? GetDocDetails(int docId);

    Dictionary<int, HashSet<int>> GetDocumentWordMatches(List<int> wordIds);
}
