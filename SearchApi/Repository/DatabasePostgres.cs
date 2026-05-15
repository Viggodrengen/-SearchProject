using Npgsql;
using SearchApi.Interfaces;
using Shared;
using Shared.Model;

namespace SearchApi.Repository;

public class DatabasePostgres : IDatabase
{
    private readonly NpgsqlConnection _connection;
    private Dictionary<string, int>? _exactWordMap;
    private Dictionary<string, List<int>>? _caseInsensitiveWordMap;

    public DatabasePostgres()
    {
        _connection = new NpgsqlConnection(Paths.POSTGRES_DATABASE);
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    public List<int> GetWordIds(string word, bool caseSensitive)
    {
        EnsureWordMaps();

        if (caseSensitive)
        {
            return _exactWordMap!.TryGetValue(word, out var id) ? new List<int> { id } : new List<int>();
        }

        var key = word.ToLowerInvariant();
        return _caseInsensitiveWordMap!.TryGetValue(key, out var ids) ? new List<int>(ids) : new List<int>();
    }

    public BEDocument? GetDocDetails(int docId)
    {
        using var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = $"SELECT * FROM document WHERE id = {docId}";

        using var reader = selectCmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new BEDocument
        {
            Id = reader.GetInt32(0),
            Url = reader.GetString(1),
            IdxTime = reader.GetDateTime(2),
            CreationTime = reader.GetDateTime(3)
        };
    }

    public Dictionary<int, HashSet<int>> GetDocumentWordMatches(List<int> wordIds)
    {
        var result = new Dictionary<int, HashSet<int>>();
        if (wordIds.Count == 0)
        {
            return result;
        }

        using var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = $"SELECT docId, wordId FROM Occ WHERE wordId IN {AsSqlTuple(wordIds)}";

        using var reader = selectCmd.ExecuteReader();
        while (reader.Read())
        {
            var docId = reader.GetInt32(0);
            var wordId = reader.GetInt32(1);

            if (!result.TryGetValue(docId, out var ids))
            {
                ids = new HashSet<int>();
                result[docId] = ids;
            }

            ids.Add(wordId);
        }

        return result;
    }

    private void EnsureWordMaps()
    {
        if (_exactWordMap is not null && _caseInsensitiveWordMap is not null)
        {
            return;
        }

        _exactWordMap = new Dictionary<string, int>();
        _caseInsensitiveWordMap = new Dictionary<string, List<int>>();

        using var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = "SELECT id, name FROM word";

        using var reader = selectCmd.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetInt32(0);
            var word = reader.GetString(1);

            if (!_exactWordMap.ContainsKey(word))
            {
                _exactWordMap[word] = id;
            }

            var lower = word.ToLowerInvariant();
            if (!_caseInsensitiveWordMap.TryGetValue(lower, out var ids))
            {
                ids = new List<int>();
                _caseInsensitiveWordMap[lower] = ids;
            }
            ids.Add(id);
        }
    }

    private static string AsSqlTuple(List<int> values)
    {
        return $"({string.Join(',', values)})";
    }
}
