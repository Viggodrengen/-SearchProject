namespace SearchApi.Services;

public class SearchCacheOptions
{
    public const string SectionName = "SearchCache";

    public bool Enabled { get; set; }

    public int TtlSeconds { get; set; } = 60;
}
