using System;

namespace Shared;

public static class Paths
{
    public static string SQLITE_DATABASE =>
        Environment.GetEnvironmentVariable("SEARCH_SQLITE_PATH")
        ?? @"/Users/victorrodam/Downloads/Data/searchDBmedium.db";

    public static string POSTGRES_DATABASE =>
        Environment.GetEnvironmentVariable("SEARCH_POSTGRES_CONNECTION")
        ?? "Server=127.0.0.1:5432;User Id=oler;Password=1234;database=searchmedium";
}
