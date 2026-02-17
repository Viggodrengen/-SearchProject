namespace Shared.Model;

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;

    public int MaxAmount { get; set; } = 10;

    public bool CaseSensitive { get; set; }

    public string Database { get; set; } = "sqlite";
}
