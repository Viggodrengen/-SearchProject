using SearchApi.Domain;

namespace SearchApi.Infrastructure.Persistence;

public static class SearchIndexRepositoryFactory
{
    public static ISearchIndexRepository Create(string? database)
    {
        if (string.Equals(database, "postgres", StringComparison.OrdinalIgnoreCase))
        {
            return new PostgresSearchIndexRepository();
        }

        return new SqliteSearchIndexRepository();
    }
}
