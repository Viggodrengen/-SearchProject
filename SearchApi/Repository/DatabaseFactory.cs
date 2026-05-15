using SearchApi.Interfaces;

namespace SearchApi.Repository;

public static class DatabaseFactory
{
    public static IDatabase Create(string? database)
    {
        if (string.Equals(database, "postgres", StringComparison.OrdinalIgnoreCase))
        {
            return new DatabasePostgres();
        }

        return new DatabaseSqlite();
    }
}
