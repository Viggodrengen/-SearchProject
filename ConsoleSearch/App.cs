using Shared.Model;

namespace ConsoleSearch;

public class App
{
    private readonly Config _config = new();
    private SearchApiClient? _searchClient;

    public async Task RunAsync()
    {
        ConfigureStartup();
        Console.WriteLine("Console Search (API client)");
        Console.WriteLine("Type '/help' for available commands\n");

        while (true)
        {
            Console.WriteLine("enter search terms - q for quit");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (input.Equals("q", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (input.StartsWith('/'))
            {
                HandleCommand(input);
                continue;
            }

            await SearchAndPrintAsync(input);
        }

        _searchClient?.Dispose();
    }

    private void ConfigureStartup()
    {
        Console.Write($"API base URL [{_config.ApiBaseUrl}]: ");
        var apiUrl = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(apiUrl))
        {
            _config.ApiBaseUrl = apiUrl.Trim();
        }

        Console.Write("Use SQLite (1) or Postgres (2) database? ");
        var databaseInput = Console.ReadLine();
        _config.Database = databaseInput == "2" ? "postgres" : "sqlite";

        _searchClient = new SearchApiClient(_config.ApiBaseUrl);
    }

    private async Task SearchAndPrintAsync(string input)
    {
        if (_searchClient is null)
        {
            _searchClient = new SearchApiClient(_config.ApiBaseUrl);
        }

        var request = new SearchRequest
        {
            Query = input,
            CaseSensitive = _config.CaseSensitive,
            Database = _config.Database,
            MaxAmount = _config.MaxAmount
        };

        try
        {
            var result = await _searchClient.SearchAsync(request);
            if (result is null)
            {
                Console.WriteLine("No response received from API.\n");
                return;
            }

            if (result.Ignored.Count > 0)
            {
                Console.WriteLine($"Ignored: {string.Join(',', result.Ignored)}");
            }

            var idx = 1;
            foreach (var doc in result.DocumentHits)
            {
                if (doc.Document is null)
                {
                    continue;
                }

                Console.WriteLine($"{idx} : {doc.Document.Url} -- contains {doc.NoOfHits} search terms");
                Console.WriteLine($"Index time: {doc.Document.IdxTime}");
                Console.WriteLine($"Missing: {ArrayAsString(doc.Missing.ToArray())}");
                idx++;
            }

            Console.WriteLine($"Documents: {result.NoOfHits}. Time: {result.TimeUsed.TotalMilliseconds}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Search failed: {ex.Message}");
            Console.WriteLine("Ensure SearchApi is running and URL is correct.\n");
        }
    }

    private void HandleCommand(string command)
    {
        if (command.Equals("/help", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("\nAvailable commands:");
            Console.WriteLine("  /casesensitive=on        - Enable case-sensitive search");
            Console.WriteLine("  /casesensitive=off       - Disable case-sensitive search");
            Console.WriteLine("  /database=sqlite         - Search in SQLite index");
            Console.WriteLine("  /database=postgres       - Search in Postgres index");
            Console.WriteLine("  /max=<number>            - Max docs returned (1-100)");
            Console.WriteLine("  /api=<url>               - Change API base URL");
            Console.WriteLine("  /help                    - Show this help message");
            Console.WriteLine("  q                        - Quit the application\n");
            return;
        }

        if (command.Equals("/casesensitive=on", StringComparison.OrdinalIgnoreCase))
        {
            _config.CaseSensitive = true;
            Console.WriteLine("Case-sensitive search enabled.\n");
            return;
        }

        if (command.Equals("/casesensitive=off", StringComparison.OrdinalIgnoreCase))
        {
            _config.CaseSensitive = false;
            Console.WriteLine("Case-insensitive search enabled.\n");
            return;
        }

        if (command.Equals("/database=sqlite", StringComparison.OrdinalIgnoreCase))
        {
            _config.Database = "sqlite";
            Console.WriteLine("Database changed to SQLite.\n");
            return;
        }

        if (command.Equals("/database=postgres", StringComparison.OrdinalIgnoreCase))
        {
            _config.Database = "postgres";
            Console.WriteLine("Database changed to Postgres.\n");
            return;
        }

        if (command.StartsWith("/max=", StringComparison.OrdinalIgnoreCase))
        {
            var raw = command["/max=".Length..];
            if (int.TryParse(raw, out var maxAmount) && maxAmount >= 1 && maxAmount <= 100)
            {
                _config.MaxAmount = maxAmount;
                Console.WriteLine($"Max results set to {maxAmount}.\n");
            }
            else
            {
                Console.WriteLine("Invalid max value. Use /max=<number> where number is 1-100.\n");
            }

            return;
        }

        if (command.StartsWith("/api=", StringComparison.OrdinalIgnoreCase))
        {
            var raw = command["/api=".Length..].Trim();
            if (Uri.TryCreate(raw, UriKind.Absolute, out var uri))
            {
                _config.ApiBaseUrl = uri.ToString().TrimEnd('/');
                _searchClient?.Dispose();
                _searchClient = new SearchApiClient(_config.ApiBaseUrl);
                Console.WriteLine($"API URL changed to {_config.ApiBaseUrl}\n");
            }
            else
            {
                Console.WriteLine("Invalid URL. Example: /api=http://localhost:5017\n");
            }

            return;
        }

        Console.WriteLine($"Unknown command: {command}");
        Console.WriteLine("Type '/help' for available commands\n");
    }

    private static string ArrayAsString(string[] values)
    {
        return values.Length == 0 ? "[]" : $"[{string.Join(',', values)}]";
    }
}
