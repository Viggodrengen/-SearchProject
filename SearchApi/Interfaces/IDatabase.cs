using System;
using System.Collections.Generic;
using Shared.Model;

namespace SearchApi.Interfaces;

public interface IDatabase : IDisposable
{
    List<int> GetWordIds(string word, bool caseSensitive);

    BEDocument? GetDocDetails(int docId);

    Dictionary<int, HashSet<int>> GetDocumentWordMatches(List<int> wordIds);
}
