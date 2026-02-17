namespace ConsoleSearch;

public class Config
{
    public bool CaseSensitive { get; set; } = false;

    public int MaxAmount { get; set; } = 10;

    public string Database { get; set; } = "sqlite";

    public string ApiBaseUrl { get; set; } = "http://localhost:5017";
}
