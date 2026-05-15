using System;
using System.Collections.Generic;
using System.Linq;

namespace Indexer;

public class Config
{
    public static string FOLDER =>
        Environment.GetEnvironmentVariable("INDEXER_FOLDER")
        ?? @"/Users/victorrodam/Downloads/Data/seData copy/medium";

    public static string DATABASE =>
        Environment.GetEnvironmentVariable("INDEXER_DATABASE")
        ?? string.Empty;

    public static bool NON_INTERACTIVE =>
        bool.TryParse(Environment.GetEnvironmentVariable("INDEXER_NON_INTERACTIVE"), out var value) && value;

    public static int TOP_WORDS =>
        int.TryParse(Environment.GetEnvironmentVariable("INDEXER_TOP_WORDS"), out var value) ? Math.Max(0, value) : 0;

    public static string REDIS_CONNECTION_STRING =>
        Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
        ?? string.Empty;

    public static List<string> EXTENSIONS =>
        (Environment.GetEnvironmentVariable("INDEXER_EXTENSIONS") ?? ".txt")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.StartsWith('.') ? x : $".{x}")
            .ToList();
}
