namespace SearchApi.Search;

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
